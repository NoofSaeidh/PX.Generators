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
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var bqlTablesContext = BqlFieldsCollector.Instance.Collect(context);

            context.RegisterSourceOutput(bqlTablesContext, GenerateCode);
        }
        
        private void GenerateCode(SourceProductionContext context, BqlTableInfo bqlTable)
        {
            var (name, text) = BqlFieldsCodeGenerator.Instance.GenerateCode(bqlTable, context.CancellationToken);
            if (name == null || text == null)
                return;


            context.AddSource(name, text);
        }
    }
}