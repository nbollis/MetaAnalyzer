using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MzLibUtil;

namespace Profiling
{
    [MemoryDiagnoser]
    public class ToleranceBenchmarks
    {
        private List<double> _list;
        private List<double> _values;

        public OldPpmTolerance OldTolerance;
        public PpmTolerance NewTolerance;

        [GlobalSetup]
        public void Setup()
        {
            OldTolerance = new OldPpmTolerance(10);
            NewTolerance = new PpmTolerance(10);
            Random rand = new Random(42);
            _list = new List<double>(2000);
            for (int i = 0; i < 2000; i++)
            {
                _list.Add(rand.Next(0, 30000) * 0.01);
            }
            _list = _list.OrderBy(p => p).ToList();
            _values = new List<double>(2000);
            for (int i = 0; i < 2000; i++)
            {
                _values.Add(rand.Next(0, 30000) * 0.01);
            }
            _values = _values.OrderBy(p => p).ToList();
        }

        [Benchmark]
        public bool OldTolerance()
        {

        }
    }
}
