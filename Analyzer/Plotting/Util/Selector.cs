using Analyzer.SearchType;

namespace Analyzer.Plotting.Util
{
    public class Selector
    {
        #region Static Implementations

        public static Selector TopDownJurkatSelector { get; }
        public static Selector BottomUpMann11Selector { get; }
        public static Selector TopDownEcoliSelector { get; }
        public static Selector PaperTopDownJurkatSelector { get; }
        public static Selector PaperTopDownEcoliSelector { get; }
        public static Selector PaperBottomUpMann11Selector { get; }


        static Selector()
        {
            PaperTopDownJurkatSelector = new Selector()
            {
                DatasetName = "Jurkat",
                IsTopDown = true,
                IndividualFileComparisonSelector = new[]
                {
                    "MetaMorpheus_Chimeras",
                    "MsPathFinderTWithMods_15Rep2",
                    "ProsightPdChimeras_Rep2_15_10ppm",
                },
                InternalMetaMorpheusFileComparisonSelector = new[]
                {
                    "MetaMorpheus_Chimeras", "MetaMorpheus_NoChimeras"
                },
                BulkResultComparisonSelector = new[]
                {
                    "MetaMorpheus_NoChimeras", "MetaMorpheus_Chimeras",
                    "MsPathFinderT_NoChimeras", "MsPathFinderT_7", "MsPathFinderT_15",
                    "ProsightPD_NoChimeras", "ProsightPD_7", "ProsightPD_15",
                },
                SingleResultSelector = new[] { "MetaMorpheus_Chimeras" }
            };

            PaperTopDownEcoliSelector = new Selector()
            {
                DatasetName = "Ecoli",
                IsTopDown = true,
                IndividualFileComparisonSelector = new[]
                {
                    "MetaMorpheus_Chimeras",
                    "MsPathFinderTWithMods_15Rep2",
                    "ProsightPdChimeras_Rep2_15_10ppm",
                },
                InternalMetaMorpheusFileComparisonSelector = new[]
                {
                    "MetaMorpheus_Chimeras", "MetaMorpheus_NoChimeras"
                },
                BulkResultComparisonSelector = new[]
                {
                    "MetaMorpheus_NoChimeras", "MetaMorpheus_Chimeras",
                    "MsPathFinderT_NoChimeras", "MsPathFinderT_7", "MsPathFinderT_15",
                    "ProsightPD_NoChimeras", "ProsightPD_7", "ProsightPD_15",
                },
                SingleResultSelector = new[] { "MetaMorpheus_Chimeras" }
            };

            PaperBottomUpMann11Selector = new Selector()
            {
                DatasetName = "Mann_11cell_analysis",
                IsTopDown = false,
                IndividualFileComparisonSelector = new[]
                {
                    "MetaMorpheus_ExternalComparison_NoChimeras", "MetaMorpheus_ExternalComparison_Chimeras",
                    "MsFraggerDDA", "MsFraggerDDA+",
                    "Chimerys"
                },
                InternalMetaMorpheusFileComparisonSelector = new[]
                {
                    "MetaMorpheus_Chimeras", "MetaMorpheus_NoChimeras"
                },
                BulkResultComparisonSelector = new[]
                {
                    "MetaMorpheus_ExternalComparison_NoChimeras", "MetaMorpheus_ExternalComparison_Chimeras",
                    "MsFraggerDDA", "MsFraggerDDA+",
                    "Chimerys"
                },
                SingleResultSelector = new[] { "MetaMorpheus_Chimeras" }
            };

            TopDownJurkatSelector = new Selector()
            {
                DatasetName = "Jurkat",
                IsTopDown = true,
                IndividualFileComparisonSelector = new[]
                {
                    //"MetaMorpheusNoChimeras",
                    //"MetaMorpheus_Rep2_WithLibrary", // metamorpheus rep2 with library and old pep
                    "MetaMorpheus_Rep2_WithLibrary_NewPEP_NoNorm",

                    /*"MsPathFinderTWithModsNoChimerasRep2", "MsPathFinderTWithMods_7Rep2", */"MsPathFinderTWithMods_15Rep2",

                    /*"ProsightPDChimeras_Rep2",*/ /*"ProsightPDNoChimeras_Rep2",*/
                    "ProsightPdChimeras_Rep2_15_10ppm", /*"ProsightPDChimeras_Rep2_7_10ppm",*/
                },
                InternalMetaMorpheusFileComparisonSelector = new[]
                {
                    "MetaMorpheusNoChimeras", "MetaMorpheus_Rep2_WithLibrary_NewPEP_NoNorm"
                },
                BulkResultComparisonSelector = new[]
                {
                    /*"MetaMorpheus",*/ "MetaMorpheusNoChimeras",/* "MetaMorpheus_FullPEPChimeraIncorporation",*/
                    //"MetaMorpheus_Rep2_WithLibrary", // metamorpheus rep2 with library and old pep
                    //"MetaMorpheus_NewPEP_NoNormNoMult",
                    "MetaMorpheus_Rep2_WithLibrary_NewPEP_NoNorm",

                    "MsPathFinderTWithModsNoChimerasRep2", "MsPathFinderTWithMods_7Rep2", "MsPathFinderTWithMods_15Rep2",

                    /*"ProsightPDChimeras_Rep2",*/ "ProsightPDNoChimeras_Rep2", /*"ProsightPDChimeras_Rep2_15",*/
                    "ProsightPdChimeras_Rep2_15_10ppm", "ProsightPDChimeras_Rep2_7_10ppm",
                },
                SingleResultSelector = new[] { "MetaMorpheus_Rep2_WithLibrary_NewPEP_NoNorm" }
            };
            TopDownEcoliSelector = new Selector()
            {
                DatasetName = "Ecoli",
                IsTopDown = true,
                IndividualFileComparisonSelector = new[]
                {
                    //"MetaMorpheus", "MetaMorpheusNoChimeras",
                    "MetaMorpheus_NewPEP_NoNorm", // ecoli ran with new pep
                    "MetaMorpheus_NewPEP_NoNormNoMult", // ecoli ran with new pep no mult

                    /*"MsPathFinderTWithModsNoChimeras", "MsPathFinderTWithMods_7", */"MsPathFinderTWithMods_15",

                    /*"ProsightPDNoChimeras", "ProsightPDChimeras_7",*/ "ProsightPDChimeras_15"
                },
                InternalMetaMorpheusFileComparisonSelector = new[]
                {
                    "MetaMorpheusNoChimeras",
                    "MetaMorpheus_NewPEP_NoNormNoMult",
                },
                BulkResultComparisonSelector = new[] {
                    "MetaMorpheus_NewPEP_NoNormNoMult", "MetaMorpheusNoChimeras",
                    //"MetaMorpheus_NewPEP_NoNorm", // ecoli ran with new pep

                    "MsPathFinderTWithModsNoChimeras", "MsPathFinderTWithMods_7", "MsPathFinderTWithMods_15",

                    "ProsightPDChimeras_7", "ProsightPDNoChimeras", "ProsightPDChimeras_15"
                },
                SingleResultSelector = new[] { "MetaMorpheus_NewPEP_NoNormNoMult" }
            };
            BottomUpMann11Selector = new Selector()
            {
                DatasetName = "Mann_11cell_analysis",
                IsTopDown = false,
                IndividualFileComparisonSelector = new[] 
                {
                    //"MetaMorpheusWithLibrary", "MetaMorpheusNoChimerasWithLibrary", "MetaMorpheus_NoNormalization",

                    //"MetaMorpheusFraggerEquivalentChimeras_IndividualFiles",
                    "MetaMorpheusFraggerEquivalent_IndividualFilesFraggerEquivalentNoChimeras",
                    "MetaMorpheusFraggerEquivalent_IndividualFilesFraggerEquivalentWithChimeras",

                    "ReviewdDatabaseNoPhospho_MsFraggerDDA", "ReviewdDatabaseNoPhospho_MsFraggerDDA+", "ReviewdDatabaseNoPhospho_MsFragger",
                    //"Chimerys"
                },
                InternalMetaMorpheusFileComparisonSelector = new[]
                {
                    "MetaMorpheusWithLibrary", "MetaMorpheusNoChimerasWithLibrary"
                },
                BulkResultComparisonSelector = new[] 
                {
                    "MetaMorpheusFraggerEquivalent_IndividualFilesFraggerEquivalentNoChimeras",
                    "MetaMorpheusFraggerEquivalent_IndividualFilesFraggerEquivalentWithChimeras",

                    "ReviewdDatabaseNoPhospho_MsFraggerDDA+", "ReviewdDatabaseNoPhospho_MsFragger",
                    //"Chimerys"
                },
                SingleResultSelector = new[] { "MetaMorpheusWithLibrary" }
            };

            DatasetSelectors = new Dictionary<(string, bool), Selector>
            {
                { (TopDownJurkatSelector.DatasetName, true), TopDownJurkatSelector },
                { (TopDownEcoliSelector.DatasetName, true), TopDownEcoliSelector },
                { (BottomUpMann11Selector.DatasetName, false), BottomUpMann11Selector },
                { ("A549", false), BottomUpMann11Selector },
                { ("GAMG", false), BottomUpMann11Selector },
                { ("HEK293", false), BottomUpMann11Selector },
                { ("Hela", false), BottomUpMann11Selector },
                { ("HepG2", false), BottomUpMann11Selector },
                { ("Jurkat", false), BottomUpMann11Selector },
                { ("K562", false), BottomUpMann11Selector },
                { ("LanCap", false), BottomUpMann11Selector },
                { ("MCF7", false), BottomUpMann11Selector },
                { ("RKO", false), BottomUpMann11Selector },
                { ("U2OS", false), BottomUpMann11Selector },
            };
        }

