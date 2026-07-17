using Analyzer.FileTypes.Internal;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Omics.Fragmentation;
using Plotly.NET;
using Plotly.NET.TraceObjects;
using Chart = Plotly.NET.CSharp.Chart;
using Plotting.Util;
using Readers;
using ResultAnalyzerUtil;

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

        CreateViolinPlot(records, datasetLabel)
            .SaveInRunResultOnly(run, $"{FileIdentifiers.ChimericFragmentIonAnalysisViolin}{suffix}", 1000, 600);

        CreateHistogramPlot(records, datasetLabel)
            .SaveInRunResultOnly(run, $"ChimericFragmentIonAnalysis_Histogram{suffix}", 1000, 600);

        CreateUniqueFractionBoxPlot(records, datasetLabel)
            .SaveInRunResultOnly(run, $"ChimericFragmentIonAnalysis_UniqueFractionByGroupSize{suffix}", 1000, 600);

        CreatePairedScatterPlot(records, datasetLabel)
            .SaveInRunResultOnly(run, $"ChimericFragmentIonAnalysis_PairedScatter{suffix}", 1000, 600);

        CreateStackedBarTopN(records, datasetLabel)
            .SaveInRunResultOnly(run, $"ChimericFragmentIonAnalysis_StackedBarTop10{suffix}", 1000, 600);

        CreateHeatmapTopN(records, datasetLabel)
            .SaveInRunResultOnly(run, $"ChimericFragmentIonAnalysis_HeatmapTop10{suffix}", 1000, 600);

        CreateCombinedGrid(records, datasetLabel)
            .SaveInRunResultOnly(run, $"ChimericFragmentIonAnalysis_Grid{suffix}", 1400, 1000);
    }

    private static GenericChart.GenericChart CreateViolinPlot(List<ChimericFragmentIonAnalysisRecord> records, string titlePrefix)
    {
        var totalValues = records.Select(p => (double)p.TotalMatchedFragmentIons).ToArray();
        var uniqueValues = records.Select(p => (double)p.UniqueMatchedFragmentIons).ToArray();
        var totalLabels = Enumerable.Repeat("Total Matched Fragment Ions", records.Count).ToArray();
        var uniqueLabels = Enumerable.Repeat("Unique Matched Fragment Ions", records.Count).ToArray();

        return Chart.Combine(new[]
            {
                Chart.Violin<string, double, string>(totalLabels, totalValues, null,
                    MarkerColor: "No Chimeras".ConvertConditionToColor(),
                    MeanLine: MeanLine.init(true, "No Chimeras".ConvertConditionToColor()), ShowLegend: false),
                Chart.Violin<string, double, string>(uniqueLabels, uniqueValues, null,
                    MarkerColor: "Chimeras".ConvertConditionToColor(),
                    MeanLine: MeanLine.init(true, "Chimeras".ConvertConditionToColor()), ShowLegend: false)
            })
            .WithTitle($"{titlePrefix} Fragment Ion Support for Chimeric Identifications",
                TitleFont: Font.init(Size: PlotlyBase.TitleSize))
            .WithYAxisStyle(Title.init("Matched Fragment Ion Count", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
            .WithLayout(PlotlyBase.DefaultLayout)
            .WithSize(1000, 600);
    }

    private static GenericChart.GenericChart CreateHistogramPlot(List<ChimericFragmentIonAnalysisRecord> records, string titlePrefix)
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
            .WithXAxisStyle(Title.init("Matched Fragment Ion Count", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
            .WithYAxisStyle(Title.init("Count", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
    }

    private static GenericChart.GenericChart CreateUniqueFractionBoxPlot(List<ChimericFragmentIonAnalysisRecord> records, string titlePrefix)
    {
        var groups = records.GroupBy(r => r.ProteoformCountInSpectrum).OrderBy(g => g.Key).ToList();
        var labels = new List<string>();
        var values = new List<double>();
        foreach (var group in groups)
        {
            labels.AddRange(Enumerable.Repeat($"{group.Key} proteoforms", group.Count()));
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
            .WithXAxisStyle(Title.init("Proteoforms per Spectrum", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
            .WithYAxisStyle(Title.init("Unique Fraction", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
            .WithLayout(PlotlyBase.DefaultLayout)
            .WithSize(1000, 600);
    }

    private static GenericChart.GenericChart CreatePairedScatterPlot(List<ChimericFragmentIonAnalysisRecord> records, string titlePrefix)
    {
        var grouped = records.GroupBy(r => (r.FileNameWithoutExtension, r.ScanNumber))
            .Where(g => g.Count() >= 2)
            .Take(50)
            .ToList();

        var traces = new List<GenericChart.GenericChart>();
        foreach (var group in grouped)
        {
            var ordered = group.OrderBy(r => r.ProteoformIndex).ToList();
            traces.Add(Chart.Line<double, double, string>(
                ordered.Select(r => (double)r.ProteoformIndex),
                ordered.Select(r => (double)r.UniqueMatchedFragmentIons))
                .WithLine(Line.init(Color: Color.fromString("gray"), Width: 0.5f))
                .WithLegend(false));
        }

        var allPoints = grouped.SelectMany(g => g).ToList();
        traces.Add(Chart.Scatter<double, double, string>(
            allPoints.Select(r => (double)r.ProteoformIndex),
            allPoints.Select(r => (double)r.UniqueMatchedFragmentIons),
            StyleParam.Mode.Markers,
            "Unique Ions per Proteoform",
            MarkerColor: "Chimeras".ConvertConditionToColor()));

        return Chart.Combine(traces)
            .WithTitle($"{titlePrefix} Unique Ions per Proteoform within Chimeric Spectra (first 50)",
                TitleFont: Font.init(Size: PlotlyBase.TitleSize))
            .WithXAxisStyle(Title.init("Proteoform Rank in Spectrum", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
            .WithYAxisStyle(Title.init("Unique Matched Fragment Ions", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
    }

    private static GenericChart.GenericChart CreateStackedBarTopN(List<ChimericFragmentIonAnalysisRecord> records, string titlePrefix)
    {
        var topGroups = records.GroupBy(r => (r.FileNameWithoutExtension, r.ScanNumber))
            .OrderByDescending(g => g.Sum(r => r.TotalMatchedFragmentIons))
            .Take(10)
            .ToList();

        var xLabels = topGroups.Select((g, i) => $"#{i + 1}").ToArray();
        var uniqueValues = new double[topGroups.Count];
        var sharedValues = new double[topGroups.Count];
        for (int i = 0; i < topGroups.Count; i++)
        {
            uniqueValues[i] = topGroups[i].Sum(r => r.UniqueMatchedFragmentIons);
            sharedValues[i] = topGroups[i].Sum(r => r.SharedMatchedFragmentIons);
        }

        return Chart.Combine(new[]
            {
                Chart.Bar<double, string, string>(sharedValues, xLabels, Name: "Shared Fragment Ions",
                    MarkerColor: "No Chimeras".ConvertConditionToColor()),
                Chart.Bar<double, string, string>(uniqueValues, xLabels, Name: "Unique Fragment Ions",
                    MarkerColor: "Chimeras".ConvertConditionToColor())
            })
            .WithTitle($"{titlePrefix} Top 10 Chimeric Spectra by Total Matched Ions",
                TitleFont: Font.init(Size: PlotlyBase.TitleSize))
            .WithXAxisStyle(Title.init("Spectrum Rank", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
            .WithYAxisStyle(Title.init("Matched Fragment Ion Count", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
    }

    private static GenericChart.GenericChart CreateHeatmapTopN(List<ChimericFragmentIonAnalysisRecord> records, string titlePrefix)
    {
        var topGroups = records.GroupBy(r => (r.FileNameWithoutExtension, r.ScanNumber))
            .Where(g => g.Count() >= 2)
            .OrderByDescending(g => g.Sum(r => r.UniqueMatchedFragmentIons))
            .Take(20)
            .ToList();

        int maxCount = topGroups.Max(g => g.Count());
        var yLabels = topGroups.Select((g, i) => $"#{i + 1}").ToArray();

        var z = new double[topGroups.Count][];
        for (int i = 0; i < topGroups.Count; i++)
        {
            var ordered = topGroups[i].OrderBy(r => r.ProteoformIndex).ToList();
            z[i] = new double[maxCount];
            for (int j = 0; j < maxCount; j++)
                z[i][j] = j < ordered.Count ? ordered[j].UniqueMatchedFragmentFraction : double.NaN;
        }

        var xLabels = Enumerable.Range(1, maxCount).Select(i => $"Proteoform {i}").ToArray();

        return Chart.Heatmap<double, double, string, string>(
                z.Select(row => row.Select(v => v).ToArray()).ToArray(),
                X: Enumerable.Range(1, maxCount).Select(i => (double)i).ToArray(),
                Y: yLabels,
                ShowLegend: false,
                ColorScale: StyleParam.Colorscale.Viridis)
            .WithTitle($"{titlePrefix} Unique Fraction per Proteoform (top 20 spectra)",
                TitleFont: Font.init(Size: PlotlyBase.TitleSize))
            .WithXAxisStyle(Title.init("Proteoform Rank", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
            .WithYAxisStyle(Title.init("Spectrum", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
            .WithLayout(PlotlyBase.DefaultLayout)
            .WithSize(1000, 600);
    }

    private static GenericChart.GenericChart CreateCombinedGrid(List<ChimericFragmentIonAnalysisRecord> records, string titlePrefix)
    {
        return Chart.Grid(
                new[]
                {
                    CreateViolinPlot(records, titlePrefix),
                    CreateHistogramPlot(records, titlePrefix),
                    CreateUniqueFractionBoxPlot(records, titlePrefix),
                    CreatePairedScatterPlot(records, titlePrefix)
                },
                2, 2,
                Pattern: StyleParam.LayoutGridPattern.Independent)
            .WithTitle($"{titlePrefix} Chimeric Fragment-Ion Analysis — Overview",
                TitleFont: Font.init(Size: PlotlyBase.TitleSize))
            .WithLayout(PlotlyBase.DefaultLayout)
            .WithSize(1400, 1000);
    }
}
