using MzLibUtil;

namespace GradientDevelopment
{
    public static class StoredInformation
    {
        internal static string GradientDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Gradients");
        internal static string GradientDevelopmentDataDirectory = @"B:\Users\Nic\RNA\FLuc\GradientDevelopment";
        internal static string FluxMethDataDirectory = @"B:\Users\Nic\RNA\FLuc\FLuc Methylation Experiment\m5C_transcription";
        internal static string GradientDevelopmentBigSearch = Path.Combine(GradientDevelopmentDataDirectory, @"Round8Search\Task1-RnaSearchTask\AllOSMs.osmtsv");

        public static List<RunInformation> RunInformationList = new List<RunInformation>()
        {
            // old runs
            new(Path.Combine(FluxMethDataDirectory, "241025_FLuc_dig_A.raw"),
                Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch, "ACN"),
            new(Path.Combine(FluxMethDataDirectory, "241025_FLuc_dig_B.raw"),
                Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch, "ACN"),
            new(Path.Combine(FluxMethDataDirectory, "241025_FLuc_dig_C.raw"),
                Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch, "ACN"),

            // Controls
            new(Path.Combine(GradientDevelopmentDataDirectory, "241031_FLuc_15digAAwash_80ACNB_std-grad.raw"),
                Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch, "ACN", new DoubleRange(0, 127)),
            new(Path.Combine(GradientDevelopmentDataDirectory, "241031_FLuc_15dig_AAwash_80Met_std-grad1.raw"),
                Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch, "MeOH", new DoubleRange(0, 127)),
            new(Path.Combine(GradientDevelopmentDataDirectory, "241031_FLuc_90dig_AAwash_80Met_std-grad2.raw"),
                Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch, "MeOH", new DoubleRange(0, 127)),

            // Day 1
            new(Path.Combine(GradientDevelopmentDataDirectory, "241031_FLuc_15dig_AAwash_80Met_Pfizer.raw"),
                Path.Combine(GradientDirectory, "Pfizer1.csv"), GradientDevelopmentBigSearch, "MeOH"),
            new(Path.Combine(GradientDevelopmentDataDirectory, "241101_FLuc_88dig_AAwash_80Met_185MinPfizer.raw"),
                Path.Combine(GradientDirectory, "Pfizer2.csv"), GradientDevelopmentBigSearch, "MeOH"),
            new(Path.Combine(GradientDevelopmentDataDirectory, "241101_FLuc_88dig_AAwash_80Met_205MinPfizer.raw"),
                Path.Combine(GradientDirectory, "Pfizer3.csv"), GradientDevelopmentBigSearch, "MeOH"),
            new(Path.Combine(GradientDevelopmentDataDirectory, "241101_FLuc_88dig_AAwash_80Met_290MinPfizer_SourceVoltageToggle.raw"),
                Path.Combine(GradientDirectory, "Pfizer1.csv"), GradientDevelopmentBigSearch, "MeOH"),
            new(Path.Combine(GradientDevelopmentDataDirectory, "241101_FLuc_88dig_AAwash_80Met_70MinPfizer.raw"),
                Path.Combine(GradientDirectory, "Pfizer4.csv"), GradientDevelopmentBigSearch, "MeOH"),

            // Round 6
            new(Path.Combine(GradientDevelopmentDataDirectory, "241104_FLuc_15Dig_TEAAandAA_NoSpike_2ug_120MinPfizer5.raw"),
                Path.Combine(GradientDirectory, "Pfizer5.csv"), GradientDevelopmentBigSearch, "MeOH"),
            new(Path.Combine(GradientDevelopmentDataDirectory, "241104_FLuc_15Dig_TEAAandAA_Spike_2ug_120MinPfizer5.raw"),
                Path.Combine(GradientDirectory, "Pfizer5.csv"), GradientDevelopmentBigSearch, "MeOH"),
            new(Path.Combine(GradientDevelopmentDataDirectory, "241104_FLuc_15Dig_TEAAandAA_NoSpike_3ug_110MinPfizer6.raw"),
                Path.Combine(GradientDirectory, "Pfizer6.csv"), GradientDevelopmentBigSearch, "MeOH"),
            new(Path.Combine(GradientDevelopmentDataDirectory, "241104_FLuc_15Dig_TEAAandAA_Spike_3ug_110MinPfizer6.raw"),
                Path.Combine(GradientDirectory, "Pfizer6.csv"), GradientDevelopmentBigSearch, "MeOH"),
            new(Path.Combine(GradientDevelopmentDataDirectory, "241104_FLuc_15Dig_TEAAandAA_Spike_3ug_110MinPfizer6_2.raw"),
                Path.Combine(GradientDirectory, "Pfizer6.csv"), GradientDevelopmentBigSearch, "MeOH"),

            // Round 7
            new(Path.Combine(GradientDevelopmentDataDirectory, "241105_FLuc_45Dig_TEAAandAA_Spike_6ug_120-3Pfizer8.raw"),
                Path.Combine(GradientDirectory, "Pfizer8.csv"), GradientDevelopmentBigSearch, "MeOH"),
            new(Path.Combine(GradientDevelopmentDataDirectory, "241105_FLuc_45Dig_TEAAandAA_Spike_6ug_120-2Pfizer9.raw"),
                Path.Combine(GradientDirectory, "Pfizer9.csv"), GradientDevelopmentBigSearch, "MeOH"),

            // Round 8
            new(Path.Combine(GradientDevelopmentDataDirectory, "241105_FLuc_45Dig_TEAAandAA_Spike_6ug_130Pfizer10.raw"),
                Path.Combine(GradientDirectory, "Pfizer10.csv"), GradientDevelopmentBigSearch, "MeOH"),
            new(Path.Combine(GradientDevelopmentDataDirectory, "241105_FLuc_45Dig_TEAAandAA_Spike_6ug_210-2Pfizer11.raw"),
                Path.Combine(GradientDirectory, "Pfizer11.csv"), GradientDevelopmentBigSearch, "MeOH"),
            new(Path.Combine(GradientDevelopmentDataDirectory, "241105_FLuc_45Dig_TEAAandAA_Spike_12ug_290Pfizer1.raw"),
                Path.Combine(GradientDirectory, "Pfizer1.csv"), GradientDevelopmentBigSearch, "MeOH"),
        };


        public static void UpdateTimesToDisplay(this List<ExtractedInformation> infos)
        {
            foreach (var info in infos)
            {
                var run = RunInformationList.First(p => p.DataFileName == info.DataFileName);
                if (run.MinMaxToDisplay is null)
                    continue;

                info.MinRtToDisplay = run.MinMaxToDisplay.Minimum;
                info.MaxRtToDisplay = run.MinMaxToDisplay.Maximum;
            }
        }
    }

    

}