        #endregion

        #region Static Methods

        public static Dictionary<(string, bool), Selector> DatasetSelectors { get; }

        public static Selector GetSelector(string datasetName, bool isTopDown)
        {
            if (DatasetSelectors.TryGetValue((datasetName, isTopDown), out var selector))
                return selector;
            throw new ArgumentException($"Dataset {datasetName} not found in selectors");
        }


        #endregion



        public string DatasetName { get; private init; }
        public bool IsTopDown { get; private init; }

        private string[]? _allSelectors;

        public string[] AllSelectors =>
            _allSelectors ??= IndividualFileComparisonSelector
                .Concat(InternalMetaMorpheusFileComparisonSelector)
                .Concat(BulkResultComparisonSelector)
                .Concat(SingleResultSelector)
                .Distinct()
                .ToArray();
        

        #region Chimera Paper

        public string[] IndividualFileComparisonSelector { get; private init; }
        public string[] InternalMetaMorpheusFileComparisonSelector { get; private init; }
        public string[] BulkResultComparisonSelector { get; private init; }
        public string[] SingleResultSelector { get; private init; }

        #endregion

        
    }

    public static class SelectorExtensions
    {
        public static string[] GetIndividualFileComparisonSelector(this bool isTopDown, string datasetName)
        {
            return Selector.GetSelector(datasetName, isTopDown).IndividualFileComparisonSelector;
        }
        
