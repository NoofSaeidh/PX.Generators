using Microsoft.CodeAnalysis;
using PX.Generators.Common;

namespace PX.Generators.DacGenerators.BqlFieldsGeneration
{
    [Generator(LanguageNames.CSharp)]
    public class BqlFieldsGenerator : IIncrementalGenerator
    {
        private static ICodeGenerator<BqlTableInfo> CodeGenerator => BqlFieldsSimpleCodeGenerator.Instance;
        private static BqlFieldsCollector Collector => BqlFieldsCollector.Instance;

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