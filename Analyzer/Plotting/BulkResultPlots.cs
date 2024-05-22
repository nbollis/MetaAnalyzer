using Analyzer.Interfaces;
using Analyzer.SearchType;
using Analyzer.Util;
using Plotly.NET;
using Plotly.NET.ImageExport;
using Plotly.NET.TraceObjects;
using Chart = Plotly.NET.CSharp.Chart;

namespace Analyzer.Plotting;

public static class BulkResultPlots
{
    public static void PlotStackedIndividualFileComparison(this AllResults allResults)
    {
        int width = 0;
        int height = 0;

        double heightScaler = allResults.First().First().IsTopDown ? 1.5 : 2.5;
        var title = allResults.First().First().IsTopDown ? "PrSMs" : "Peptides";
        var resultType = allResults.First().First().IsTopDown ? ResultType.Psm : ResultType.Peptide;
        var chart = Chart.Grid(
                allResults.Select(p => p.GetIndividualFileResultsBarChart(out width, out height)
                    .WithYAxisStyle(Title.init(p.CellLine))),
                allResults.Count(), 1, Pattern: StyleParam.LayoutGridPattern.Independent, YGap: 0.2)
            .WithTitle($"Individual File Comparison 1% {title}")
            .WithSize(width, (int)(height * allResults.Count() / heightScaler))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend);
        string outpath = Path.Combine(allResults.GetFigureDirectory(), $"AllResults_{FileIdentifiers.IndividualFileComparisonFigure}_Stacked");
        chart.SavePNG(outpath, null, width, (int)(height * allResults.Count() / heightScaler));
    }

    public static void PlotInternalMMComparison(this AllResults allResults)
    {
        bool isTopDown = allResults.First().First().IsTopDown;
        var results = allResults.SelectMany(p => p.BulkResultCountComparisonFile.Results)
            .Where(p => isTopDown.InternalMMComparisonSelector().Contains(p.Condition))
            .ToList();
        var labels = results.Select(p => p.DatasetName).Distinct().ConvertConditionNames().ToList();

        var noChimeras = results.Where(p => p.Condition.Contains("NoChimeras")).ToList();
        var withChimeras = results.Where(p => !p.Condition.Contains("NoChimeras") && !p.Condition.Contains("PEP")).ToList();

        var psmChart = Chart.Combine(new[]
        {
            Chart2D.Chart.Column<int, string, string, int, int>(noChimeras.Select(p => p.OnePercentPsmCount),
                labels, null, "No Chimeras", MarkerColor: noChimeras.First().Condition.ConvertConditionToColor()),
            Chart2D.Chart.Column<int, string, string, int, int>(withChimeras.Select(p => p.OnePercentPsmCount),
                labels, null, "Chimeras", MarkerColor: withChimeras.First().Condition.ConvertConditionToColor()),
            //Chart2D.Chart.Column<int, string, string, int, int>(others.Select(chimeraGroup => chimeraGroup.OnePercentPsmCount),
            //    labels, null, "Others", MarkerColor: ConditionToColorDictionary[others.First().Condition])
        });
        var smLabel = allResults.First().First().IsTopDown ? "PrSMs" : "PSMs";
        psmChart.WithTitle($"MetaMorpheus 1% FDR {smLabel}")
            .WithXAxisStyle(Title.init("Cell Line"))
            .WithYAxisStyle(Title.init("Count"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend);
        string psmOutpath = Path.Combine(allResults.GetFigureDirectory(), $"InternalMetaMorpheusComparison_{smLabel}");
        psmChart.SavePNG(psmOutpath);

        var peptideChart = Chart.Combine(new[]
        {
            Chart2D.Chart.Column<int, string, string, int, int>(noChimeras.Select(p => p.OnePercentPeptideCount),
                labels, null, "No Chimeras", MarkerColor: noChimeras.First().Condition.ConvertConditionToColor()),
            Chart2D.Chart.Column<int, string, string, int, int>(withChimeras.Select(p => p.OnePercentPeptideCount),
                labels, null, "Chimeras", MarkerColor: withChimeras.First().Condition.ConvertConditionToColor()),
            //Chart2D.Chart.Column<int, string, string, int, int>(others.Select(chimeraGroup => chimeraGroup.OnePercentPeptideCount),
            //    labels, null, "Others", MarkerColor: ConditionToColorDictionary[others.First().Condition])
        });
        peptideChart.WithTitle($"MetaMorpheus 1% FDR {allResults.First().First().ResultType}s")
            .WithXAxisStyle(Title.init("Cell Line"))
            .WithYAxisStyle(Title.init("Count"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend);
        string peptideOutpath = Path.Combine(allResults.GetFigureDirectory(), $"InternalMetaMorpheusComparison_{allResults.First().First().ResultType}s");
        peptideChart.SavePNG(peptideOutpath);

        var proteinChart = Chart.Combine(new[]
        {
            Chart2D.Chart.Column<int, string, string, int, int>(noChimeras.Select(p => p.OnePercentProteinGroupCount),
                labels, null, "No Chimeras", MarkerColor: noChimeras.First().Condition.ConvertConditionToColor()),
            Chart2D.Chart.Column<int, string, string, int, int>(withChimeras.Select(p => p.OnePercentProteinGroupCount),
                labels, null, "Chimeras", MarkerColor: withChimeras.First().Condition.ConvertConditionToColor()),
            //Chart2D.Chart.Column<int, string, string, int, int>(others.Select(chimeraGroup => chimeraGroup.OnePercentProteinGroupCount),
            //    labels, null, "Chimeras", MarkerColor: ConditionToColorDictionary[others.First().Condition]),
        });
        proteinChart.WithTitle("MetaMorpheus 1% FDR Proteins")
            .WithXAxisStyle(Title.init("Cell Line"))
            .WithYAxisStyle(Title.init("Count"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend);
        string proteinOutpath = Path.Combine(allResults.GetFigureDirectory(), "InternalMetaMorpheusComparison_Proteins");
        proteinChart.SavePNG(proteinOutpath);
    }

    public static void PlotBulkResultComparisons(this AllResults allResults, string? outputDirectory = null, bool filterByCondition = true)
    {
        bool isTopDown = allResults.First().First().IsTopDown;
        outputDirectory ??= allResults.GetFigureDirectory();

        var results = allResults.CellLineResults.SelectMany(p => p.BulkResultCountComparisonFile.Results)
            .Where(p => filterByCondition ?
                isTopDown.BulkResultComparisonSelector(p.DatasetName).Contains(p.Condition) : p != null)
            .OrderBy(p => p.Condition.ConvertConditionName())
            .ToList();

        var psmChart = GenericPlots.BulkResultBarChart(results, isTopDown, ResultType.Psm);
        var peptideChart = GenericPlots.BulkResultBarChart(results, isTopDown, ResultType.Peptide);
        var proteinChart = GenericPlots.BulkResultBarChart(results, isTopDown, ResultType.Protein);

        var psmPath = Path.Combine(outputDirectory, $"BulkResultComparison_{GenericPlots.Label(isTopDown, ResultType.Psm)}");
        var peptidePath = Path.Combine(outputDirectory, $"BulkResultComparison_{GenericPlots.Label(isTopDown, ResultType.Peptide)}");
        var proteinPath = Path.Combine(outputDirectory, $"BulkResultComparison_{GenericPlots.Label(isTopDown, ResultType.Protein)}");

        psmChart.SavePNG(psmPath);
        peptideChart.SavePNG(peptidePath);
        proteinChart.SavePNG(proteinPath);
    }

    /// <summary>
    /// Stacked column: Plots the type of chimeric identifications as a function of the degree of chimericity
    /// </summary>
    /// <param name="allResults"></param>
    public static void PlotBulkResultChimeraBreakDown(this AllResults allResults)
    {
        var selector = allResults.First().First().IsTopDown.ChimeraBreakdownSelector();
        bool isTopDown = allResults.First().First().IsTopDown;
        var smLabel = isTopDown ? "PrSM" : "PSM";
        var pepLabel = isTopDown ? "Proteoform" : "Peptide";
        var results = allResults.SelectMany(z => z.Results
                .Where(p => p is IChimeraBreakdownCompatible && selector.Contains(p.Condition))
                .SelectMany(p => ((IChimeraBreakdownCompatible)p).ChimeraBreakdownFile.Results))
            .ToList();

        var psmChart = results.GetChimeraBreakDownStackedColumn(ResultType.Psm, isTopDown, out int width);
        var psmOutPath = Path.Combine(allResults.GetFigureDirectory(),
            $"AllResults_{FileIdentifiers.ChimeraBreakdownComparisonFigure}{smLabel}s");
        psmChart.SavePNG(psmOutPath, null, width, GenericPlots.DefaultHeight);

        var stackedAreaPsmChart = results.GetChimeraBreakDownStackedArea(ResultType.Psm, isTopDown, out width);
        var stackedAreaPsmOutPath = Path.Combine(allResults.GetFigureDirectory(),
                       $"AllResults_{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaFigure}{smLabel}s_StackedArea");
        stackedAreaPsmChart.SavePNG(stackedAreaPsmOutPath, null, width, GenericPlots.DefaultHeight);

        var stackedAreaPercentPsmChart = results.GetChimeraBreakDownStackedArea(ResultType.Psm, isTopDown, out width, true);
        var stackedAreaPercentPsmOutPath = Path.Combine(allResults.GetFigureDirectory(),
                       $"AllResults_{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaPercentFigure}{smLabel}s_StackedArea_Percent");
        stackedAreaPercentPsmChart.SavePNG(stackedAreaPercentPsmOutPath, null, width, GenericPlots.DefaultHeight);

        if (results.All(p => p.Type == ResultType.Psm))
            return;

        var peptideChart = results.GetChimeraBreakDownStackedColumn(ResultType.Peptide, isTopDown, out width);
        var peptideOutPath = Path.Combine(allResults.GetFigureDirectory(),
            $"AllResults_{FileIdentifiers.ChimeraBreakdownComparisonFigure}{pepLabel}s");
        peptideChart.SavePNG(peptideOutPath, null, width, GenericPlots.DefaultHeight);

        var stackedAreaPeptideChart = results.GetChimeraBreakDownStackedArea(ResultType.Peptide, isTopDown, out width);
        var stackedAreaPeptideOutPath = Path.Combine(allResults.GetFigureDirectory(),
            $"AllResults_{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaFigure}{pepLabel}s_StackedArea");
        stackedAreaPeptideChart.SavePNG(stackedAreaPeptideOutPath, null, width, GenericPlots.DefaultHeight);

        var stackedAreaPercentPeptideChart = results.GetChimeraBreakDownStackedArea(ResultType.Peptide, isTopDown, out width, true);
        var stackedAreaPercentPeptideOutPath = Path.Combine(allResults.GetFigureDirectory(),
                                             $"AllResults_{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaPercentFigure}{pepLabel}s_StackedArea_Percent");
        stackedAreaPercentPeptideChart.SavePNG(stackedAreaPercentPeptideOutPath, null, width, GenericPlots.DefaultHeight);
    }

    #region Retention Time

    // Too big to export
    public static void PlotBulkResultRetentionTimePredictions(this AllResults allResults)
    {
        var retentionTimePredictions = allResults.CellLineResults
            .SelectMany(p => p.Where(p => false.FdrPlotSelector().Contains(p.Condition))
                .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
                .Select(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile))
            .ToList();

        var chronologer = retentionTimePredictions
            .SelectMany(p => p.Where(m => m.ChronologerPrediction != 0 && m.PeptideModSeq != ""))
            .ToList();
        var ssrCalc = retentionTimePredictions
            .SelectMany(p => p.Where(m => m.SSRCalcPrediction is not 0 or double.NaN or -1))
            .ToList();

        var chronologerPlot = Chart.Combine(new[]
            {
                Chart2D.Chart.Scatter<double, double, string>(
                    chronologer.Where(p => !p.IsChimeric).Select(p => p.RetentionTime),
                    chronologer.Where(p => !p.IsChimeric).Select(p => p.ChronologerPrediction), StyleParam.Mode.Markers,
                    "No Chimeras", MarkerColor: "No Chimeras".ConvertConditionToColor()),
                Chart2D.Chart.Scatter<double, double, string>(
                    chronologer.Where(p => p.IsChimeric).Select(p => p.RetentionTime),
                    chronologer.Where(p => p.IsChimeric).Select(p => p.ChronologerPrediction), StyleParam.Mode.Markers,
                    "Chimeras", MarkerColor: "Chimeras".ConvertConditionToColor())
            })
            .WithTitle($"All Results Chronologer Predicted HI vs Retention Time (1% Peptides)")
            .WithXAxisStyle(Title.init("Retention Time"))
            .WithYAxisStyle(Title.init("Chronologer Prediction"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend)
            .WithSize(1000, 600);


        PuppeteerSharpRendererOptions.launchOptions.Timeout = 0;
        string outpath = Path.Combine(allResults.GetFigureDirectory(), $"AllResults_{FileIdentifiers.ChronologerFigure}_Aggregated");
        chronologerPlot.SavePNG(outpath, ExportEngine.PuppeteerSharp, 1000, 600);

        var ssrCalcPlot = Chart.Combine(new[]
            {
                Chart2D.Chart.Scatter<double, double, string>(
                    ssrCalc.Where(p => !p.IsChimeric).Select(p => p.RetentionTime),
                    ssrCalc.Where(p => !p.IsChimeric).Select(p => p.SSRCalcPrediction), StyleParam.Mode.Markers,
                    "No Chimeras", MarkerColor: "No Chimeras".ConvertConditionToColor()),
                Chart2D.Chart.Scatter<double, double, string>(
                    ssrCalc.Where(p => p.IsChimeric).Select(p => p.RetentionTime),
                    ssrCalc.Where(p => p.IsChimeric).Select(p => p.SSRCalcPrediction), StyleParam.Mode.Markers,
                    "Chimeras", MarkerColor: "Chimeras".ConvertConditionToColor())
            })
            .WithTitle($"All Results SSRCalc3 Predicted HI vs Retention Time (1% Peptides)")
            .WithXAxisStyle(Title.init("Retention Time"))
            .WithYAxisStyle(Title.init("SSRCalc3 Prediction"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
        outpath = Path.Combine(allResults.GetFigureDirectory(), $"AllResults_{FileIdentifiers.SSRCalcFigure}_Aggregated");
        ssrCalcPlot.SavePNG(outpath, null, 1000, 600);
    }

    // too big to export
    public static void PlotStackedRetentionTimePredictions(this AllResults allResults)
    {
        var results = allResults.Select(p => p.GetCellLineRetentionTimePredictions()).ToList();

        var chronologer = Chart.Grid(results.Select(p => p.Chronologer),
                results.Count(), 1, Pattern: StyleParam.LayoutGridPattern.Independent, YGap: 0.2,
                XSide: StyleParam.LayoutGridXSide.Bottom)
            .WithTitle("Chronologer Predicted HI vs Retention Time (1% Peptides)")
            .WithSize(1000, 400 * results.Count())
            .WithXAxisStyle(Title.init("Retention Time"))
            .WithYAxisStyle(Title.init("Chronologer Prediction"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend);
        string outpath = Path.Combine(allResults.GetFigureDirectory(), $"AllResults_{FileIdentifiers.ChronologerFigure}_Stacked");
        chronologer.SavePNG(outpath, ExportEngine.PuppeteerSharp, 1000, 400 * results.Count());

        var ssrCalc = Chart.Grid(results.Select(p => p.SSRCalc3),
                results.Count(), 1, Pattern: StyleParam.LayoutGridPattern.Independent, YGap: 0.2)
            .WithTitle("SSRCalc3 Predicted HI vs Retention Time (1% Peptides)")
            .WithSize(1000, 400 * results.Count())
            .WithXAxisStyle(Title.init("Retention Time"))
            .WithYAxisStyle(Title.init("SSRCalc3 Prediction"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend);
        outpath = Path.Combine(allResults.GetFigureDirectory(), $"AllResults_{FileIdentifiers.SSRCalcFigure}_Stacked");
        ssrCalc.SavePNG(outpath, null, 1000, 400 * results.Count());
    }

    #endregion

    #region Spectral Similarity

    public static void PlotStackedSpectralSimilarity(this AllResults allResults)
    {
        bool isTopDown = allResults.First().First().IsTopDown;
        var chart = Chart.Grid(
                allResults.Select(p => p.GetCellLineSpectralSimilarity().WithYAxisStyle(Title.init(p.CellLine))),
                4, 3, Pattern: StyleParam.LayoutGridPattern.Independent, YGap: 0.2)
            .WithTitle($"Spectral Angle Distribution (1% {GenericPlots.ResultLabel(isTopDown)})")
            .WithSize(1000, 800)
            .WithLayout(GenericPlots.DefaultLayout);
        string outpath = Path.Combine(allResults.GetFigureDirectory(), $"AllResults_{FileIdentifiers.SpectralAngleFigure}_Stacked");
        chart.SavePNG(outpath, null, 1000, 800);
    }

    public static void PlotAggregatedSpectralSimilarity(this AllResults allResults)
    {
        bool isTopDown = allResults.First().First().IsTopDown;
        var results = allResults.CellLineResults.SelectMany(n => n
            .Where(p => isTopDown.FdrPlotSelector().Contains(p.Condition))
            .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
            .SelectMany(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.Results.Where(m => m.SpectralAngle is not -1 or double.NaN)))
            .ToList();

        double[] chimeraAngles = results.Where(p => p.IsChimeric).Select(p => p.SpectralAngle).ToArray();
        double[] nonChimeraAngles = results.Where(p => !p.IsChimeric).Select(p => p.SpectralAngle).ToArray();
        var violin = GenericPlots.SpectralAngleChimeraComparisonViolinPlot(chimeraAngles, nonChimeraAngles, "AllResults", isTopDown)
            .WithTitle($"All Results Spectral Angle Distribution (1% {GenericPlots.ResultLabel(isTopDown)})")
            .WithYAxisStyle(Title.init("Spectral Angle"))
            .WithLayout(GenericPlots.DefaultLayout)
            .WithSize(1000, 600);
        string outpath = Path.Combine(allResults.GetFigureDirectory(),
            $"AllResults_{FileIdentifiers.SpectralAngleFigure}_Aggregated");
        violin.SavePNG(outpath);
    }

    #endregion

    #region Target Decoy

    /// <summary>
    /// Stacked Column: Plots the target decoy distribution as a function of the degree of chimericity
    /// </summary>
    /// <param name="allResults"></param>
    public static void PlotBulkResultChimeraBreakDown_TargetDecoy(this AllResults allResults)
    {
        var selector = allResults.First().First().IsTopDown.ChimeraBreakdownSelector();
        bool isTopDown = allResults.First().First().IsTopDown;
        var smLabel = isTopDown ? "PrSM" : "PSM";
        var pepLabel = isTopDown ? "Proteoform" : "Peptide";
        var results = allResults.SelectMany(z => z.Results
                       .Where(p => p is MetaMorpheusResult && selector.Contains(p.Condition))
                       .SelectMany(p => ((MetaMorpheusResult)p).ChimeraBreakdownFile.Results))
            .ToList();
        var psmChart =
            results.GetChimeraBreakDownStackedColumn_TargetDecoy(ResultType.Psm, isTopDown, false, out int width);
        var psmOutPath = Path.Combine(allResults.GetFigureDirectory(),
                                          $"AllResults_{FileIdentifiers.ChimeraBreakdownTargetDecoy}_{smLabel}");
        psmChart.SavePNG(psmOutPath, null, width, GenericPlots.DefaultHeight);

        var peptideChart =
            results.GetChimeraBreakDownStackedColumn_TargetDecoy(ResultType.Peptide, isTopDown, false, out width);
        var peptideOutPath = Path.Combine(allResults.GetFigureDirectory(),
                                          $"AllResults_{FileIdentifiers.ChimeraBreakdownTargetDecoy}_{pepLabel}");
        peptideChart.SavePNG(peptideOutPath, null, width, GenericPlots.DefaultHeight);
    }

    #endregion

}