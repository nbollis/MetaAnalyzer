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
        private List<double> values;
        private Tolerance tolerance;

        private static readonly HashSetPool<(double Min, double Max)> RangeSetPool = new (64);

        [Params(4, 16, 400, 2000)]
        public int NumberOfValues;

        [GlobalSetup]
        public void Setup()
        {
            tolerance = new PpmTolerance(10);
            Random rand = new(42);



            // populate lists
            list = new List<double>(NumberOfValues);
            for (int i = 0; i < NumberOfValues; i++)
            {
                list.Add(rand.Next(0, 30000) * 0.01);
            }
            list = list.OrderBy(p => p).ToList();

            var valueCount = rand.Next(NumberOfValues / 2, NumberOfValues * 2);
            values = new List<double>(valueCount);
            for (int i = 0; i < valueCount; i++)
            {
                values.Add(rand.Next(0, 30000) * 0.01);
            }
            values = values.OrderBy(p => p).ToList();
        }


        #region ContainsWithin

        [Benchmark]
        public bool Each_OriginalContainsWithin()
        {
            foreach (var value in values)
            {
                return list.ContainsWithin(value, tolerance);
            }
            return false;
        }

        [Benchmark]
        public bool Each_BinaryContainsWithin()
        {
            foreach (var value in values)
            {
                return list.BinaryContainsWithin(value, tolerance);
            }
            return false;
        }


        #endregion


        #region list contains within

        [Benchmark]
        public bool List_Iterate()
        {
            for (var index = 0; index < values.Count; index++)
            {
                if (!list.ContainsWithin(values[index], tolerance))
                    return false;
            }

            return true;
        }

        [Benchmark]
        public bool List_Iterate_WithBinary()
        {
            for (var index = 0; index < values.Count; index++)
            {
                if (!list.BinaryContainsWithin(values[index], tolerance))
                    return false;
            }

            return true;
        }

        [Benchmark]
        public bool List_TwoPointer()
        {
            return list.IsSuperSetSorted(values, tolerance);
        }
        
        [Benchmark]
        public bool List_TwoPointer_Hybrid()
        {
            return list.ListContainsWithin(values, tolerance);
        }

        #endregion





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
