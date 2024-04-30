﻿namespace ResultAnalyzer.Util
{
    public static class FileIdentifiers
    {
        // Ryan Julian
        public static string FragmentIndex => "FragmentIndexFile.csv";
        public static string FragmentCountHistogram => "FragmentCountHistogram.csv";
        public static string MinFragmentNeeded => "MinFragmentsNeededHistogram.csv";




        // Chimera Analysis
        public static string ChimeraCountingFile => "ChimeraCounting.csv";
        public static string IndividualFileComparison => "IndividualFileComparison.csv";
        public static string IndividualFileComparisonFigure => "IndividualFileComparison";
        public static string BottomUpResultComparison => "BottomUpResultComparison.csv";
        public static string ChimeraBreakdownComparison => "ChimeraBreakdownComparison_QValue.csv";
        public static string ChimeraBreakdownComparisonFigure => "ChimeraBreakdown_1%";
        public static string ChimeraBreakdownTargetDecoy => "ChimeraBreakdown_TargetDecoy";



        public static string InternalChimeraComparison => "InternalChimeraComparison.csv";
        public static string IndividualFraggerFileComparison => "IndividualFraggerFileComparison.csv";

        // retention time prediction and fdr
        public static string RetentionTimePredictionReady => "RetentionTimePredictionReady.tsv";
        public static string ChronologerReadyFile => "ChronologerReady.tsv";
        public static string ChoronologerResults => "ChronologerOut.tsv";
        public static string SSRCalcFigure => "RetentionTimeVsSSRCalc3";
        public static string ChronologerFigure => "RetentionTimeVsChronologer";
        public static string SpectralAngleFigure => "SpectralAngleComparison";

    }
}