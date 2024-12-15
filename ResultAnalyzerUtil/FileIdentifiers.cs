namespace ResultAnalyzerUtil
{
    public static partial class FileIdentifiers
    {
        // Radical Fragmentation
        public static string FragmentIndex => "FragmentIndexFile";
        public static string FragmentCountHistogram => "FragmentCountHistogram";
        public static string MinFragmentNeeded => "MinFragmentsNeededHistogram";

        // Gradient Analysis

        public static string ExtractedGradientInformation = "ExtractedGradientData";
        public static string GradientFigure = "IdsVsRt";

        // Chimera Analysis
        public static string ChimeraCountingFile => "ChimeraCounting.csv";
        public static string IndividualFileComparison => "IndividualFileComparison.csv";
        public static string IndividualFileComparisonFigure => "IndividualFileComparison";
        public static string BottomUpResultComparison => "BottomUpResultComparison.csv";
        public static string BulkResultComparisonMultipleFilters => "ResultComparisonManyFilters.csv";
        public static string IndividualFileComparisonMultipleFilters => "IndividualFileResultComparisonManyFilters.csv";
        public static string MaximumChimeraEstimate => "MaximumChimeraEstimate.csv";
        public static string ChimericSpectrumSummary => "ChimericSpectrumSummary.csv";
        public static string MaximumChimeraEstimateCalibAveraged => "MaximumChimeraEstimateCalibAveragedHybrid.csv";
        public static string ProformaFile => "ProformaFile.tsv";
        public static string ProteinCountingFile => "ProteinCounting.tsv";
        public static string ModificationDistributionFigure => "ModificationDistribution";
        public static string ComparativeResultFilteringFigure => "AllResults_ComparingPRs";
        public static string IndividualFileComparativeResultFilteringFigure => "AllResults_ComparingPRs_IndividualFile";
        public static string ComparativeFileResults_TargetDecoyAbsolute => "AllResults_ComparingPRs_TargetDecoy_Absolute";
        public static string ComparativeFileResults_TargetDecoyRelative => "AllResults_ComparingPRs_TargetDecoy_Relative";
        public static string ComparativeTopDownResults => "AllResults_TopDownSummary.png";




        public static string InternalChimeraComparison => "InternalChimeraComparison.csv";
        public static string IndividualFraggerFileComparison => "IndividualFraggerFileComparison.csv";

        // Chimera Breakdown Plots
        public static string ChimeraBreakdownComparison => "ChimeraBreakdownComparison.csv";
        public static string ChimeraBreakdownComparisonFigure => "ChimeraBreakdown_1%";
        public static string ChimeraBreakdownComparisonStackedAreaFigure => "ChimeraBreakdownStackedArea_1%";
        public static string ChimeraBreakdownComparisonStackedAreaPercentFigure => "ChimeraBreakdownStackedAreaPercent_1%";
        public static string ChimeraBreakdownByChargeStateFigure => "ChimeraBreakdownByChargeState";
        public static string ChimeraBreakdownByMassFigure => "ChimeraBreakdownByPrecursorMass";
        public static string ChimeraBreakdownTargetDecoy => "ChimeraBreakdown_TargetDecoy";

        // Retention Time Predictions
        public static string RetentionTimePredictionReady => "RetentionTimePredictionReady.tsv";
        public static string ChronologerReadyFile => "ChronologerReady.tsv";
        public static string ChoronologerResults => "ChronologerOut.tsv";
        public static string CalibratedRetentionTimeFile => "AdjustedRetentionTimes.csv";
        public static string SSRCalcFigure => "RetentionTimeVsSSRCalc3";
        public static string ChronologerFigure => "RetentionTimeVsChronologer";
        public static string ChronologerFigureACN => "PercentACNVsChronologer";
        public static string ChronologerDeltaKdeFigure => "ChronologerDeltaDistribution_KDE";
        public static string ChronologerDeltaBoxAndWhiskers => "ChronologerDeltaDistribution_BoxAndWhisker";
        public static string ChronologerDeltaRange => "ChronologerDeltaDistribution_Range";


        // Feature Finding Plots
        public static string RetentionTimeShift_MM => "RetentionTimeShift_MetaMorpheus";
        public static string RetentionTimeShiftHistogram_MM => "RetentionTimeShiftHistogram_MetaMorpheus";
        public static string RetentionTimeShift_Fragger => "RetentionTimeShift_Fragger";
        public static string RetentionTimeShiftHistogram_Fragger => "RetentionTimeShiftHistogram_Fragger";
        public static string RetentionTimeShift_Stacked => "RetentionTimeShift_Stacked";
        public static string RetentionTimeShiftHistogram_Stacked => "RetentionTimeShiftHistogram_Stacked";
        public static string RetentionTimeShiftFullGrid_Stacked => "RetentionTimeShiftFullGrid_Stacked";


        // Target Decoy
        public static string RetentionTimeFigure => "RetentionTimeComparison";
        public static string SpectralAngleFigure => "SpectralAngleComparison";
        public static string PepGridChartFigure => "PepFeatureAnalysis";
        public static string TargetDecoyCurve => "TargetDecoyCurve";

        // Jenkins Runs
        public static string PepTestingSummaryFigure => "RunSummary";

    }
}
