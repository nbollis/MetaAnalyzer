using Plotly.NET;

namespace Plotting.Util;

public static class PlottingTranslators
{
    static PlottingTranslators()
    {
        ColorQueue = new PlotlyColorQueue();
    }

    #region Conversion Dictionaries

    /// <summary>
    /// Colors for plots
    /// MetaMorpheus -> Purple
    /// Fragger -> Blue
    /// Chimerys ->
    /// MsPathFinderT -> Yellow
    /// ProsightPD -> Red
    /// </summary>
    public static Dictionary<string, Color> ConditionToColorDictionary = new()
    {
        // Bottom Up
        {"MetaMorpheusWithLibrary", Color.fromKeyword(ColorKeyword.Purple) },
        {"MetaMorpheus_Chimeras", Color.fromKeyword(ColorKeyword.Purple) },
        {"MetaMorpheusNoChimerasWithLibrary", Color.fromKeyword(ColorKeyword.Plum) },
        {"MetaMorpheus_NoChimeras", Color.fromKeyword(ColorKeyword.Plum) },
        { "MetaMorpheusFraggerEquivalent_IndividualFilesFraggerEquivalentWithChimeras", Color.fromKeyword(ColorKeyword.Purple) },
        { "MetaMorpheus_ExternalComparison_Chimeras", Color.fromKeyword(ColorKeyword.Purple) },
        { "MetaMorpheusFraggerEquivalent_IndividualFilesFraggerEquivalentNoChimeras", Color.fromKeyword(ColorKeyword.Plum)  },
        { "MetaMorpheus_ExternalComparison_NoChimeras", Color.fromKeyword(ColorKeyword.Plum)  },
        {"MsFraggerDDA", Color.fromKeyword(ColorKeyword.LightAkyBlue) }, // Old fragger params
        {"MsFraggerDDA+", Color.fromKeyword(ColorKeyword.RoyalBlue) },
        {"ReviewdDatabaseNoPhospho_MsFraggerDDA", Color.fromKeyword(ColorKeyword.LightAkyBlue) },
        {"ReviewdDatabaseNoPhospho_MsFragger", Color.fromKeyword(ColorKeyword.LightAkyBlue) },
        {"ReviewdDatabaseNoPhospho_MsFraggerDDA+", Color.fromKeyword(ColorKeyword.RoyalBlue) },
        {"Chimerys", Color.fromKeyword(ColorKeyword.Green) },

        // General
        {"Chimeras", Color.fromKeyword(ColorKeyword.Purple) }, // (128,0,128)
        {"Chimeric", Color.fromKeyword(ColorKeyword.Purple) }, //  	(221,160,221)
        {"Chimeric ID", Color.fromKeyword(ColorKeyword.Purple) }, //  	(221,160,221)
        {"No Chimeras", Color.fromKeyword(ColorKeyword.SeaGreen) },
        {"Non-Chimeric", Color.fromKeyword(ColorKeyword.SeaGreen) },
        {"Non-Chimeric ID", Color.fromKeyword(ColorKeyword.SeaGreen) },
        {"No ID", Color.fromKeyword(ColorKeyword.Red) },
        {"Unidentified MS2", Color.fromKeyword(ColorKeyword.Red) },
        {"Identified MS2", Color.fromKeyword(ColorKeyword.Blue) },

        // RT shift plot
        {"Chimeric All Psms", Color.fromKeyword(ColorKeyword.Purple) },
        {"Chimeric 1% Psms", Color.fromKeyword(ColorKeyword.Red) },
        {"Chimeric All Peptides", Color.fromKeyword(ColorKeyword.Blue) },
        {"Chimeric 1% Peptides", Color.fromKeyword(ColorKeyword.Green) },
        {"Non-Chimeric All Psms", Color.fromKeyword(ColorKeyword.Plum) },
        {"Non-Chimeric 1% Psms", Color.fromKeyword(ColorKeyword.PaleVioletRed) },
        {"Non-Chimeric All Peptides", Color.fromKeyword(ColorKeyword.DeepSkyBlue) },
        {"Non-Chimeric 1% Peptides", Color.fromKeyword(ColorKeyword.Lime) },

        // Top Down
        {"MetaMorpheus", Color.fromKeyword(ColorKeyword.Purple) }, // ecoli
        {"MetaMorpheus\u2800", Color.fromKeyword(ColorKeyword.Purple) }, // ecoli
        {"MetaMorpheusNoChimeras", Color.fromKeyword(ColorKeyword.Plum) }, // shared
        {"MetaMorpheus No Chimeras", Color.fromKeyword(ColorKeyword.Plum) }, // shared
        {"MetaMorpheus_Rep2_WithLibrary", Color.fromKeyword(ColorKeyword.MediumOrchid)}, // jurkat
        {"MetaMorpheus_Rep2_WithLibrary_NewPEP_NoNorm", Color.fromKeyword(ColorKeyword.Purple)},
        {"MetaMorpheus_NewPEP_NoNormNoMult", Color.fromKeyword(ColorKeyword.Purple)},

        {"MsPathFinderTWithModsNoChimeras", Color.fromKeyword(ColorKeyword.Moccasin)}, // ecoli
        {"MsPathFinderT_NoChimeras", Color.fromKeyword(ColorKeyword.Moccasin)}, // ecoli
        {"MsPathFinderTWithMods_7", Color.fromKeyword(ColorKeyword.Gold)},
        {"MsPathFinderT_7", Color.fromKeyword(ColorKeyword.Gold)},
        {"MsPathFinderTWithMods_15", Color.fromKeyword(ColorKeyword.GoldenRod)},
        {"MsPathFinderT_15", Color.fromKeyword(ColorKeyword.GoldenRod)},
        {"MsPathFinderTWithModsNoChimerasRep2", Color.fromKeyword(ColorKeyword.Moccasin)}, // jurkat
        {"MsPathFinderTWithMods_7Rep2", Color.fromKeyword(ColorKeyword.Gold)},
        {"MsPathFinderTWithMods_15Rep2", Color.fromKeyword(ColorKeyword.GoldenRod)},

        {"ProsightPDNoChimeras", Color.fromKeyword(ColorKeyword.PaleVioletRed)}, // ecoli
        {"ProsightPDChimeras", Color.fromKeyword(ColorKeyword.IndianRed)},
        {"ProsightPDChimeras_7", Color.fromKeyword(ColorKeyword.Red)},
        {"ProsightPDChimeras_15", Color.fromKeyword(ColorKeyword.Red)},
        {"ProsightPDNoChimeras_Rep2", Color.fromKeyword(ColorKeyword.PaleVioletRed)}, // jurkat
        {"ProsightPD_NoChimeras", Color.fromKeyword(ColorKeyword.PaleVioletRed)}, // jurkat
        {"ProsightPDChimeras_Rep2", Color.fromKeyword(ColorKeyword.IndianRed)},
        {"ProsightPDChimeras_Rep2_7", Color.fromKeyword(ColorKeyword.IndianRed)},
        {"ProsightPD_7", Color.fromKeyword(ColorKeyword.IndianRed)},
        {"ProsightPDChimeras_Rep2_7_10ppm", Color.fromKeyword(ColorKeyword.IndianRed)},
        {"ProsightPDChimeras_Rep2_15", Color.fromKeyword(ColorKeyword.DarkRed)},
        {"ProsightPD_15", Color.fromKeyword(ColorKeyword.DarkRed)},
        {"ProsightPDChimeras_Rep2_15_10ppm", Color.fromKeyword(ColorKeyword.DarkRed)},





        // Chimera Breakdown plot
        {"Isolated Species", Color.fromKeyword(ColorKeyword.LightAkyBlue)},
        {"Unique Proteoform", Color.fromKeyword(ColorKeyword.MediumVioletRed)},
        {"Unique Peptidoform", Color.fromKeyword(ColorKeyword.MediumVioletRed)},
        {"Unique Protein", Color.fromKeyword(ColorKeyword.MediumAquamarine)},
        {"Targets", Color.fromKeyword(ColorKeyword.LightAkyBlue)},
        {"Decoys", Color.fromKeyword(ColorKeyword.Gold)},
        {"Duplicates", Color.fromKeyword(ColorKeyword.RoyalBlue)},

        // PEP testing
        {"MetaMorpheus_Rep1_BuildLibrary", Color.fromKeyword(ColorKeyword.LightAkyBlue)},
        {"MetaMorpheus_Rep2_NoLibrary", Color.fromKeyword(ColorKeyword.MediumVioletRed)},
        {"Full_ChimeraIncorporation", Color.fromKeyword(ColorKeyword.GoldenRod)},
        {"MetaMorpheus_FullPEPChimeraIncorporation", Color.fromKeyword(ColorKeyword.GoldenRod)},
        {"Full_ChimeraIncorporation_NoNormalization", Color.fromKeyword(ColorKeyword.Plum)},
        {"Small_ChimeraIncorporation", Color.fromKeyword(ColorKeyword.RoyalBlue)},

        {"Semi-Specific", Color.fromKeyword(ColorKeyword.Indigo)},
        {"Non-Specific", Color.fromKeyword(ColorKeyword.SteelBlue)},
        {"Modern", Color.fromKeyword(ColorKeyword.YellowGreen)},
        {"Classic - Initial", Color.fromKeyword(ColorKeyword.Blue)},
        {"Classic - Post Calibration", Color.fromKeyword(ColorKeyword.Orange)},
        {"Classic - Post GPTMD", Color.fromKeyword(ColorKeyword.Green)},
        {"TopDown - Initial", Color.fromKeyword(ColorKeyword.Navy)},
        {"TopDown - Post Calibration", Color.fromKeyword(ColorKeyword.DarkGoldenRod)},
        {"TopDown - Post Averaging", Color.fromKeyword(ColorKeyword.DarkRed)},
        {"TopDown - Post GPTMD", Color.fromKeyword(ColorKeyword.DarkGreen)},


        // Isolation Window Study 
        {"1 m/z - Rep1", Color.fromHex("#440154")},
        {"1 m/z - Rep2", Color.fromHex("#440154")},
        {"1 m/z - Rep3", Color.fromHex("#440154")},
        {"1 m/z - YeastPeppiB", Color.fromHex("#440154")},
        {"2 m/z - Rep1", Color.fromHex("#472b7a")},
        {"2 m/z - Rep2", Color.fromHex("#472b7a")},
        {"2 m/z - Rep3", Color.fromHex("#472b7a")},
        {"2 m/z - YeastPeppiB", Color.fromHex("#472b7a")},
        {"4 m/z - Rep1", Color.fromHex("#3b528b")},
        {"4 m/z - Rep2", Color.fromHex("#3b528b")},
        {"4 m/z - Rep3", Color.fromHex("#3b528b")},
        {"4 m/z - YeastPeppiB", Color.fromHex("#3b528b")},
        {"6 m/z - Rep1", Color.fromHex("#2c728e")},
        {"6 m/z - Rep2", Color.fromHex("#2c728e")},
        {"6 m/z - Rep3", Color.fromHex("#2c728e")},
        {"6 m/z - YeastPeppiB", Color.fromHex("#2c728e")},
        {"8 m/z - Rep1", Color.fromHex("#21918c")},
        {"8 m/z - Rep2", Color.fromHex("#21918c")},
        {"8 m/z - Rep3", Color.fromHex("#21918c")},
        {"8 m/z - YeastPeppiB", Color.fromHex("#21918c")},
        {"10 m/z - Rep1", Color.fromHex("#35b779")},
        {"10 m/z - Rep2", Color.fromHex("#35b779")},
        {"10 m/z - Rep3", Color.fromHex("#35b779")},
        {"10 m/z - YeastPeppiB", Color.fromHex("#35b779")},
        {"15 m/z - Rep1", Color.fromHex("#7ad151")},
        {"15 m/z - Rep2", Color.fromHex("#7ad151")},
        {"15 m/z - Rep3", Color.fromHex("#7ad151")},
        {"15 m/z - YeastPeppiB", Color.fromHex("#7ad151")},
        {"20 m/z - Rep1", Color.fromHex("#fde725")},
        {"20 m/z - Rep2", Color.fromHex("#fde725")},
        {"20 m/z - Rep3", Color.fromHex("#fde725")},
        {"20 m/z - YeastPeppiB", Color.fromHex("#fde725")},


        // Gradient Development
        {"MeOH", Color.fromKeyword(ColorKeyword.Purple)},
        {"ACN", Color.fromKeyword(ColorKeyword.FireBrick)},

        // Radical Fragmentation
        {"Peptide", Color.fromKeyword(ColorKeyword.LightCoral)},
        {"Peptidoform", Color.fromKeyword(ColorKeyword.Crimson)},
        {"Proteoform", Color.fromKeyword(ColorKeyword.CornflowerBlue)},
        {"Protein", Color.fromKeyword(ColorKeyword.Navy)},
    };

