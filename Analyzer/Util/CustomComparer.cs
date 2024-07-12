using Analyzer.FileTypes.External;
using Plotly.NET;
using Proteomics.PSM;

namespace Analyzer.Util
{


    public class CustomComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, object>[] propertySelectors;

        public CustomComparer(params Func<T, object>[] propertySelectors)
        {
            this.propertySelectors = propertySelectors;
        }

        public bool Equals(T x, T y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;

            foreach (var selector in propertySelectors)
            {
                if (selector.Target is IEnumerable<double> enumerable)
                {
                    if (!enumerable.SequenceEqual((IEnumerable<double>)selector(y)))
                        return false;
                }
                if (selector.Target is IEnumerable<int> enumerableInt)
                {
                    if (!enumerableInt.SequenceEqual((IEnumerable<int>)selector(y)))
                        return false;
                }
                else if (!Equals(selector(x), selector(y)))
                    return false;
            }

            return true;
        }

        public int GetHashCode(T obj)
        {
            unchecked
            {
                int hash = 17;
                foreach (var selector in propertySelectors)
                {
                    hash = hash * 23 + (selector(obj)?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }


        #region Custom Implementations

        // MetaMorpheus
        public static CustomComparer<PsmFromTsv> ChimeraComparer =>
            new(psm => psm.PrecursorScanNum, psm => psm.Ms2ScanNumber,
                psm => psm.FileNameWithoutExtension.Replace("-calib", "").Replace("-averaged", ""));

        public static CustomComparer<PsmFromTsv> MetaMorpheusDuplicatedPsmFromDifferentPrecursorPeaksComparer =>
            new(psm => psm.FullSequence, psm => psm.Ms2ScanNumber, psm => psm.PrecursorScanNum,
                psm => psm.FileNameWithoutExtension.Replace("-calib", "").Replace("-averaged", "")); 

        // MsFragger
        public static CustomComparer<MsFraggerPsm> MsFraggerChimeraComparer =>
            new(psm => psm.OneBasedScanNumber, psm => psm.FileNameWithoutExtension);

  

        public static CustomComparer<MsFraggerPeptide> MsFraggerPeptideDistinctComparer =>
            new(peptide => peptide.BaseSequence,
                peptide => peptide.ProteinAccession,
                peptide => peptide.AssignedModifications.Length,
                peptide => peptide.AssignedModifications.FirstOrDefault(),
                peptide => peptide.AssignedModifications.LastOrDefault(),
                peptide => peptide.NextAminoAcid,
                peptide => peptide.PreviousAminoAcid);

        
        // ProsightPD 
        public static CustomComparer<ProteomeDiscovererPsmRecord> PSPDPrSMChimeraComparer =>
            new CustomComparer<ProteomeDiscovererPsmRecord>(
                prsm => prsm.FileID,
                prsm => prsm.Ms2ScanNumber);

        public static CustomComparer<ProteomeDiscovererPsmRecord> PSPDPrSMDistinctPsmComparer => 
            new CustomComparer<ProteomeDiscovererPsmRecord>(
                prsm => prsm.ProteinAccessions,
                           prsm => prsm.Ms2ScanNumber,
                           prsm => prsm.FileID,
                           prsm => prsm.AnnotatedSequence);
        //public static CustomComparer<ProteomeDiscovererProteoformRecord> PSPDPrSMDistinctProteoformComparer =>
        //    new CustomComparer<ProteomeDiscovererProteoformRecord>(
        //        prsm => prsm.ProteinAccessions,
        //        prsm => prsm.Sequence,
        //        prsm => prsm.Modifications);

        //public static CustomComparer<ProteomeDiscovererPsmRecord> PSPDPrSMDistinctProteinComparer => 
        //new CustomComparer<ProteomeDiscovererPsmRecord>(
        //                   prsm => prsm.ProteinAccessions);



        // MsPathFinderT
        public static CustomComparer<MsPathFinderTResult> MsPathFinderTChimeraComparer =>
            new CustomComparer<MsPathFinderTResult>(
                prsm => prsm.OneBasedScanNumber,
                prsm => prsm.FileNameWithoutExtension);

        public static CustomComparer<MsPathFinderTResult> MsPathFinderTDistinctProteoformComparer =>
            new CustomComparer<MsPathFinderTResult>(
                prsm => prsm.BaseSequence,
                prsm => prsm.Modifications);

        public static CustomComparer<MsPathFinderTResult> MsPathFinderTDistinctProteinComparer =>
            new CustomComparer<MsPathFinderTResult>(
                prsm => prsm.Accession);

        public static CustomComparer<MsPathFinderTCrossTabResultRecord> MsPathFinderTCrossTabDistinctProteoformComparer =>
            new CustomComparer<MsPathFinderTCrossTabResultRecord>(
                prsm => prsm.BaseSequence,
                prsm => string.Join('.', prsm.Modifications),
                prsm => prsm.StartResidue,
                prsm => prsm.EndResidue,
                prsm => prsm.ProteinAccession
            );

        #endregion

    }
}
