using Analyzer.SearchType;
using Analyzer.FileTypes.Internal;
using Analyzer.Plotting;
using Analyzer.Plotting.Util;
using Analyzer.Util;
using Chart = Plotly.NET.CSharp.Chart;
using Plotly.NET;

namespace TaskLayer.ChimeraAnalysis
{
    public class SingleRunChimericSpectrumSummaryTask : BaseResultAnalyzerTask
    {
        // TODO: Break this out into its own project and make this a CMD parameter
        public static bool IncludeNoIdInPlots = false;
        public static string NoIdString = "No ID";

        public override MyTask MyTask => MyTask.ChimericSpectrumSummary;
        public override SingleRunAnalysisParameters Parameters { get; }

        public SingleRunChimericSpectrumSummaryTask(SingleRunAnalysisParameters parameters)
        {
            Parameters = parameters;
        }

        protected override void RunSpecific()
        {
            if (Parameters.RunResult is not MetaMorpheusResult mm)
                return;

            // Run the parsing
            mm.Override = Parameters.Override; 
            var summary = mm.GetChimericSpectrumSummaryFile();
            mm.Override = false;

            // Plot the results
            var dataDictionary = summary.GroupBy(p => p.Type)
                .ToDictionary(p => p.Key,
                    p => p.GroupBy(m => m.IsChimeric)
                        .ToDictionary(n => n.Key,
                            n => n.Select(b => (b.PossibleFeatureCount, b.IdPerSpectrum, b.PrecursorFractionalIntensity, b.FragmentFractionalIntensity))
                                .ToArray()));

            // Features per MS2 Isolation Window Histograms
            Log("Creating Feature Count Plots");
            GeneratePossibleFeaturePlots(ResultType.Psm, summary.Results);
            GeneratePossibleFeaturePlots(ResultType.Peptide, summary.Results);

            // Fractional Intensity Histograms
            Log("Creating Precursor Fractional Intensity Plots");
            GenerateFractionalIntensityPlots(ResultType.Psm, summary.Results, true, true);
            GenerateFractionalIntensityPlots(ResultType.Psm, summary.Results, true, false);
            GenerateFractionalIntensityPlots(ResultType.Peptide, summary.Results, true, true);
            GenerateFractionalIntensityPlots(ResultType.Peptide, summary.Results, true, false);

            Log("Creating Fragment Fractional Intensity Plots");
            GenerateFractionalIntensityPlots(ResultType.Psm, summary.Results, false, false);
            GenerateFractionalIntensityPlots(ResultType.Psm, summary.Results, false, true);
            GenerateFractionalIntensityPlots(ResultType.Peptide, summary.Results, false, false);
            GenerateFractionalIntensityPlots(ResultType.Peptide, summary.Results, false, true);


            //var summedPrecursorIntDict = summary.Results
            //    .GroupBy(p => p.Type)
            //    .ToDictionary(p => p.Key,
            //        p => p.GroupBy(m => m,
            //                new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber, result => result.FileName))
            //            .ToDictionary(n => n.Key, n => n.Select(b => b.PrecursorFractionalIntensity).Sum()));

            //var ogPsmNonChimeric = summedPrecursorIntDict[ResultType.Psm.ToString()].Count(p => !p.Key.IsChimeric);
            //var ogPsmChimeric = summedPrecursorIntDict[ResultType.Psm.ToString()].Count(p => p.Key.IsChimeric);
            //var ogPeptideNonChimeric = summedPrecursorIntDict[ResultType.Peptide.ToString()].Count(p => !p.Key.IsChimeric);
            //var ogPeptideChimeric = summedPrecursorIntDict[ResultType.Peptide.ToString()].Count(p => p.Key.IsChimeric);



            mm.Dispose();
        }

