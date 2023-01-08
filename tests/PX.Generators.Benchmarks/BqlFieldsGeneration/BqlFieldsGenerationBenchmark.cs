using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PX.Generators.DacGenerators.BqlFieldsGeneration;
using PX.Generators.Tests.Common;
using Microsoft.CodeAnalysis.Text;
using PX.Generators.Common;
using BenchmarkDotNet.Jobs;

namespace PX.Generators.Benchmarks.BqlFieldsGeneration
{
    [SimpleJob(runtimeMoniker: RuntimeMoniker.NetCoreApp20), MemoryDiagnoser]
    public class BqlFieldsGenerationBenchmark
    {
        private static readonly string ExamplesFolder = "../../../../PX.Generators.Tests/DacGeneratorsTests/Examples";


        [Params(typeof(BqlFieldsRoslynCodeGenerator), typeof(BqlFieldsSimpleCodeGenerator))]
        public Type GeneratorType { get; set; }

        private  GeneratorDriver _driver;
        private Compilation _compilation;

        [IterationSetup]
        public void Setup()
        {
            var codeGenerator = (ICodeGenerator<BqlTableInfo>) Activator.CreateInstance(GeneratorType);
            BqlFieldsGenerator.CodeGenerator = codeGenerator;

            var example   = GetExample("Example2");
            var generator = new BqlFieldsGenerator();

            _compilation = TestsInitialization.CreateCompilation(example);
            _driver      = CSharpGeneratorDriver.Create(generator);
        }

        [Benchmark]
        public void Compile()
        {
            _driver.RunGeneratorsAndUpdateCompilation(_compilation, out _, out _);
        }

        private SourceText GetExample(string exampleName)
        {
            return SourceText.From(File.OpenRead(Path.Combine(ExamplesFolder, $"{exampleName}.in.cs")));
        }
    }
}