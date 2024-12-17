using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MzLibUtil;
using ResultAnalyzerUtil;
using Easy.Common.Extensions;

namespace Profiling
{
    [MemoryDiagnoser]
    public class ListContainsWithinBenchmark
    {
        private List<double> list;
        private List<double> values;
        private Tolerance tolerance;
        private static readonly HashSetPool<(double Min, double Max)> RangeSetPool = new (64);
        private static readonly int NumberOfValues = 2000;


        [GlobalSetup]
        public void Setup()
        {
            tolerance = new PpmTolerance(10);
            Random rand = new(42);
            list = new List<double>(NumberOfValues);

            var valueCount = rand.Next(NumberOfValues / 2, NumberOfValues * 2);
            values = new List<double>(valueCount);

            Console.WriteLine($"Number of values: {valueCount}");

            // populate lists
            for (int i = 0; i < NumberOfValues; i++)
            {
                list.Add(rand.Next(0, 30000) * 0.01);
            }

            for (int i = 0; i < valueCount; i++)
            {
                values.Add(rand.Next(0, 30000) * 0.01);
            }
            list = list.OrderBy(p => p).ToList();
        }

        


    }
}