        private void GeneratePossibleFeaturePlots(ResultType resultType,
            List<ChimericSpectrumSummary> summaryRecords)
        {
            var records = summaryRecords.Where(p => p.Type == resultType.ToString() || (IncludeNoIdInPlots && p.Type == NoIdString) && p.PossibleFeatureCount != 0).ToList();
            var chimeric = records.Where(p => p.IsChimeric && p.Type != NoIdString).ToList();
            var nonChimeric = records.Where(p => !p.IsChimeric && p.Type != NoIdString).ToList();
            var noId = summaryRecords.Where(p => p.Type == NoIdString).ToList();

            // Chimera Stratified
            var chimericHist = GenericPlots.Histogram(chimeric.Select(p => (double)p.PossibleFeatureCount).ToList(), "Chimeric ID", "Features per Isolation Window", "Number of Spectra");
            var nonChimericHist = GenericPlots.Histogram(nonChimeric.Select(p => (double)p.PossibleFeatureCount).ToList(), "Non-Chimeric ID", "Features per Isolation Window", "Number of Spectra");
            var noIdHist = GenericPlots.Histogram(noId.Select(p => (double)p.PossibleFeatureCount).ToList(), "No ID", "Features per Isolation Window", "Number of Spectra");

            var toCombine = IncludeNoIdInPlots
                ? new List<GenericChart.GenericChart> { chimericHist, nonChimericHist, noIdHist }
                : new List<GenericChart.GenericChart> { chimericHist, nonChimericHist };

            var hist = Chart.Combine(toCombine)
                .WithTitle($"1% {Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)} Detected Features Per MS2 Isolation Window");

            var outname = $"SpectrumSummary_FeatureCount_{Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)}_Histogram";
            hist.SaveInRunResultOnly(Parameters.RunResult, outname);

            // Id vs Not
            if (resultType is ResultType.Psm)
            {
                var identifiedData = chimeric.Concat(nonChimeric).ToList();
                
                var identifiedHist = GenericPlots.Histogram(identifiedData.Select(p => (double)p.PossibleFeatureCount).ToList(),
                    "Identified MS2", "Features per Isolation Window", "Number of Spectra");
                var unidentifiedHist = GenericPlots.Histogram(noId.Select(p => (double)p.PossibleFeatureCount).ToList(), 
                    "Unidentified MS2", "Features per Isolation Window", "Number of Spectra");

                string titleInfo = Parameters.RunResult.IsTopDown ? "Top-Down" : "Bottom-Up";
                var idVsNot = Chart.Combine(new[] { identifiedHist, unidentifiedHist })
                    .WithTitle($"{titleInfo} Features Per Isolation Window");
                idVsNot.Show();

                var temp = Chart.Combine(new[]
                {
                    Chart.Spline<double, int, string>(
                        identifiedData.OrderBy(p => p.RetentionTime).Select(p => p.RetentionTime),
                        identifiedData.OrderBy(p => p.RetentionTime).Select(p => p.PossibleFeatureCount),
                        Name: "Identified MS2", MarkerColor: "Identified MS2".ConvertConditionToColor()
                        ),
                    Chart.Spline<double, int, string>(
                        noId.OrderBy(p => p.RetentionTime).Select(p => p.RetentionTime),
                        noId.OrderBy(p => p.RetentionTime).Select(p => p.PossibleFeatureCount),
                        Name: "Unidentified MS2", MarkerColor: "Unidentified MS2".ConvertConditionToColor()
                        ),
                })
                .WithXAxisStyle(Title.init("Retention Time"))
                .WithYAxisStyle(Title.init("Features per Isolation Window"))
                .WithTitle($"{titleInfo} Features Per Isolation Window");
                temp.Show();
            }

            var chimericKde = GenericPlots.KernelDensityPlot(chimeric.Select(p => (double)p.PossibleFeatureCount).ToList(), "Chimeric ID", "Features per Isolation Window", "Density", 0.5);
            var nonChimericKde = GenericPlots.KernelDensityPlot(nonChimeric.Select(p => (double)p.PossibleFeatureCount).ToList(), "Non-Chimeric ID", "Features per Isolation Window", "Density", 0.5);
            var noIdKde = GenericPlots.KernelDensityPlot(noId.Select(p => (double)p.PossibleFeatureCount).ToList(), "No ID", "Features per Isolation Window", "Density", 0.5);

            var toCombineKde = IncludeNoIdInPlots
                ? new List<GenericChart.GenericChart> { chimericKde, nonChimericKde, noIdKde }
                : new List<GenericChart.GenericChart> { chimericKde, nonChimericKde };

            var kde = Chart.Combine(toCombineKde)
                .WithTitle($"1% {Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)} Detected Features Per MS2 Isolation Window");
            outname = $"SpectrumSummary_FeatureCount_{Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)}_KernelDensity";
            kde.SaveInRunResultOnly(Parameters.RunResult, outname);
        }

