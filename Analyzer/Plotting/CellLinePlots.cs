using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Analyzer.FileTypes.Internal;
using Analyzer.Interfaces;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using CsvHelper.Expressions;
using Easy.Common.Extensions;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using Microsoft.FSharp.Core;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.ImageExport;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;
using Proteomics.PSM;
using Readers;
using TopDownProteomics.IO.PsiMod;
using Chart = Plotly.NET.CSharp.Chart;
using GenericChartExtensions = Plotly.NET.CSharp.GenericChartExtensions;

namespace Analyzer.Plotting;

public static class CellLinePlots
{
    #region Individual File Results

    public static void PlotIndividualFileResults(this CellLineResults cellLine, ResultType? resultType = null,
        string? outputDirectory = null, bool filterByCondition = true)
    {
        bool isTopDown = cellLine.First().IsTopDown;
        resultType ??= isTopDown ? ResultType.Psm : ResultType.Peptide;
        outputDirectory ??= cellLine.GetChimeraPaperFigureDirectory();

        string outPath = Path.Combine(outputDirectory, $"{FileIdentifiers.IndividualFileComparisonFigure}_{resultType}_{cellLine.CellLine}");
        var chart = cellLine.GetIndividualFileResultsBarChart(out int width, out int height, resultType.Value, filterByCondition);
        chart.SavePNG(outPath, null, width, height);

        outPath = Path.Combine(cellLine.FigureDirectory, $"{FileIdentifiers.IndividualFileComparisonFigure}_{resultType}_{cellLine.CellLine}");
        chart.SavePNG(outPath, null, width, height);
    }

    public static GenericChart.GenericChart GetIndividualFileResultsBarChart(this CellLineResults cellLine, out int width,
        out int height, ResultType resultType = ResultType.Psm, bool filterByCondition = true)
    {
        bool isTopDown = cellLine.First().IsTopDown;
        var fileResults = (filterByCondition ? cellLine.Select(p => p.IndividualFileComparisonFile)
                    .Where(p => p != null && p.Any() && isTopDown.IndividualFileComparisonSelector(cellLine.CellLine).Contains(p.First().Condition))
                : cellLine.Select(p => p.IndividualFileComparisonFile))
            .OrderBy(p => p.First().Condition.ConvertConditionName())
            .ToList();

        return GenericPlots.IndividualFileResultBarChart(fileResults, out width, out height, cellLine.CellLine,
            isTopDown, resultType);
    }


    public static void PlotModificationDistribution(this CellLineResults cellLine,
        ResultType resultType = ResultType.Psm, bool filterByCondition = true)
    {
        bool isTopDown = cellLine.First().IsTopDown;
        var fileResults = (filterByCondition ? cellLine.Select(p => p)
                    .Where(p => isTopDown.FdrPlotSelector().Contains(p.Condition))
                : cellLine.Select(p => p))
            .OrderBy(p => p.Condition.ConvertConditionName())
            .ToList();
        string resultTypeLabel = isTopDown ? resultType == ResultType.Psm ? "PrSM" : "Proteoform" :
            resultType == ResultType.Psm ? "PSM" : "Peptide";

        foreach (var bulkResult in fileResults.Where(p => p is MetaMorpheusResult))
        {
            var result = (MetaMorpheusResult)bulkResult;
            List<PsmFromTsv> results = resultType switch
            {
                ResultType.Psm => result.AllPsms.Where(p => p is { DecoyContamTarget: "T", PEP_QValue: <= 0.01, AmbiguityLevel: "1" })
                    .ToList(),
                ResultType.Peptide => result.AllPsms.Where(p => p is { DecoyContamTarget: "T", PEP_QValue: <= 0.01, AmbiguityLevel: "1" })
                    .GroupBy(p => p.FullSequence)
                    .Select(p => p.First())
                    .ToList(),
                _ => throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null)
            };
            var grouped = results.GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer)
                .GroupBy(m => m.Count())
                .ToDictionary(p => p.Key, p => p.SelectMany(m => m));

            var nonChimeric = grouped[1]
                .Select(p => p.FullSequence)
                .ToList();
            var chimeric = grouped.Where(p => p.Key != 1)
                .SelectMany(p => p.Value)
                .Select(p => p.FullSequence)
                .ToList();