    private static Dictionary<string, string> FileNameConversionDictionary = new()
    {
        // Bottom Up Mann-11
        { "20100604_Velos1_TaGe_SA_A549_1", "A549_1_1" },
        { "20100604_Velos1_TaGe_SA_A549_2", "A549_1_2" },
        { "20100604_Velos1_TaGe_SA_A549_3", "A549_1_3" },
        { "20100604_Velos1_TaGe_SA_A549_4", "A549_1_4" },
        { "20100604_Velos1_TaGe_SA_A549_5", "A549_1_5" },
        { "20100604_Velos1_TaGe_SA_A549_6", "A549_1_6" },
        { "20100721_Velos1_TaGe_SA_A549_01", "A549_2_1" },
        { "20100721_Velos1_TaGe_SA_A549_02", "A549_2_2" },
        { "20100721_Velos1_TaGe_SA_A549_03", "A549_2_3" },
        { "20100721_Velos1_TaGe_SA_A549_04", "A549_2_4" },
        { "20100721_Velos1_TaGe_SA_A549_05", "A549_2_5" },
        { "20100721_Velos1_TaGe_SA_A549_06", "A549_2_6" },
        { "20101215_Velos1_TaGe_SA_A549_01", "A549_3_1" },
        { "20101215_Velos1_TaGe_SA_A549_02", "A549_3_2" },
        { "20101215_Velos1_TaGe_SA_A549_03", "A549_3_3" },
        { "20101215_Velos1_TaGe_SA_A549_04", "A549_3_4" },
        { "20101215_Velos1_TaGe_SA_A549_05", "A549_3_5" },
        { "20101215_Velos1_TaGe_SA_A549_06", "A549_3_6" },
        { "20100609_Velos1_TaGe_SA_GAMG_1", "GAMG_1_1" },
        { "20100609_Velos1_TaGe_SA_GAMG_2", "GAMG_1_2" },
        { "20100609_Velos1_TaGe_SA_GAMG_3", "GAMG_1_3" },
        { "20100609_Velos1_TaGe_SA_GAMG_4", "GAMG_1_4" },
        { "20100609_Velos1_TaGe_SA_GAMG_5", "GAMG_1_5" },
        { "20100609_Velos1_TaGe_SA_GAMG_6", "GAMG_1_6" },
        { "20100723_Velos1_TaGe_SA_Gamg_1", "GAMG_2_1" },
        { "20100723_Velos1_TaGe_SA_Gamg_2", "GAMG_2_2" },
        { "20100723_Velos1_TaGe_SA_Gamg_3", "GAMG_2_3" },
        { "20100723_Velos1_TaGe_SA_Gamg_4", "GAMG_2_4" },
        { "20100723_Velos1_TaGe_SA_Gamg_5", "GAMG_2_5" },
        { "20100723_Velos1_TaGe_SA_Gamg_6", "GAMG_2_6" },
        { "20101227_Velos1_TaGe_SA_GAMG1", "GAMG_3_1" },
        { "20101227_Velos1_TaGe_SA_GAMG_101230100451", "GAMG_3_1" },
        { "20101227_Velos1_TaGe_SA_GAMG2_101229143203", "GAMG_3_2" },
        { "20101227_Velos1_TaGe_SA_GAMG2", "GAMG_3_2" },
        { "20101227_Velos1_TaGe_SA_GAMG3", "GAMG_3_3" },
        { "20101227_Velos1_TaGe_SA_GAMG4", "GAMG_3_4" },
        { "20101227_Velos1_TaGe_SA_GAMG5", "GAMG_3_5" },
        { "20101227_Velos1_TaGe_SA_GAMG6", "GAMG_3_6" },
        { "20100609_Velos1_TaGe_SA_Hek293_01", "HEK293_1_1" },
        { "20100609_Velos1_TaGe_SA_Hek293_02", "HEK293_1_2" },
        { "20100609_Velos1_TaGe_SA_Hek293_03", "HEK293_1_3" },
        { "20100609_Velos1_TaGe_SA_Hek293_04", "HEK293_1_4" },
        { "20100609_Velos1_TaGe_SA_Hek293_05", "HEK293_1_5" },
        { "20100609_Velos1_TaGe_SA_Hek293_06", "HEK293_1_6" },
        { "20100609_Velos1_TaGe_SA_293_1", "HEK293_1_1" },
        { "20100609_Velos1_TaGe_SA_293_2", "HEK293_1_2" },
        { "20100609_Velos1_TaGe_SA_293_3", "HEK293_1_3" },
        { "20100609_Velos1_TaGe_SA_293_4", "HEK293_1_4" },
        { "20100609_Velos1_TaGe_SA_293_5", "HEK293_1_5" },
        { "20100609_Velos1_TaGe_SA_293_6", "HEK293_1_6" },
        { "20101227_Velos1_TaGe_SA_HEK293_01", "HEK293_2_1" },
        { "20101227_Velos1_TaGe_SA_HEK293_02", "HEK293_2_2" },
        { "20101227_Velos1_TaGe_SA_HEK293_03", "HEK293_2_3" },
        { "20101227_Velos1_TaGe_SA_HEK293_04", "HEK293_2_4" },
        { "20101227_Velos1_TaGe_SA_HEK293_05", "HEK293_2_5" },
        { "20101227_Velos1_TaGe_SA_HEK293_06", "HEK293_2_6" },
        { "20100723_Velos1_TaGe_SA_Hek293_01", "HEK293_3_1" },
        { "20100723_Velos1_TaGe_SA_Hek293_02", "HEK293_3_2" },
        { "20100723_Velos1_TaGe_SA_Hek293_03", "HEK293_3_3" },
        { "20100723_Velos1_TaGe_SA_Hek293_04", "HEK293_3_4" },
        { "20100723_Velos1_TaGe_SA_Hek293_05", "HEK293_3_5" },
        { "20100723_Velos1_TaGe_SA_Hek293_06", "HEK293_3_6" },
        { "20100611_Velos1_TaGe_SA_Hela_1", "Hela_1_1" },
        { "20100611_Velos1_TaGe_SA_Hela_2", "Hela_1_2" },
        { "20100611_Velos1_TaGe_SA_Hela_3", "Hela_1_3" },
        { "20100611_Velos1_TaGe_SA_Hela_4", "Hela_1_4" },
        { "20100611_Velos1_TaGe_SA_Hela_5", "Hela_1_5" },
        { "20100611_Velos1_TaGe_SA_Hela_6", "Hela_1_6" },
        { "20100726_Velos1_TaGe_SA_HeLa_3", "Hela_2_1" },
        { "20100726_Velos1_TaGe_SA_HeLa_4", "Hela_2_2" },
        { "20100726_Velos1_TaGe_SA_HeLa_5", "Hela_2_3" },
        { "20100726_Velos1_TaGe_SA_HeLa_6", "Hela_2_4" },
        { "20100726_Velos1_TaGe_SA_HeLa_1", "Hela_2_5" },
        { "20100726_Velos1_TaGe_SA_HeLa_2", "Hela_2_6" },
        { "20101224_Velos1_TaGe_SA_HeLa_05", "Hela_3_1" },
        { "20101224_Velos1_TaGe_SA_HeLa_06", "Hela_3_2" },
        { "20101224_Velos1_TaGe_SA_HeLa_01", "Hela_3_3" },
        { "20101224_Velos1_TaGe_SA_HeLa_02", "Hela_3_4" },
        { "20101224_Velos1_TaGe_SA_HeLa_03", "Hela_3_5" },
        { "20101224_Velos1_TaGe_SA_HeLa_04", "Hela_3_6" },
        { "20100726_Velos1_TaGe_SA_HepG2_1", "HepG2_2_1" },
        { "20100726_Velos1_TaGe_SA_HepG2_2", "HepG2_2_2" },
        { "20100726_Velos1_TaGe_SA_HepG2_3", "HepG2_2_3" },
        { "20100726_Velos1_TaGe_SA_HepG2_4", "HepG2_2_4" },
        { "20100726_Velos1_TaGe_SA_HepG2_5", "HepG2_2_5" },
        { "20100726_Velos1_TaGe_SA_HepG2_6", "HepG2_2_6" },
        { "20101224_Velos1_TaGe_SA_HepG2_1", "HepG2_3_1" },
        { "20101224_Velos1_TaGe_SA_HepG2_2", "HepG2_3_2" },
        { "20101224_Velos1_TaGe_SA_HepG2_3", "HepG2_3_3" },
        { "20101224_Velos1_TaGe_SA_HepG2_4", "HepG2_3_4" },
        { "20101224_Velos1_TaGe_SA_HepG2_5", "HepG2_3_5" },
        { "20101224_Velos1_TaGe_SA_HepG2_6", "HepG2_3_6" },
        { "20100611_Velos1_TaGe_SA_HepG2_1", "HepG2_1_1" },
        { "20100611_Velos1_TaGe_SA_HepG2_2", "HepG2_1_2" },
        { "20100611_Velos1_TaGe_SA_HepG2_3", "HepG2_1_3" },
        { "20100611_Velos1_TaGe_SA_HepG2_4", "HepG2_1_4" },
        { "20100611_Velos1_TaGe_SA_HepG2_5", "HepG2_1_5" },
        { "20100611_Velos1_TaGe_SA_HepG2_6", "HepG2_1_6" },
        { "20100614_Velos1_TaGe_SA_Jurkat_1", "Jurkat_1_1" },
        { "20100614_Velos1_TaGe_SA_Jurkat_2", "Jurkat_1_2" },
        { "20100614_Velos1_TaGe_SA_Jurkat_3", "Jurkat_1_3" },
        { "20100614_Velos1_TaGe_SA_Jurkat_4", "Jurkat_1_4" },
        { "20100614_Velos1_TaGe_SA_Jurkat_5", "Jurkat_1_5" },
        { "20100614_Velos1_TaGe_SA_Jurkat_6", "Jurkat_1_6" },
        { "20100730_Velos1_TaGe_SA_Jurkat_01", "Jurkat_2_1" },
        { "20100730_Velos1_TaGe_SA_Jurkat_02", "Jurkat_2_2" },
        { "20100730_Velos1_TaGe_SA_Jurkat_03", "Jurkat_2_3" },
        { "20100730_Velos1_TaGe_SA_Jurkat_04", "Jurkat_2_4" },
        { "20100730_Velos1_TaGe_SA_Jurkat_05", "Jurkat_2_5" },
        { "20100730_Velos1_TaGe_SA_Jurkat_06_100731121305", "Jurkat_2_6" },
        { "20101230_Velos1_TaGe_SA_Jurkat1", "Jurkat_3_1" },
        { "20101230_Velos1_TaGe_SA_Jurkat2", "Jurkat_3_2" },
        { "20101230_Velos1_TaGe_SA_Jurkat3", "Jurkat_3_3" },
        { "20101230_Velos1_TaGe_SA_Jurkat4", "Jurkat_3_4" },
        { "20101230_Velos1_TaGe_SA_Jurkat5", "Jurkat_3_5" },
        { "20101230_Velos1_TaGe_SA_Jurkat6", "Jurkat_3_6" },
        { "20100614_Velos1_TaGe_SA_K562_1", "K562_1_1" },
        { "20100614_Velos1_TaGe_SA_K562_2", "K562_1_2" },
        { "20100614_Velos1_TaGe_SA_K562_3", "K562_1_3" },
        { "20100614_Velos1_TaGe_SA_K562_4", "K562_1_4" },
        { "20100614_Velos1_TaGe_SA_K562_5", "K562_1_5" },
        { "20100614_Velos1_TaGe_SA_K562_6", "K562_1_6" },
        { "20100730_Velos1_TaGe_SA_K562_1", "K562_2_1" },
        { "20100730_Velos1_TaGe_SA_K562_2", "K562_2_2" },
        { "20100730_Velos1_TaGe_SA_K564_3", "K562_2_3" },
        { "20100730_Velos1_TaGe_SA_K565_4", "K562_2_4" },
        { "20100730_Velos1_TaGe_SA_K565_5", "K562_2_5" },
        { "20100730_Velos1_TaGe_SA_K565_6", "K562_2_6" },
        { "20101222_Velos1_TaGe_SA_K562_01", "K562_3_1" },
        { "20101222_Velos1_TaGe_SA_K562_02", "K562_3_2" },
        { "20101222_Velos1_TaGe_SA_K562_03", "K562_3_3" },
        { "20101222_Velos1_TaGe_SA_K562_04", "K562_3_4" },
        { "20101222_Velos1_TaGe_SA_K562_05", "K562_3_5" },
        { "20101222_Velos1_TaGe_SA_K562_06", "K562_3_6" },
        { "20100618_Velos1_TaGe_SA_LanCap_1", "LanCap_1_1" },
        { "20100618_Velos1_TaGe_SA_LanCap_2", "LanCap_1_2" },
        { "20100618_Velos1_TaGe_SA_LanCap_3", "LanCap_1_3" },
        { "20100618_Velos1_TaGe_SA_LanCap_4", "LanCap_1_4" },
        { "20100618_Velos1_TaGe_SA_LanCap_5", "LanCap_1_5" },
        { "20100618_Velos1_TaGe_SA_LanCap_6", "LanCap_1_6" },
        { "20100719_Velos1_TaGe_SA_LnCap_1", "LanCap_2_1" },
        { "20100719_Velos1_TaGe_SA_LnCap_2", "LanCap_2_2" },
        { "20100719_Velos1_TaGe_SA_LnCap_3", "LanCap_2_3" },
        { "20100719_Velos1_TaGe_SA_LnCap_4", "LanCap_2_4" },
        { "20100719_Velos1_TaGe_SA_LnCap_5", "LanCap_2_5" },
        { "20100719_Velos1_TaGe_SA_LnCap_6", "LanCap_2_6" },
        { "20101210_Velos1_AnWe_SA_LnCap_1", "LanCap_3_1" },
        { "20101210_Velos1_AnWe_SA_LnCap_2", "LanCap_3_2" },
        { "20101210_Velos1_AnWe_SA_LnCap_3", "LanCap_3_3" },
        { "20101210_Velos1_AnWe_SA_LnCap_4", "LanCap_3_4" },
        { "20101210_Velos1_AnWe_SA_LnCap_5", "LanCap_3_5" },
        { "20101210_Velos1_AnWe_SA_LnCap_6", "LanCap_3_6" },
        { "20100616_Velos1_TaGe_SA_MCF7_1", "MCF7_1_1" },
        { "20100616_Velos1_TaGe_SA_MCF7_2", "MCF7_1_2" },
        { "20100616_Velos1_TaGe_SA_MCF7_3", "MCF7_1_3" },
        { "20100616_Velos1_TaGe_SA_MCF7_4", "MCF7_1_4" },
        { "20100616_Velos1_TaGe_SA_MCF7_5", "MCF7_1_5" },
        { "20100616_Velos1_TaGe_SA_MCF7_6", "MCF7_1_6" },
        { "20100719_Velos1_TaGe_SA_MCF7_01", "MCF7_2_1" },
        { "20100719_Velos1_TaGe_SA_MCF7_02", "MCF7_2_2" },
        { "20100719_Velos1_TaGe_SA_MCF7_03", "MCF7_2_3" },
        { "20100719_Velos1_TaGe_SA_MCF7_04", "MCF7_2_4" },
        { "20100719_Velos1_TaGe_SA_MCF7_05", "MCF7_2_5" },
        { "20100719_Velos1_TaGe_SA_MCF7_06", "MCF7_2_6" },
        { "20101210_Velos1_AnWe_SA_MCF7_1", "MCF7_3_1" },
        { "20101210_Velos1_AnWe_SA_MCF7_2", "MCF7_3_2" },
        { "20101210_Velos1_AnWe_SA_MCF7_3", "MCF7_3_3" },
        { "20101210_Velos1_AnWe_SA_MCF7_4", "MCF7_3_4" },
        { "20101210_Velos1_AnWe_SA_MCF7_5", "MCF7_3_5" },
        { "20101210_Velos1_AnWe_SA_MCF7_6", "MCF7_3_6" },
        { "20100616_Velos1_TaGe_SA_RKO_1", "RKO_1_1" },
        { "20100616_Velos1_TaGe_SA_RKO_2", "RKO_1_2" },
        { "20100616_Velos1_TaGe_SA_RKO_3", "RKO_1_3" },
        { "20100616_Velos1_TaGe_SA_RKO_4", "RKO_1_4" },
        { "20100616_Velos1_TaGe_SA_RKO_5", "RKO_1_5" },
        { "20100616_Velos1_TaGe_SA_RKO_6", "RKO_1_6" },
        { "20100801_Velos1_TaGe_SA_RKO_01", "RKO_2_1" },
        { "20100801_Velos1_TaGe_SA_RKO_02", "RKO_2_2" },
        { "20100801_Velos1_TaGe_SA_RKO_03", "RKO_2_3" },
        { "20100801_Velos1_TaGe_SA_RKO_04", "RKO_2_4" },
        { "20100805_Velos1_TaGe_SA_RKO_05", "RKO_2_5" },
        { "20100805_Velos1_TaGe_SA_RKO_06", "RKO_2_6" },
        { "20101223_Velos1_TaGe_SA_RKO_01", "RKO_3_1" },
        { "20101223_Velos1_TaGe_SA_RKO_02", "RKO_3_2" },
        { "20101223_Velos1_TaGe_SA_RKO_03", "RKO_3_3" },
        { "20101223_Velos1_TaGe_SA_RKO_04", "RKO_3_4" },
        { "20101223_Velos1_TaGe_SA_RKO_05", "RKO_3_5" },
        { "20101223_Velos1_TaGe_SA_RKO_06", "RKO_3_6" },
        { "20100618_Velos1_TaGe_SA_U2OS_1", "U2OS_1_1" },
        { "20100618_Velos1_TaGe_SA_U2OS_2", "U2OS_1_2" },
        { "20100618_Velos1_TaGe_SA_U2OS_3", "U2OS_1_3" },
        { "20100618_Velos1_TaGe_SA_U2OS_4", "U2OS_1_4" },
        { "20100618_Velos1_TaGe_SA_U2OS_5", "U2OS_1_5" },
        { "20100618_Velos1_TaGe_SA_U2OS_6", "U2OS_1_6" },
        { "20100721_Velos1_TaGe_SA_U2OS_1", "U2OS_2_1" },
        { "20100721_Velos1_TaGe_SA_U2OS_2", "U2OS_2_2" },
        { "20100721_Velos1_TaGe_SA_U2OS_3", "U2OS_2_3" },
        { "20100721_Velos1_TaGe_SA_U2OS_4", "U2OS_2_4" },
        { "20100721_Velos1_TaGe_SA_U2OS_5", "U2OS_2_5" },
        { "20100721_Velos1_TaGe_SA_U2OS_6", "U2OS_2_6" },
        { "20101210_Velos1_AnWe_SA_U2OS_1", "U2OS_3_1" },
        { "20101210_Velos1_AnWe_SA_U2OS_2", "U2OS_3_2" },
        { "20101210_Velos1_AnWe_SA_U2OS_3", "U2OS_3_3" },
        { "20101210_Velos1_AnWe_SA_U2OS_4", "U2OS_3_4" },
        { "20101210_Velos1_AnWe_SA_U2OS_5", "U2OS_3_5" },
        { "20101210_Velos1_AnWe_SA_U2OS_6", "U2OS_3_6" },

        // Top Down Jurkat
        {"02-17-20_jurkat_td_rep1_fract1","1_01"},
        {"02-17-20_jurkat_td_rep1_fract2","1_02"},
        {"02-17-20_jurkat_td_rep1_fract3","1_03"},
        {"02-17-20_jurkat_td_rep1_fract4","1_04"},
        {"02-17-20_jurkat_td_rep2_fract1","2_01"},
        {"02-17-20_jurkat_td_rep2_fract2","2_02"},
        {"02-17-20_jurkat_td_rep2_fract3","2_03"},
        {"02-17-20_jurkat_td_rep2_fract4","2_04"},
        {"02-18-20_jurkat_td_rep1_fract10","1_10"},
        {"02-18-20_jurkat_td_rep1_fract5","1_05"},
        {"02-18-20_jurkat_td_rep1_fract6","1_06"},
        {"02-18-20_jurkat_td_rep1_fract7","1_07"},
        {"02-18-20_jurkat_td_rep1_fract8","1_08"},
        {"02-18-20_jurkat_td_rep1_fract9","1_09"},
        {"02-18-20_jurkat_td_rep2_fract10","2_10"},
        {"02-18-20_jurkat_td_rep2_fract5","2_05"},
        {"02-18-20_jurkat_td_rep2_fract6","2_06"},
        {"02-18-20_jurkat_td_rep2_fract7","2_07"},
        {"02-18-20_jurkat_td_rep2_fract8","2_08"},
        {"02-18-20_jurkat_td_rep2_fract9","2_09"},
        {"Ecoli_SEC1_F1","1_01"},
        {"Ecoli_SEC1_F2","1_02"},
        {"Ecoli_SEC1_F3","1_03"},
        {"Ecoli_SEC1_F4","1_04"},
        {"Ecoli_SEC3_F1","3_01"},
        {"Ecoli_SEC3_F10","3_10"},
        {"Ecoli_SEC3_F11","3_11"},
        {"Ecoli_SEC3_F12","3_12"},
        {"Ecoli_SEC3_F13","3_13"},
        {"Ecoli_SEC3_F2","3_02"},
        {"Ecoli_SEC3_F3","3_03"},
        {"Ecoli_SEC3_F4","3_04"},
        {"Ecoli_SEC3_F5","3_05"},
        {"Ecoli_SEC3_F6","3_06"},
        {"Ecoli_SEC3_F7","3_07"},
        {"Ecoli_SEC3_F8","3_08"},
        {"Ecoli_SEC3_F9","3_09"},
        {"Ecoli_SEC4_F1","4_01"},
        {"Ecoli_SEC4_F10","4_10"},
        {"Ecoli_SEC4_F11","4_11"},
        {"Ecoli_SEC4_F12","4_12"},
        {"Ecoli_SEC4_F2","4_02"},
        {"Ecoli_SEC4_F3","4_03"},
        {"Ecoli_SEC4_F4","4_04"},
        {"Ecoli_SEC4_F5","4_05"},
        {"Ecoli_SEC4_F6","4_06"},
        {"Ecoli_SEC4_F7","4_07"},
        {"Ecoli_SEC4_F8","4_08"},
        {"Ecoli_SEC4_F9","4_09"},
        {"Ecoli_SEC5_F1","5_01"},
        {"Ecoli_SEC5_F10","5_10"},
        {"Ecoli_SEC5_F11","5_11"},
        {"Ecoli_SEC5_F12","5_12"},
        {"Ecoli_SEC5_F13","5_13"},
        {"Ecoli_SEC5_F14","5_14"},
        {"Ecoli_SEC5_F2","5_02"},
        {"Ecoli_SEC5_F3","5_03"},
        {"Ecoli_SEC5_F4","5_04"},
        {"Ecoli_SEC5_F5","5_05"},
        {"Ecoli_SEC5_F6","5_06"},
        {"Ecoli_SEC5_F7","5_07"},
        {"Ecoli_SEC5_F8","5_08"},
        {"Ecoli_SEC5_F9","5_09"},


        // Jenkins
        {"04-30-13_CAST_Frac4_6uL", "Frac4"},
        {"04-30-13_CAST_Frac5_4uL", "Frac5"},


        // Isolation With
        {"05-11-17_YL_1-2iso_control", "1-25 Iso"},
        {"05-11-17_YL_2-5iso", "2-5 Iso"},
        {"05-11-17_YL_5iso", "5 Iso"},
        {"05-11-17_YL_10iso", "10 Iso"},
        {"05-11-17_YL_15iso", "15 Iso"},
        {"05-11-17_YL_20iso", "20 Iso"},
        {"05-11-17_YL_25iso", "25 Iso"},
        {"1.25 Iso", " 1.25 Iso"},
        {"2.5 Iso", " 2.5 Iso"},
        {"5 Iso", " 5 Iso"},


    };

