using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace PX.Generators.Tests.Common
{
    public static class TestsInitialization
    {
        public static Compilation CreateCompilation(SourceText source)
        {
            return CSharpCompilation.Create("compilation",
                                            new[] { CSharpSyntaxTree.ParseText(source) },
                                            null,
                                            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                                    .WithReferences(TestsInitialization.GetMetadataReferences());
        }

        public static IEnumerable<MetadataReference> GetMetadataReferences()
        {
            var dotnetPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            string[] libs =
            {
                "mscorlib.dll",
                //"netstandard.dll",
                "System.dll",
                "System.Core.dll",
                //"System.Private.CoreLib.dll"
                //"System.Runtime.dll",
            };

            foreach (var lib in libs)
            {
                yield return MetadataReference.CreateFromFile(Path.Combine(dotnetPath, lib));
            }

            var path = @"..\..\..\..\..\lib";
            foreach (var dll in Directory.GetFiles(path, "PX.Data*.dll"))
            {
                yield return MetadataReference.CreateFromFile(dll);
            }
        }
    }
}