        private void GenerateFractionalIntensityPlots(ResultType resultType,
            List<ChimericSpectrumSummary> summaryRecords, bool isPrecursor, bool sumPrecursor)
        {
            var records = summaryRecords.Where(p => p.Type == resultType.ToString() || (IncludeNoIdInPlots && p.Type == NoIdString)).ToList();
            var chimeric = records.Where(p => p.IsChimeric && p.Type != NoIdString).ToList();
            var nonChimeric = records.Where(p => !p.IsChimeric && p.Type != NoIdString).ToList();
            var noId = records.Where(p => p.Type == NoIdString).ToList();

            var label = isPrecursor ? sumPrecursor ? "Precursor Scan Fractional Intensity" : "Precursor ID Fractional Intensity" : "Fragment Fractional Intensity";
            var titleEnd = isPrecursor ? sumPrecursor ? "Per Isolation Window" : "Per ID" : "Per MS2";
            var outType = isPrecursor ? sumPrecursor ? "SummedPrecursor" : "IndependentPrecursor" : "Fragment";

            var chimericFractionalIntensity = sumPrecursor
                ? isPrecursor
                    ? chimeric.GroupBy(p => p,
                            new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber,
                                result => result.FileName))
                        .ToDictionary(p => p.Key, p => p.Select(b => b.PrecursorFractionalIntensity).Sum())
                        .Values.ToList()
                    : chimeric.GroupBy(p => p,
                            new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber,
                                result => result.FileName))
                        .ToDictionary(p => p.Key, p => p.Select(b => b.FragmentFractionalIntensity).Sum())
                        .Values.ToList()
                : chimeric.Select(p => isPrecursor ? p.PrecursorFractionalIntensity : p.FragmentFractionalIntensity)
                    .ToList();
            var nonChimericFractionalIntensity = sumPrecursor
                ? isPrecursor
                    ? nonChimeric.GroupBy(p => p,
                            new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber,
                                result => result.FileName))
                        .ToDictionary(p => p.Key, p => p.Select(b => b.PrecursorFractionalIntensity).Sum())
                        .Values.ToList()
                    : nonChimeric.GroupBy(p => p,
                            new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber,
                                result => result.FileName))
                        .ToDictionary(p => p.Key, p => p.Select(b => b.FragmentFractionalIntensity).Sum())
                        .Values.ToList()
                : nonChimeric.Select(p => isPrecursor ? p.PrecursorFractionalIntensity : p.FragmentFractionalIntensity)
                    .ToList();
            var noIdFractionalIntensity = sumPrecursor
                ? isPrecursor
                    ? noId.GroupBy(p => p,
                            new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber,
                                result => result.FileName))
                        .ToDictionary(p => p.Key, p => p.Select(b => b.PrecursorFractionalIntensity).Sum())
                        .Values.ToList()
                    : noId.GroupBy(p => p,
                            new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber,
                                result => result.FileName))
                        .ToDictionary(p => p.Key, p => p.Select(b => b.FragmentFractionalIntensity).Sum())
                        .Values.ToList()
                : noId.Select(p => isPrecursor ? p.PrecursorFractionalIntensity : p.FragmentFractionalIntensity)
                    .ToList();

            var chimericHist = GenericPlots.Histogram(chimericFractionalIntensity, "Chimeric ID", label, "Number of Spectra");
            var nonChimericHist = GenericPlots.Histogram(nonChimericFractionalIntensity, "Non-Chimeric ID", label, "Number of Spectra");
            var noIdHist = GenericPlots.Histogram(noIdFractionalIntensity, "No ID", label, "Number of Spectra");

            var toCombine = IncludeNoIdInPlots
                ? new List<GenericChart.GenericChart> { chimericHist, nonChimericHist, noIdHist }
                : new List<GenericChart.GenericChart> { chimericHist, nonChimericHist };

            var hist = Chart.Combine(toCombine)
                .WithTitle($"1% {Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)} Detected Features {titleEnd}");
            var outName = $"SpectrumSummary_{outType}FractionalIntensity_{Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)}_Histogram";
            hist.SaveInRunResultOnly(Parameters.RunResult, outName);

            var chimericKde = GenericPlots.KernelDensityPlot(chimericFractionalIntensity, "Chimeric ID", label, "Density", 0.02);
            var nonChimericKde = GenericPlots.KernelDensityPlot(nonChimericFractionalIntensity, "Non-Chimeric ID", label, "Density", 0.02);
            var noIdKde = GenericPlots.KernelDensityPlot(noIdFractionalIntensity, "No ID", label, "Density", 0.02);

            var toCombineKde = IncludeNoIdInPlots
                ? new List<GenericChart.GenericChart> { chimericKde, nonChimericKde, noIdKde }
                : new List<GenericChart.GenericChart> { chimericKde, nonChimericKde };

            var kde = Chart.Combine(toCombineKde)
                .WithTitle($"1% {Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)} Detected Features {titleEnd}");
            outName = $"SpectrumSummary_{outType}FractionalIntensity_{Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)}_KernelDensity";
            kde.SaveInRunResultOnly(Parameters.RunResult, outName);

            //if (isPrecursor && sumPrecursor)
            //{
            //    var histFunc = StyleParam.HistFunc.Avg;
            //    var histNorm = StyleParam.HistNorm.None;
            //    var chimericData = chimeric.GroupBy(p => p,
            //            new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber,
            //                result => result.FileName))
            //        .SelectMany(p =>
            //            p.Select(m => (m.IsolationWindowAbsoluteIntensity, p.Select(n => n.PrecursorFractionalIntensity).Sum())))
            //        .ToList();

            //    var heatmap = Chart.Histogram2D<double, double, double>(chimericData.Select(p => p.Item2),
            //            chimericData.Select(p => p.IsolationWindowAbsoluteIntensity), Name: "Chimeric", 
            //            HistFunc: histFunc, HistNorm: histNorm)
            //        .WithXAxisStyle(Title.init("Precursor Fractional Intensity"))
            //        .WithYAxisStyle(Title.init("Isolation Window Intensity"))
            //        .WithTitle("Chimeric");
                

            //    var nonChimericdata = nonChimeric.GroupBy(p => p,
            //            new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber,
            //                result => result.FileName))
            //        .SelectMany(p =>
            //            p.Select(m => (m.IsolationWindowAbsoluteIntensity,
            //                p.Select(n => n.PrecursorFractionalIntensity).Sum()))
            //        ).ToList();

            //    var nonChimericHeatMap = Chart.Histogram2D<double, double, double>(
            //            nonChimericdata.Select(p => p.Item2),
            //            nonChimericdata.Select(p => p.IsolationWindowAbsoluteIntensity), Name: "Non-Chimeric",
            //            HistFunc: histFunc, HistNorm: histNorm)
            //        .WithXAxisStyle(Title.init("Precursor Fractional Intensity"))
            //        .WithYAxisStyle(Title.init("Isolation Window Intensity"))
            //        .WithTitle("Non-Chimeric");

            //    var grid = Chart.Grid(new[] { heatmap, nonChimericHeatMap }, 1, 2)
            //        .WithSize(800, 600)
            //        .WithTitle(
            //            $"1% {Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)} Precursor Intensities");
            //    grid.Show();
            //}
        }
    }

}
