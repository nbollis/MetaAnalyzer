using Analyzer.SearchType;
using Analyzer.Util;
using Plotly.NET;
using Plotly.NET.ImageExport;
using Proteomics.PSM;
using Chart = Plotly.NET.CSharp.Chart;

namespace Analyzer.Plotting;

public static class CellLinePlots
{
    #region Individual File Results

    public static void PlotIndividualFileResults(this CellLineResults cellLine, ResultType? resultType = null,
        string? outputDirectory = null)
    {
        bool isTopDown = cellLine.First().IsTopDown;
        resultType ??= isTopDown ? ResultType.Psm : ResultType.Peptide;
        outputDirectory ??= cellLine.GetFigureDirectory();

        string outPath = Path.Combine(outputDirectory, $"{FileIdentifiers.IndividualFileComparisonFigure}_{resultType}_{cellLine.CellLine}");
        var chart = cellLine.GetIndividualFileResultsBarChart(out int width, out int height, resultType.Value);
        chart.SavePNG(outPath, null, width, height);
    }

    public static GenericChart.GenericChart GetIndividualFileResultsBarChart(this CellLineResults cellLine, out int width,
        out int height, Util.ResultType resultType = ResultType.Psm, bool filterByCondition = true)
    {
        bool isTopDown = cellLine.First().IsTopDown;
        var fileResults = (filterByCondition ? cellLine.Select(p => p.IndividualFileComparisonFile)
                    .Where(p => p != null && isTopDown.IndividualFileComparisonSelector().Contains(p.First().Condition))
                : cellLine.Select(p => p.IndividualFileComparisonFile))
            .OrderBy(p => p.First().Condition.ConvertConditionName())
            .ToList();

        return GenericPlots.IndividualFileResultBarChart(fileResults, out width, out height, cellLine.CellLine,
            isTopDown, resultType);
    }

    #endregion

    #region Retention Time

    public static void PlotCellLineRetentionTimePredictions(this CellLineResults cellLine)
    {
        var plots = cellLine.GetCellLineRetentionTimePredictions();
        string outPath = Path.Combine(cellLine.GetFigureDirectory(), $"{FileIdentifiers.ChronologerFigure}_{cellLine.CellLine}");
        plots.Chronologer.SavePNG(outPath, null, 1000, GenericPlots.DefaultHeight);

        outPath = Path.Combine(cellLine.GetFigureDirectory(), $"{FileIdentifiers.SSRCalcFigure}_{cellLine.CellLine}");
        plots.SSRCalc3.SavePNG(outPath, null, 1000, GenericPlots.DefaultHeight);
    }

