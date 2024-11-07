using Analyzer.FileTypes.Internal;
using Analyzer.SearchType;
using ResultAnalyzerUtil;

namespace Analyzer.Util
{
    public static class ChimeraPaperNumbers
    {
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
