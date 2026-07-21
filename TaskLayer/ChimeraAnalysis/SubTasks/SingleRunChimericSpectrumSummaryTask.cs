using Analyzer.FileTypes.Internal;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotting;
using Plotting.Util;
using ResultAnalyzerUtil;
using System.Diagnostics;
using static Analyzer.Plotting.ProteinCountPlots;
using Chart = Plotly.NET.CSharp.Chart;

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

        private MetaMorpheusResult RunResult;

        protected override void RunSpecific()
        {
            Log("Starting Chimeric Spectrum Summary Task");
            MetaMorpheusResult mm;
            switch (Parameters.RunResult)
            {
                case null:
                    try
                    {
                        mm = new MetaMorpheusResult(Parameters.SingleRunResultsDirectoryPath);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }

                    break;
                case MetaMorpheusResult m:
                    mm = m;
                    break;
                default:
                    return;
            }

            // Run the parsing
            RunResult = mm;
            var sw = Stopwatch.StartNew();
            mm.Override = Parameters.Override;
            Log("Creating Feature Count Plots");
            var summary = mm.GetChimericSpectrumSummaryFile();
            mm.Override = false;
            if (sw.Elapsed.TotalMinutes <= 1)
                return;

            bool isTopDown = mm.IsTopDown;

            GetIntensityPlot(summary.Results, ResultType.Psm, DistributionPlotTypes.ViolinPlot, isTopDown, true)
                .SaveInRunResultOnly(RunResult, $"SpectrumSummary_PrecursorIntensity_PSM_Violin", 800, 600);
            GetIntensityPlot(summary.Results, ResultType.Psm, DistributionPlotTypes.Histogram, isTopDown, true)
                .SaveInRunResultOnly(RunResult, $"SpectrumSummary_PrecursorIntensity_PSM_Histogram", 800, 600);
            GetIntensityPlot(summary.Results, ResultType.Peptide, DistributionPlotTypes.ViolinPlot, isTopDown, true)
                .SaveInRunResultOnly(RunResult, $"SpectrumSummary_PrecursorIntensity_Peptide_Violin", 800, 600);
            GetIntensityPlot(summary.Results, ResultType.Peptide, DistributionPlotTypes.Histogram, isTopDown, true)
                .SaveInRunResultOnly(RunResult, $"SpectrumSummary_PrecursorIntensity_Peptide_Histogram", 800, 600);

            //// Features per MS2 Isolation Window Histograms
            //Log("Creating Feature Count Plots");
            //GeneratePossibleFeaturePlots(ResultType.Psm, summary.Results, isTopDown);
            //GeneratePossibleFeaturePlots(ResultType.Peptide, summary.Results, isTopDown);

            //// Fractional Intensity Histograms
            //Log("Creating Precursor Fractional Intensity Plots");
            //GenerateFractionalIntensityPlots(ResultType.Psm, summary.Results, true, true, isTopDown);
            //GenerateFractionalIntensityPlots(ResultType.Psm, summary.Results, true, false, isTopDown);
            //GenerateFractionalIntensityPlots(ResultType.Peptide, summary.Results, true, true, isTopDown);
            //GenerateFractionalIntensityPlots(ResultType.Peptide, summary.Results, true, false, isTopDown);

            //Log("Creating Fragment Fractional Intensity Plots");
            //GenerateFractionalIntensityPlots(ResultType.Psm, summary.Results, false, false, isTopDown);
            //GenerateFractionalIntensityPlots(ResultType.Psm, summary.Results, false, true, isTopDown);
            //GenerateFractionalIntensityPlots(ResultType.Peptide, summary.Results, false, false, isTopDown);
            //GenerateFractionalIntensityPlots(ResultType.Peptide, summary.Results, false, true, isTopDown);
            
            mm.Dispose();
        }

        public static GenericChart.GenericChart GetIntensityPlot(List<ChimericSpectrumSummary> records, ResultType resultType, DistributionPlotTypes plotType, bool isTopDown, bool logY = false)
        {
            string xTitle = "" /*"Condition"*/;
            string yTitle = "Precursor Intensity";
            string title = yTitle;
            if (logY)
                yTitle = $"Log10 {yTitle}";

            List<GenericChart.GenericChart> toCombine = new();

            foreach (var record in records
                        .Where(p => p.Type == resultType.ToString() && p.PrecursorAbsoluteIntensity > 0)
                         .GroupBy(p => (p.Condition.ConvertConditionName(), p.IsChimeric))
                         .OrderBy(p => p.Key))
            {
                var condition = record.Key.Item1 + (record.Key.Item2 ? " - Chimeric" : " - Non-Chimeric");
                var data = record.Select(p => p.PrecursorAbsoluteIntensity).ToList();

                int max = (int)(data.Max() + (data.Max() * 0.1));
                int min = -10;
                if (logY)
                {
                    max = (int)(Math.Log10(data.Max()) + (Math.Log10(data.Max()) * 0.1));
                    min = 0;
                    data = data.Select(Math.Log10).ToList();
                }
                switch (plotType)
                {
                    case DistributionPlotTypes.ViolinPlot:
                        toCombine.Add(GenericPlots.ViolinPlot(data, condition, xTitle, yTitle)
                            .WithYAxisStyle<int, int, string>(MinMax: new Tuple<int, int>(min, max)));
                        break;

                    case DistributionPlotTypes.Histogram:
                        toCombine.Add(GenericPlots.Histogram(data, condition, xTitle, yTitle)
                            .WithXAxisStyle<int, int, string>(MinMax: new Tuple<int, int>(0, max)));
                        break;

                    case DistributionPlotTypes.BoxPlot:
                        toCombine.Add(GenericPlots.BoxPlot(data, condition, xTitle, yTitle, false)
                            .WithYAxisStyle<int, int, string>(MinMax: new Tuple<int, int>(min, max))
                            .WithLegend(false));
                        break;

                    case DistributionPlotTypes.KernelDensity:
                        toCombine.Add(GenericPlots.KernelDensityPlot(data, condition, xTitle, yTitle, 0.5)
                            .WithXAxisStyle<int, int, string>(MinMax: new Tuple<int, int>(0, max)));
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(plotType), plotType, null);
                }
            }

            var finalPlot = Chart.Combine(toCombine)
                .WithTitle($"{title} (1% {Labels.GetLabel(isTopDown, resultType)})")
                .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText);
            return finalPlot;
        }

        internal void GeneratePossibleFeaturePlots(ResultType resultType,
            List<ChimericSpectrumSummary> summaryRecords, bool isTopDown = false)
        {
            var records = summaryRecords.Where(p => p.Type == resultType.ToString() && p.PossibleFeatureCount != 0).ToList();
            var chimeric = records.Where(p => p.IsChimeric && p.Type != NoIdString).ToList();
            var nonChimeric = records.Where(p => !p.IsChimeric && p.Type != NoIdString).ToList();
            var noId = summaryRecords.Where(p => p.Type == NoIdString && p.PossibleFeatureCount != 0).ToList();

            (double,double)? minMax = isTopDown ? (0.0, 50.0) : (0.0, 15.0);
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
                .WithTitle($"1% {Labels.GetLabel(isTopDown, resultType)} Detected Features Per MS2 Isolation Window");


            var outname = $"SpectrumSummary_FeatureCount_{Labels.GetLabel(isTopDown, resultType)}_Histogram";
            hist.SaveInRunResultOnly(RunResult, outname, 800, 600);

            // Id vs Not
            if (resultType is ResultType.Psm)
            {
                var identifiedData = chimeric.Concat(nonChimeric).ToList();
                
                var identifiedHist = GenericPlots.Histogram(identifiedData.Select(p => (double)p.PossibleFeatureCount).ToList(),
                    "Identified MS2", "Features per Isolation Window", "Number of Spectra");
                var unidentifiedHist = GenericPlots.Histogram(noId.Select(p => (double)p.PossibleFeatureCount).ToList(), 
                    "Unidentified MS2", "Features per Isolation Window", "Number of Spectra");

                string titleInfo = isTopDown ? "Top-Down" : "Bottom-Up";
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
                .WithTitle($"1% {Labels.GetLabel(isTopDown, resultType)} Detected Features Per MS2 Isolation Window");
            outname = $"SpectrumSummary_FeatureCount_{Labels.GetLabel(isTopDown, resultType)}_KernelDensity";
            kde.SaveInRunResultOnly(RunResult, outname, 800, 600);
        }

        internal void GenerateFractionalIntensityPlots(ResultType resultType,
            List<ChimericSpectrumSummary> summaryRecords, bool isPrecursor, bool sumPrecursor, bool isTopDown = false)
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

            (double, double) minMax = isTopDown switch
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
                    $"1% {Labels.GetLabel(isTopDown, resultType)} Identified {outPrecursor} Intensity {titleEnd}")
                .WithAxisAnchor(Y: 1);
            var outName = $"SpectrumSummary_{outPrecursor}FractionalIntensity_{outType}_{Labels.GetLabel(isTopDown, resultType)}_Histogram";
            hist.SaveInRunResultOnly(RunResult, outName);

            var chimericKde = GenericPlots.KernelDensityPlot(chimericFractionalIntensity, "Chimeric ID", label, "Density", 0.04);
            var nonChimericKde = GenericPlots.KernelDensityPlot(nonChimericFractionalIntensity, "Non-Chimeric ID", label, "Density", 0.04);
            var noIdKde = GenericPlots.KernelDensityPlot(noIdFractionalIntensity, "No ID", label, "Density", 0.04);

            var toCombineKde = IncludeNoIdInPlots
                ? new List<GenericChart.GenericChart> { chimericKde, nonChimericKde, noIdKde }
                : new List<GenericChart.GenericChart> { chimericKde, nonChimericKde };

            var kde = Chart.Combine(toCombineKde)
                .WithTitle($"1% {Labels.GetLabel(isTopDown, resultType)} Identified {outPrecursor} Intensity {titleEnd}")
                .WithLayout(PlotlyBase.DefaultLayout)
                .WithAxisAnchor(Y: 2);
            outName = $"SpectrumSummary_{outPrecursor}FractionalIntensity_{outType}_{Labels.GetLabel(isTopDown, resultType)}_KernelDensity";
            kde.SaveInRunResultOnly(RunResult, outName);


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
                $"SpectrumSummary_{outPrecursor}FractionalIntensity_{outType}_{Labels.GetLabel(isTopDown, resultType)}_Combined";
            combinedHistKde.SaveInRunResultOnly(RunResult, outName);
        }
    }

}