    internal static (GenericChart.GenericChart Chronologer, GenericChart.GenericChart SSRCalc3) GetCellLineRetentionTimePredictions(this CellLineResults cellLine)
    {
        var individualFiles = cellLine.Results
            .Where(p => false.FdrPlotSelector().Contains(p.Condition))
            .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
            .Select(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile)
            .ToList();
        var chronologer = individualFiles
            .SelectMany(p => p.Where(m => m.ChronologerPrediction != 0 && m.PeptideModSeq != ""))
            .ToList();
        var ssrCalc = individualFiles
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
            .WithTitle($"{cellLine.CellLine} Chronologer Predicted HI vs Retention Time (1% Peptides)")
            .WithXAxisStyle(Title.init("Retention Time"))
            .WithYAxisStyle(Title.init("Chronologer Prediction"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend)
            .WithSize(1000, GenericPlots.DefaultHeight);

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
            .WithTitle($"{cellLine.CellLine} SSRCalc3 Predicted HI vs Retention Time (1% Peptides)")
            .WithXAxisStyle(Title.init("Retention Time"))
            .WithYAxisStyle(Title.init("SSRCalc3 Prediction"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend)
            .WithSize(1000, GenericPlots.DefaultHeight);
        return (chronologerPlot, ssrCalcPlot);
    }

    #endregion

    #region Spectral Similarity

    public static void PlotCellLineSpectralSimilarity(this CellLineResults cellLine)
    {
        string outpath = Path.Combine(cellLine.GetFigureDirectory(), $"{FileIdentifiers.SpectralAngleFigure}_{cellLine.CellLine}");
        cellLine.GetCellLineSpectralSimilarity().SavePNG(outpath);
    }

    internal static GenericChart.GenericChart GetCellLineSpectralSimilarity(this CellLineResults cellLine)
    {
        bool isTopDown = cellLine.First().IsTopDown;
        double[] chimeraAngles;
        double[] nonChimeraAngles;
        if (isTopDown)
        {
            var angles = cellLine.Results
                .Where(p => isTopDown.FdrPlotSelector().Contains(p.Condition))
                .SelectMany(p => ((MetaMorpheusResult)p).AllPeptides.Where(m => m.SpectralAngle is not -1 or double.NaN))
                .GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer)
                .SelectMany(chimeraGroup =>
                    chimeraGroup.Select(prsm => (prsm.SpectralAngle ?? -1, chimeraGroup.Count() > 1)))
                .ToList();
            chimeraAngles = angles.Where(p => p.Item2).Select(p => p.Item1).ToArray();
            nonChimeraAngles = angles.Where(p => !p.Item2).Select(p => p.Item1).ToArray();
        }
        else
        {
            var angles = cellLine.Results
                .Where(p => isTopDown.FdrPlotSelector().Contains(p.Condition))
                .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
                .Select(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile)
                .SelectMany(p => p.Where(m => m.SpectralAngle is not -1 or double.NaN))
                .ToList();
            chimeraAngles = angles.Where(p => p.IsChimeric).Select(p => p.SpectralAngle).ToArray();
            nonChimeraAngles = angles.Where(p => !p.IsChimeric).Select(p => p.SpectralAngle).ToArray();
        }

        return GenericPlots.SpectralAngleChimeraComparisonViolinPlot(chimeraAngles, nonChimeraAngles, cellLine.CellLine, isTopDown);
    }

    #endregion

    #region Target Decoy

    /// <summary>
    /// Stacked Column: Plots the target decoy distribution as a function of the degree of chimericity
    /// </summary>
    /// <param name="cellLine"></param>
    /// <param name="absolute"></param>
    public static void PlotCellLineChimeraBreakdown_TargetDecoy(this CellLineResults cellLine, bool absolute = false)
    {
        var selector = cellLine.First().IsTopDown.ChimeraBreakdownSelector();
        var smLabel = cellLine.First().IsTopDown ? "PrSM" : "PSM";
        var pepLabel = cellLine.First().IsTopDown ? "Proteoform" : "Peptide";

        var results = cellLine.Results
            .Where(p => p is MetaMorpheusResult && selector.Contains(p.Condition))
            .SelectMany(p => ((MetaMorpheusResult)p).ChimeraBreakdownFile)
            .ToList();
        var psmChart =
            results.GetChimeraBreakDownStackedColumn_TargetDecoy(ResultType.Psm, cellLine.First().IsTopDown, absolute, out int width);
        string psmOutPath = Path.Combine(cellLine.GetFigureDirectory(),
            $"{FileIdentifiers.ChimeraBreakdownTargetDecoy}_{smLabel}_{cellLine.CellLine}");
        psmChart.SavePNG(psmOutPath, null, width, GenericPlots.DefaultHeight);

        var peptideChart =
            results.GetChimeraBreakDownStackedColumn_TargetDecoy(ResultType.Peptide, cellLine.First().IsTopDown, absolute, out width);
        string peptideOutPath = Path.Combine(cellLine.GetFigureDirectory(),
            $"{FileIdentifiers.ChimeraBreakdownTargetDecoy}_{pepLabel}_{cellLine.CellLine}");
        peptideChart.SavePNG(peptideOutPath, null, width, GenericPlots.DefaultHeight);
    }

    #endregion


    /// <summary>
    /// Stacked column: Plots the type of chimeric identifications as a function of the degree of chimericity
    /// </summary>
    /// <param name="cellLine"></param>
    public static void PlotCellLineChimeraBreakdown(this CellLineResults cellLine)
    {
        var selector = cellLine.First().IsTopDown.ChimeraBreakdownSelector();
        var smLabel = cellLine.First().IsTopDown ? "PrSM" : "PSM";
        var pepLabel = cellLine.First().IsTopDown ? "Proteoform" : "Peptide";

        var results = cellLine.Results
            .Where(p => p is MetaMorpheusResult && selector.Contains(p.Condition))
            .SelectMany(p => ((MetaMorpheusResult)p).ChimeraBreakdownFile)
            .ToList();
        var psmChart =
            results.GetChimeraBreakDownStackedColumn(ResultType.Psm, cellLine.First().IsTopDown, out int width);
        string psmOutPath = Path.Combine(cellLine.GetFigureDirectory(),
            $"{FileIdentifiers.ChimeraBreakdownComparisonFigure}_{smLabel}_{cellLine.CellLine}");
        psmChart.SavePNG(psmOutPath, null, width, GenericPlots.DefaultHeight);

        var peptideChart =
            results.GetChimeraBreakDownStackedColumn(ResultType.Peptide, cellLine.First().IsTopDown, out width);
        string peptideOutPath = Path.Combine(cellLine.GetFigureDirectory(),
            $"{FileIdentifiers.ChimeraBreakdownComparisonFigure}_{pepLabel}_{cellLine.CellLine}");
        peptideChart.SavePNG(peptideOutPath, null, width, GenericPlots.DefaultHeight);
    }

}