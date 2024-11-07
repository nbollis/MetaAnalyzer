using ResultAnalyzerUtil;

namespace Plotting.Util
{
    public static class Labels
    {
        internal const string BottomUpSpectrumMatchLabel = "PSMs";
        internal const string TopDownSpectrumMatchLabel = "PrSMs";
        internal const string BottomUpPeptideLabel = "Peptides";
        internal const string BottomUpPeptidoformLabel = "Peptidoform";
        internal const string TopDownProteoformLabel = "Proteoform";
        internal const string TopDownProteoformsLabel = "Proteoforms";
        internal const string ProteinLabel = "Proteins";

        public static string GetLabel(bool isTopDown, ResultType resultType) =>
            resultType switch
            {
                ResultType.Psm => isTopDown ? TopDownSpectrumMatchLabel : BottomUpSpectrumMatchLabel,
                ResultType.Peptide => isTopDown ? TopDownProteoformsLabel : BottomUpPeptideLabel,
                ResultType.Protein => ProteinLabel,
                _ => throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null)
            };

        public static string GetSpectrumMatchLabel(bool isTopDown) => isTopDown ? TopDownSpectrumMatchLabel : BottomUpSpectrumMatchLabel;

        public static string GetPeptideLabel(bool isTopDown) => isTopDown ? TopDownProteoformsLabel : BottomUpPeptideLabel;

        public static string GetDifferentFormLabel(bool isTopDown) =>
            isTopDown ? TopDownProteoformLabel : BottomUpPeptidoformLabel;
    }
}
