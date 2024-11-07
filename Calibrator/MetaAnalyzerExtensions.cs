namespace Calibrator
{
    public static class MetaAnalyzerExtensions
    {
        public static IEnumerable<CalibratedRetentionTimeRecord> ToRecords(this Dictionary<string, List<(string, double)>> fileWiseCalibration) 
            => fileWiseCalibration.Select(record => new CalibratedRetentionTimeRecord(record.Key, record.Value));
    }
}
