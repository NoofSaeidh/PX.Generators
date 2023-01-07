using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;
using System.IO;

namespace PX.Generators.DacGenerators.BqlFieldsGeneration
{
    [Generator(LanguageNames.CSharp)]
    public class BqlFieldsGenerator : 
        //ISourceGenerator
        IIncrementalGenerator
    {
        // ReSharper disable InconsistentNaming
        private const string IBqlTableName = "PX.Data.IBqlTable";
        private const string PXEventSubscriberAttributeName = "PX.Data.PXEventSubscriberAttribute";
        // ReSharper enable InconsistentNaming

        private static readonly SymbolDisplayFormat PropertyDisplayFormat =
            new (typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        public void Initialize(GeneratorInitializationContext context)
        {
            //context.RegisterForSyntaxNotifications(() => new BqlFieldsSyntaxReceiver());
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
                        var addnew = baseProps.Contains(property.Name);
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
                var inNamespace = classSymbol.ContainingNamespace != null;
                if (inNamespace)
                {
                    sb.AppendLine($"namespace {classSymbol.ContainingNamespace}");
                    sb.AppendLine("{");
                }

                var indentation          = "\t";
                var namespaceIndentation = inNamespace ? indentation : string.Empty;
                sb.AppendLine($"{namespaceIndentation}partial class {classSymbol.Name}");
                sb.AppendLine($"{namespaceIndentation}{{");
                foreach (var (name, bqlType, addnew) in classesToAdd)
                {
                    var addnewValue = addnew ? "new " : string.Empty;
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

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var bqlTablesContext = GetBqlTablesContext(context);

            context.RegisterSourceOutput(bqlTablesContext, GenerateCode);
        }

        private IncrementalValuesProvider<BqlTableInfo> GetBqlTablesContext(
            IncrementalGeneratorInitializationContext context)
        {
            var types = GetTypes(context);

            return context.SyntaxProvider
                          .CreateSyntaxProvider(
                              predicate: static (n, _) => n is ClassDeclarationSyntax cd && IsPartial(cd),
                              transform: static (ctx, _) => ctx)
                          .Combine(types)
                          // Where with cancellation is internal :(
                          .Select(static (ctx, ct) => TryGetBqlTableInfo(ctx.Left, ctx.Right, ct))
                          .Where(static (bqlTable) => bqlTable?.Fields?.Count > 0)
                          .Collect()
                          .SelectMany(static (bqlTable, _) => bqlTable.Distinct())!;

            static bool IsPartial(ClassDeclarationSyntax classDeclaration)
            {
                return classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
            }

            static BqlTableInfo? TryGetBqlTableInfo(GeneratorSyntaxContext classContext,
                                                    ReferencesTypesSymbols referencesTypes,
                                                    CancellationToken cancellationToken)
            {
                var classDeclaration = (ClassDeclarationSyntax) classContext.Node;
                var semanticModel    = classContext.SemanticModel;
                var classSymbol      = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
                if (classSymbol == null)
                    return null;

                // todo: support nested types
                if (classSymbol.ContainingType is not null)
                    return null;

                if (classSymbol.AllInterfaces.Contains(referencesTypes.IBqlTableSymbol) is false)
                    return null;

                var properties = classSymbol.GetMembers()
                                            .OfType<IPropertySymbol>()
                                            .Where(IsBqlFieldProperty);

                var bqlFields = new List<BqlFieldInfo>();

                foreach (var property in properties)
                {
                    if (BqlFieldPropertyType.TryParse(property.Type.ToDisplayString(PropertyDisplayFormat),
                                                 out var bqlPropertyType))
                    {
                        bqlFields.Add(new () { Name = property.Name, Type = bqlPropertyType });
                    }
                }

                if (bqlFields.Count > 0)
                    return new BqlTableInfo
                    {
                        Name      = classSymbol.Name,
                        Namespace = classSymbol.ContainingNamespace?.Name,
                        Fields    = bqlFields
                    };

                return null;

                bool IsBqlFieldProperty(IPropertySymbol property)
                {
                    return property.IsImplicitlyDeclared is false
                        && property.IsStatic is false
                        && property.IsIndexer is false
                        && property.GetAttributes()
                                   .Select(a => a.AttributeClass)
                                   .Where(a => a is not null)
                                   .Any(IsEventSubscriber)
                        && IsPascalCase();

                    // todo: support camelCase properties
                    bool IsPascalCase() => property.Name.Length > 0 && char.IsUpper(property.Name[0]);

                    bool IsEventSubscriber(ITypeSymbol? type)
                    {
                        while (true)
                        {
                            if (type == null)
                                return false;

                            if (type.Equals(referencesTypes.PXEventSubscriberAttributeSymbol, SymbolEqualityComparer.Default))
                                return true;

                            type = type.BaseType;
                        }
                    }
                }
            }
        }


        private IncrementalValueProvider<ReferencesTypesSymbols>
            GetTypes(IncrementalGeneratorInitializationContext context)
        {
            return context.CompilationProvider.Select(
                (cmp, ct) =>
                {
                    var iBqlTable       = cmp.GetTypeByMetadataName(IBqlTableName);
                    var eventSubscriber = cmp.GetTypeByMetadataName(PXEventSubscriberAttributeName);
                    if (iBqlTable is null || eventSubscriber is null)
                        //todo: properly handle error
                        throw new InvalidOperationException("Cannot find required type.");
                    return new ReferencesTypesSymbols
                    {
                        IBqlTableSymbol                  = iBqlTable,
                        PXEventSubscriberAttributeSymbol = eventSubscriber,
                    };
                });
        }

        private void GenerateCode(SourceProductionContext context, BqlTableInfo bqlTable)
        {
            var (name, text) = BqlFieldsCodeGenerator.Instance.Compile(bqlTable);
            if (name == null || text == null)
                return;


            context.AddSource(name, text);
        }

        private readonly struct ReferencesTypesSymbols
        {
            public INamedTypeSymbol IBqlTableSymbol { get; init; }
            public INamedTypeSymbol PXEventSubscriberAttributeSymbol { get; init; }
        }
    }
}