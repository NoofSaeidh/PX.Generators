using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using PX.Generators.DacGenerators;
using PX.Generators.DacGenerators.BqlFieldsGeneration;
using PX.Generators.Tests.Common;
using Xunit;

namespace PX.Generators.Tests
{
    public class BqlFieldsGeneratorTests
    {
        private static readonly string ExamplesFolder = "../../../DacGeneratorsTests/Examples";

        [Theory]
        [InlineData("Example1")]
        [InlineData("Example2")]
        public void Generator_Should_Generate_All_Missing_Fields(string exampleName)
        {
            // arrange
            var (In, Out) = GetInAndOutExamples(exampleName);
            var             compilation = TestsInitialization.CreateCompilation(In);
            var             generator   = new BqlFieldsGenerator();
            GeneratorDriver driver      = CSharpGeneratorDriver.Create(generator);

            // act

            driver = driver.RunGeneratorsAndUpdateCompilation(
                compilation, out var outputCompilation, out var diagnostics);

            // assert
            using (new AssertionScope())
            {
                outputCompilation.GetDiagnostics()
                                 // todo: remove warnings
                                 .Where(d => d.WarningLevel == 0)
                                 .Should().BeEmpty();

                diagnostics.Should().BeEmpty();
                outputCompilation.SyntaxTrees.Should().HaveCount(2);
                // todo: fix errors
                //outputCompilation.GetDiagnostics().Should().BeEmpty();

                var runResult = driver.GetRunResult();
                runResult.Diagnostics.Should().BeEmpty();
                runResult.GeneratedTrees.Should().HaveCount(1);

                var generatorResult = runResult.Results[0];
                generatorResult.Diagnostics.Should().BeEmpty();
                generatorResult.GeneratedSources.Should().HaveCount(1);
                generatorResult.Exception.Should().BeNull();

                generatorResult.GeneratedSources[0].HintName.Should()
                               .Be($"PX.Generators.Tests.Examples.{exampleName}.bqlfields.g.cs");
                var actual   = ClearUp(generatorResult.GeneratedSources[0].SourceText.ToString());
                var expected = ClearUp(Out.ToString());

                actual.Should().BeEquivalentTo(expected, "result of generator should be same as in example");
            }

            string ClearUp(string input)
            {
                return input.Replace("\r\n", "\n").Replace("\t", "    ");
            }
        }

        private static (SourceText In, SourceText Out) GetInAndOutExamples(string exampleName)
        {
            var In  = SourceText.From(File.OpenRead(Path.Combine(ExamplesFolder, $"{exampleName}.in.cs")));
            var Out = SourceText.From(File.OpenRead(Path.Combine(ExamplesFolder, $"{exampleName}.out.cs")));
            return (In, Out);
        }



    }
}