            var chart = Chart.Combine(new[]
            {
                GenericPlots.ModificationDistribution(nonChimeric, "Non-Chimeric", "Modification", "Percent"),
                GenericPlots.ModificationDistribution(chimeric, "Chimeric", "Modification", "Percent"),
            })
                .WithTitle($"{cellLine.CellLine} 1% {resultType} Modification Distribution")
                .WithSize(1200, 800)
                .WithXAxis(LinearAxis.init<string, string, string, string, string, string>(TickAngle:45))
                .WithLayout(PlotlyBase.DefaultLayout);
            var outName = $"{FileIdentifiers.ModificationDistributionFigure}_{resultTypeLabel}_{cellLine.CellLine}";
            if (filterByCondition)
                chart.SaveInCellLineAndMann11Directories(cellLine, outName, 800, 600);
            else 
                chart.SaveInCellLineOnly(cellLine, outName, 800, 600);
        }
    }

    #endregion

    #region Retention Time

    public static void PlotCellLineRetentionTimePredictions(this CellLineResults cellLine)
    {
        var plots = cellLine.GetCellLineRetentionTimePredictions();
        string outPath = Path.Combine(cellLine.GetChimeraPaperFigureDirectory(), $"{FileIdentifiers.ChronologerFigure}_{cellLine.CellLine}");
        plots.Chronologer.SavePNG(outPath, null, 1000, PlotlyBase.DefaultHeight);
        outPath = Path.Combine(cellLine.FigureDirectory, $"{FileIdentifiers.ChronologerFigure}_{cellLine.CellLine}");
        plots.Chronologer.SavePNG(outPath, null, 1000, PlotlyBase.DefaultHeight);

        outPath = Path.Combine(cellLine.GetChimeraPaperFigureDirectory(), $"{FileIdentifiers.SSRCalcFigure}_{cellLine.CellLine}");
        plots.SSRCalc3.SavePNG(outPath, null, 1000, PlotlyBase.DefaultHeight);
        outPath = Path.Combine(cellLine.FigureDirectory, $"{FileIdentifiers.SSRCalcFigure}_{cellLine.CellLine}");
        plots.SSRCalc3.SavePNG(outPath, null, 1000, PlotlyBase.DefaultHeight);
    }

    internal static (GenericChart.GenericChart Chronologer, GenericChart.GenericChart SSRCalc3) GetCellLineRetentionTimePredictions(this CellLineResults cellLine)
    {
        var individualFiles = cellLine.Results
            .Where(p => false.FdrPlotSelector().Contains(p.Condition))
            .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
            .Select(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile)
            .ToList();
        var chronologer = individualFiles
            .SelectMany(p => p.Where(m => m.ChronologerPrediction != 0 && m.PeptideModSeq != "" ))
            .Select(p => (p.ChronologerPrediction, p.RetentionTime, p.IsChimeric))
            .ToList();
        
        var chronologerInterceptSlope = Fit.Line(chronologer.Select(p => p.RetentionTime).ToArray(),
            chronologer.Select(p => p.ChronologerPrediction).ToArray());
        var chimeraR2 = GoodnessOfFit.CoefficientOfDetermination(
            chronologer.Where(p => p.IsChimeric)
                .Select(p => p.RetentionTime * chronologerInterceptSlope.B + chronologerInterceptSlope.A),
            chronologer.Where(p => p.IsChimeric)
                .Select(p => p.ChronologerPrediction)).Round(4);
        var nonChimericR2 = GoodnessOfFit.CoefficientOfDetermination(
            chronologer.Where(p => !p.IsChimeric)
                .Select(p => p.RetentionTime * chronologerInterceptSlope.B + chronologerInterceptSlope.A),
            chronologer.Where(p => !p.IsChimeric)
                .Select(p => p.ChronologerPrediction)).Round(4);

        (double RT, double Prediction)[] line = new[]
        {
            (chronologer.Min(p => p.RetentionTime), chronologerInterceptSlope.A + chronologerInterceptSlope.B * chronologer.Min(p => p.RetentionTime)),
            (chronologer.Max(p => p.RetentionTime), chronologerInterceptSlope.A + chronologerInterceptSlope.B * chronologer.Max(p => p.RetentionTime))
        };
        var chronologerPlot = Chart.Combine(new[]
            {
                Chart2D.Chart.Scatter<double, double, string>(
                    chronologer.Where(p => !p.IsChimeric).Select(p => p.RetentionTime),
                    chronologer.Where(p => !p.IsChimeric).Select(p => p.ChronologerPrediction), StyleParam.Mode.Markers,
                    $"No Chimeras - R^2={nonChimericR2}", MarkerColor: "No Chimeras".ConvertConditionToColor()),
                Chart2D.Chart.Scatter<double, double, string>(
                    chronologer.Where(p => p.IsChimeric).Select(p => p.RetentionTime),
                    chronologer.Where(p => p.IsChimeric).Select(p => p.ChronologerPrediction), StyleParam.Mode.Markers,
                    $"Chimeras - R^2={chimeraR2}", MarkerColor: "Chimeras".ConvertConditionToColor()),
                Chart.Line<double, double, string>(line.Select(p => p.RT), line.Select(p => p.Prediction))
                    .WithLegend(false)
            })
            .WithTitle($"{cellLine.CellLine} Chronologer Predicted HI vs Retention Time (1% Peptides)")
            .WithXAxisStyle(Title.init("Retention Time"))
            .WithYAxisStyle(Title.init("Chronologer Prediction"))
            .WithLayout(Layout.init<string>(PaperBGColor: Color.fromKeyword(ColorKeyword.White),
                PlotBGColor: Color.fromKeyword(ColorKeyword.White),
                ShowLegend: true,
                Legend: Legend.init(X: 0.5, Y: -0.2, Orientation: StyleParam.Orientation.Horizontal, EntryWidth: 0,
                    VerticalAlign: StyleParam.VerticalAlign.Bottom,
                    XAnchor: StyleParam.XAnchorPosition.Center,
                    YAnchor: StyleParam.YAnchorPosition.Top
                )))
            .WithSize(1000, PlotlyBase.DefaultHeight);

        var ssrCalc = individualFiles
            .SelectMany(p => p.Where(m => m.SSRCalcPrediction is not 0 or double.NaN or -1 ))
            .Select(p => (p.SSRCalcPrediction, p.RetentionTime, p.IsChimeric))
            .ToList();
        var ssrCalcInterceptSlope = Fit.Line(ssrCalc.Select(p => p.RetentionTime).ToArray(),
            ssrCalc.Select(p => p.SSRCalcPrediction).ToArray());

        chimeraR2 = GoodnessOfFit.CoefficientOfDetermination(
            ssrCalc.Where(p => p.IsChimeric)
                .Select(p => p.RetentionTime * ssrCalcInterceptSlope.B + ssrCalcInterceptSlope.A),
            ssrCalc.Where(p => p.IsChimeric)
                .Select(p => p.SSRCalcPrediction)).Round(4);
        nonChimericR2 = GoodnessOfFit.CoefficientOfDetermination(
            ssrCalc.Where(p => !p.IsChimeric)
                .Select(p => p.RetentionTime * ssrCalcInterceptSlope.B + ssrCalcInterceptSlope.A),
            ssrCalc.Where(p => !p.IsChimeric)
                .Select(p => p.SSRCalcPrediction)).Round(4);

        line = new[]
        {
            (ssrCalc.Min(p => p.RetentionTime), ssrCalcInterceptSlope.A + ssrCalcInterceptSlope.B * ssrCalc.Min(p => p.RetentionTime)),
            (ssrCalc.Max(p => p.RetentionTime), ssrCalcInterceptSlope.A + ssrCalcInterceptSlope.B * ssrCalc.Max(p => p.RetentionTime))
        };

        var ssrCalcPlot = Chart.Combine(new[]
            {
                Chart2D.Chart.Scatter<double, double, string>(
                    ssrCalc.Where(p => !p.IsChimeric).Select(p => p.RetentionTime),
                    ssrCalc.Where(p => !p.IsChimeric).Select(p => p.SSRCalcPrediction), StyleParam.Mode.Markers,
                    $"No Chimeras - R^2={nonChimericR2}", MarkerColor: "No Chimeras".ConvertConditionToColor()),
                Chart2D.Chart.Scatter<double, double, string>(
                    ssrCalc.Where(p => p.IsChimeric).Select(p => p.RetentionTime),
                    ssrCalc.Where(p => p.IsChimeric).Select(p => p.SSRCalcPrediction), StyleParam.Mode.Markers,
                    $"Chimeras - R^2={chimeraR2}", MarkerColor: "Chimeras".ConvertConditionToColor()),
                Chart.Line<double, double, string>(line.Select(p => p.RT), line.Select(p => p.Prediction))
            })
            .WithTitle($"{cellLine.CellLine} SSRCalc3 Predicted HI vs Retention Time (1% Peptides)")
            .WithXAxisStyle(Title.init("Retention Time"))
            .WithYAxisStyle(Title.init("SSRCalc3 Prediction"))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(1000, PlotlyBase.DefaultHeight);
        return (chronologerPlot, ssrCalcPlot);
    }

    #region Chronologer Exploration

    

  

    public static void PlotChronologerVsPercentHi(this CellLineResults cellLine)
    {
        cellLine.GetChronologerHIScatterPlot()
            .SaveInCellLineOnly(cellLine, $"{FileIdentifiers.ChronologerFigureACN}_{cellLine.CellLine}", 1000, PlotlyBase.DefaultHeight);
    }

    internal static GenericChart.GenericChart GetChronologerHIScatterPlot(this CellLineResults cellLine)
    {
        var individualFiles = cellLine.Results
            .Where(p => false.FdrPlotSelector().Contains(p.Condition))
            .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
            .Select(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile)
            .ToList();
        var chronologer = individualFiles
            .SelectMany(p => p.Where(m => m.ChronologerPrediction != 0 && m.PeptideModSeq != ""))
            .Select(p => (p.ChronologerPrediction, p.PercentHI, p.IsChimeric))
            .ToList();

        var chronologerInterceptSlope = Fit.Line(chronologer.Select(p => p.PercentHI).ToArray(),
            chronologer.Select(p => p.ChronologerPrediction).ToArray());
        var chimeraR2 = GoodnessOfFit.CoefficientOfDetermination(
            chronologer.Where(p => p.IsChimeric)
                .Select(p => p.PercentHI * chronologerInterceptSlope.B + chronologerInterceptSlope.A),
            chronologer.Where(p => p.IsChimeric)
                .Select(p => p.ChronologerPrediction)).Round(4);
        var nonChimericR2 = GoodnessOfFit.CoefficientOfDetermination(
            chronologer.Where(p => !p.IsChimeric)
                .Select(p => p.PercentHI * chronologerInterceptSlope.B + chronologerInterceptSlope.A),
            chronologer.Where(p => !p.IsChimeric)
                .Select(p => p.ChronologerPrediction)).Round(4);

        (double RT, double Prediction)[] line = new[]
        {
            (chronologer.Min(p => p.PercentHI), chronologerInterceptSlope.A + chronologerInterceptSlope.B * chronologer.Min(p => p.PercentHI)),
            (chronologer.Max(p => p.PercentHI), chronologerInterceptSlope.A + chronologerInterceptSlope.B * chronologer.Max(p => p.PercentHI))
        };
        var chronologerPlot = Chart.Combine(new[]
            {
                Chart2D.Chart.Scatter<double, double, string>(
                    chronologer.Where(p => !p.IsChimeric).Select(p => p.PercentHI),
                    chronologer.Where(p => !p.IsChimeric).Select(p => p.ChronologerPrediction), StyleParam.Mode.Markers,
                    $"No Chimeras - R^2={nonChimericR2}", MarkerColor: "No Chimeras".ConvertConditionToColor()),
                Chart2D.Chart.Scatter<double, double, string>(
                    chronologer.Where(p => p.IsChimeric).Select(p => p.PercentHI),
                    chronologer.Where(p => p.IsChimeric).Select(p => p.ChronologerPrediction), StyleParam.Mode.Markers,
                    $"Chimeras - R^2={chimeraR2}", MarkerColor: "Chimeras".ConvertConditionToColor()),
                Chart.Line<double, double, string>(line.Select(p => p.RT), line.Select(p => p.Prediction))
                    .WithLegend(false)
            })
            .WithTitle($"{cellLine.CellLine} Chronologer Predicted HI vs Retention Time (1% Peptides)")
            .WithXAxisStyle(Title.init("Percent ACN"))
            .WithYAxisStyle(Title.init("Chronologer Prediction"))
            .WithLayout(Layout.init<string>(PaperBGColor: Color.fromKeyword(ColorKeyword.White),
                PlotBGColor: Color.fromKeyword(ColorKeyword.White),
                ShowLegend: true,
                Legend: Legend.init(X: 0.5, Y: -0.2, Orientation: StyleParam.Orientation.Horizontal, EntryWidth: 0,
                    VerticalAlign: StyleParam.VerticalAlign.Bottom,
                    XAnchor: StyleParam.XAnchorPosition.Center,
                    YAnchor: StyleParam.YAnchorPosition.Top
                )))
            .WithSize(1000, PlotlyBase.DefaultHeight);
        return chronologerPlot;
    }


    public static void PlotChronologerDeltaKernelPDF(this CellLineResults cellLine, Kernels kernel = Kernels.Gaussian)
    {
        var chart = cellLine.GetChronologerDeltaPlotKernelPDF(kernel);
        //GenericChartExtensions.Show(chart);
        chart.SaveInCellLineAndMann11Directories(cellLine, $"{FileIdentifiers.ChronologerDeltaKdeFigure}_{cellLine.CellLine}", 600, 600);
    }

    internal static GenericChart.GenericChart GetChronologerDeltaPlotKernelPDF(this CellLineResults cellLine, Kernels kernel = Kernels.Gaussian)
    {
        var individualFiles = cellLine.Results
            .Where(p => false.FdrPlotSelector().Contains(p.Condition))
            .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
            .Select(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile)
            .ToList();
        var chronologer = individualFiles
            .SelectMany(p => p.Where(m => m.ChronologerPrediction != 0 && m.PeptideModSeq != "" ))
            .Select(p => (p.ChronologerPrediction, p.PercentHI, p.IsChimeric, p.DeltaChronologerRT))
            .ToList();

        
        var chimericSamples = chronologer.Where(p => p.IsChimeric)
            .Select(p => p.DeltaChronologerRT)
            .ToList();
        var nonChimericSamples = chronologer.Where(p => !p.IsChimeric)
            .Select(p => p.DeltaChronologerRT)
            .ToList();


        nonChimericSamples = nonChimericSamples.OrderBy(p => p).ToList();
        chimericSamples = chimericSamples.OrderBy(p => p).ToList();
        var chart = Chart.Combine(new[]
            {
                GenericPlots.KernelDensityPlot(chimericSamples, "Chimeric", "Delta RT", "Probability", 0.3),
                GenericPlots.KernelDensityPlot(nonChimericSamples, "Non-Chimeric", "Delta RT", "Probability", 0.3),
                //Chart.Line<double, double, string>(new [] {0.0, 0},
                //    new [] {0.0, 0.35},
                //    LineColor: new Optional<Color>(Color.fromKeyword(ColorKeyword.DarkGray), true), 
                //    LineDash: StyleParam.DrawingStyle.Dash, Opacity:0.5)
            })
            .WithTitle($" {cellLine.CellLine} Chronologer Delta Kernel Density")
            .WithSize(400, 400)
            .WithXAxisStyle(Title.init("Delta Chronologer"))
            .WithYAxisStyle(Title.init("Density"))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend);

        return chart;
    }

    public static void PlotChronologerDeltaRangePlot(this CellLineResults cellLine)
    {
        var chart = cellLine.GetChronologerDeltaRangePlot();
        //GenericChartExtensions.Show(chart);
        chart.SaveInCellLineOnly(cellLine, $"{FileIdentifiers.ChronologerDeltaRange}_{cellLine.CellLine}", 1200, 800);
    }

    internal static GenericChart.GenericChart GetChronologerDeltaRangePlot(this CellLineResults cellLine)
    {
        // Nested dictionary where first is split on chimeric or not and second is split by RT rounded to 0.1 minute
        var chronologerResults = cellLine.Results
            .Where(p => false.FdrPlotSelector().Contains(p.Condition))
            .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
            .Select(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile)
            .SelectMany(p => p.Where(m => m.ChronologerPrediction != 0 && m.PeptideModSeq != ""))
            .GroupBy(p => p.IsChimeric)
            .ToDictionary(p => p.Key, p => p.GroupBy(m => m.RetentionTime.Round(1))
                .OrderBy(n => n.Key)
                .ToDictionary(n => n.Key, n => n.ToArray()));

        List<(double RT, double Mean, double Lower, double Upper)> chimericResults =
            (from kvp in chronologerResults[true]
                let meanStd = kvp.Value.Select(p => p.ChronologerPrediction).MeanStandardDeviation()
                select (kvp.Key, meanStd.Mean, double.IsNaN(meanStd.StandardDeviation) ? 0 : meanStd.Mean - meanStd.StandardDeviation, double.IsNaN(meanStd.StandardDeviation) ? 0 : meanStd.Mean + meanStd.StandardDeviation))
            .ToList();

        List<(double RT, double Mean, double Lower, double Upper)> nonChimericResults =
            (from kvp in chronologerResults[false]
                let meanStd = kvp.Value.Select(p => p.ChronologerPrediction).MeanStandardDeviation()
                select (kvp.Key, meanStd.Mean, double.IsNaN(meanStd.StandardDeviation) ? 0 : meanStd.Mean - meanStd.StandardDeviation, double.IsNaN(meanStd.StandardDeviation) ? 0 : meanStd.Mean + meanStd.StandardDeviation))
            .ToList();


        int averageStep = 21;
        var chart = Chart.Combine(new[]
        {
            Chart.Range<double, double, string>(chimericResults.Select(p => p.RT).MovingAverageZeroFill(averageStep), chimericResults.Select(p => p.Mean).MovingAverageZeroFill(averageStep),
                chimericResults.Select(p => p.Upper).MovingAverageZeroFill(averageStep), chimericResults.Select(p => p.Lower).MovingAverageZeroFill(averageStep),
                StyleParam.Mode.Lines, LineWidth:4, RangeColor: Color.fromString("rgba(120, 0, 128, 0.6)"), LineColor: "Chimeric".ConvertConditionToColor(), 
                LowerLine: Line.init(Color: Color.fromString("rgba(120, 0, 128, 0.4)"), Width: 2),
                UpperLine:Line.init(Color: Color.fromString("rgba(120, 0, 128, 0.4)"), Width: 2)),
            Chart.Range<double, double, string>(nonChimericResults.Select(p => p.RT).MovingAverageZeroFill(averageStep), nonChimericResults.Select(p => p.Mean).MovingAverageZeroFill(averageStep),
                nonChimericResults.Select(p => p.Upper).MovingAverageZeroFill(averageStep), nonChimericResults.Select(p => p.Lower).MovingAverageZeroFill(averageStep),
                StyleParam.Mode.Lines, LineWidth:4, RangeColor: Color.fromString("rgba(221, 160, 221, 0.6)"), LineColor: "Non-Chimeric".ConvertConditionToColor(),
                LowerLine: Line.init(Color: Color.fromString("rgba(221, 160, 221, 0.4)"), Width: 2),
                UpperLine:Line.init(Color: Color.fromString("rgba(221, 160, 221, 0.4)"), Width: 2)),
        })
            .WithTitle($"{cellLine.CellLine} Chronologer Delta Range")
            .WithXAxisStyle(Title.init("Retention Time"))
            .WithYAxisStyle(Title.init("Chronologer Prediction"))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(1200, 1000);

        return chart;
    }

    public static void PlotChronologerDeltaPlotBoxAndWhisker(this CellLineResults cellLine)
    {
        cellLine.GetChronologerDeltaPlotBoxAndWhisker()
            .SaveInCellLineOnly(cellLine, $"{FileIdentifiers.ChronologerDeltaBoxAndWhiskers}_{cellLine.CellLine}", 1200, 800);
    }

    internal static GenericChart.GenericChart GetChronologerDeltaPlotBoxAndWhisker(this CellLineResults cellLine)
    {
        var chronologerResults = cellLine.Results
            .Where(p => false.FdrPlotSelector().Contains(p.Condition))
            .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
            .Select(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile)
            .SelectMany(p => p.Where(m => m.ChronologerPrediction != 0 && m.PeptideModSeq != ""))
            .ToList();

        List<int> retentionTimes = new();
        List<double> chimericYValues = new();
        List<double> nonChimericYValues = new();

        foreach (var result in chronologerResults)
        {
            retentionTimes.Add((int)result.RetentionTime.Round(-1));
            if (result.IsChimeric)
                chimericYValues.Add(result.ChronologerPrediction);
            else
                nonChimericYValues.Add(result.ChronologerPrediction);
        }

        var chart = Chart.Combine(new[]
            {
                Chart.BoxPlot<int, double, string>(retentionTimes, chimericYValues,
                    "Chimeric", MarkerColor:"Chimeric".ConvertConditionToColor() ),
                Chart.BoxPlot<int, double, string>(retentionTimes.Select(p => p+5).ToArray(), nonChimericYValues,
                    "Non-Chimeric", MarkerColor: "Non-Chimeric".ConvertConditionToColor())
            })
            .WithTitle($"{cellLine.CellLine} Chronologer Predicted HI vs Retention Time (1% Peptides)")
            .WithXAxisStyle(Title.init("Retention Time"))
            .WithYAxisStyle(Title.init("Chronologer Prediction"))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(1200, 1000);
        return chart;
    }

    public static void PlotAccuracyByModificationType(this CellLineResults cellLine)
    {
        var plot = cellLine.GetAccuracyByModTypePlot_2();
        GenericChartExtensions.Show(plot);
        //plot.SaveInCellLineOnly(cellLine, $"{FileIdentifiers.AccuracyByModType}_{cellLine.CellLine}", 800, 800);
    }

    internal static GenericChart.GenericChart GetAccuracyByModTypePlot(this CellLineResults cellLine)
    {
        var chronologerResults = cellLine.Results
            .Where(p => false.FdrPlotSelector().Contains(p.Condition))
            .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
            .Select(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile)
            .SelectMany(p => p.Where(m => m.ChronologerPrediction != 0 && m.PeptideModSeq != ""))
            .ToList();

        var mods = chronologerResults.SelectMany(p => p.Modifications)
            .Distinct()
            .OrderBy(p => p)
            .ToDictionary(p => p, p => new List<RetentionTimePredictionEntry>());

        chronologerResults.ForEach(p =>
        {
            foreach (var mod in p.Modifications)
                mods[mod].Add(p);
        });


        List<string> xValues = new();
        List<double> yValuesChronologerErrorRTChimeric = new();
        List<double> yValuesChronologerErrorRTNonChimeric = new();
        foreach (var modType in mods)
        {
            foreach (var result in modType.Value)
            {
                if (result.IsChimeric)
                    yValuesChronologerErrorRTChimeric.Add(result.DeltaChronologerRT);
                else
                    yValuesChronologerErrorRTNonChimeric.Add(result.DeltaChronologerRT);
                xValues.Add(modType.Key);
            }
        }

        var chimericChronErrorPlot = GenericPlots.Histogram2D(xValues, yValuesChronologerErrorRTChimeric, "Chimeric",
            "Modification Type", "Chronologer Error");

        var nonChimericChronErrorPlot = GenericPlots.Histogram2D(xValues, yValuesChronologerErrorRTNonChimeric, "Non-Chimeric",
                       "Modification Type", "Chronologer Error");

        var chronErrorPlot = Chart.Grid(new []{chimericChronErrorPlot, nonChimericChronErrorPlot}, 1, 2)
            .WithTitle($"{cellLine.CellLine} Chronologer Error by Modification Type")
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(1200, 600);

        return chronErrorPlot;
    }

    internal static GenericChart.GenericChart GetAccuracyByModTypePlot_2(this CellLineResults cellLine)
    {
        var resultFiles = cellLine.Results
            .Where(p => false.FdrPlotSelector().Contains(p.Condition))
            .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
            .Select(p => (((MetaMorpheusResult)p).RetentionTimePredictionFile, cellLine.MaximumChimeraEstimationFile,
                ((MetaMorpheusResult)p).ChimeraBreakdownFile))
            .First();

        var chronologerResultsDict = resultFiles.Item1
            .Where(m => m.ChronologerPrediction != 0 && m.PeptideModSeq != "")
            .GroupBy(p => p.FileNameWithoutExtension.ConvertFileName())
            .ToDictionary(p => p.Key, p => p.ToList());

        var maxChimeraResultsDict = resultFiles.Item2?
            .GroupBy(p => p.FileName.ConvertFileName())
            .ToDictionary(p => p.Key, p => p.ToList());

        var chimeraBreakdownDict = resultFiles.Item3
            .Where(p => p.Type == ResultType.Peptide)
            .GroupBy(p => p.FileName.ConvertFileName())
            .ToDictionary(p => p.Key, p => p.ToList());

        var mods = chronologerResultsDict.Values.SelectMany(m => m.SelectMany(p => p.Modifications))
            .Distinct()
            .OrderBy(p => p)
            .ToDictionary(p => p, p => new List<RetentionTimePredictionEntry>());
        chronologerResultsDict.SelectMany(p => p.Value).ForEach(p =>
        {
            foreach (var mod in p.Modifications)
                mods[mod].Add(p);
        });


        // TODO Find accuracy of chronologer predicted peptides as a function of modification type
        // accuracy will be calculated by T/D ratio, chronologer RT accuracy, and decon RT accuracy for chimeric and nonchimeric identifications

        List<string> chronologerErrorChimericXValues = new();
        List<string> chronologerErrorNonChimericXValues = new();
        List<double> chronologerErrorRTChimericyValues = new();
        List<double> chronologerErrorRTNonChimericYValues = new();

        List<string> tdRationChimericXValues = new();
        List<string> tdRationNonChimericXValues = new();
        List<double> tDRatioChimericYValues = new();
        List<double> tDRatioNonChimericYValues = new();

        List<string> deconRTAccuracyChimericXValues = new();
        List<string> deconRTAccuracyNonChimericXValues = new();
        List<double> deconRTAccuracyChimericYValues = new();
        List<double> deconRTAccuracyNonChimericYValues = new();

        foreach (var modType in mods)
        {
            foreach (var result in modType.Value) 
            {
                var breakdown = chimeraBreakdownDict[result.FileNameWithoutExtension.ConvertFileName()]
                    .FirstOrDefault(p =>  Math.Abs(p.Ms2ScanNumber - result.ScanNumber) < 0.001);
                var rtRecord = maxChimeraResultsDict?[result.FileNameWithoutExtension.ConvertFileName()]
                    .FirstOrDefault(p => Math.Abs(p.Ms2ScanNumber - result.ScanNumber) < 0.001);
                if (result.IsChimeric)
                {
                    chronologerErrorRTChimericyValues.Add(result.DeltaChronologerRT);
                    chronologerErrorChimericXValues.Add(modType.Key);
                    if (rtRecord is not null)
                    {
                        deconRTAccuracyChimericYValues.AddRange(rtRecord.OnePercentRetentionTimeShift_MetaMorpheus_Peptides);
                        deconRTAccuracyChimericXValues.AddRange(Enumerable.Repeat(modType.Key, rtRecord.OnePercentRetentionTimeShift_MetaMorpheus_Peptides.Length));
                    }

                    if (breakdown is not null)
                    {
                        tDRatioChimericYValues.Add(breakdown.TargetCount / (double)breakdown.IdsPerSpectra);
                        tdRationChimericXValues.Add(modType.Key);
                    }
                }
                else
                {
                    chronologerErrorRTNonChimericYValues.Add(result.DeltaChronologerRT);
                    chronologerErrorNonChimericXValues.Add(modType.Key);
                    if (rtRecord is not null)
                    {
                        deconRTAccuracyNonChimericYValues.AddRange(rtRecord.OnePercentRetentionTimeShift_MetaMorpheus_Peptides);
                        deconRTAccuracyNonChimericXValues.AddRange(Enumerable.Repeat(modType.Key, rtRecord.OnePercentRetentionTimeShift_MetaMorpheus_Peptides.Length));
                    }
                    if (breakdown is not null)
                    {
                        tDRatioNonChimericYValues.Add(breakdown.TargetCount / (double)breakdown.IdsPerSpectra);
                        tdRationNonChimericXValues.Add(modType.Key);
                    }
                }

            }
        }

        var chimericChronErrorPlot = GenericPlots.Histogram2D(chronologerErrorChimericXValues, chronologerErrorRTChimericyValues, "Chimeric",
                       "Chimeric Modification Type", "Chronologer Error", true);
        var nonChimericChronErrorPlot = GenericPlots.Histogram2D(chronologerErrorNonChimericXValues, chronologerErrorRTNonChimericYValues, "Non-Chimeric",
                                  "Modification Type", "Chronologer Error", true);
        var chimericTDRatioPlot = GenericPlots.Histogram2D(tdRationChimericXValues, tDRatioChimericYValues, "Chimeric",
                       "Chimeric Modification Type", "T/D Ratio", true);
        var nonChimericTDRatioPlot = GenericPlots.Histogram2D(tdRationNonChimericXValues, tDRatioNonChimericYValues, "Non-Chimeric",
                                             "Modification Type", "T/D Ratio", true);
        var chimericDeconRTPlot = GenericPlots.Histogram2D(deconRTAccuracyChimericXValues, deconRTAccuracyChimericYValues, "Chimeric",
            "Chimeric Modification Type", "Decon RT Accuracy", true);
        var nonChimericDeconRTPlot = GenericPlots.Histogram2D(deconRTAccuracyNonChimericXValues, deconRTAccuracyNonChimericYValues, "Non-Chimeric",
            "Modification Type", "Decon RT Accuracy", true);

        var chronErrorPlot = Chart.Grid(new[]
        {
            chimericChronErrorPlot, nonChimericChronErrorPlot,
            //chimericTDRatioPlot, nonChimericTDRatioPlot,
            chimericDeconRTPlot, nonChimericDeconRTPlot

        }, 2, 2)
            .WithTitle($"{cellLine.CellLine} Accuracy by Modification Type")
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(1000, 1000);


        //var chimericChronErrorPlot = PlotlyBase.Histogram2D(chronologerErrorChimericXValues, chronologerErrorRTChimericyValues, "Chimeric",
        //    "Modification Type", "Chronologer Error");
        //var nonChimericChronErrorPlot = PlotlyBase.Histogram2D(chronologerErrorNonChimericXValues, chronologerErrorRTNonChimericYValues, "Non-Chimeric",
        //    "Modification Type", "Chronologer Error");
        //var chimericTDRatioPlot = PlotlyBase.Histogram2D(tdRationChimericXValues, tDRatioChimericYValues, "Chimeric",
        //    "Modification Type", "T/D Ratio");
        //var nonChimericTDRatioPlot = PlotlyBase.Histogram2D(tdRationNonChimericXValues, tDRatioNonChimericYValues, "Non-Chimeric",
        //    "Modification Type", "T/D Ratio");
        //var chimericDeconRTPlot = PlotlyBase.Histogram2D(deconRTAccuracyChimericXValues, deconRTAccuracyChimericYValues, "Chimeric",
        //    "Modification Type", "Decon RT Accuracy");
        //var nonChimericDeconRTPlot = PlotlyBase.Histogram2D(deconRTAccuracyNonChimericXValues, deconRTAccuracyNonChimericYValues, "Non-Chimeric",
        //    "Modification Type", "Decon RT Accuracy");

        //var chronErrorPlot = Chart.Grid(new[]
        //{
        //    Chart.Combine(new[] { chimericChronErrorPlot, nonChimericChronErrorPlot }),
        //    Chart.Combine(new[] { chimericTDRatioPlot, nonChimericTDRatioPlot }),
        //    Chart.Combine(new[] { chimericDeconRTPlot, nonChimericDeconRTPlot })

        //}, 3, 1)
        //    .WithTitle($"{cellLine.CellLine} Accuracy by Modification Type")
        //    .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
        //    .WithSize(1200, 1800);



        return chronErrorPlot;
    }

    #endregion

    #region From Deconv Results

    public static void PlotAverageRetentionTimeShiftPlotKernelPdf(this CellLineResults cellLine, bool useRawFiles = true,
        Kernels kernel = Kernels.Gaussian)
    {
        var file = useRawFiles ? cellLine.MaximumChimeraEstimationFile : cellLine.MaximumChimeraEstimationCalibAveragedFile;
        string suffix = useRawFiles ? "" : "_Hybrid";
        if (file is null)
            return;


        var mmPsm = file.GetAverageRetentionTimeShiftPlotKernelPdf(Software.MetaMorpheus, ResultType.Psm, false);
        string exportName = $"{FileIdentifiers.RetentionTimeShift_MM}_{cellLine.CellLine}_Psms{suffix}";
        mmPsm?.SaveInCellLineOnly(cellLine, exportName, 600, 600);

        var mmOnePercentPsm = file.GetAverageRetentionTimeShiftPlotKernelPdf(Software.MetaMorpheus, ResultType.Psm, true);
        exportName = $"{FileIdentifiers.RetentionTimeShift_MM}_{cellLine.CellLine}_1%Psms{suffix}";
        mmOnePercentPsm?.SaveInCellLineOnly(cellLine, exportName, 600, 600);


        var mmPep = file.GetAverageRetentionTimeShiftPlotKernelPdf(Software.MetaMorpheus, ResultType.Peptide, false);
        exportName = $"{FileIdentifiers.RetentionTimeShift_MM}_{cellLine.CellLine}_Peptides{suffix}";
        mmPep?.SaveInCellLineOnly(cellLine, exportName, 600, 600);

        var mmOnePercentPep = file.GetAverageRetentionTimeShiftPlotKernelPdf(Software.MetaMorpheus, ResultType.Peptide, true);
        exportName = $"{FileIdentifiers.RetentionTimeShift_MM}_{cellLine.CellLine}_1%Peptides{suffix}";
        mmOnePercentPep?.SaveInCellLineOnly(cellLine, exportName, 600, 600);


        var fragPsm = file.GetAverageRetentionTimeShiftPlotKernelPdf(Software.Unspecified, ResultType.Psm, false);
        exportName = $"{FileIdentifiers.RetentionTimeShift_Fragger}_{cellLine.CellLine}_Psms{suffix}";
        fragPsm?.SaveInCellLineOnly(cellLine, exportName, 600, 600);

        var fragOnePercentPsm = file.GetAverageRetentionTimeShiftPlotKernelPdf(Software.Unspecified, ResultType.Psm, true);
        exportName = $"{FileIdentifiers.RetentionTimeShift_Fragger}_{cellLine.CellLine}_1%Psms{suffix}";
        fragOnePercentPsm?.SaveInCellLineOnly(cellLine, exportName, 600, 600);


        var stacked = Chart.Grid(new[]
        {
            mmPsm, mmOnePercentPsm, mmPep, mmOnePercentPep, fragPsm, fragOnePercentPsm
        }, 3, 2)
        .WithSize(1200, 600 * 3)
            .WithTitle($"{cellLine.CellLine} Average Retention Time Shift Kernel Density")
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
        exportName = $"AllResults_{FileIdentifiers.RetentionTimeShift_Stacked}_{cellLine.CellLine}{suffix}";
        stacked.SaveInCellLineAndMann11Directories(cellLine, exportName, 1200, 600 * 3);
    }

    internal static GenericChart.GenericChart? GetAverageRetentionTimeShiftPlotKernelPdf(this MaximumChimeraEstimationFile file, Software software = Software.MetaMorpheus,
        ResultType resultType = ResultType.Psm, bool onePercent = true, Kernels kernel = Kernels.Gaussian)
    {
        int min = -2;
        int max = 2;
        List<double> chimeric;
        List<double> nonChimeric;
        string chimericLabel;
        string nonChimericLabel;
        string titleLabel;
        string softwareLabel = software == Software.MetaMorpheus ? "MetaMorpheus" : "MsFraggerDDA+";
        switch (resultType)
        {
            case ResultType.Psm:
                if (software == Software.MetaMorpheus)
                {
                    var trimmedSamples = file.Where(p => p.PsmCount_MetaMorpheus != 0).ToList();
                    if (onePercent)
                    {
                        chimeric = trimmedSamples.Where(p => p.OnePercentRetentionTimeShift_MetaMorpheus_PSMs.Any() && p.IsChimeric)
                            .SelectMany(p => p.OnePercentRetentionTimeShift_MetaMorpheus_PSMs)
                            .OrderBy(p => p).ToList();
                        nonChimeric = trimmedSamples.Where(p => p.OnePercentRetentionTimeShift_MetaMorpheus_PSMs.Any() && !p.IsChimeric)
                            .SelectMany(p => p.OnePercentRetentionTimeShift_MetaMorpheus_PSMs)
                            .OrderBy(p => p).ToList();
                        chimericLabel = "Chimeric 1% Psms";
                        nonChimericLabel = "Non-Chimeric 1% Psms";
                        titleLabel = "1% Psms";
                    }
                    else
                    {
                        chimeric = trimmedSamples.Where(p => p.RetentionTimeShift_MetaMorpheus_PSMs.Any() && p.IsChimeric)
                            .SelectMany(p => p.RetentionTimeShift_MetaMorpheus_PSMs)
                            .OrderBy(p => p).ToList();
                        nonChimeric = trimmedSamples.Where(p => p.RetentionTimeShift_MetaMorpheus_PSMs.Any() && !p.IsChimeric)
                            .SelectMany(p => p.RetentionTimeShift_MetaMorpheus_PSMs)
                            .OrderBy(p => p).ToList();
                        chimericLabel = "Chimeric All Psms";
                        nonChimericLabel = "Non-Chimeric All Psms";
                        titleLabel = "All Psms";
                    }
                }
                else
                {
                    var trimmedSamples = file.Where(p => p.PsmCount_Fragger != 0).ToList();
                    if (onePercent)
                    {
                        chimeric = trimmedSamples.Where(p => p.OnePercentRetentionTimeShift_Fragger_PSMs.Any() && p.IsChimeric)
                            .SelectMany(p => p.OnePercentRetentionTimeShift_Fragger_PSMs)
                            .OrderBy(p => p).ToList();
                        nonChimeric = trimmedSamples.Where(p => p.OnePercentRetentionTimeShift_Fragger_PSMs.Any() && !p.IsChimeric)
                            .SelectMany(p => p.OnePercentRetentionTimeShift_Fragger_PSMs)
                            .OrderBy(p => p).ToList();
                        chimericLabel = "Chimeric 1% Psms";
                        nonChimericLabel = "Non-Chimeric 1% Psms";
                        titleLabel = "1% Psms";
                    }
                    else
                    {
                        chimeric = trimmedSamples.Where(p => p.RetentionTimeShift_Fragger_PSMs.Any() && p.IsChimeric)
                            .SelectMany(p => p.RetentionTimeShift_Fragger_PSMs)
                            .OrderBy(p => p).ToList();
                        nonChimeric = trimmedSamples.Where(p => p.RetentionTimeShift_Fragger_PSMs.Any() && !p.IsChimeric)
                            .SelectMany(p => p.RetentionTimeShift_Fragger_PSMs)
                            .OrderBy(p => p).ToList();
                        chimericLabel = "Chimeric All Psms";
                        nonChimericLabel = "Non-Chimeric All Psms";
                        titleLabel = "All Psms";
                    }
                }
                break;
            case ResultType.Peptide:
                if (software == Software.MetaMorpheus)
                {
                    var trimmedSamples = file.Where(p => p.PeptideCount_MetaMorpheus != 0).ToList();
                    if (onePercent)
                    {
                        chimeric = trimmedSamples.Where(p =>
                                p.OnePercentRetentionTimeShift_MetaMorpheus_Peptides.Any() && p.IsChimeric)
                            .SelectMany(p => p.OnePercentRetentionTimeShift_MetaMorpheus_Peptides)
                            .OrderBy(p => p).ToList();
                        nonChimeric = trimmedSamples.Where(p =>
                                p.OnePercentRetentionTimeShift_MetaMorpheus_Peptides.Any() && !p.IsChimeric)
                            .SelectMany(p => p.OnePercentRetentionTimeShift_MetaMorpheus_Peptides)
                            .OrderBy(p => p).ToList();
                        chimericLabel = "Chimeric 1% Peptides";
                        nonChimericLabel = "Non-Chimeric 1% Peptides";
                        titleLabel = "1% Peptides";
                    }
                    else
                    {
                        chimeric = trimmedSamples.Where(p =>
                                p.RetentionTimeShift_MetaMorpheus_Peptides.Any() && p.IsChimeric)
                            .SelectMany(p => p.RetentionTimeShift_MetaMorpheus_Peptides)
                            .OrderBy(p => p).ToList();
                        nonChimeric = trimmedSamples.Where(p =>
                                p.RetentionTimeShift_MetaMorpheus_Peptides.Any() && !p.IsChimeric)
                            .SelectMany(p => p.RetentionTimeShift_MetaMorpheus_Peptides)
                            .OrderBy(p => p).ToList();
                        chimericLabel = "Chimeric All Peptides";
                        nonChimericLabel = "Non-Chimeric All Peptides";
                        titleLabel = "All Peptides";
                    }
                }
                else
                    return null;
                break;
            case ResultType.Protein:
            default:
                return null;
        }

        chimeric = chimeric.Where(p => p >= min && p <= max).ToList();
        nonChimeric = nonChimeric.Where(p => p >= min && p <= max).ToList();
        var kernelPlot = Chart.Combine(new[]
            {
                GenericPlots.KernelDensityPlot(nonChimeric, nonChimericLabel, "RT Shift", "Density", 0.05, kernel),
                GenericPlots.KernelDensityPlot(chimeric, chimericLabel, "RT Shift", "Density", 0.05, kernel),
                Chart.Line<double, double, string>(new[] { 0.0, 0 }, new[] { 0.0, 0.35 },
                    LineColor: new Optional<Color>(Color.fromKeyword(ColorKeyword.DarkGray), true),
                    LineDash: StyleParam.DrawingStyle.Dash, Opacity: 0.5)
            })
            .WithTitle($"{softwareLabel} {file.First().CellLine} Average {titleLabel} RT Shift")
            .WithSize(800, 800)
            .WithXAxisStyle(Title.init("RT Shift"), MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(-5, 5)))
            .WithYAxisStyle(Title.init("Density"))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend);

        return kernelPlot;
    }

    public static void PlotAverageRetentionTimeShiftPlotHistogram(this CellLineResults cellLine, bool useRawFiles = true)
    {
        var file = useRawFiles ? cellLine.MaximumChimeraEstimationFile : cellLine.MaximumChimeraEstimationCalibAveragedFile;
        string suffix = useRawFiles ? "" : "_Hybrid";
        if (file is null)
            return;

        var mmPsmHist = file.GetAverageRetentionTimeShiftHistogram(Software.MetaMorpheus, ResultType.Psm, false);
        string exportName = $"{FileIdentifiers.RetentionTimeShiftHistogram_MM}_{cellLine.CellLine}_Psms{suffix}";
        mmPsmHist?.SaveInCellLineOnly(cellLine, exportName, 600, 600);

        var mmOnePercentPsmHist = file.GetAverageRetentionTimeShiftHistogram(Software.MetaMorpheus, ResultType.Psm, true);
        exportName = $"{FileIdentifiers.RetentionTimeShiftHistogram_MM}_{cellLine.CellLine}_1%Psms{suffix}";
        mmOnePercentPsmHist?.SaveInCellLineOnly(cellLine, exportName, 600, 600);


        var mmPepHist = file.GetAverageRetentionTimeShiftHistogram(Software.MetaMorpheus, ResultType.Peptide, false);
        exportName = $"{FileIdentifiers.RetentionTimeShiftHistogram_MM}_{cellLine.CellLine}_Peptides{suffix}";
        mmPepHist?.SaveInCellLineOnly(cellLine, exportName, 600, 600);

        var mmOnePercentPepHist = file.GetAverageRetentionTimeShiftHistogram(Software.MetaMorpheus, ResultType.Peptide, true);
        exportName = $"{FileIdentifiers.RetentionTimeShiftHistogram_MM}_{cellLine.CellLine}_1%Peptides{suffix}";
        mmOnePercentPepHist?.SaveInCellLineOnly(cellLine, exportName, 600, 600);


        var fragPsmHist = file.GetAverageRetentionTimeShiftHistogram(Software.Unspecified, ResultType.Psm, false);
        exportName = $"{FileIdentifiers.RetentionTimeShiftHistogram_Fragger}_{cellLine.CellLine}_Psms{suffix}";
        fragPsmHist?.SaveInCellLineOnly(cellLine, exportName, 600, 600);

        var fragOnePercentPsmHist = file.GetAverageRetentionTimeShiftHistogram(Software.Unspecified, ResultType.Psm, true);
        exportName = $"{FileIdentifiers.RetentionTimeShiftHistogram_Fragger}_{cellLine.CellLine}_1%Psms{suffix}";
        fragOnePercentPsmHist?.SaveInCellLineOnly(cellLine, exportName, 600, 600);


        var stacked = Chart.Grid(new[]
        {
            mmPsmHist, mmOnePercentPsmHist, mmPepHist, mmOnePercentPepHist, fragPsmHist, fragOnePercentPsmHist
        }, 3, 2)
        .WithSize(1200, 1800)
            .WithTitle($"{cellLine.CellLine} Average Retention Time Shift Histogram")
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
        exportName = $"AllResults_{FileIdentifiers.RetentionTimeShiftHistogram_Stacked}_{cellLine.CellLine}{suffix}";
        stacked.SaveInCellLineAndMann11Directories(cellLine, exportName, 1200, 600 * 3);
    }

    internal static GenericChart.GenericChart? GetAverageRetentionTimeShiftHistogram(
        this MaximumChimeraEstimationFile file, Software software = Software.MetaMorpheus,
        ResultType resultType = ResultType.Psm, bool onePercent = true, Kernels kernel = Kernels.Gaussian)
    {
        int min = -2;
        int max = 2;
        List<double> chimeric;
        List<double> nonChimeric;
        string chimericLabel;
        string nonChimericLabel;
        string titleLabel;
        string softwareLabel = software == Software.MetaMorpheus ? "MetaMorpheus" : "MsFraggerDDA+";
        switch (resultType)
        {
            case ResultType.Psm:
                if (software == Software.MetaMorpheus)
                {
                    var trimmedSamples = file.Where(p => p.PsmCount_MetaMorpheus != 0).ToList();
                    if (onePercent)
                    {
                        chimeric = trimmedSamples.Where(p => p.OnePercentRetentionTimeShift_MetaMorpheus_PSMs.Any() && p.IsChimeric)
                            .SelectMany(p => p.OnePercentRetentionTimeShift_MetaMorpheus_PSMs)
                            .OrderBy(p => p).ToList();
                        nonChimeric = trimmedSamples.Where(p => p.OnePercentRetentionTimeShift_MetaMorpheus_PSMs.Any() && !p.IsChimeric)
                            .SelectMany(p => p.OnePercentRetentionTimeShift_MetaMorpheus_PSMs)
                            .OrderBy(p => p).ToList();
                        chimericLabel = "Chimeric 1% Psms";
                        nonChimericLabel = "Non-Chimeric 1% Psms";
                        titleLabel = "1% Psms";
                    }
                    else
                    {
                        chimeric = trimmedSamples.Where(p => p.RetentionTimeShift_MetaMorpheus_PSMs.Any() && p.IsChimeric)
                            .SelectMany(p => p.RetentionTimeShift_MetaMorpheus_PSMs)
                            .OrderBy(p => p).ToList();
                        nonChimeric = trimmedSamples.Where(p => p.RetentionTimeShift_MetaMorpheus_PSMs.Any() && !p.IsChimeric)
                            .SelectMany(p => p.RetentionTimeShift_MetaMorpheus_PSMs)
                            .OrderBy(p => p).ToList();
                        chimericLabel = "Chimeric All Psms";
                        nonChimericLabel = "Non-Chimeric All Psms";
                        titleLabel = "All Psms";
                    }
                }
                else
                {
                    var trimmedSamples = file.Where(p => p.PsmCount_Fragger != 0).ToList();
                    if (onePercent)
                    {
                        chimeric = trimmedSamples.Where(p => p.OnePercentRetentionTimeShift_Fragger_PSMs.Any() && p.IsChimeric)
                            .SelectMany(p => p.OnePercentRetentionTimeShift_Fragger_PSMs)
                            .OrderBy(p => p).ToList();
                        nonChimeric = trimmedSamples.Where(p => p.OnePercentRetentionTimeShift_Fragger_PSMs.Any() && !p.IsChimeric)
                            .SelectMany(p => p.OnePercentRetentionTimeShift_Fragger_PSMs)
                            .OrderBy(p => p).ToList();
                        chimericLabel = "Chimeric 1% Psms";
                        nonChimericLabel = "Non-Chimeric 1% Psms";
                        titleLabel = "1% Psms";
                    }
                    else
                    {
                        chimeric = trimmedSamples.Where(p => p.RetentionTimeShift_Fragger_PSMs.Any() && p.IsChimeric)
                            .SelectMany(p => p.RetentionTimeShift_Fragger_PSMs)
                            .OrderBy(p => p).ToList();
                        nonChimeric = trimmedSamples.Where(p => p.RetentionTimeShift_Fragger_PSMs.Any() && !p.IsChimeric)
                            .SelectMany(p => p.RetentionTimeShift_Fragger_PSMs)
                            .OrderBy(p => p).ToList();
                        chimericLabel = "Chimeric All Psms";
                        nonChimericLabel = "Non-Chimeric All Psms";
                        titleLabel = "All Psms";
                    }
                }
                break;
            case ResultType.Peptide:
                if (software == Software.MetaMorpheus)
                {
                    var trimmedSamples = file.Where(p => p.PeptideCount_MetaMorpheus != 0).ToList();
                    if (onePercent)
                    {
                        chimeric = trimmedSamples.Where(p =>
                                p.OnePercentRetentionTimeShift_MetaMorpheus_Peptides.Any() && p.IsChimeric)
                            .SelectMany(p => p.OnePercentRetentionTimeShift_MetaMorpheus_Peptides)
                            .OrderBy(p => p).ToList();
                        nonChimeric = trimmedSamples.Where(p =>
                                p.OnePercentRetentionTimeShift_MetaMorpheus_Peptides.Any() && !p.IsChimeric)
                            .SelectMany(p => p.OnePercentRetentionTimeShift_MetaMorpheus_Peptides)
                            .OrderBy(p => p).ToList();
                        chimericLabel = "Chimeric 1% Peptides";
                        nonChimericLabel = "Non-Chimeric 1% Peptides";
                        titleLabel = "1% Peptides";
                    }
                    else
                    {
                        chimeric = trimmedSamples.Where(p =>
                                p.RetentionTimeShift_MetaMorpheus_Peptides.Any() && p.IsChimeric)
                            .SelectMany(p => p.RetentionTimeShift_MetaMorpheus_Peptides)
                            .OrderBy(p => p).ToList();
                        nonChimeric = trimmedSamples.Where(p =>
                                p.RetentionTimeShift_MetaMorpheus_Peptides.Any() && !p.IsChimeric)
                            .SelectMany(p => p.RetentionTimeShift_MetaMorpheus_Peptides)
                            .OrderBy(p => p).ToList();
                        chimericLabel = "Chimeric All Peptides";
                        nonChimericLabel = "Non-Chimeric All Peptides";
                        titleLabel = "All Peptides";
                    }
                }
                else
                    return null;
                break;
            case ResultType.Protein:
            default:
                return null;
        }

        chimeric = chimeric.Where(p => p >= min && p <= max).ToList();
        nonChimeric = nonChimeric.Where(p => p >= min && p <= max).ToList();
        var hist = Chart.Combine(new[]
            {
                Chart.Histogram<double, double, string>(chimeric,
                    MarkerColor: chimericLabel.ConvertConditionToColor()),
                Chart.Histogram<double, double, string>(nonChimeric,
                    MarkerColor: nonChimericLabel.ConvertConditionToColor())

            }).WithTitle($"{softwareLabel} {file.First().CellLine} Average {titleLabel} RT Shift")
            .WithSize(600, 600)
            .WithXAxisStyle(Title.init("RT Shift"), MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(-3, 3)))
            .WithYAxisStyle(Title.init("Count"))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend);

        return hist;
    }


    public static void PlotAllRetentionTimeShiftPlots(this CellLineResults cellLine, bool useRawFiles = true, Kernels kernel = Kernels.Gaussian)
    {
        var file = useRawFiles ? cellLine.MaximumChimeraEstimationFile : cellLine.MaximumChimeraEstimationCalibAveragedFile;
        string suffix = useRawFiles ? "" : "_Hybrid";
        if (file is null)
            return;

        var mmPsmHist = file.GetAverageRetentionTimeShiftHistogram(Software.MetaMorpheus, ResultType.Psm, false);
        var mmOnePercentPsmHist = file.GetAverageRetentionTimeShiftHistogram(Software.MetaMorpheus, ResultType.Psm, true);
        var mmPepHist = file.GetAverageRetentionTimeShiftHistogram(Software.MetaMorpheus, ResultType.Peptide, false);
        var mmOnePercentPepHist = file.GetAverageRetentionTimeShiftHistogram(Software.MetaMorpheus, ResultType.Peptide, true);
        var fragPsmHist = file.GetAverageRetentionTimeShiftHistogram(Software.Unspecified, ResultType.Psm, false);
        var fragOnePercentPsmHist = file.GetAverageRetentionTimeShiftHistogram(Software.Unspecified, ResultType.Psm, true);
        var mmPsm = file.GetAverageRetentionTimeShiftPlotKernelPdf(Software.MetaMorpheus, ResultType.Psm, false);
        var mmOnePercentPsm = file.GetAverageRetentionTimeShiftPlotKernelPdf(Software.MetaMorpheus, ResultType.Psm, true);
        var mmPep = file.GetAverageRetentionTimeShiftPlotKernelPdf(Software.MetaMorpheus, ResultType.Peptide, false);
        var mmOnePercentPep = file.GetAverageRetentionTimeShiftPlotKernelPdf(Software.MetaMorpheus, ResultType.Peptide, true);
        var fragPsm = file.GetAverageRetentionTimeShiftPlotKernelPdf(Software.Unspecified, ResultType.Psm, false);
        var fragOnePercentPsm = file.GetAverageRetentionTimeShiftPlotKernelPdf(Software.Unspecified, ResultType.Psm, true);

        var stacked = Chart.Grid(new[]
        {
            mmPsmHist, mmPsm,
            mmOnePercentPsmHist,  mmOnePercentPsm,
            mmPepHist,mmPep,
            mmOnePercentPepHist,  mmOnePercentPep,
            fragPsmHist, fragPsm,
            fragOnePercentPsmHist, fragOnePercentPsm
        }, 6, 2)
            .WithSize(1200, 600 * 6)
            .WithTitle($"{cellLine.CellLine} Average Retention Time Shift")
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
        string exportName = $"AllResults_{FileIdentifiers.RetentionTimeShiftFullGrid_Stacked}_{cellLine.CellLine}{suffix}";
        stacked.SaveInCellLineAndMann11Directories(cellLine, exportName, 1200, 600 * 6);
    }

    #endregion

    #endregion

    #region Spectral Similarity

    public static void PlotCellLineSpectralSimilarity(this CellLineResults cellLine)
    {

        string outpath = Path.Combine(cellLine.GetChimeraPaperFigureDirectory(), $"{FileIdentifiers.SpectralAngleFigure}_{cellLine.CellLine}");
        var chart = cellLine.GetCellLineSpectralSimilarity();
        chart.SavePNG(outpath);
        outpath = Path.Combine(cellLine.FigureDirectory, $"{FileIdentifiers.SpectralAngleFigure}_{cellLine.CellLine}");
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
        bool isTopDown = cellLine.First().IsTopDown;
        var selector = isTopDown.ChimeraBreakdownSelector(cellLine.CellLine);
        var smLabel = Labels.GetSpectrumMatchLabel(isTopDown);
        var pepLabel = Labels.GetPeptideLabel(isTopDown);
        string smOutName = $"{FileIdentifiers.ChimeraBreakdownTargetDecoy}_{smLabel}_{cellLine.CellLine}";
        string pepOutName = $"{FileIdentifiers.ChimeraBreakdownTargetDecoy}_{pepLabel}_{cellLine.CellLine}";


        var results = cellLine.Results
            .Where(p => p is MetaMorpheusResult && selector.Contains(p.Condition))
            .SelectMany(p => ((MetaMorpheusResult)p).ChimeraBreakdownFile)
            .ToList();

        var psmChart =
            results.GetChimeraBreakDownStackedColumn_TargetDecoy(ResultType.Psm, cellLine.First().IsTopDown, absolute, out int width);
        string psmOutPath = Path.Combine(cellLine.GetChimeraPaperFigureDirectory(), smOutName);
        psmChart.SavePNG(psmOutPath, null, width, PlotlyBase.DefaultHeight);
        psmOutPath = Path.Combine(cellLine.FigureDirectory, smOutName);
        psmChart.SavePNG(psmOutPath, null, width, PlotlyBase.DefaultHeight);

        var peptideChart =
            results.GetChimeraBreakDownStackedColumn_TargetDecoy(ResultType.Peptide, cellLine.First().IsTopDown, absolute, out width);
        string peptideOutPath = Path.Combine(cellLine.GetChimeraPaperFigureDirectory(), pepOutName);
        peptideChart.SavePNG(peptideOutPath, null, width, PlotlyBase.DefaultHeight);
        peptideOutPath = Path.Combine(cellLine.FigureDirectory, pepOutName);
        peptideChart.SavePNG(peptideOutPath, null, width, PlotlyBase.DefaultHeight);
    }

    #endregion


    /// <summary>
    /// Stacked column: Plots the resultType of chimeric identifications as a function of the degree of chimericity
    /// </summary>
    /// <param name="cellLine"></param>
    public static void PlotCellLineChimeraBreakdown(this CellLineResults cellLine)
    {
        bool isTopDown = cellLine.First().IsTopDown;
        var selector = isTopDown.ChimeraBreakdownSelector(cellLine.CellLine);
        var smLabel = Labels.GetSpectrumMatchLabel(isTopDown);
        var pepLabel = Labels.GetPeptideLabel(isTopDown);
        string smOutName = $"{FileIdentifiers.ChimeraBreakdownComparisonFigure}_{smLabel}_{cellLine.CellLine}";
        string smAreaOutName = $"{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaFigure}_{smLabel}_{cellLine.CellLine}";
        string smAreaRelativeName = $"{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaPercentFigure}_{smLabel}_{cellLine.CellLine}";
        string pepOutName = $"{FileIdentifiers.ChimeraBreakdownComparisonFigure}_{pepLabel}_{cellLine.CellLine}";
        string pepAreaOutName = $"{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaFigure}_{pepLabel}_{cellLine.CellLine}";
        string pepAreaRelativeName = $"{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaPercentFigure}_{pepLabel}_{cellLine.CellLine}";

        // plot aggregated cell line result for specific targeted file from the selector
        var results = cellLine.Where(p => p is IChimeraBreakdownCompatible && selector.Contains(p.Condition))
            .SelectMany(p => ((IChimeraBreakdownCompatible)p).ChimeraBreakdownFile.Results).ToList();

        var psmChart = results.GetChimeraBreakDownStackedColumn(ResultType.Psm, cellLine.First().IsTopDown, out int width); 
        string psmOutPath = Path.Combine(cellLine.GetChimeraPaperFigureDirectory(), smOutName);
        psmChart.SavePNG(psmOutPath, null, width, PlotlyBase.DefaultHeight);

        var stackedAreaPsmChart = results.GetChimeraBreakDownStackedArea(ResultType.Psm, cellLine.First().IsTopDown, out width);
        string stackedAreaPsmOutPath = Path.Combine(cellLine.GetChimeraPaperFigureDirectory(), smAreaOutName);
        stackedAreaPsmChart.SavePNG(stackedAreaPsmOutPath, null, width, PlotlyBase.DefaultHeight);

        var statckedAreaPsmChartRelative = results.GetChimeraBreakDownStackedArea(ResultType.Psm, cellLine.First().IsTopDown, out width, true);
        string stackedAreaPsmRelativeOutPath = Path.Combine(cellLine.GetChimeraPaperFigureDirectory(), smAreaRelativeName);
        statckedAreaPsmChartRelative.SavePNG(stackedAreaPsmRelativeOutPath, null, width, PlotlyBase.DefaultHeight);

        if (results.All(p => p.Type == ResultType.Psm))
            goto IndividualResults;

        var peptideChart = results.GetChimeraBreakDownStackedColumn(ResultType.Peptide, cellLine.First().IsTopDown, out width);
        string peptideOutPath = Path.Combine(cellLine.GetChimeraPaperFigureDirectory(), pepOutName);
        peptideChart.SavePNG(peptideOutPath, null, width, PlotlyBase.DefaultHeight);

        var stackedAreaPeptideChart = results.GetChimeraBreakDownStackedArea(ResultType.Peptide, cellLine.First().IsTopDown, out width);
        string stackedAreaPeptideOutPath = Path.Combine(cellLine.GetChimeraPaperFigureDirectory(), pepAreaOutName);
        stackedAreaPeptideChart.SavePNG(stackedAreaPeptideOutPath, null, width, PlotlyBase.DefaultHeight);

        var stackedAreaPeptideChartRelative = results.GetChimeraBreakDownStackedArea(ResultType.Peptide, cellLine.First().IsTopDown, out width, true);
        string stackedAreaPeptideRelativeOutPath = Path.Combine(cellLine.GetChimeraPaperFigureDirectory(), pepAreaRelativeName);
        stackedAreaPeptideChartRelative.SavePNG(stackedAreaPeptideRelativeOutPath, null, width, PlotlyBase.DefaultHeight);


        IndividualResults:
        // plot individual result for each IChimeraBreakdownCompatible file resultType
        var compatibleResults = cellLine.Where(m => m is IChimeraBreakdownCompatible)
            .Cast<IChimeraBreakdownCompatible>().ToList();
        foreach (var file in compatibleResults)
        {
            results = file.ChimeraBreakdownFile.Results;

            psmChart = results.GetChimeraBreakDownStackedColumn(ResultType.Psm, cellLine.First().IsTopDown, out width, file.Condition);
            psmOutPath = Path.Combine(file.FigureDirectory, smOutName);
            psmChart.SavePNG(psmOutPath, null, width, PlotlyBase.DefaultHeight);


            stackedAreaPsmChart = results.GetChimeraBreakDownStackedArea(ResultType.Psm, cellLine.First().IsTopDown, out width, false, file.Condition);
            stackedAreaPsmOutPath = Path.Combine(file.FigureDirectory, smAreaOutName);
            stackedAreaPsmChart.SavePNG(stackedAreaPsmOutPath, null, width, PlotlyBase.DefaultHeight);


            statckedAreaPsmChartRelative = results.GetChimeraBreakDownStackedArea(ResultType.Psm, cellLine.First().IsTopDown, out width, true, file.Condition);
            stackedAreaPsmRelativeOutPath = Path.Combine(file.FigureDirectory, smAreaRelativeName);
            statckedAreaPsmChartRelative.SavePNG(stackedAreaPsmRelativeOutPath, null, width, PlotlyBase.DefaultHeight);


            if (results.All(p => p.Type == ResultType.Psm))
                continue;

            peptideChart = results.GetChimeraBreakDownStackedColumn(ResultType.Peptide, cellLine.First().IsTopDown, out width, file.Condition);
            peptideOutPath = Path.Combine(file.FigureDirectory, pepOutName);
            peptideChart.SavePNG(peptideOutPath, null, width, PlotlyBase.DefaultHeight);

            stackedAreaPeptideChart = results.GetChimeraBreakDownStackedArea(ResultType.Peptide, cellLine.First().IsTopDown, out width, false, file.Condition);
            stackedAreaPeptideOutPath = Path.Combine(file.FigureDirectory, pepAreaOutName);
            stackedAreaPeptideChart.SavePNG(stackedAreaPeptideOutPath, null, width, PlotlyBase.DefaultHeight);

            stackedAreaPeptideChartRelative = results.GetChimeraBreakDownStackedArea(ResultType.Peptide, cellLine.First().IsTopDown, out width, true, file.Condition);
            stackedAreaPeptideRelativeOutPath = Path.Combine(file.FigureDirectory, pepAreaRelativeName);
            stackedAreaPeptideChartRelative.SavePNG(stackedAreaPeptideRelativeOutPath, null, width, PlotlyBase.DefaultHeight);
        }
    }


    public static void PlotChimeraBreakdownByMassAndCharge(this CellLineResults cellLine)
    {
        var (chargePlot, massPlot) = cellLine.GetChimeraBreakdownByMassAndCharge(ResultType.Psm);
        chargePlot.SaveInCellLineOnly(cellLine, $"{FileIdentifiers.ChimeraBreakdownByChargeStateFigure}_{cellLine.CellLine}_{ResultType.Psm}", 600, 600);
        massPlot.SaveInCellLineOnly(cellLine, $"{FileIdentifiers.ChimeraBreakdownByMassFigure}_{cellLine.CellLine}_{ResultType.Psm}", 600, 600);

        (chargePlot, massPlot) = cellLine.GetChimeraBreakdownByMassAndCharge(ResultType.Peptide);
        chargePlot.SaveInCellLineOnly(cellLine, $"{FileIdentifiers.ChimeraBreakdownByChargeStateFigure}_{cellLine.CellLine}_{ResultType.Peptide}", 600, 600);
        massPlot.SaveInCellLineOnly(cellLine, $"{FileIdentifiers.ChimeraBreakdownByMassFigure}_{cellLine.CellLine}_{ResultType.Peptide}", 600, 600);
    }

    internal static (GenericChart.GenericChart Charge, GenericChart.GenericChart Mass) GetChimeraBreakdownByMassAndCharge(this CellLineResults cellLine, ResultType resultType = ResultType.Psm)
    {
        bool isTopDown = cellLine.First().IsTopDown;
        var selector = isTopDown.ChimeraBreakdownSelector(cellLine.CellLine);
        var smLabel = Labels.GetSpectrumMatchLabel(isTopDown);
        var pepLabel = Labels.GetPeptideLabel(isTopDown);
        var label = resultType == ResultType.Psm ? smLabel : pepLabel;

        List<double> yValuesMass = new();
        List<int> yValuesCharge = new();
        List<int> xValues = new();
        foreach (var result in cellLine.Where(p => p is IChimeraBreakdownCompatible && selector.Contains(p.Condition))
                     .SelectMany(p => ((IChimeraBreakdownCompatible)p).ChimeraBreakdownFile.Results)
                     .Where(p => p.Type == resultType))
        {
            if (resultType == ResultType.Psm)
            {
                yValuesMass.AddRange(result.PsmMasses);
                yValuesCharge.AddRange(result.PsmCharges);
                xValues.AddRange(Enumerable.Repeat(result.IdsPerSpectra, result.PsmMasses.Length));
            }
            else
            {
                yValuesMass.AddRange(result.PeptideMasses);
                yValuesCharge.AddRange(result.PeptideCharges);
                xValues.AddRange(Enumerable.Repeat(result.IdsPerSpectra, result.PeptideMasses.Length));
            }
        }

        var chargePlot =
            Chart.BoxPlot<int, int, string>(xValues, yValuesCharge)
                .WithXAxisStyle(Title.init("Degree of Chimerism"))
                .WithYAxisStyle(Title.init("Precursor Charge State"))
                .WithTitle($"1% {label} Charge vs Degree of Chimerism");

        var massPlot =
            Chart.BoxPlot<int, double, string>(xValues, yValuesMass)
                .WithXAxisStyle(Title.init("Degree of Chimerism"))
                .WithYAxisStyle(Title.init("Precursor Mass"))
                .WithTitle($"1% {label} Mass vs Degree of Chimerism");

        return (chargePlot, massPlot);
    }

}