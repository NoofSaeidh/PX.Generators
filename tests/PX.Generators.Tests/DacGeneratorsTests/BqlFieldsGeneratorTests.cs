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
using PX.Generators.Tests.DacGeneratorsTests.Examples;
using Xunit;

namespace PX.Generators.Tests
{
    public class BqlFieldsGeneratorTests
    {
        private static readonly string ExamplesFolder = "../../../DacGeneratorsTests/Examples";
        private static readonly string ExamplesNamespace = typeof(SimpleExample).Namespace;

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
                var hintPaths         = generatorResult.GeneratedSources.Select(s => s.HintName);
                var expectedFileNames = expected.Select(e => e.FileName);
                hintPaths.Should().BeEquivalentTo(expectedFileNames,
                                                  config => config.WithoutStrictOrdering(),
                                                  "hint paths should be equal to expected file names");
                generatorResult.Exception.Should().BeNull();

                // todo
                //generatorResult.GeneratedSources[0].HintName.Should()
                //               .Be($"PX.Generators.Tests.Examples.{exampleName}.bqlfields.g.cs");
                foreach (var generatedSource in generatorResult.GeneratedSources)
                {
                    var actualText = ClearUp(generatedSource.SourceText.ToString());
                    var expectedText =
                        ClearUp(expected.First(e => e.FileName == generatedSource.HintName).Text.ToString());
                    actualText.Should()
                              .BeEquivalentTo(expectedText, "result of generator should be same as in example");
                }
            }

            string ClearUp(string input)
            {
                return input.Replace("\r\n", "\n").Replace("\t", "    ");
            }
        }

        private static (SourceText Input, List<(string FileName, SourceText Text)> Expected) GetInAndOutExamples(
            string exampleName)
        {
            
            var input = TryGetSourceText(Path.Combine(ExamplesFolder, $"{exampleName}.in.cs"))
                     ?? throw new InvalidOperationException($"Cannot find file for {exampleName}.in.cs");
            var output = GetExpected().ToList();
            if (output.Count == 0)
                throw new InvalidOperationException($"Cannot find file for {exampleName}.out.cs");

            return (input, output);

            SourceText? TryGetSourceText(string path)
            {
                if (File.Exists(path))
                    return SourceText.From(File.OpenRead(path));
                return null;
            }

            IEnumerable<(string, SourceText)> GetExpected()
            {
                foreach (var file in Directory.EnumerateFiles(ExamplesFolder, $"{exampleName}*.out.cs"))
                {
                    var name = $"{ExamplesNamespace}.{Path.GetFileName(file).Replace(".out.cs", ".bqlfields.g.cs")}";
                    if (TryGetSourceText(file) is { } expectedPart)
                        yield return (name, expectedPart);
                }
            }
        }
    }
}