        public static string[] GetInternalMetaMorpheusFileComparisonSelector(this bool isTopDown, string datasetName)
        {
            return Selector.GetSelector(datasetName, isTopDown).InternalMetaMorpheusFileComparisonSelector;
        }

        public static string[] GetBulkResultComparisonSelector(this bool isTopDown, string datasetName)
        {
            return Selector.GetSelector(datasetName, isTopDown).BulkResultComparisonSelector;
        }

        public static string[] GetSingleResultSelector(this bool isTopDown, string datasetName)
        {
            return Selector.GetSelector(datasetName, isTopDown).SingleResultSelector;
        }

        public static string[] GetAllSelectors(this bool isTopDown, string datasetName)
        {
            return Selector.GetSelector(datasetName, isTopDown).AllSelectors;
        }

        // From Cell Line
        public static string[] GetIndividualFileComparisonSelector(this CellLineResults results)
        {
            return results.First().IsTopDown.GetIndividualFileComparisonSelector(results.CellLine);
        }

        public static string[] GetInternalMetaMorpheusFileComparisonSelector(this CellLineResults results)
        {
            return results.First().IsTopDown.GetInternalMetaMorpheusFileComparisonSelector(results.CellLine);
        }

        public static string[] GetBulkResultComparisonSelector(this CellLineResults results)
        {
            return results.First().IsTopDown.GetBulkResultComparisonSelector(results.CellLine);
        }

        public static string[] GetSingleResultSelector(this CellLineResults results)
        {
            return results.First().IsTopDown.GetSingleResultSelector(results.CellLine);
        }

        public static string[] GetAllSelectors(this CellLineResults results)
        {
            return results.First().IsTopDown.GetAllSelectors(results.CellLine);
        }

        // From All Results
        public static string[] GetIndividualFileComparisonSelector(this AllResults results)
        {
            return results.SelectMany(p => p.GetIndividualFileComparisonSelector()).ToArray();
        }

        public static string[] GetInternalMetaMorpheusFileComparisonSelector(this AllResults results)
        {
            return results.SelectMany(p => p.GetInternalMetaMorpheusFileComparisonSelector()).ToArray();
        }

        public static string[] GetBulkResultComparisonSelector(this AllResults results)
        {
            return results.SelectMany(p => p.GetBulkResultComparisonSelector()).ToArray();
        }

        public static string[] GetSingleResultSelector(this AllResults results)
        {
            return results.SelectMany(p => p.GetSingleResultSelector()).ToArray();
        }

        public static string[] GetAllSelectors(this AllResults results)
        {
            return results.SelectMany(p => p.GetAllSelectors()).Distinct().ToArray();
        }

    }
}
