using Analyzer.FileTypes.Internal;
using Analyzer.SearchType;
using ResultAnalyzerUtil;

namespace Analyzer.Util
{
    public class PaperNumbers
    {
        public ResultType ResultType { get; set; }
        public string Condition { get; set; }

        // Raw Numbers
        public int TotalIds { get; set; }
        public int TotalConfidentIds { get; set; }

        // Chimeric Identifications
        public int MaxIdsPerScan { get; set; }
        public int MaxConfidentIdsPerScan { get; set; }
        public double PercentOfScansWithIds { get; set; }
        public double PercentOfScansWithASingleId { get; set; }
        public double PercentOfScansWithChimericIds { get; set; }
        public double PercentOfIdentifiedScansWithASingleId { get; set; }
        public double PercentOfIdentifiedScansWithChimericIds { get; set; }

        // Features
        public int MaxFeaturesPerConfidentId { get; set; }
        public double PercentOfConfidentIdsWithNoFeatures { get; set; }
        public double PercentOfConfidentIdsWithSingleFeature { get; set; }
        public double PercentOfConfidentIdsWithMultipleFeatures { get; set; }
    }

    public static class ChimeraPaperNumbers
    {
        public static PaperNumbers CalculateMetrics(this IEnumerable<MetaMorpheusResult> results, ResultType resultType = ResultType.Psm)
        {
            string condition = results.ElementAt(0).Condition;
            int totalIds = 0, totalConfidentIds = 0, maxIdsPerScan = 0, maxFeatures = 0, maxConfidentIdsPerScan;
            double percentOfScansWithIds = 0, percentOfScansWithASingleId = 0, percentOfScansWithChimericIds = 0, percentOfIdentifiedScansWithASingleId = 0,
                percentOfIdentifiedScansWithChimericIds = 0, percentOfConfidentIdsWithNoFeatures = 0, percentOfConfidentIdsWithSingleFeature = 0, percentOfConfidentIdsWithMultipleFeatures = 0;

            // Extract all summary records of the specified result type
            var summaryRecords = results.SelectMany(p => p.ChimericSpectrumSummaryFile)
                .Where(p => p.Type == resultType.ToString() || p.Type == "No ID")
                .ToList();
            int totalScanCount = summaryRecords
                .GroupBy(p => new { p.FileName, p.Ms2ScanNumber })
                .Count();
            var recordsOfType = summaryRecords
                .Where(p => p.Type == resultType.ToString())
                .ToList();
            var confidentSummaryRecords = recordsOfType
                .Where(p => p.PEP_QValue <= 0.01)
                .ToList();


            totalIds = recordsOfType.Count;
            totalConfidentIds = confidentSummaryRecords.Count;

            // Chimeric Identification Metrics
            var recordsOfTypeGroupedByScan = recordsOfType
                .GroupBy(p => new { p.FileName, p.Ms2ScanNumber })
                .ToList();
            maxIdsPerScan = recordsOfTypeGroupedByScan.Select(p => p.Count()).DefaultIfEmpty(0).Max();
            percentOfScansWithIds = recordsOfTypeGroupedByScan.Count / (double)totalScanCount * 100;
            percentOfScansWithASingleId = recordsOfTypeGroupedByScan.Count(p => p.Count() == 1) / (double)totalScanCount * 100;
            percentOfScansWithChimericIds = recordsOfTypeGroupedByScan.Count(p => p.Count() > 1) / (double)totalScanCount * 100;

            var confidentSummaryRecordsGroupedByScan = confidentSummaryRecords
                .GroupBy(p => new { p.FileName, p.Ms2ScanNumber })
                .ToList();
            maxConfidentIdsPerScan = confidentSummaryRecordsGroupedByScan.Select(p => p.Count()).DefaultIfEmpty(0).Max();
            percentOfIdentifiedScansWithASingleId = confidentSummaryRecordsGroupedByScan.Count(p => p.Count() == 1) / (double)confidentSummaryRecordsGroupedByScan.Count * 100;
            percentOfIdentifiedScansWithChimericIds = confidentSummaryRecordsGroupedByScan.Count(p => p.Count() > 1) / (double)confidentSummaryRecordsGroupedByScan.Count * 100;

            // Features
            maxFeatures = confidentSummaryRecords.Select(p => p.PossibleFeatureCount).DefaultIfEmpty(0).Max();
            percentOfConfidentIdsWithNoFeatures = confidentSummaryRecords.Count(p => p.PossibleFeatureCount == 0) / (double)totalConfidentIds * 100;
            percentOfConfidentIdsWithSingleFeature = confidentSummaryRecords.Count(p => p.PossibleFeatureCount == 1) / (double)totalConfidentIds * 100;
            percentOfConfidentIdsWithMultipleFeatures = confidentSummaryRecords.Count(p => p.PossibleFeatureCount > 1) / (double)totalConfidentIds * 100;

            return new PaperNumbers
            {
                ResultType = resultType,
                Condition = condition,
                TotalIds = totalIds,
                TotalConfidentIds = totalConfidentIds,
                MaxIdsPerScan = maxIdsPerScan,
                MaxFeaturesPerConfidentId = maxFeatures,
                MaxConfidentIdsPerScan = maxConfidentIdsPerScan,
                PercentOfScansWithIds = percentOfScansWithIds,
                PercentOfScansWithASingleId = percentOfScansWithASingleId,
                PercentOfScansWithChimericIds = percentOfScansWithChimericIds,
                PercentOfIdentifiedScansWithASingleId = percentOfIdentifiedScansWithASingleId,
                PercentOfIdentifiedScansWithChimericIds = percentOfIdentifiedScansWithChimericIds,
                PercentOfConfidentIdsWithNoFeatures = percentOfConfidentIdsWithNoFeatures,
                PercentOfConfidentIdsWithSingleFeature = percentOfConfidentIdsWithSingleFeature,
                PercentOfConfidentIdsWithMultipleFeatures = percentOfConfidentIdsWithMultipleFeatures
            };
        }


        public static double GetFractionContainingSinglePrecursorLeadingToConfidentId(this MetaMorpheusResult mmResult, ResultType resultType = ResultType.Psm)
        {
            var summary = mmResult.ChimericSpectrumSummaryFile;
            var resultSpecific = summary.Where(p => p.Type == resultType.ToString()).ToList();
            var confident = resultSpecific.Where(p => p.PEP_QValue <= 0.01).ToList();
            var nonZero = confident.Where(p => p.PossibleFeatureCount > 0).ToList();

            // group by scan, then by possible feature count
            var scanBasis = nonZero.GroupBy(p => p,
                    new CustomComparer<ChimericSpectrumSummary>(p => p.FileName, p => p.Ms2ScanNumber))
                .Select(p => p.First())
                .GroupBy(p => p.PossibleFeatureCount)
                .OrderBy(p => p.Key)
                .ToDictionary(p => p.Key, p => p.ToList());

            var single = scanBasis[1].Count();
            var total = scanBasis.Sum(p => p.Value.Count);
            var percent = single / (double)total * 100;
            return percent;
        }
    }
}
