using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easy.Common.Interfaces;

namespace Calibrator
{
    public static class MetaAnalyzerExtensions
    {
        public static IEnumerable<CalibratedRetentionTimeRecord> ToRecords(this Dictionary<string, List<(string, double)>> fileWiseCalibration) 
            => fileWiseCalibration.Select(record => new CalibratedRetentionTimeRecord(record.Key, record.Value));
    }
}
