using Proteomics.PSM;

namespace Analyzer.Util
{
    public static class Extensions
    {
        public static bool IsDecoy(this PsmFromTsv psm) => psm.DecoyContamTarget == "D";
    }
}
