using Microsoft.CodeAnalysis;
using PX.Generators.Common;

namespace PX.Generators.DacGenerators.BqlFieldsGeneration
{
    [Generator(LanguageNames.CSharp)]
    public class BqlFieldsGenerator : IIncrementalGenerator
    {
        internal static ICodeGenerator<BqlTableInfo> CodeGenerator { get; set; } = BqlFieldsSimpleCodeGenerator.Instance;
        internal static BqlFieldsCollector Collector { get; set; } = BqlFieldsCollector.Instance;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var bqlTablesContext = Collector.Collect(context);

            context.RegisterSourceOutput(bqlTablesContext, GenerateCode);
        }

        private static void GenerateCode(SourceProductionContext context, BqlTableInfo bqlTable)
        {
            if (CodeGenerator.GenerateCode(bqlTable, context.CancellationToken) is { IsSuccess: true } result)
            {
                context.AddSource(result.FileName, result.Code);
            }
        }
    }
}