using MzLibUtil;

namespace GradientDevelopment;

public enum ExperimentalGroup
{
    OldRuns,
    InitialDevelopment,
    DifferentialMethylFluc,
    Gradient14,
    DifferentialMethylFlucRound2,
    DifferentialMethylFlucRound3,
    DifferentialMethylFlucRound3_0,
    DifferentialMethylFlucRound3_25,
    DifferentialMethylFlucRound3_5,
    DifferentialMethylFlucRound3_75,
    DifferentialMethylFlucRound3_1,
}

public static class StoredInformation
{
    internal static string GradientDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Gradients");

    // Gradient Development
    internal static string GradientDevelopmentDataDirectory = @"B:\Users\Nic\RNA\FLuc\GradientDevelopment";
    internal static string FluxMethDataDirectory = @"B:\Users\Nic\RNA\FLuc\FLuc Methylation Experiment\m5C_transcription";
    internal static string GradientDevelopmentBigSearch = Path.Combine(GradientDevelopmentDataDirectory, @"Searches\Round8Search\Task1-RnaSearchTask\AllOSMs.osmtsv");

    // DifferentialMethylFluc
    internal static string MixedMethylDirectory = @"B:\Users\Nic\RNA\FLuc\250220_FlucDifferentialMethylations";
    internal static string DifferentialMethylBigSearch = Path.Combine(MixedMethylDirectory, @"Searches\AllFiles_MethOnlyGPTMD_VariableMeth\Task2-SearchVariableMeth\AllOSMs.osmtsv");

    internal static string MixedMethylDirectory2 = @"B:\Users\Nic\RNA\FLuc\250313_FLucDifferentialMethylations_More";
    internal static string DifferentialMethylBigSearch2 = Path.Combine(MixedMethylDirectory2, @"Searches\SearcheAllFiles_MethOnlyGPTMD_VariableMeth_AdductAcceptor\Task2-RnaSearchTask\AllOSMs.osmtsv");

    internal static string MixedMethylDirectory3 = @"B:\Users\Nic\RNA\FLuc\250317_FlucDifferentialMethylations";
    //internal static string DifferentialMethylBigSearch3 = Path.Combine(MixedMethylDirectory3, @"Searches\SearchAllFiles_MethMetalGPTMD_VariableMeth\Task2-RnaSearchTask\AllOSMs.osmtsv");
    internal static string DifferentialMethylBigSearch3 = Path.Combine(MixedMethylDirectory3, @"Searches\SearchAllFiles_MetalGPTMD_VariableMeth\Task2-RnaSearchTask\AllOSMs.osmtsv");
    internal static string DifferentialMethylBigSearch3_0 = Path.Combine(MixedMethylDirectory3, @"Searches\0_MethMetalGPTMD_VariableMeth\Task2-RnaSearchTask\AllOSMs.osmtsv");
    internal static string DifferentialMethylBigSearch3_25 = Path.Combine(MixedMethylDirectory3, @"Searches\0-25_MethMetalGPTMD_VariableMeth\Task2-RnaSearchTask\AllOSMs.osmtsv");
    internal static string DifferentialMethylBigSearch3_5 = Path.Combine(MixedMethylDirectory3, @"Searches\0-5_MethMetalGPTMD_VariableMeth\Task2-RnaSearchTask\AllOSMs.osmtsv");
    internal static string DifferentialMethylBigSearch3_75 = Path.Combine(MixedMethylDirectory3, @"Searches\0-75_MethMetalGPTMD_VariableMeth\Task2-RnaSearchTask\AllOSMs.osmtsv");
    internal static string DifferentialMethylBigSearch3_1 = Path.Combine(MixedMethylDirectory3, @"Searches\1_MethMetalGPTMD_VariableMeth\Task2-RnaSearchTask\AllOSMs.osmtsv");

    // Gradient14 only
    internal static string Gradient14Search = Path.Combine(MixedMethylDirectory, @"Searches\AllGrad14_MethOnlyGptmd_VariableMeth\Task2-SearchVariableMeth\AllOSMs.osmtsv");

    // Gradient13 Only