    private static Dictionary<string, string> ConditionNameConversionDictionary = new()
    {

        // MM General
        { "MetaMorpheusWithChimeras_105_ChimericLibrary_Rep1", "MetaMorpheus⠀" },
        { "MetaMorpheusWithChimeras_105_ChimericLibrary_Rep2", "MetaMorpheus⠀" },
        { "MetaMorpheusWithChimeras_105_ChimericLibrary_Rep3", "MetaMorpheus⠀" },
        { "MetaMorpheusNoChimeras_105_NonChimericLibrary_Rep1", "MetaMorpheus No Chimeras" },
        { "MetaMorpheusNoChimeras_105_NonChimericLibrary_Rep2", "MetaMorpheus No Chimeras" },
        { "MetaMorpheusNoChimeras_105_NonChimericLibrary_Rep3", "MetaMorpheus No Chimeras" },
        { "MetaMorpheusWithChimeras_105_NonChimericLibrary_Rep1", "MetaMorpheus Chimeras (Non-chimeric library)" },
        { "MetaMorpheusWithChimeras_105_NonChimericLibrary_Rep2", "MetaMorpheus Chimeras (Non-chimeric library)" },
        { "MetaMorpheusWithChimeras_105_NonChimericLibrary_Rep3", "MetaMorpheus Chimeras (Non-chimeric library)" },
        { "MetaMorpheusWithChimeras_106_ChimericLibrary_Rep1", "MetaMorpheus\u2800" },
        { "MetaMorpheusWithChimeras_106_ChimericLibrary_Rep2", "MetaMorpheus\u2800" },
        { "MetaMorpheusWithChimeras_106_ChimericLibrary_Rep3", "MetaMorpheus\u2800" },
        { "MetaMorpheusNoChimeras_106_NonChimericLibrary_Rep1", "MetaMorpheus No Chimeras" },
        { "MetaMorpheusNoChimeras_106_NonChimericLibrary_Rep2", "MetaMorpheus No Chimeras" },
        { "MetaMorpheusNoChimeras_106_NonChimericLibrary_Rep3", "MetaMorpheus No Chimeras" },
        { "MetaMorpheusWithChimeras_106_NonChimericLibrary_Rep1", "MetaMorpheus Chimeras (Non-chimeric library)" },
        { "MetaMorpheusWithChimeras_106_NonChimericLibrary_Rep2", "MetaMorpheus Chimeras (Non-chimeric library)" },
        { "MetaMorpheusWithChimeras_106_NonChimericLibrary_Rep3", "MetaMorpheus Chimeras (Non-chimeric library)" },

        // Bottom up
        { "MetaMorpheusWithLibrary", "MetaMorpheus⠀" }, // old
        { "MetaMorpheusNoChimerasWithLibrary", "MetaMorpheus No Chimeras" }, // old
        { "MetaMorpheusFraggerEquivalent_IndividualFilesFraggerEquivalentWithChimeras", "Reduced MetaMorpheus⠀" },
        { "MetaMorpheus_ExternalComparison_Chimeras", "MetaMorpheus⠀" },
        { "MetaMorpheusFraggerEquivalent_IndividualFilesFraggerEquivalentNoChimeras", "MetaMorpheus No Chimeras" },
        { "MetaMorpheus_ExternalComparison_NoChimeras", "MetaMorpheus No Chimeras" },
        
        { "ReviewdDatabaseNoPhospho_MsFragger", "No P MsFragger" },
        { "ReviewdDatabaseNoPhospho_MsFraggerDDA", "No P MsFragger" },
        { "MsFraggerDDA", "MsFraggerDDA" },
        { "ReviewdDatabaseNoPhospho_MsFraggerDDA+", "No P MsFraggerDDA+" },
        { "ReviewdDatabase_MsFraggerDDA+", "MsFraggerDDA+" },
        { "MsFraggerDDA+", "OG MsFraggerDDA+" },
        { "Chimerys", "\u2800Chimerys"},

        // Top Down
        { "MetaMorpheus", "MetaMorpheus\u2800" },
        { "MetaMorpheus_Rep2_WithLibrary", "MetaMorpheus\u2800" }, // temp until actual final is finished
        { "MetaMorpheus_Rep2_WithLibrary_NewPEP_NoNorm", "MetaMorpheus\u2800" }, // temp until actual final is finished
        { "MetaMorpheus_Chimeras", "MetaMorpheus\u2800" }, // temp until actual final is finished
        { "MetaMorpheus_NewPEP_NoNormNoMult", "MetaMorpheus\u2800" }, // temp until actual final is finished
        { "MetaMorpheusNoChimeras", "MetaMorpheus No Chimeras" },
        { "MetaMorpheus_NoChimeras", "MetaMorpheus No Chimeras" },


        { "MsPathFinderTWithModsNoChimeras", "MsPathFinderT  No Chimeras" },
        { "MsPathFinderTWithModsNoChimerasRep2", "MsPathFinderT  No Chimeras" },
        { "MsPathFinderT_NoChimeras", "MsPathFinderT \u28007" },
        { "MsPathFinderTWithMods_7Rep2", "MsPathFinderT \u28007" },
        { "MsPathFinderT_7", "MsPathFinderT \u28007" },
        { "MsPathFinderTWithMods_15", "MsPathFinderT\u280015" },
        { "MsPathFinderTWithMods_15Rep2", "MsPathFinderT\u280015" },
        { "MsPathFinderT_15", "MsPathFinderT\u280015" },

        {"ProsightPDNoChimeras", "ProsightPD  No Chimeras"},
        {"ProsightPD_NoChimeras", "ProsightPD  No Chimeras"},
        {"ProsightPDNoChimeras_Rep2", "ProsightPD  No Chimeras"},
        {"ProsightPDChimeras", "ProsightPD \u28007 Chimeras"},
        {"ProsightPDChimeras_Rep2_7_10ppm", "ProsightPD \u28007 Chimeras"},
        {"ProsightPD_7", "ProsightPD \u28007 Chimeras"},
        {"ProsightPDChimeras_7", "ProsightPD \u28007 Chimeras"},
        {"ProsightPDChimeras_15", "ProsightPD\u280015 Chimeras"},
        {"ProsightPdChimeras_Rep2_15_10ppm", "ProsightPD\u280015 Chimeras"},
        {"ProsightPD_15", "ProsightPD\u280015 Chimeras"},
    };

