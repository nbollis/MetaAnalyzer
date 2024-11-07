using Proteomics.PSM;
using ResultAnalyzerUtil;

namespace Analyzer.Util
{
    public static class MzLibExtensions
    {

        /// <summary>
        /// Determine if a PSM is a decoy
        /// </summary>
        /// <param name="psm"></param>
        /// <returns></returns>
        public static bool IsDecoy(this PsmFromTsv psm) => psm.DecoyContamTarget == "D";

        public static Dictionary<int, List<PsmFromTsv>> ToChimeraGroupedDictionary(this IEnumerable<PsmFromTsv> psms)
        {
            return psms.GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer)
                .GroupBy(m => m.Count())
                .ToDictionary(p => p.Key, p => p.SelectMany(m => m).ToList());
        }

        public static bool PassesConfidenceFilter(this PsmFromTsv psm) => psm is { DecoyContamTarget: "T", PEP_QValue: <= 0.01 };
    }
}
