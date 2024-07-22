using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMD
{
    public static class ImportantPaths
    {
        internal static string MetaMorpheusLocation =>
            @"C:\Users\Nic\source\repos\MetaMorpheus\MetaMorpheus\CMD\bin\Release\net6.0";

        internal static string UniprotHumanProteomeAndReviewedXml =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";


        #region Bottom Up

        // Result and data paths
        public static string Mann11FdrRunPath =>
            @"B:\Users\Nic\Chimeras\FdrAnalysis\UseProvidedLibraryOnAllFiles_Mann11_Ind";

        public static string Mann11AllRunPath =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis";

        public static string Mann11DataFilePath =>
            @"B:\RawSpectraFiles\Mann_11cell_lines";


        // Internal MM Comparison
        public static string Search_BuildChimericLibraryMann11 =>
            @"";
        public static string Search_BuildNonChimericLibraryMann11 =>
            @"";

        public static string NonChimericLibraryMann11 =>
            @"B:\Users\Nic\Chimeras\FdrAnalysis\Mann11_Reps1+2_NonChimericLibrary.msp";

        public static string ChimericLibraryMann11 =>
            @"";

        public static string GptmdNoChimerasMann11 =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\GPTMD_NoChimeras.toml";
        public static string GptmdWithChimerasMann11 =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\GPTMD_WithChimeras.toml";

        public static string SearchNoChimerasMann11 =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\Search_NoChimeras.toml";
        public static string SearchWithChimerasMann11 =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\Search_WithChimeras.toml";

        public static string SearchNoChimerasMann11_BuildLibrary => 
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\Search_NoChimeras_BuildLibrary.toml";

        public static string SearchWithChimerasMann11_BuildLibrary =>
        @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\Search_WithChimeras_BuildLibrary.toml";

        #endregion

        #region Top-Down

        // Result and data paths
        public static string TdJurkatFdrRunPath =>
            @"B:\Users\Nic\Chimeras\FdrAnalysis\UseProvidedLibraryOnAllFiles_JurkatTD";

        public static string TdJurkatAllRunPath =>
            @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat";

        public static string TdJurkatDataFilePath =>
            @"";


        // Internal MM Comparison
        public static string Search_BuildChimericLibraryJurkatTd =>
            @"";
        public static string Search_BuildNonChimericLibraryJurkatTd =>
            @"";

        public static string NonChimericLibraryJurkatTd =>
            @"";

        public static string ChimericLibraryJurkatTd =>
            @"";

        public static string GptmdNoChimerasJurkatTd =>
            @"";
        public static string GptmdWithChimerasJurkatTd =>
            @"";

        public static string SearchNoChimerasJurkatTd =>
            @"";
        public static string SearchWithChimerasJurkatTd =>
            @"";

        #endregion


    }
}
