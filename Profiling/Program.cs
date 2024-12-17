using BenchmarkDotNet.Running;
using System;

namespace Profiling
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<ContainsWithinBenchmark>();
        }
    }
}