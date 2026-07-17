namespace ResultAnalyzerUtil
{
    public static partial class FileIdentifiers
    {
        // Radical Fragmentation
        public const string FragmentIndex = "FragmentIndexFile";
        public const string FragmentCountHistogram = "FragmentCountHistogram";
        public const string MinFragmentNeeded = "MinFragmentsNeededHistogram";
        public const string FragNeededSummary = "FragmentsNeeded_Summary";
        public const string PrecursorCompetitionSummary = "PrecursorCompetition_Summary";

        // Gradient Analysis

        public static string ExtractedGradientInformation = "ExtractedGradientData";
        public static string GradientFigure = "IdsVsRt";

        // Chimera Analysis
        public const string ChimeraCountingFile = "ChimeraCounting.csv";
        public const string IndividualFileComparison = "IndividualFileComparison.csv";
        public const string IndividualFileComparisonFigure = "IndividualFileComparison";
        public const string BottomUpResultComparison = "BottomUpResultComparison.csv";
        public const string BulkResultComparisonMultipleFilters = "ResultComparisonManyFilters.csv";
        public const string IndividualFileComparisonMultipleFilters = "IndividualFileResultComparisonManyFilters.csv";
        public const string MaximumChimeraEstimate = "MaximumChimeraEstimate.csv";
        public const string ChimericSpectrumSummary = "ChimericSpectrumSummary.csv";
        public const string ChimericFragmentIonAnalysis = "ChimericFragmentIonAnalysis.tsv";
        public const string ChimericFragmentIonAnalysisViolin = "ChimericFragmentIonAnalysis_Violin";
        public const string MaximumChimeraEstimateCalibAveraged = "MaximumChimeraEstimateCalibAveragedHybrid.csv";
        public const string ProformaFile = "ProformaFile.tsv";
        public const string ProteinCountingFile = "ProteinCounting.tsv";
        public const string ModificationDistributionFigure = "ModificationDistribution";
        public const string ComparativeResultFilteringFigure = "AllResults_ComparingPRs";
        public const string IndividualFileComparativeResultFilteringFigure = "AllResults_ComparingPRs_IndividualFile";
        public const string ComparativeFileResults_TargetDecoyAbsolute = "AllResults_ComparingPRs_TargetDecoy_Absolute";
        public const string ComparativeFileResults_TargetDecoyRelative = "AllResults_ComparingPRs_TargetDecoy_Relative";
        public const string ComparativeTopDownResults = "AllResults_TopDownSummary.png";
        public const string TargetDecoyCurveFigure = "AllResults_TopDownSummary.png";




        public const string InternalChimeraComparison = "InternalChimeraComparison.csv";
        public const string IndividualFraggerFileComparison = "IndividualFraggerFileComparison.csv";

        // Chimera Breakdown Plots
        public const string ChimeraBreakdownComparison = "ChimeraBreakdownComparison.csv";
        public const string ChimeraBreakdownComparisonFigure = "ChimeraBreakdown_1%";
        public const string ChimeraBreakdownComparisonStackedAreaFigure = "ChimeraBreakdownStackedArea_1%";
        public const string ChimeraBreakdownComparisonStackedAreaPercentFigure = "ChimeraBreakdownStackedAreaPercent_1%";
        public const string ChimeraBreakdownByChargeStateFigure = "ChimeraBreakdownByChargeState";
        public const string ChimeraBreakdownByMassFigure = "ChimeraBreakdownByPrecursorMass";
        public const string ChimeraBreakdownTargetDecoy = "ChimeraBreakdown_TargetDecoy";

        // Retention Time Predictions
        public const string RetentionTimePredictionReady = "RetentionTimePredictionReady.tsv";
        public const string ChronologerReadyFile = "ChronologerReady.tsv";
        public const string ChoronologerResults = "ChronologerOut.tsv";
        public const string CalibratedRetentionTimeFile = "AdjustedRetentionTimes.csv";
        public const string SSRCalcFigure = "RetentionTimeVsSSRCalc3";
        public const string ChronologerFigure = "RetentionTimeVsChronologer";
        public const string CzeMobility = "MigrationTimeVsMobility";
        public const string CzeMigrationTime = "MigrationTimeVsMigrationTime";
        public const string ChronologerFigureACN = "PercentACNVsChronologer";
        public const string ChronologerDeltaKdeFigure = "ChronologerDeltaDistribution_KDE"; 
        public const string CzeDeltaKdeFigure = "CZEDeltaDistribution_KDE";
        public const string ChronologerDeltaBoxAndWhiskers = "ChronologerDeltaDistribution_BoxAndWhisker";
        public const string ChronologerDeltaRange = "ChronologerDeltaDistribution_Range";


        // Feature Finding Plots
        public const string RetentionTimeShift_MM = "RetentionTimeShift_MetaMorpheus";
        public const string RetentionTimeShiftHistogram_MM = "RetentionTimeShiftHistogram_MetaMorpheus";
        public const string RetentionTimeShift_Fragger = "RetentionTimeShift_Fragger";
        public const string RetentionTimeShiftHistogram_Fragger = "RetentionTimeShiftHistogram_Fragger";
        public const string RetentionTimeShift_Stacked = "RetentionTimeShift_Stacked";
        public const string RetentionTimeShiftHistogram_Stacked = "RetentionTimeShiftHistogram_Stacked";
        public const string RetentionTimeShiftFullGrid_Stacked = "RetentionTimeShiftFullGrid_Stacked";


        // Target Decoy
        public const string RetentionTimeFigure = "RetentionTimeComparison";
        public const string SpectralAngleFigure = "SpectralAngleComparison";
        public const string PepGridChartFigure = "PepFeatureAnalysis";
        public const string TargetDecoyCurve = "TargetDecoyCurve";

        // Jenkins Runs
        public const string PepTestingSummaryFigure = "RunSummary";

    }
}
