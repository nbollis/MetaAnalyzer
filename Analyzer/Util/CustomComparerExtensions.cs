﻿using Analyzer.FileTypes.External;
using ResultAnalyzerUtil;

namespace Analyzer.Util
{
    internal static class CustomComparerExtensions
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
                prsm => string.Join<int>('.', prsm.Modifications),
                prsm => prsm.StartResidue,
                prsm => prsm.EndResidue,
                prsm => prsm.ProteinAccession
            );
    }
}
