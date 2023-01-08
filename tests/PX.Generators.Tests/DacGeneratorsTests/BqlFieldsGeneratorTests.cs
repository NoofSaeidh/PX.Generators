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
using PX.Generators.Tests.DacGeneratorsTests.Examples.Simple;
using Xunit;

namespace PX.Generators.Tests
{
    public class BqlFieldsGeneratorTests
    {
        private static readonly string ExamplesFolder = "../../../DacGeneratorsTests/Examples";
        private static readonly string ExamplesNamespace = "PX.Generators.Tests.DacGeneratorsTests.Examples";

        [Theory]
        [MemberData(nameof(GetExamples))]
        public void Generator_Should_Generate_All_Missing_Fields(ExamplesData data)
        {
            // arrange
            var             input          = data.Input;
            var             expectedOutput = data.ExpectedOutput;
            var             compilation    = TestsInitialization.CreateCompilation(input);
            var             generator      = new BqlFieldsGenerator();
            GeneratorDriver driver         = CSharpGeneratorDriver.Create(generator);

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
                outputCompilation.SyntaxTrees.Should().HaveCount(expectedOutput.Count + 1);
                outputCompilation.GetDiagnostics().Should().BeEmpty();

                var runResult = driver.GetRunResult();
                runResult.Diagnostics.Should().BeEmpty();
                runResult.GeneratedTrees.Should().HaveCount(expectedOutput.Count);

                var generatorResult = runResult.Results[0];
                generatorResult.Diagnostics.Should().BeEmpty();
                generatorResult.GeneratedSources.Should().HaveCount(expectedOutput.Count);
                var hintPaths         = generatorResult.GeneratedSources.Select(s => s.HintName);
                var expectedFileNames = expectedOutput.Select(e => e.FileName);
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
                        ClearUp(expectedOutput.First(e => e.FileName == generatedSource.HintName).Text.ToString());
                    actualText.Should()
                              .BeEquivalentTo(expectedText, "result of generator should be same as in example");
                }
            }

            string ClearUp(string input)
            {
                return input.Replace("\r\n", "\n").Replace("\t", "    ");
            }
        }

        public static IEnumerable<object[]> GetExamples()
        {
            foreach (var folder in Directory.EnumerateDirectories(ExamplesFolder))
            {
                var exampleName = Path.GetFileName(folder);
                var inputClass = Directory.EnumerateFiles(folder, "*.cs").Single();
                var input      = SourceText.From(File.ReadAllText(inputClass));
                var outFolder  = Directory.EnumerateDirectories(folder, "out").Single();
                var expectedOutput = Directory.EnumerateFiles(outFolder, "*.cs")
                                              .Select(f => (GetExpectedFileName(exampleName, f),
                                                            SourceText.From(File.ReadAllText(f))))
                                              .ToList();
                if (expectedOutput.Count == 0)
                {
                    throw new InvalidOperationException("No output files found");
                }

                yield return new object[] {new ExamplesData(exampleName, input, expectedOutput)};
            }

            static string GetExpectedFileName(string exampleName, string fileName) =>
                $"{ExamplesNamespace}.{exampleName}.{Path.GetFileName(fileName).Replace(".cs", ".bqlfields.g.cs")}";
        }

        public readonly struct ExamplesData
        {
            public ExamplesData(string exampleName, SourceText input,
                                List<(string FileName, SourceText Text)> expectedOutput)
            {
                ExampleName    = exampleName;
                Input          = input;
                ExpectedOutput = expectedOutput;
            }

            public string ExampleName { get; }
            public SourceText Input { get; }
            public List<(string FileName, SourceText Text)> ExpectedOutput { get; }

            public override string ToString() => ExampleName;
        }
    }
}