    public static ColorQueue<Color> ColorQueue { get; }

    #endregion

    #region Conversion Methods

    public static IEnumerable<string> ConvertFileNames(this IEnumerable<string> fileNames)
    {
        return fileNames.Select(p => p.Replace("-calib", "").Replace("-averaged", ""))
            .Select(p => FileNameConversionDictionary.ContainsKey(p) ? FileNameConversionDictionary[p] : p);
    }

    public static string ConvertFileName(this string fileName)
    {
        var name = fileName.Replace("-calib", "").Replace("-averaged", "");


        if (FileNameConversionDictionary.TryGetValue(name, out string? value) && value != null)
        {
            return value;
        }
        else if (fileName.Contains("_DDA_Rep") || fileName.Contains("_DDA_YeastPeppiB")) // for my isolation window experiments
        {
            var splits = fileName.Split('_');

            if (splits[2].Length == 1) // if single digit, add space to ensure proper order
                splits[2] = $" {splits[2]}";

            var returnName = $"{splits[2]} m/z - {splits[4]}";
            FileNameConversionDictionary.TryAdd(name, returnName);
            return returnName;
        }
        else
        {
            return name;
        }
    }

    public static IEnumerable<string> ConvertConditionNames(this IEnumerable<string> conditions)
    {
        return conditions.Select(p => ConditionNameConversionDictionary.ContainsKey(p) ? ConditionNameConversionDictionary[p] : p);
    }

