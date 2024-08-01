using System.Diagnostics;
using Analyzer.SearchType;
using Analyzer.FileTypes.Internal;
using Analyzer.Plotting;
using Analyzer.Plotting.Util;
using Analyzer.Util;
using Microsoft.FSharp.Core;
using Chart = Plotly.NET.CSharp.Chart;
using Plotly.NET;
using Plotly.NET.CSharp;
using Tuple = Tensorboard.Tuple;
using static Plotly.NET.StyleParam.Range;

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
            MetaMorpheusResult mm;
            if (Parameters.RunResult is null)
                try
                {
                    mm = new MetaMorpheusResult(Parameters.SingleRunResultsDirectoryPath);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            else if (Parameters.RunResult is MetaMorpheusResult m)
                mm = m;
            else
                return;

            // Run the parsing
            mm.Override = Parameters.Override; 
            var summary = mm.GetChimericSpectrumSummaryFile();
            mm.Override = false;

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
            
            mm.Dispose();
        }

        private void GeneratePossibleFeaturePlots(ResultType resultType,
            List<ChimericSpectrumSummary> summaryRecords)
        {
            var records = summaryRecords.Where(p => p.Type == resultType.ToString() && p.PossibleFeatureCount != 0).ToList();
            var chimeric = records.Where(p => p.IsChimeric && p.Type != NoIdString).ToList();
            var nonChimeric = records.Where(p => !p.IsChimeric && p.Type != NoIdString).ToList();
            var noId = summaryRecords.Where(p => p.Type == NoIdString && p.PossibleFeatureCount != 0).ToList();

            (double,double)? minMax = Parameters.RunResult.IsTopDown ? (0.0, 50.0) : (0.0, 15.0);
            if (resultType != ResultType.Psm)
                minMax = null;
            // Chimera Stratified
            var chimericHist = GenericPlots.Histogram(chimeric.Select(p => (double)p.PossibleFeatureCount).ToList(), "Chimeric ID", "Features per Isolation Window", "Number of Spectra", false, minMax);
            var nonChimericHist = GenericPlots.Histogram(nonChimeric.Select(p => (double)p.PossibleFeatureCount).ToList(), "Non-Chimeric ID", "Features per Isolation Window", "Number of Spectra", false, minMax);
            var noIdHist = GenericPlots.Histogram(noId.Select(p => (double)p.PossibleFeatureCount).ToList(), "No ID", "Features per Isolation Window", "Number of Spectra", false, minMax);

            var toCombine = IncludeNoIdInPlots
                ? new List<GenericChart.GenericChart> { chimericHist, nonChimericHist, noIdHist }
                : new List<GenericChart.GenericChart> { chimericHist, nonChimericHist };


            var hist = Chart.Combine(toCombine)
                .WithTitle($"1% {Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)} Detected Features Per MS2 Isolation Window");


            var outname = $"SpectrumSummary_FeatureCount_{Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)}_Histogram";
            hist.SaveInRunResultOnly(Parameters.RunResult, outname, 800, 600);

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
                //idVsNot.Show();

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
                //temp.Show();
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
            kde.SaveInRunResultOnly(Parameters.RunResult, outname, 800, 600);
        }

        private void GenerateFractionalIntensityPlots(ResultType resultType,
            List<ChimericSpectrumSummary> summaryRecords, bool isPrecursor, bool sumPrecursor)
        {
            var records = summaryRecords.Where(p => p.Type == resultType.ToString() || (IncludeNoIdInPlots && p.Type == NoIdString)).ToList();
            var chimeric = records.Where(p => p.IsChimeric && p.Type != NoIdString).ToList();
            var nonChimeric = records.Where(p => !p.IsChimeric && p.Type != NoIdString).ToList();
            var noId = records.Where(p => p.Type == NoIdString).ToList();

            

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

            (double, double) minMax = Parameters.RunResult.IsTopDown switch
            {
                true when sumPrecursor && isPrecursor => (0.0, 1.0),
                true when sumPrecursor && !isPrecursor => (0.0, 0.8),
                true when !sumPrecursor && isPrecursor => (0.0, 0.3),
                true when !sumPrecursor && !isPrecursor => (0.0, 0.25),

                false when sumPrecursor && isPrecursor => (0.0, 1.0),
                false when sumPrecursor && !isPrecursor => (0.0, 0.6),
                false when !sumPrecursor && isPrecursor => (0.0, 1.0),
                false when !sumPrecursor && !isPrecursor => (0.0, 0.6),

                _ => (0.0, 1.0)
            };


            var label = /*isPrecursor ? sumPrecursor ?*/ "Percent Identified Intensity" /*: "Precursor ID Fractional Intensity" : "Fragment Fractional Intensity"*/;
            var titleEnd = sumPrecursor
                ? isPrecursor ? "Per Isolation Window" : "Per MS2"
                : "Per ID";
            var outPrecursor = isPrecursor ? "Precursor" : "Fragment";
            var outType = sumPrecursor ? "Summed" : "Independent";

            var chimericHist = GenericPlots.Histogram(chimericFractionalIntensity, "Chimeric ID", label, "Number of Spectra",false, minMax);
            var nonChimericHist = GenericPlots.Histogram(nonChimericFractionalIntensity, "Non-Chimeric ID", label, "Number of Spectra", false, minMax);
            var noIdHist = GenericPlots.Histogram(noIdFractionalIntensity, "No ID", label, "Number of Spectra", false, minMax);

            var toCombine = IncludeNoIdInPlots
                ? new List<GenericChart.GenericChart> { chimericHist, nonChimericHist, noIdHist }
                : new List<GenericChart.GenericChart> { chimericHist, nonChimericHist };

            var hist = Chart.Combine(toCombine)
                .WithTitle(
                    $"1% {Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)} Identified {outPrecursor} Intensity {titleEnd}")
                .WithAxisAnchor(Y: 1);
            var outName = $"SpectrumSummary_{outPrecursor}FractionalIntensity_{outType}_{Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)}_Histogram";
            hist.SaveInRunResultOnly(Parameters.RunResult, outName);

            var chimericKde = GenericPlots.KernelDensityPlot(chimericFractionalIntensity, "Chimeric ID", label, "Density", 0.04);
            var nonChimericKde = GenericPlots.KernelDensityPlot(nonChimericFractionalIntensity, "Non-Chimeric ID", label, "Density", 0.04);
            var noIdKde = GenericPlots.KernelDensityPlot(noIdFractionalIntensity, "No ID", label, "Density", 0.04);

            var toCombineKde = IncludeNoIdInPlots
                ? new List<GenericChart.GenericChart> { chimericKde, nonChimericKde, noIdKde }
                : new List<GenericChart.GenericChart> { chimericKde, nonChimericKde };

            var kde = Chart.Combine(toCombineKde)
                .WithTitle($"1% {Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)} Identified {outPrecursor} Intensity {titleEnd}")
                .WithLayout(PlotlyBase.DefaultLayout)
                .WithAxisAnchor(Y: 2);
            outName = $"SpectrumSummary_{outPrecursor}FractionalIntensity_{outType}_{Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)}_KernelDensity";
            kde.SaveInRunResultOnly(Parameters.RunResult, outName);


            var combinedHistKde = Chart.Combine(new[]
                {
                    kde.WithLineStyle(Dash: StyleParam.DrawingStyle.Dot).WithMarkerStyle(Opacity: 0.8),
                    hist
                })
                .WithYAxisStyle(Title.init("Number of Spectra"), Side: StyleParam.Side.Left, Id: StyleParam.SubPlotId.NewYAxis(1))
                .WithYAxisStyle(Title.init("Density"), Side: StyleParam.Side.Right, Id: StyleParam.SubPlotId.NewYAxis(2),
                    Overlaying: StyleParam.LinearAxisId.NewY(1))
                .WithLayout(PlotlyBase.DefaultLayoutNoLegend);
            outName =
                $"SpectrumSummary_{outPrecursor}FractionalIntensity_{outType}_{Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)}_Combined";
            combinedHistKde.SaveInRunResultOnly(Parameters.RunResult, outName);
        }
    }

}
