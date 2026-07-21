using Analyzer.FileTypes.Internal;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Easy.Common.Extensions;
using Omics.Fragmentation;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;
using Plotting;
using Plotting.Util;
using Readers;
using ResultAnalyzerUtil;
using SharpLearning.Optimization.Transforms;
using Chart = Plotly.NET.CSharp.Chart;

namespace TaskLayer.ChimeraAnalysis;

public class SingleRunChimericFragmentIonAnalysisParameters : SingleRunAnalysisParameters
{
    public bool ExcludeInternalFragments { get; }

    public SingleRunChimericFragmentIonAnalysisParameters(string inputDirectoryPath, bool overrideFiles, bool runOnAll,
        SingleRunResults runResult, DistributionPlotTypes distributionPlotType, bool excludeInternalFragments)
        : base(inputDirectoryPath, overrideFiles, runOnAll, runResult, distributionPlotType)
    {
        ExcludeInternalFragments = excludeInternalFragments;
    }
}

public class SingleRunChimericFragmentIonAnalysisTask : BaseResultAnalyzerTask
{
    public override MyTask MyTask => MyTask.ChimericFragmentIonAnalysis;
    public override SingleRunChimericFragmentIonAnalysisParameters Parameters { get; }

    public SingleRunChimericFragmentIonAnalysisTask(SingleRunChimericFragmentIonAnalysisParameters parameters)
    {
        Parameters = parameters;
    }

    protected override void RunSpecific()
    {
        if (Parameters.RunResult is not MetaMorpheusResult run)
            run = new MetaMorpheusResult(Parameters.SingleRunResultsDirectoryPath);

        run.Override = Parameters.Override;
        var analysisFile = run.CreateChimericFragmentIonAnalysisFile(Parameters.ExcludeInternalFragments);
        var suffix = Parameters.ExcludeInternalFragments ? "_NoInternal" : string.Empty;

        if (analysisFile.Results.Count == 0)
            return;

        var records = analysisFile.Results;
        var datasetLabel = $"{run.DatasetName} {run.Condition}";

        ChimericFragmentIonAnalysisPlots.CreateHistogramPlot(records, datasetLabel)
            .SaveInRunResultOnly(run, $"ChimericFragmentIonAnalysis_Histogram{suffix}", 1000, 600);

        ChimericFragmentIonAnalysisPlots.CreateUniqueFractionBoxPlot(records, datasetLabel, run.IsTopDown)
            .SaveInRunResultOnly(run, $"ChimericFragmentIonAnalysis_UniqueFractionByGroupSize{suffix}", 1000, 600);
    }
}

