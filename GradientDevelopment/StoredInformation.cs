﻿using Easy.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradientDevelopment
{
    public static class StoredInformation
    {
        internal static string GradientDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Gradients");
        internal static string GradientDevelopmentDataDirectory = @"B:\Users\Nic\RNA\FLuc\GradientDevelopment";
        internal static string FluxMethDataDirectory = @"B:\Users\Nic\RNA\FLuc\FLuc Methylation Experiment\m5C_transcription";
        internal static string GradientDevelopmentBigSearch = Path.Combine(GradientDevelopmentDataDirectory, @"ThirdBigSearch\Task1-RnaSearchTask\AllOSMs.osmtsv");

        public static List<RunInformation> RunInformationList = new List<RunInformation>()
        {
            new( Path.Combine(FluxMethDataDirectory, "241025_FLuc_dig_A.raw"), Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch, "ACN"),
            new( Path.Combine(FluxMethDataDirectory, "241025_FLuc_dig_B.raw"), Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch, "ACN"),
            new( Path.Combine(FluxMethDataDirectory, "241025_FLuc_dig_C.raw"), Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch, "ACN"),
            new( Path.Combine(GradientDevelopmentDataDirectory, "241031_FLuc_15digAAwash_80ACNB_std-grad.raw"), Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch, "ACN"),
            new( Path.Combine(GradientDevelopmentDataDirectory, "241031_FLuc_15dig_AAwash_80Met_std-grad1.raw"), Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch, "MeOH"),
            new( Path.Combine(GradientDevelopmentDataDirectory, "241031_FLuc_90dig_AAwash_80Met_std-grad2.raw"), Path.Combine(GradientDirectory, "itwStandard.csv"), GradientDevelopmentBigSearch, "MeOH"),
            new( Path.Combine(GradientDevelopmentDataDirectory, "241031_FLuc_15dig_AAwash_80Met_Pfizer.raw"), Path.Combine(GradientDirectory, "Pfizer1.csv"), GradientDevelopmentBigSearch, "MeOH"),
            new( Path.Combine(GradientDevelopmentDataDirectory, "241101_FLuc_88dig_AAwash_80Met_185MinPfizer.raw"), Path.Combine(GradientDirectory, "Pfizer2.csv"), GradientDevelopmentBigSearch, "MeOH"),
            new( Path.Combine(GradientDevelopmentDataDirectory, "241101_FLuc_88dig_AAwash_80Met_205MinPfizer.raw"), Path.Combine(GradientDirectory, "Pfizer3.csv"), GradientDevelopmentBigSearch, "MeOH"),
            new( Path.Combine(GradientDevelopmentDataDirectory, "241101_FLuc_88dig_AAwash_80Met_290MinPfizer_SourceVoltageToggle.raw"), Path.Combine(GradientDirectory, "Pfizer1.csv"), GradientDevelopmentBigSearch, "MeOH"),
            new( Path.Combine(GradientDevelopmentDataDirectory, "241101_FLuc_88dig_AAwash_80Met_70MinPfizer.raw"), Path.Combine(GradientDirectory, "Pfizer4.csv"), GradientDevelopmentBigSearch, "MeOH"),
        };
    }

    

}