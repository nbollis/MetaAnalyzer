using Analyzer.FileTypes.External;
using ResultAnalyzerUtil;

namespace Analyzer.Util
{
    public static class CustomComparerExtensions
    {
        // MsFragger
        public static CustomComparer<MsFraggerPsm> MsFraggerChimeraComparer =>
            new(psm => psm.OneBasedScanNumber, psm => psm.FileNameWithoutExtension);

        public static CustomComparer<MsFraggerPeptide> MsFraggerPeptideDistinctComparer =>
            new(peptide => peptide.BaseSequence,
                peptide => peptide.ProteinAccession,
                peptide => peptide.AssignedModifications.Length,
                peptide => Enumerable.FirstOrDefault<string>(peptide.AssignedModifications),
                peptide => Enumerable.LastOrDefault<string>(peptide.AssignedModifications),
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

        public static CustomComparer<ProteomeDiscovererProteoformRecord> PSPDPrSMDistinctProteoformComparer =>
            new CustomComparer<ProteomeDiscovererProteoformRecord>(
                prsm => prsm.ProteinAccessions,
                prsm => prsm.Sequence,
                prsm => prsm.Modifications);

        public static CustomComparer<ProteomeDiscovererProteinRecord> PSPDPrSMDistinctProteinComparer =>
        new CustomComparer<ProteomeDiscovererProteinRecord>(
                           prsm => prsm.Accession);

        // Chimerys
        public static CustomComparer<ChimerysPsm> ChimerysChimeraComparer =>
            new CustomComparer<ChimerysPsm>(
                prsm => prsm.OneBasedScanNumber,
                prsm => prsm.FileNameWithoutExtension);

        public static CustomComparer<ChimerysPsm> ChimerysDistinctPsmComparer =>
            new CustomComparer<ChimerysPsm>(
                prsm => prsm.BaseSequence,
                prsm => prsm.OneBasedScanNumber,
                prsm => prsm.FileNameWithoutExtension,
                prsm => prsm.ProteinAccession,
                prsm => prsm.ModifiedSequence);

        public static CustomComparer<ChimerysPeptide> ChimerysDistinctPeptideComparer =>
            new CustomComparer<ChimerysPeptide>(
                prsm => prsm.BaseSequence,
                prsm => string.Join(',', prsm.ProteinIds),
                prsm => string.Join(',',prsm.ModifiedPeptideIds));

        public static CustomComparer<ChimerysProteinGroup> ChimerysDistinctProteinComparer =>
            new CustomComparer<ChimerysProteinGroup>(
                prsm => prsm.ProteinGroupId);

        // MsPathFinderT
        public static CustomComparer<MsPathFinderTResult> MsPathFinderTChimeraComparer =>
            new CustomComparer<MsPathFinderTResult>(
                prsm => prsm.OneBasedScanNumber,
                prsm => prsm.FileNameWithoutExtension);

        public static CustomComparer<MsPathFinderTResult> MsPathFinderTDistinctProteoformComparer =>
            new CustomComparer<MsPathFinderTResult>(
                prsm => prsm.FullSequence,
                prsm => prsm.OneBasedScanNumber);

        public static CustomComparer<MsPathFinderTResult> MsPathFinderTDistinctProteinComparer =>
            new CustomComparer<MsPathFinderTResult>(
                prsm => prsm.Accession);

        public static CustomComparer<MsPathFinderTCrossTabResultRecord> MsPathFinderTCrossTabDistinctProteoformComparer =>
            new CustomComparer<MsPathFinderTCrossTabResultRecord>(
                prsm => prsm.BaseSequence,
                prsm => prsm.Modifications
                .Select(p => (p.Name, p.ModifiedResidue)).OrderBy(p => p.Item2),
                prsm => prsm.StartResidue,
                prsm => prsm.EndResidue,
                prsm => prsm.ProteinAccession
            );
    }
}
