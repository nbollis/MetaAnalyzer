using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chemistry;
using MzLibUtil;

namespace RadicalFragmentation.Util
{
    internal class MissedMonoisotopicTolerance : PpmTolerance
    {
        private readonly double[] AcceptableSortedMassShifts;

        public MissedMonoisotopicTolerance(double value, int missedMonoisotopics = 0) : base(value)
        {
            AcceptableSortedMassShifts = GetMissedMonos(missedMonoisotopics);
        }

        public override DoubleRange GetRange(double mean)
        {
            throw new NotImplementedException();
        }

        public override double GetMinimumValue(double mean)
        {
            throw new NotImplementedException();
        }

        public override double GetMaximumValue(double mean)
        {
            throw new NotImplementedException();
        }

        public override bool Within(double experimental, double theoretical)
        {
            for (int i = 0; i < AcceptableSortedMassShifts.Length; i++)
            {
                var val = (experimental - theoretical + AcceptableSortedMassShifts[i]) / theoretical * 1000000.0;
                if (Math.Abs(val) <= Value)
                {
                    return true;
                }
            }
            return false;
        }

        private double[] GetMissedMonos(int toGet)
        {
            double[] result = new double[toGet+1];
            for (int i = 0; i < toGet + 1; i++)
            {
                result[i] = i * Constants.C13MinusC12;
            }
            return result;
        }
    }
}
