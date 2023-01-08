using Microsoft.CodeAnalysis;

namespace PX.Generators.DacGenerators.BqlFieldsGeneration
{
    [Generator(LanguageNames.CSharp)]
    public class BqlFieldsGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var bqlTablesContext = BqlFieldsCollector.Instance.Collect(context);

            context.RegisterSourceOutput(bqlTablesContext, GenerateCode);
        }

        private void GenerateCode(SourceProductionContext context, BqlTableInfo bqlTable)
        {
            if (BqlFieldsRoslynCodeGenerator.Instance.GenerateCode(bqlTable, context.CancellationToken)
                is { IsSuccess: true } result)
            {
                context.AddSource(result.FileName, result.Code);
            }
        }
    }
}