public static class ChimericFragmentIonAnalysisPlots
{
    public static GenericChart.GenericChart CreateUniqueRatioDistribution(List<ChimericFragmentIonAnalysisRecord> records, string titlePrefix, DistributionPlotTypes plotType)
    {
        var data = records.Select(p => p.UniqueMatchedFragmentFraction).ToList();

        string xTitle = "Percent Unique Fragment Ions";
        string yTitle = "Count";
        string title = yTitle;

        List<GenericChart.GenericChart> toCombine = new();

        foreach (var record in records 
                     .GroupBy(p => p.Condition.ConvertConditionName())
                     .OrderBy(p => p.Key))
        {
            var condition = record.Key;

            int max = (int)(data.Max() + (data.Max() * 0.1));
            switch (plotType)
            {
                case DistributionPlotTypes.ViolinPlot:
                    toCombine.Add(GenericPlots.ViolinPlot(data, condition, xTitle, yTitle)
                        .WithYAxisStyle<int, int, string>(MinMax: new Tuple<int, int>(0, max)));
                    break;

                case DistributionPlotTypes.Histogram:
                    toCombine.Add(GenericPlots.Histogram(data, condition, xTitle, yTitle)
                        .WithXAxisStyle<int, int, string>(MinMax: new Tuple<int, int>(0, max)));
                    break;

                case DistributionPlotTypes.BoxPlot:
                    toCombine.Add(GenericPlots.BoxPlot(data, condition, xTitle, yTitle, false)
                        .WithYAxisStyle<int, int, string>(MinMax: new Tuple<int, int>(min, max)));
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
            .WithTitle($"{title} (1% {Labels.GetLabel(isTopDown, ResultType.Psm)})")
            .WithLayout(PlotlyBase.DefaultLayoutNoLegendLargererText);
        return finalPlot;


        return null;
    }


    public static GenericChart.GenericChart CreateHistogramPlot(List<ChimericFragmentIonAnalysisRecord> records, string titlePrefix)
    {
        var uniqueVals = records.Select(p => (double)p.UniqueMatchedFragmentIons).ToArray();
        var sharedVals = records.Select(p => (double)p.SharedMatchedFragmentIons).ToArray();

        return Chart.Combine(new[]
            {
                Chart.Histogram<double, double, string>(sharedVals, Name: "Shared Matched Fragment Ions",
                    MarkerColor: "No Chimeras".ConvertConditionToColor(), Opacity: 0.6),
                Chart.Histogram<double, double, string>(uniqueVals, Name: "Unique Matched Fragment Ions",
                    MarkerColor: "Chimeras".ConvertConditionToColor(), Opacity: 0.6)
            })
            .WithTitle($"{titlePrefix} Shared vs Unique Fragment Ion Distribution",
                TitleFont: Font.init(Size: PlotlyBase.TitleSize))
            //.WithXAxis(LinearAxis.init<string, string, int, string, string, string>(TickFont: Font.init(Size: PlotlyBase.AxisTitleFontSize - 2), TickAngle: 45, TickMode: StyleParam.TickMode.Linear))
            .WithXAxisStyle(Title.init("Matched Fragment Ion Count", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
            //.WithYAxis(LinearAxis.init<string, string, int, string, string, string>(TickFont: Font.init(Size: PlotlyBase.AxisTitleFontSize - 2), TickMode: StyleParam.TickMode.Linear))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
    }

    public static GenericChart.GenericChart CreateUniqueFractionBoxPlot(List<ChimericFragmentIonAnalysisRecord> records, string titlePrefix, bool isTopDown)
    {
        var groups = records.GroupBy(r => r.ProteoformCountInSpectrum).OrderBy(g => g.Key).ToList();
        var labels = new List<string>();
        var values = new List<double>();
        foreach (var group in groups)
        {
            labels.AddRange(Enumerable.Repeat($"{group.Key} {Labels.GetLabel(isTopDown, ResultType.Peptide)}s", group.Count()));
            values.AddRange(group.Select(r => r.UniqueMatchedFragmentFraction));
        }

        return Chart.Combine(new[]
            {
                Chart.BoxPlot<string, double, string>(Y: values.ToArray(), X: labels.ToArray(), ShowLegend: false,
                    MarkerColor: "Chimeras".ConvertConditionToColor(),
                    BoxPoints: StyleParam.BoxPoints.Outliers)
            })
            .WithTitle($"{titlePrefix} Unique Fraction by Chimera Group Size",
                TitleFont: Font.init(Size: PlotlyBase.TitleSize))
            //.WithXAxis(LinearAxis.init<string, string, int, string, string, string>(TickFont: Font.init(Size: PlotlyBase.AxisTitleFontSize - 2), TickAngle: 45))
            .WithXAxisStyle(Title.init($"{Labels.GetLabel(isTopDown, ResultType.Peptide)}s per Spectrum", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)), MinMax: new(new(0, 14)))
            //.WithYAxis(LinearAxis.init<string, string, int, string, string, string>(TickFont: Font.init(Size: PlotlyBase.AxisTitleFontSize - 2), TickMode: StyleParam.TickMode.Linear))
            .WithYAxisStyle(Title.init("Unique Fraction", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
            .WithLayout(PlotlyBase.DefaultLayout)
            .WithSize(1000, 600);
    }

    public static void PlotCellLineChimericFragmentIonAnalysis(this CellLineResults cellLine, bool excludeInternalFragments = true)
    {
        var records = cellLine.GetChimericFragmentIonAnalysisFile(excludeInternalFragments).Results;
        if (records.Count == 0)
            return;
        var titlePrefix = cellLine.CellLine;
        var suffix = excludeInternalFragments ? "_NoInternal" : string.Empty;

        CreateHistogramPlot(records, titlePrefix)
            .SaveInCellLineOnly(cellLine, $"ChimericFragmentIonAnalysis_Histogram{suffix}", 1000, 600);
        CreateUniqueFractionBoxPlot(records, titlePrefix, cellLine.First().IsTopDown)
            .SaveInCellLineOnly(cellLine, $"ChimericFragmentIonAnalysis_UniqueFractionByGroupSize{suffix}", 1000, 600);
    }

    public static void PlotBulkChimericFragmentIonAnalysis(this AllResults allResults, bool excludeInternalFragments = true)
    {
        var records = allResults.GetChimericFragmentIonAnalysisFile(excludeInternalFragments).Results;
        if (records.Count == 0)
            return;
        var titlePrefix = "All Cell Lines";
        var suffix = excludeInternalFragments ? "_NoInternal" : string.Empty;

        CreateHistogramPlot(records, titlePrefix)
            .SaveInAllResultsOnly(allResults, $"ChimericFragmentIonAnalysis_Histogram{suffix}", 1000, 600);
        CreateUniqueFractionBoxPlot(records, titlePrefix, allResults.First().First().IsTopDown)
            .SaveInAllResultsOnly(allResults, $"ChimericFragmentIonAnalysis_UniqueFractionByGroupSize{suffix}", 1000, 600);
    }
}
