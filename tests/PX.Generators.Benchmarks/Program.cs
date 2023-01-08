using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using PX.Generators.Benchmarks.BqlFieldsGeneration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PX.Generators.Benchmarks
{
    internal class Program
    {
        public static void Main()
        {
            //BenchmarkRunner.Run<BqlFieldsGenerationBenchmark>();
            BenchmarkRunner.Run<BqlFieldsGenerationBenchmark>(new DebugInProcessConfig());
        }
    }
}