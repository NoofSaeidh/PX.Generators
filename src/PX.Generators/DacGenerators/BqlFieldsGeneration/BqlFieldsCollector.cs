using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;

namespace PX.Generators.DacGenerators.BqlFieldsGeneration
{
    internal class BqlFieldsCollector
    {
        public static BqlFieldsCollector Instance { get; } = new();

        // ReSharper disable InconsistentNaming
        private const string IBqlTableName = "PX.Data.IBqlTable";

        private const string PXEventSubscriberAttributeName = "PX.Data.PXEventSubscriberAttribute";
        // ReSharper enable InconsistentNaming

        private static readonly SymbolDisplayFormat PropertyDisplayFormat =
            new (typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        public IncrementalValuesProvider<BqlTableInfo> Collect(
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

            static bool IsPartial(TypeDeclarationSyntax classDeclaration)
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

                if (IsBqlTable(classSymbol) is false)
                    return null;

                if (CheckContainingTypes(classSymbol, out var containingTypes) is false)
                    return null;

                var allTypes = GetAllBqlTableTypeFor(classSymbol).ToList();

#pragma warning disable RS1024
                var properties = allTypes.SelectMany(t => t.GetMembers()
                                                           .OfType<IPropertySymbol>()
                                                           .Where(IsBqlFieldProperty))
                                         .Distinct(PropertyNameEqualityComparer.Instance);
#pragma warning restore RS1024

                var selfNestedClasses = classSymbol.GetTypeMembers();
                var parentNestedClasses = allTypes.Skip(1)
                                                  .SelectMany(m => m.GetTypeMembers())
                                                  .ToList();


                var bqlFields = new List<BqlFieldInfo>();

                foreach (IPropertySymbol property in properties)
                {
                    if (BqlFieldPropertyType.TryParse(property.Type.ToDisplayString(PropertyDisplayFormat),
                                                      out var bqlPropertyType))
                    {
                        if (CannotAddClassField())
                            continue;

                        bqlFields.Add(new ()
                        {
                            Name              = property.Name,
                            Type              = bqlPropertyType,
                            IsHidingBaseClass = IsHidingBaseClass(),
                        });
                    }

                    bool CannotAddClassField() => selfNestedClasses
                        .Any(c => StringComparer.InvariantCultureIgnoreCase.Equals(property.Name, c.Name));

                    bool IsHidingBaseClass() => parentNestedClasses!
                        .Any(c => StringComparer.InvariantCulture.Equals(
                                 BqlFieldInfo.GetClassName(property.Name), c.Name));
                }

                if (bqlFields.Count > 0)
                {
                    var baseClasses = containingTypes?.Select(n => n.Name).Reverse().ToList();

                    return new BqlTableInfo
                    {
                        Name        = classSymbol.Name,
                        Namespace   = classSymbol.ContainingNamespace?.ToDisplayString(),
                        Fields      = bqlFields,
                        BaseClasses = baseClasses,
                    };
                }

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

                            if (type.Equals(referencesTypes.PXEventSubscriberAttributeSymbol,
                                            SymbolEqualityComparer.Default))
                                return true;

                            type = type.BaseType;
                        }
                    }
                }


                IEnumerable<ITypeSymbol> GetAllBqlTableTypeFor(ITypeSymbol type)
                {
                    for (ITypeSymbol? symbol = type; IsBqlTable(symbol); symbol = symbol.BaseType!)
                    {
                        yield return symbol;
                    }
                }

                bool IsBqlTable(ITypeSymbol? type)
                {
                    return type?.AllInterfaces.Contains(referencesTypes.IBqlTableSymbol) is true;
                }

                static bool CheckContainingTypes(INamedTypeSymbol classSymbol,
                                                 out List<INamedTypeSymbol>? baseClassesList)
                {
                    baseClassesList = null;
                    INamedTypeSymbol? baseSymbol = classSymbol.ContainingType;
                    while (baseSymbol != null)
                    {
                        bool isPartial = false;
                        foreach (var syntaxRef in baseSymbol.DeclaringSyntaxReferences)
                        {
                            var typeDecl = syntaxRef.GetSyntax() as TypeDeclarationSyntax;
                            if (typeDecl != null && IsPartial(typeDecl))
                            {
                                isPartial = true;
                                break;
                            }
                        }

                        // todo: check if is enough
                        if (isPartial is false)
                            return false;

                        baseClassesList ??= new();
                        baseClassesList.Add(baseSymbol);
                        baseSymbol = baseSymbol.ContainingType;
                    }

                    return true;
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


        private readonly struct ReferencesTypesSymbols
        {
            public INamedTypeSymbol IBqlTableSymbol { get; init; }
            public INamedTypeSymbol PXEventSubscriberAttributeSymbol { get; init; }
        }

        private class PropertyNameEqualityComparer : IEqualityComparer<IPropertySymbol>
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static PropertyNameEqualityComparer Instance { get; } = new();

            public bool Equals(IPropertySymbol x, IPropertySymbol y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (ReferenceEquals(x, null))
                {
                    return false;
                }

                if (ReferenceEquals(y, null))
                {
                    return false;
                }

                if (x.GetType() != y.GetType())
                {
                    return false;
                }

                return StringComparer.OrdinalIgnoreCase.Equals(x.Name, y.Name);
            }

            public int GetHashCode(IPropertySymbol obj)
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name);
            }
        }
    }
}