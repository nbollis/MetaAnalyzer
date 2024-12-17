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
    public class ContainsWithinBenchmark
    {
        private List<double> list;
        private HashSet<double> list_h;
        private List<double> values;
        private HashSet<double> values_h;
        private Tolerance tolerance;
        private static readonly HashSetPool<(double Min, double Max)> RangeSetPool = new (64);
        private static readonly int NumberOfValues = 4;


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
            values = values.OrderBy(p => p).ToList();
            values_h = values.ToHashSet();
            list_h = list.ToHashSet();
        }

        [Benchmark]
        public bool List_OriginalContainsWithin()
        {
            for (var index = 0; index < values.Count; index++)
            {
                if (!OriginalContainsWithin(list, values[index], tolerance))
                    return false;
            }

            return true;
        }

        [Benchmark]
        public bool List_OriginalContainsWithin_WithBinary()
        {
            for (var index = 0; index < values.Count; index++)
            {
                if (!BinaryContainsWithin(list, values[index], tolerance))
                    return false;
            }

            return true;
        }

        [Benchmark]
        public bool IsSuperSetSorted()
        {
            int i = 0, j = 0;

            while (i < list.Count && j < values.Count)
            {
                if (tolerance.Within(list[i], values[j]))
                {
                    // Match found within tolerance, move to the next subset element
                    j++;
                }
                else if (list[i] < tolerance.GetMinimumValue(values[j]))
                {
                    // Superset value is too small, move to the next superset element
                    i++;
                }
                else
                {
                    // Subset element not found within current superset range
                    return false;
                }
            }

            return j == values.Count; // If we matched all subset elements, return true
        }

        public static bool OriginalContainsWithin(List<double> list, double value, Tolerance tolerance)
        {
            foreach (var item in list)
            {
                if (tolerance.Within(item, value))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool BinaryContainsWithin(List<double> list, double value, Tolerance tolerance)
        {
            int index = list.BinarySearch(value);

            if (index >= 0) return true; // Exact match found

            // Check neighboring elements within tolerance
            index = ~index; // Insertion point
            return (index > 0 && Math.Abs(list[index - 1] - value) <= tolerance.Value) ||
                   (index < list.Count && Math.Abs(list[index] - value) <= tolerance.Value);
        }

        //[Benchmark]
        //public void OriginalContainsWithin()
        //{
        //    foreach (var value in values)
        //    {
        //        OriginalContainsWithin(list, value, tolerance);
        //    }
        //}

        //[Benchmark]
        //public void RangeContainsWithin()
        //{
        //    foreach (var value in values)
        //    {
        //        RangeContainsWithin(list, value, tolerance);
        //    }
        //}

        //[Benchmark]
        //public void BinaryContainsWithin()
        //{
        //    foreach (var value in values)
        //    {
        //        BinaryContainsWithin(list, value, tolerance);
        //    }
        //}




        //public static bool RangeContainsWithin(List<double> list, double value, Tolerance tolerance)
        //{
        //    // Precompute a range-based hash set (tolerance-aware)
        //    var rangeSet = RangeSetPool.Get();
        //    try
        //    {
        //        foreach (var item in list)
        //        {
        //            rangeSet.Add((item - tolerance.Value, item + tolerance.Value));
        //        }

        //        foreach (var range in rangeSet)
        //        {
        //            if (value >= range.Min && value <= range.Max)
        //                return true;
        //        }

        //        return false;
        //    }
        //    finally
        //    {
        //        RangeSetPool.Return(rangeSet);
        //    }
        //}


        //[Benchmark]
        //public bool List_RangeContainsWithin()
        //{
        //    var rangeSet = RangeSetPool.Get();
        //    try
        //    {
        //        foreach (var item in list)
        //            rangeSet.Add((item - tolerance.Value, item + tolerance.Value));

        //        foreach (var value in values)
        //        {
        //            if (!rangeSet.Any(range => value >= range.Min && value <= range.Max))
        //                return false;
        //        }
        //        return true;
        //    }
        //    finally
        //    {
        //        RangeSetPool.Return(rangeSet);
        //    }
        //}
    }
}
