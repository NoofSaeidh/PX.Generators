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
    public class BqlFieldsGenerator : IIncrementalGenerator
    {
        // ReSharper disable InconsistentNaming
        private const string IBqlTableName = "PX.Data.IBqlTable";
        private const string PXEventSubscriberAttributeName = "PX.Data.PXEventSubscriberAttribute";
        // ReSharper enable InconsistentNaming

        private static readonly SymbolDisplayFormat PropertyDisplayFormat =
            new (typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

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