using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Util;

namespace Analyzer.Plotting.Util
{
    public static class Labels
    {
        internal const string BottomUpSpectrumMatchLabel = "PSMs";
        internal const string TopDownSpectrumMatchLabel = "PrSMs";
        internal const string BottomUpPeptideLabel = "Peptides";
        internal const string BottomUpPeptidoformLabel = "Peptidoform";
        internal const string TopDownProteoformLabel = "Proteoforms";
        internal const string ProteinLabel = "Proteins";

        public static string GetLabel(bool isTopDown, ResultType resultType) =>
            resultType switch
            {
                ResultType.Psm => isTopDown ? TopDownSpectrumMatchLabel : BottomUpSpectrumMatchLabel,
                ResultType.Peptide => isTopDown ? TopDownProteoformLabel : BottomUpPeptideLabel,
                ResultType.Protein => ProteinLabel,
                _ => throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null)
            };

        public static string GetSpectrumMatchLabel(bool isTopDown) => isTopDown ? TopDownSpectrumMatchLabel : BottomUpSpectrumMatchLabel;

        public static string GetPeptideLabel(bool isTopDown) => isTopDown ? TopDownProteoformLabel : BottomUpPeptideLabel;

        public static string GetDifferentFormLabel(bool isTopDown) =>
            isTopDown ? TopDownProteoformLabel : BottomUpPeptidoformLabel;
    }
}