    public static string ConvertConditionName(this string condition)
    {
        return ConditionNameConversionDictionary.ContainsKey(condition) ? ConditionNameConversionDictionary[condition] : condition;
    }

    public static Color ConvertConditionToColor(this string condition)
    {
        if (ConditionToColorDictionary.TryGetValue(condition, out var color))
            return color;
        else if (ConditionToColorDictionary.TryGetValue(condition.Trim(), out color))
            return color;
        else
        {
            if (ConditionNameConversionDictionary.ContainsValue(condition))
            {
                var key = ConditionNameConversionDictionary.FirstOrDefault(x => x.Value == condition).Key;
                if (key is null)
                    return Color.fromKeyword(ColorKeyword.Black);
                if (ConditionToColorDictionary.TryGetValue(key, out color))
                    return color;
            }
            else
            {
                ConditionToColorDictionary.Add(condition, ColorQueue.Dequeue());
                return ConditionToColorDictionary[condition];
            }
        }

        return Color.fromKeyword(ColorKeyword.Black);
    }

    public static IEnumerable<Color> ConvertConditionsToColors(this IEnumerable<string> conditions)
    {
        return conditions.Select(p => ConditionToColorDictionary.ContainsKey(p) ? ConditionToColorDictionary[p] : Color.fromKeyword(ColorKeyword.Black));
    }

    #endregion

}