using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PX.Generators.DacGenerators
{
    [Generator]
    public class BqlFieldsGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var interfaceType       = context.Compilation.GetTypeByMetadataName("PX.Data.IBqlTable");
            var eventSubscriberType = context.Compilation.GetTypeByMetadataName("PX.Data.PXEventSubscriberAttribute");
            var guidType            = context.Compilation.GetTypeByMetadataName("System.Guid");
            foreach (var classDeclaration in context.Compilation
                                                    .SyntaxTrees
                                                    .SelectMany(t => t.GetRoot().DescendantNodes())
                                                    .OfType<ClassDeclarationSyntax>())
            {
                if (classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)) is false)
                    continue;

                var classSymbol = (INamedTypeSymbol) ModelExtensions.GetDeclaredSymbol(context.Compilation
                                                                                              .GetSemanticModel(
                                                                                                  classDeclaration
                                                                                                      .SyntaxTree),
                                                                                       classDeclaration);
                if (classSymbol == null
                 || classSymbol.ContainingType != null // todo: support nested types
                 || classSymbol.AllInterfaces.Contains(interfaceType) is false)
                    continue;

                var allDacs       = new List<INamedTypeSymbol>();
                var currentSymbol = classSymbol;
                do
                {
                    allDacs.Add(currentSymbol);
                    currentSymbol = currentSymbol.BaseType;
                } while (currentSymbol != null && currentSymbol.SpecialType != SpecialType.System_Object);

                var existingClasses = classSymbol.GetTypeMembers();
                var classesToAdd    = new List<(string name, string bqltype, bool addnew)>();
                var props           = new HashSet<string>();
                var baseProps = new HashSet<string>(allDacs.Skip(1)
                                                           .SelectMany(c => c.GetMembers().OfType<IPropertySymbol>())
                                                           .Select(p => p.Name));
                foreach (var property in allDacs.SelectMany(c => c.GetMembers().OfType<IPropertySymbol>()))
                {
                    if (props.Add(property.Name) is false)
                        continue;

                    if (existingClasses.Any(c => c.Name.Equals(property.Name, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    if (property.Name.Length == 0
                     || char.IsLower(property.Name[0])) // only properties starts with Uppercase
                        continue;

                    if (property.GetAttributes().Select(a => a.AttributeClass).Any(IsEventSubscriber))
                    {
                        var bqlType = GetClassType();
                        if (bqlType == null)
                            continue;

                        var name = property.Name.ToCharArray();
                        name[0] = char.ToLower(name[0]);
                        bool addnew = baseProps.Contains(property.Name);
                        classesToAdd.Add((new string(name), bqlType, addnew));
                    }

                    bool IsEventSubscriber(ITypeSymbol type)
                    {
                        while (true)
                        {
                            if (type == null)
                                return false;

                            if (type.Equals(eventSubscriberType, SymbolEqualityComparer.Default))
                                return true;

                            type = type.BaseType;
                        }
                    }

                    string GetClassType()
                    {
                        if (property.Type.SpecialType == SpecialType.System_String)
                            return "BqlString";

                        var named = (INamedTypeSymbol) property.Type;

                        if (named.IsGenericType)
                        {
                            // todo: check it is Nullable<>
                            var typeArg = named.TypeArguments[0];
                            return typeArg.SpecialType switch
                            {
                                SpecialType.System_Int16    => "BqlShort",
                                SpecialType.System_Int32    => "BqlInt",
                                SpecialType.System_DateTime => "BqlDateTime",
                                SpecialType.None when typeArg.Equals(guidType, SymbolEqualityComparer.Default) =>
                                    "BqlGuid",
                                _ => null,
                            };
                        }

                        return null;
                    }
                }

                if (classesToAdd.Count == 0)
                    continue;

                var sb = new StringBuilder();
                sb.AppendLine("// <auto-generated/>");
                bool inNamespace = classSymbol.ContainingNamespace != null;
                if (inNamespace)
                {
                    sb.AppendLine($"namespace {classSymbol.ContainingNamespace}");
                    sb.AppendLine("{");
                }

                string indentation          = "\t";
                string namespaceIndentation = inNamespace ? indentation : string.Empty;
                sb.AppendLine($"{namespaceIndentation}partial class {classSymbol.Name}");
                sb.AppendLine($"{namespaceIndentation}{{");
                foreach (var (name, bqlType, addnew) in classesToAdd)
                {
                    string addnewValue = addnew ? "new " : string.Empty;
                    sb.AppendLine(
                        $"{namespaceIndentation}{indentation}public {addnewValue}abstract class {name} : PX.Data.BQL.{bqlType}.Field<{name}> {{ }}");
                }

                sb.AppendLine($"{namespaceIndentation}}}");
                if (inNamespace)
                    sb.AppendLine("}");

                context.AddSource($"{classSymbol.Name}.bqlfields.generated.cs",
                                  SourceText.From(sb.ToString(), Encoding.UTF8));
            }
        }
    }
}