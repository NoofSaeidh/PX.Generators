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
        [InlineData("SimpleExample")]
        [InlineData("InheritanceExample")]
        [InlineData("NestedExample")]
        public void Generator_Should_Generate_All_Missing_Fields(string exampleName)
        {
            // arrange
            var (input, expected) = GetInAndOutExamples(exampleName);
            var             compilation = TestsInitialization.CreateCompilation(input);
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
                outputCompilation.SyntaxTrees.Should().HaveCount(expected.Count + 1);
                outputCompilation.GetDiagnostics().Should().BeEmpty();

                var runResult = driver.GetRunResult();
                runResult.Diagnostics.Should().BeEmpty();
                runResult.GeneratedTrees.Should().HaveCount(expected.Count);

                var generatorResult = runResult.Results[0];
                generatorResult.Diagnostics.Should().BeEmpty();
                generatorResult.GeneratedSources.Should().HaveCount(expected.Count);
                generatorResult.Exception.Should().BeNull();

                // todo
                //generatorResult.GeneratedSources[0].HintName.Should()
                //               .Be($"PX.Generators.Tests.Examples.{exampleName}.bqlfields.g.cs");
                foreach (var (generatedSource, outText) in generatorResult.GeneratedSources.Zip(
                    expected, (l, r) => (l, r)))
                {
                    var actualText   = ClearUp(generatedSource.SourceText.ToString());
                    var expectedText = ClearUp(outText.ToString());
                    actualText.Should().BeEquivalentTo(expectedText, "result of generator should be same as in example");
                }
            }

            string ClearUp(string input)
            {
                return input.Replace("\r\n", "\n").Replace("\t", "    ");
            }
        }

        private static (SourceText Input, List<SourceText> Expected) GetInAndOutExamples(string exampleName)
        {
            var input = TryGetSourceText($"{exampleName}.in.cs")
                     ?? throw new InvalidOperationException($"Cannot find file for {exampleName}.in.cs");
            var output = GetExpected().ToList();
            if (output.Count == 0)
                throw new InvalidOperationException($"Cannot find file for {exampleName}.out.cs");

            return (input, output);

            SourceText? TryGetSourceText(string fileName)
            {
                var path = Path.Combine(ExamplesFolder, fileName);
                if (File.Exists(path))
                    return SourceText.From(File.OpenRead(path));
                return null;
            }

            IEnumerable<SourceText> GetExpected()
            {
                if (TryGetSourceText($"{exampleName}.out.cs") is { } expected)
                {
                    yield return expected;
                }

                for (var i = 1; ; i++)
                {
                    if (TryGetSourceText($"{exampleName}.out.{i}.cs") is { } multipleExpected)
                        yield return multipleExpected;
                    else
                        break;
                }
            }
        }
    }
}