    private static List<RunInformation>? _runInformationList;
    public static List<RunInformation> RunInformationList => _runInformationList ??= rawInputDictionary.SelectMany(p => p.Value).ToList();


    private static Dictionary<ExperimentalGroup, ExperimentalBatch>? _experimentalBatches = null!;

    public static Dictionary<ExperimentalGroup, ExperimentalBatch> ExperimentalBatches => _experimentalBatches ??=
        rawInputDictionary.ToDictionary(p => p.Key,
            p => new ExperimentalBatch(p.Key.ToString(),
                Path.Combine(p.Value.First().ParentDirectory, "ProcessedResults", p.Key.ToString()), p.Value));
    
    
    
    private static Dictionary<ExperimentalGroup, List<RunInformation>> rawInputDictionary = new()
    {
        { ExperimentalGroup.OldRuns, [
                new(Path.Combine(FluxMethDataDirectory, "241025_FLuc_dig_A.raw"),
                    Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD", "241025_FLuc_dig_A_ms1.feature"),
                    "ACN", new DoubleRange(0, 85)),

                new(Path.Combine(FluxMethDataDirectory, "241025_FLuc_dig_B.raw"),
                    Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD", "241025_FLuc_dig_B_ms1.feature"),
                    "ACN", new DoubleRange(0, 85)),

                new(Path.Combine(FluxMethDataDirectory, "241025_FLuc_dig_C.raw"),
                    Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD", "241025_FLuc_dig_C_ms1.feature"),
                    "ACN", new DoubleRange(0, 85))

            ]
        },

        { ExperimentalGroup.InitialDevelopment, [
                new(Path.Combine(GradientDevelopmentDataDirectory, "241031_FLuc_15digAAwash_80ACNB_std-grad.raw"),
                    Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241031_FLuc_15digAAwash_80ACNB_std-grad_ms1.feature"), "ACN",
                    new DoubleRange(0, 101)),

                new(Path.Combine(GradientDevelopmentDataDirectory, "241031_FLuc_15dig_AAwash_80Met_std-grad1.raw"),
                    Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241031_FLuc_15dig_AAwash_80Met_std-grad1_ms1.feature"), "MeOH",
                    new DoubleRange(12, 125)),

                new(Path.Combine(GradientDevelopmentDataDirectory, "241031_FLuc_90dig_AAwash_80Met_std-grad2.raw"),
                    Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241031_FLuc_90dig_AAwash_80Met_std-grad2_ms1.feature"), "MeOH",
                    new DoubleRange(12, 125)),

                // Day 1

                new(Path.Combine(GradientDevelopmentDataDirectory, "241031_FLuc_15dig_AAwash_80Met_Pfizer.raw"),
                    Path.Combine(GradientDirectory, "Pfizer1.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241031_FLuc_15dig_AAwash_80Met_Pfizer_ms1.feature"), "MeOH",
                    new DoubleRange(15, 260)),

                new(
                    Path.Combine(GradientDevelopmentDataDirectory,
                        "241101_FLuc_88dig_AAwash_80Met_185MinPfizer.raw"),
                    Path.Combine(GradientDirectory, "Pfizer2.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241101_FLuc_88dig_AAwash_80Met_185MinPfizer_ms1.feature"), "MeOH",
                    new DoubleRange(10, 165)),

                new(
                    Path.Combine(GradientDevelopmentDataDirectory,
                        "241101_FLuc_88dig_AAwash_80Met_205MinPfizer.raw"),
                    Path.Combine(GradientDirectory, "Pfizer3.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241101_FLuc_88dig_AAwash_80Met_205MinPfizer_ms1.feature"), "MeOH",
                    new DoubleRange(8, 200)),

                new(
                    Path.Combine(GradientDevelopmentDataDirectory,
                        "241101_FLuc_88dig_AAwash_80Met_290MinPfizer_SourceVoltageToggle.raw"),
                    Path.Combine(GradientDirectory, "Pfizer1.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241101_FLuc_88dig_AAwash_80Met_290MinPfizer_SourceVoltageToggle_ms1.feature"), "MeOH",
                    new DoubleRange(15, 260)),

                new(
                    Path.Combine(GradientDevelopmentDataDirectory,
                        "241101_FLuc_88dig_AAwash_80Met_70MinPfizer.raw"),
                    Path.Combine(GradientDirectory, "Pfizer4.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241101_FLuc_88dig_AAwash_80Met_70MinPfizer_ms1.feature"), "MeOH",
                    new DoubleRange(25, 70)),

                // Round 6

                new(
                    Path.Combine(GradientDevelopmentDataDirectory,
                        "241104_FLuc_15Dig_TEAAandAA_NoSpike_2ug_120MinPfizer5.raw"),
                    Path.Combine(GradientDirectory, "Pfizer5.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241104_FLuc_15Dig_TEAAandAA_NoSpike_2ug_120MinPfizer5_ms1.feature"), "MeOH",
                    new DoubleRange(10, 95)),

                new(
                    Path.Combine(GradientDevelopmentDataDirectory,
                        "241104_FLuc_15Dig_TEAAandAA_Spike_2ug_120MinPfizer5.raw"),
                    Path.Combine(GradientDirectory, "Pfizer5.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241104_FLuc_15Dig_TEAAandAA_Spike_2ug_120MinPfizer5_ms1.feature"), "MeOH",
                    new DoubleRange(10, 100)),

                new(
                    Path.Combine(GradientDevelopmentDataDirectory,
                        "241104_FLuc_15Dig_TEAAandAA_NoSpike_3ug_110MinPfizer6.raw"),
                    Path.Combine(GradientDirectory, "Pfizer6.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241104_FLuc_15Dig_TEAAandAA_NoSpike_3ug_110MinPfizer6_ms1.feature"), "MeOH",
                    new DoubleRange(12, 80)),

                new(
                    Path.Combine(GradientDevelopmentDataDirectory,
                        "241104_FLuc_15Dig_TEAAandAA_Spike_3ug_110MinPfizer6.raw"),
                    Path.Combine(GradientDirectory, "Pfizer6.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241104_FLuc_15Dig_TEAAandAA_Spike_3ug_110MinPfizer6_ms1.feature"), "MeOH",
                    new DoubleRange(10, 80)),

                new(
                    Path.Combine(GradientDevelopmentDataDirectory,
                        "241104_FLuc_15Dig_TEAAandAA_Spike_3ug_110MinPfizer6_2.raw"),
                    Path.Combine(GradientDirectory, "Pfizer6.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241104_FLuc_15Dig_TEAAandAA_Spike_3ug_110MinPfizer6_2_ms1.feature"), "MeOH",
                    new DoubleRange(10, 80)),

                // Round 7

                new(
                    Path.Combine(GradientDevelopmentDataDirectory,
                        "241105_FLuc_45Dig_TEAAandAA_Spike_6ug_120-3Pfizer8.raw"),
                    Path.Combine(GradientDirectory, "Pfizer8.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241105_FLuc_45Dig_TEAAandAA_Spike_6ug_120-3Pfizer8_ms1.feature"), "MeOH",
                    new DoubleRange(10, 100)),

                new(
                    Path.Combine(GradientDevelopmentDataDirectory,
                        "241105_FLuc_45Dig_TEAAandAA_Spike_6ug_120-2Pfizer9.raw"),
                    Path.Combine(GradientDirectory, "Pfizer9.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241105_FLuc_45Dig_TEAAandAA_Spike_6ug_120-2Pfizer9_ms1.feature"), "MeOH",
                    new DoubleRange(10, 100)),

                // Round 8

                new(
                    Path.Combine(GradientDevelopmentDataDirectory,
                        "241105_FLuc_45Dig_TEAAandAA_Spike_6ug_130Pfizer10.raw"),
                    Path.Combine(GradientDirectory, "Pfizer10.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241105_FLuc_45Dig_TEAAandAA_Spike_6ug_130Pfizer10_ms1.feature"), "MeOH",
                    new DoubleRange(30, 110)),

                new(
                    Path.Combine(GradientDevelopmentDataDirectory,
                        "241105_FLuc_45Dig_TEAAandAA_Spike_6ug_210-2Pfizer11.raw"),
                    Path.Combine(GradientDirectory, "Pfizer11.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241105_FLuc_45Dig_TEAAandAA_Spike_6ug_210-2Pfizer11_ms1.feature"), "MeOH",
                    new DoubleRange(10, 165)),

                new(
                    Path.Combine(GradientDevelopmentDataDirectory,
                        "241105_FLuc_45Dig_TEAAandAA_Spike_12ug_290Pfizer1.raw"),
                    Path.Combine(GradientDirectory, "Pfizer1.csv"), GradientDevelopmentBigSearch,
                    Path.Combine(GradientDevelopmentDataDirectory, "TopFD",
                        "241105_FLuc_45Dig_TEAAandAA_Spike_12ug_290Pfizer1_ms1.feature"), "MeOH",
                    new DoubleRange(20, 260))

            ]
        },

        {
            ExperimentalGroup.DifferentialMethylFluc, new List<RunInformation>
            {
                new(Path.Combine(MixedMethylDirectory, "250219_Fluc_Halfm5C_Pfizer12.raw"),
                    Path.Combine(GradientDirectory, "Pfizer12.csv"), DifferentialMethylBigSearch,
                    Path.Combine(MixedMethylDirectory, "TopFD", "250219_fluc_Halfm5C_Pfizer12_ms1.feature"),
                    "MeOH", new DoubleRange(20, 100)),

                new(Path.Combine(MixedMethylDirectory, "250219_Fluc_Halfm5C_PfizerGrad.raw"),
                    Path.Combine(GradientDirectory, "Pfizer1.csv"), DifferentialMethylBigSearch,
                    Path.Combine(MixedMethylDirectory, "TopFD", "250219_Fluc_Halfm5C_PfizerGrad_ms1.feature"),
                    "MeOH", new DoubleRange(0, 270)),

                new(Path.Combine(MixedMethylDirectory, "250219_Fluc_Halfm5C_Pflizer11.raw"),
                    Path.Combine(GradientDirectory, "Pfizer11.csv"), DifferentialMethylBigSearch,
                    Path.Combine(MixedMethylDirectory, "TopFD", "250219_Fluc_Halfm5C_Pflizer11_ms1.feature"),
                    "MeOH", new DoubleRange(0, 170)),

                new(Path.Combine(MixedMethylDirectory, "250219_Fluc_Nom5C_Gradient13.raw"),
                    Path.Combine(GradientDirectory, "Gradient13.csv"), DifferentialMethylBigSearch,
                    Path.Combine(MixedMethylDirectory, "TopFD", "250219_Fluc_Nom5C_Gradient13_ms1.feature"),
                    "MeOH", new DoubleRange(0, 180)),

                new(Path.Combine(MixedMethylDirectory, "250219_Fluc_Allm5C_Gradient13.raw"),
                    Path.Combine(GradientDirectory, "Gradient13.csv"), DifferentialMethylBigSearch,
                    Path.Combine(MixedMethylDirectory, "TopFD", "250219_Fluc_Allm5C_Gradient13_ms1.feature"),
                    "MeOH", null),

                new(Path.Combine(MixedMethylDirectory, "250219_Fluc_Nom5C_Gradient14.raw"),
                    Path.Combine(GradientDirectory, "Gradient14.csv"), DifferentialMethylBigSearch,
                    Path.Combine(MixedMethylDirectory, "TopFD", "250219_Fluc_Nom5C_Gradient14_ms1.feature"),
                    "MeOH", new DoubleRange(0, 105)),

                new(Path.Combine(MixedMethylDirectory, "250219_Fluc_Allm5C_Gradient14.raw"),
                    Path.Combine(GradientDirectory, "Gradient14.csv"), DifferentialMethylBigSearch,
                    Path.Combine(MixedMethylDirectory, "TopFD", "250219_Fluc_Allm5C_Gradient14_ms1.feature"),
                    "MeOH", new DoubleRange(0, 105)),

                new(Path.Combine(MixedMethylDirectory, "250219_Fluc_Mixedm5C_Gradient13.raw"),
                    Path.Combine(GradientDirectory, "Gradient13.csv"), DifferentialMethylBigSearch,
                    Path.Combine(MixedMethylDirectory, "TopFD", "250219_Fluc_Mixedm5C_Gradient13_ms1.feature"),
                    "MeOH", new DoubleRange(0, 180)),

                new(Path.Combine(MixedMethylDirectory, "250219_Fluc_Mixedm5C_Gradient14.raw"),
                    Path.Combine(GradientDirectory, "Gradient14.csv"), DifferentialMethylBigSearch,
                    Path.Combine(MixedMethylDirectory, "TopFD", "250219_Fluc_Mixedm5C_Gradient14_ms1.feature"),
                    "MeOH", new DoubleRange(0, 105)),

                new(Path.Combine(MixedMethylDirectory, "250219_Fluc_Halfm5C_Gradient14.raw"),
                    Path.Combine(GradientDirectory, "Gradient14.csv"), DifferentialMethylBigSearch,
                    Path.Combine(MixedMethylDirectory, "TopFD", "250219_Fluc_Halfm5C_Gradient14_ms1.feature"),
                    "MeOH", new DoubleRange(0, 105))
            }
        },

        {
            ExperimentalGroup.Gradient14, new()
            {
                new(Path.Combine(MixedMethylDirectory, "250219_Fluc_Nom5C_Gradient14.raw"),
                    Path.Combine(GradientDirectory, "Gradient14.csv"), Gradient14Search,
                    Path.Combine(MixedMethylDirectory, "TopFD", "250219_Fluc_Nom5C_Gradient14_ms1.feature"),
                    "MeOH", new DoubleRange(0, 105)),

                new(Path.Combine(MixedMethylDirectory, "250219_Fluc_Allm5C_Gradient14.raw"),
                    Path.Combine(GradientDirectory, "Gradient14.csv"), Gradient14Search,
                    Path.Combine(MixedMethylDirectory, "TopFD", "250219_Fluc_Allm5C_Gradient14_ms1.feature"),
                    "MeOH", new DoubleRange(0, 105)),

                new(Path.Combine(MixedMethylDirectory, "250219_Fluc_Mixedm5C_Gradient14.raw"),
                    Path.Combine(GradientDirectory, "Gradient14.csv"), Gradient14Search,
                    Path.Combine(MixedMethylDirectory, "TopFD", "250219_Fluc_Mixedm5C_Gradient14_ms1.feature"),
                    "MeOH", new DoubleRange(0, 105)),

                new(Path.Combine(MixedMethylDirectory, "250219_Fluc_Halfm5C_Gradient14.raw"),
                    Path.Combine(GradientDirectory, "Gradient14.csv"), Gradient14Search,
                    Path.Combine(MixedMethylDirectory, "TopFD", "250219_Fluc_Halfm5C_Gradient14_ms1.feature"),
                    "MeOH", new DoubleRange(0, 105))
            } },

        {
            ExperimentalGroup.DifferentialMethylFlucRound2, new()
            {
                new(Path.Combine(MixedMethylDirectory2, "250313_RNA_FLuc_0-5Met_T1_Grad14_Rep1.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch2,
                    Path.Combine(MixedMethylDirectory2, "TopFD", "250313_RNA_FLuc_0-5Met_T1_Grad14_Rep1_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory2, "250313_RNA_FLuc_0-25Met_T1_Grad14_Rep1.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch2,
                    Path.Combine(MixedMethylDirectory2, "TopFD", "250313_RNA_FLuc_0-25Met_T1_Grad14_Rep1_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory2, "250313_RNA_FLuc_0-75Met_T1_Grad14_Rep1.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch2,
                    Path.Combine(MixedMethylDirectory2, "TopFD", "250313_RNA_FLuc_0-75Met_T1_Grad14_Rep1_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory2, "250313_RNA_FLuc_1Met_T1_Grad14_Rep1.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch2,
                    Path.Combine(MixedMethylDirectory2, "TopFD", "250313_RNA_FLuc_1Met_T1_Grad14_Rep1_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory2, "250313_RNA_FLuc_0Met_T1_Grad14_Rep1.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch2,
                    Path.Combine(MixedMethylDirectory2, "TopFD", "250313_RNA_FLuc_0Met_T1_Grad14_Rep1_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
            } },
        {
            ExperimentalGroup.DifferentialMethylFlucRound3, new()
            {
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-5Met_T1_Grad14_Rep1.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-5Met_T1_Grad14_Rep1_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-5Met_T1_Grad14_Rep2.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-5Met_T1_Grad14_Rep2_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-5Met_T1_Grad14_Rep3.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-5Met_T1_Grad14_Rep3_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-25Met_T1_Grad14_Rep1.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-25Met_T1_Grad14_Rep1_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-25Met_T1_Grad14_Rep2.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-25Met_T1_Grad14_Rep2_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-25Met_T1_Grad14_Rep3.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-25Met_T1_Grad14_Rep3_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-75Met_T1_Grad14_Rep1.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-75Met_T1_Grad14_Rep1_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-75Met_T1_Grad14_Rep2.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-75Met_T1_Grad14_Rep2_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-75Met_T1_Grad14_Rep3.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-75Met_T1_Grad14_Rep3_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0Met_T1_Grad14_Rep1.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0Met_T1_Grad14_Rep1_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0Met_T1_Grad14_Rep2.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0Met_T1_Grad14_Rep2_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0Met_T1_Grad14_Rep3.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0Met_T1_Grad14_Rep3_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_1Met_T1_Grad14_Rep1.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_1Met_T1_Grad14_Rep1_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_1Met_T1_Grad14_Rep2.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_1Met_T1_Grad14_Rep2_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_1Met_T1_Grad14_Rep3.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_1Met_T1_Grad14_Rep3_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
            }
        },

        {
            ExperimentalGroup.DifferentialMethylFlucRound3_0, new()
            {
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0Met_T1_Grad14_Rep1.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3_0,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0Met_T1_Grad14_Rep1_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0Met_T1_Grad14_Rep2.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3_0,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0Met_T1_Grad14_Rep2_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0Met_T1_Grad14_Rep3.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3_0,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0Met_T1_Grad14_Rep3_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
            }
        },
        {
            ExperimentalGroup.DifferentialMethylFlucRound3_25, new()
            {
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-25Met_T1_Grad14_Rep1.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3_25,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-25Met_T1_Grad14_Rep1_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-25Met_T1_Grad14_Rep2.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3_25,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-25Met_T1_Grad14_Rep2_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-25Met_T1_Grad14_Rep3.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3_25,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-25Met_T1_Grad14_Rep3_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70))
            }
        },
        {
            ExperimentalGroup.DifferentialMethylFlucRound3_5, new()
            {
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-5Met_T1_Grad14_Rep1.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3_5,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-5Met_T1_Grad14_Rep1_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-5Met_T1_Grad14_Rep2.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3_5,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-5Met_T1_Grad14_Rep2_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-5Met_T1_Grad14_Rep3.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3_5,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-5Met_T1_Grad14_Rep3_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),             
            }
        },
        {
            ExperimentalGroup.DifferentialMethylFlucRound3_75, new()
            {
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-75Met_T1_Grad14_Rep1.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3_75,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-75Met_T1_Grad14_Rep1_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-75Met_T1_Grad14_Rep2.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3_75,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-75Met_T1_Grad14_Rep2_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_0-75Met_T1_Grad14_Rep3.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3_75,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_0-75Met_T1_Grad14_Rep3_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
            }
        },
        {
            ExperimentalGroup.DifferentialMethylFlucRound3_1, new()
            {
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_1Met_T1_Grad14_Rep1.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3_1,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_1Met_T1_Grad14_Rep1_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_1Met_T1_Grad14_Rep2.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3_1,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_1Met_T1_Grad14_Rep2_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70)),
                new(Path.Combine(MixedMethylDirectory3, "250315_RNA_FLuc_1Met_T1_Grad14_Rep3.raw"),
                    Path.Combine(GradientDirectory, "Gradient14_2.csv"), DifferentialMethylBigSearch3_1,
                    Path.Combine(MixedMethylDirectory3, "TopFD", "250315_RNA_FLuc_1Met_T1_Grad14_Rep3_ms1.feature"),
                    "MeOH", new DoubleRange(30, 70))
            }
        },
    };
}
