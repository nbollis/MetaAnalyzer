using System.Dynamic;
using System.Security.Cryptography.X509Certificates;
using Analyzer.FileTypes.Internal;
using Analyzer.Interfaces;
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
        outputDirectory ??= cellLine.GetFigureDirectory();

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

    #endregion

    #region Retention Time

    public static void PlotCellLineRetentionTimePredictions(this CellLineResults cellLine)
    {
        var plots = cellLine.GetCellLineRetentionTimePredictions();
        string outPath = Path.Combine(cellLine.GetFigureDirectory(), $"{FileIdentifiers.ChronologerFigure}_{cellLine.CellLine}");
        plots.Chronologer.SavePNG(outPath, null, 1000, GenericPlots.DefaultHeight);
        outPath = Path.Combine(cellLine.FigureDirectory, $"{FileIdentifiers.ChronologerFigure}_{cellLine.CellLine}");
        plots.Chronologer.SavePNG(outPath, null, 1000, GenericPlots.DefaultHeight);

        outPath = Path.Combine(cellLine.GetFigureDirectory(), $"{FileIdentifiers.SSRCalcFigure}_{cellLine.CellLine}");
        plots.SSRCalc3.SavePNG(outPath, null, 1000, GenericPlots.DefaultHeight);
        outPath = Path.Combine(cellLine.FigureDirectory, $"{FileIdentifiers.SSRCalcFigure}_{cellLine.CellLine}");
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
            .WithSize(1000, GenericPlots.DefaultHeight);

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
            .WithLayout(GenericPlots.DefaultLayoutWithLegend)
            .WithSize(1000, GenericPlots.DefaultHeight);
        return (chronologerPlot, ssrCalcPlot);
    }

    public enum Kernels
    {
        Gaussian,
        Epanechnikov,
        Triangular,
        Uniform
    }

    public static void PlotChronologerVsPercentHi(this CellLineResults cellLine)
    {
        var plot = cellLine.GetChronologerHIScatterPlot();
        string outPath = Path.Combine(cellLine.GetFigureDirectory(),
                       $"{FileIdentifiers.ChronologerFigureACN}_{cellLine.CellLine}");
        plot.SavePNG(outPath, null, 1000, GenericPlots.DefaultHeight);
        outPath = Path.Combine(cellLine.FigureDirectory,
                       $"{FileIdentifiers.ChronologerFigureACN}_{cellLine.CellLine}");
        plot.SavePNG(outPath, null, 1000, GenericPlots.DefaultHeight);
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
            .WithSize(1000, GenericPlots.DefaultHeight);
        return chronologerPlot;
    }


    public static void PlotChronologerDeltaKernelPDF(this CellLineResults cellLine, Kernels kernel = Kernels.Gaussian)
    {
        var chart = cellLine.GetChronologerDeltaPlotKernelPDF(kernel);
        string outPath = Path.Combine(cellLine.GetFigureDirectory(),
            $"{FileIdentifiers.ChronologerDeltaDistributionFigure}_{cellLine.CellLine}");
        string outPath2 = Path.Combine(cellLine.FigureDirectory,
            $"{FileIdentifiers.ChronologerDeltaDistributionFigure}_{cellLine.CellLine}");
        chart.SavePNG(outPath, null, 600, 600);
        chart.SavePNG(outPath2, null, 600, 600);
    }

    internal static GenericChart.GenericChart GetChronologerDeltaPlotKernelPDF(this CellLineResults cellLine, Kernels kernel = Kernels.Gaussian)
    {
        var individualFiles = cellLine.Results
            .Where(p => false.FdrPlotSelector().Contains(p.Condition))
            .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
            .Select(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile)
            .ToList();
        var chronologer = individualFiles
            .SelectMany(p => p.Where(m => m.ChronologerPrediction != 0 && m.PeptideModSeq != ""))
            .Select(p => (p.ChronologerPrediction, p.PercentHI, p.IsChimeric, p.DeltaChronologer))
            .ToList();

        
        var chimericSamples = chronologer.Where(p => p.IsChimeric)
            .Select(p => p.DeltaChronologer)
            .ToList();
        var nonChimericSamples = chronologer.Where(p => !p.IsChimeric)
            .Select(p => p.DeltaChronologer)
            .ToList();


        nonChimericSamples = nonChimericSamples.OrderBy(p => p).ToList();
        chimericSamples = chimericSamples.OrderBy(p => p).ToList();
        var chart = Chart.Combine(new[]
            {
                GenericPlots.KernelDensityPlot(chimericSamples, "Chimeric", "Delta %ACN", "Probability"),
                GenericPlots.KernelDensityPlot(nonChimericSamples, "Non-Chimeric", "Delta %ACN", "Probability"),
                Chart.Line<double, double, string>(new [] {0.0, 0},
                    new [] {0.0, 0.35},
                    LineColor: new Optional<Color>(Color.fromKeyword(ColorKeyword.DarkGray), true), 
                    LineDash: StyleParam.DrawingStyle.Dash, Opacity:0.5)
            })
            .WithTitle($" {cellLine.CellLine} Chronologer Delta Kernel Density")
            .WithSize(400, 400)
            .WithXAxisStyle(Title.init("Delta Chronologer"))
            .WithYAxisStyle(Title.init("Density"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend);

        return chart;
    }


    public static void PlotAverageRetentionTimeShiftPlotKernelPDF(this CellLineResults cellLine,
        Kernels kernel = Kernels.Gaussian)
    {
        var file = cellLine.MaximumChimeraEstimationFile;
        if (file is null)
            return;

        var mmPsm = file.GetAverageRetentionTimeShiftPlotKernelPDF(Software.MetaMorpheus, ResultType.Psm, false);
        string outPath = Path.Combine(cellLine.GetFigureDirectory(), $"{FileIdentifiers.RetentionTimeShift_MM}_{cellLine.CellLine}_Psms");
        string outPath2 = Path.Combine(cellLine.FigureDirectory, $"{FileIdentifiers.RetentionTimeShift_MM}_{cellLine.CellLine}_Psms");
        mmPsm.SavePNG(outPath, null, 600, 600);
        mmPsm.SavePNG(outPath2, null, 600, 600);

        var mmOnePercentPsm = file.GetAverageRetentionTimeShiftPlotKernelPDF(Software.MetaMorpheus, ResultType.Psm, true);
        outPath = Path.Combine(cellLine.GetFigureDirectory(), $"{FileIdentifiers.RetentionTimeShift_MM}_{cellLine.CellLine}_1%Psms");
        outPath2 = Path.Combine(cellLine.FigureDirectory, $"{FileIdentifiers.RetentionTimeShift_MM}_{cellLine.CellLine}_1%Psms");
        mmOnePercentPsm.SavePNG(outPath, null, 600, 600);
        mmOnePercentPsm.SavePNG(outPath2, null, 600, 600);

        var mmPep = file.GetAverageRetentionTimeShiftPlotKernelPDF(Software.MetaMorpheus, ResultType.Peptide, false);
        outPath = Path.Combine(cellLine.GetFigureDirectory(), $"{FileIdentifiers.RetentionTimeShift_MM}_{cellLine.CellLine}_Peptides");
        outPath2 = Path.Combine(cellLine.FigureDirectory, $"{FileIdentifiers.RetentionTimeShift_MM}_{cellLine.CellLine}_Peptides");
        mmPep.SavePNG(outPath, null, 600, 600);
        mmPep.SavePNG(outPath2, null, 600, 600);

        var mmOnePercentPep = file.GetAverageRetentionTimeShiftPlotKernelPDF(Software.MetaMorpheus, ResultType.Peptide, true);
        outPath = Path.Combine(cellLine.GetFigureDirectory(), $"{FileIdentifiers.RetentionTimeShift_MM}_{cellLine.CellLine}_1%Peptides");
        outPath2 = Path.Combine(cellLine.FigureDirectory, $"{FileIdentifiers.RetentionTimeShift_MM}_{cellLine.CellLine}_1%Peptides");
        mmOnePercentPep.SavePNG(outPath, null, 600, 600);
        mmOnePercentPep.SavePNG(outPath2, null, 600, 600);

        var fragPsm = file.GetAverageRetentionTimeShiftPlotKernelPDF(Software.Unspecified, ResultType.Psm, false);
        outPath = Path.Combine(cellLine.GetFigureDirectory(), $"{FileIdentifiers.RetentionTimeShift_Fragger}_{cellLine.CellLine}_Psms");
        outPath2 = Path.Combine(cellLine.FigureDirectory, $"{FileIdentifiers.RetentionTimeShift_Fragger}_{cellLine.CellLine}_Psms");
        fragPsm.SavePNG(outPath, null, 600, 600);
        fragPsm.SavePNG(outPath2, null, 600, 600);

        var fragOnePercentPsm = file.GetAverageRetentionTimeShiftPlotKernelPDF(Software.Unspecified, ResultType.Psm, true);
        outPath = Path.Combine(cellLine.GetFigureDirectory(), $"{FileIdentifiers.RetentionTimeShift_Fragger}_{cellLine.CellLine}_1%Psms");
        outPath2 = Path.Combine(cellLine.FigureDirectory, $"{FileIdentifiers.RetentionTimeShift_Fragger}_{cellLine.CellLine}_1%Psms");
        fragOnePercentPsm.SavePNG(outPath, null, 600, 600);
        fragOnePercentPsm.SavePNG(outPath2, null, 600, 600);

    }

    public static GenericChart.GenericChart? GetAverageRetentionTimeShiftPlotKernelPDF(this MaximumChimeraEstimationFile file, Software software = Software.MetaMorpheus,
        ResultType resultType = ResultType.Psm, bool onePercent = true, Kernels kernel = Kernels.Gaussian)
    {
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

        var hist = Chart.Combine(new[]
            {
                Chart.Histogram<double, double, string>(chimeric,
                    MarkerColor: chimericLabel.ConvertConditionToColor()),
                Chart.Histogram<double, double, string>(nonChimeric,
                    MarkerColor: nonChimericLabel.ConvertConditionToColor())

            }).WithTitle($"{softwareLabel} {file.First().CellLine} Average {titleLabel} RT Shift")
            .WithSize(800, 800)
            .WithXAxisStyle(Title.init("RT Shift"))
            .WithYAxisStyle(Title.init("Count"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend);

        var kernelPlot = Chart.Combine(new[]
            {
                GenericPlots.KernelDensityPlot(chimeric, chimericLabel, "RT Shift", "Density", 0.01, kernel),
                GenericPlots.KernelDensityPlot(nonChimeric, nonChimericLabel, "RT Shift", "Density", 0.01, kernel),
                Chart.Line<double, double, string>(new[] { 0.0, 0 }, new[] { 0.0, 0.35 },
                    LineColor: new Optional<Color>(Color.fromKeyword(ColorKeyword.DarkGray), true),
                    LineDash: StyleParam.DrawingStyle.Dash, Opacity: 0.5)
            })
            .WithTitle($"{softwareLabel} {file.First().CellLine} Average {titleLabel} RT Shift")
            .WithSize(800, 800)
            .WithXAxisStyle(Title.init("RT Shift"))
            .WithYAxisStyle(Title.init("Density"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend);

        var plot = Chart.Grid(new[] { hist, kernelPlot }, 1, 2)
            .WithSize(1200, 600)
            .WithTitle($"{softwareLabel} {file.First().CellLine} Average {titleLabel} RT Shift");

        return plot;
    }

    #endregion

    #region Spectral Similarity

    public static void PlotCellLineSpectralSimilarity(this CellLineResults cellLine)
    {

        string outpath = Path.Combine(cellLine.GetFigureDirectory(), $"{FileIdentifiers.SpectralAngleFigure}_{cellLine.CellLine}");
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
        var selector = isTopDown.ChimeraBreakdownSelector();
        var smLabel = GenericPlots.SpectralMatchLabel(isTopDown);
        var pepLabel = GenericPlots.ResultLabel(isTopDown);
        string smOutName = $"{FileIdentifiers.ChimeraBreakdownTargetDecoy}_{smLabel}_{cellLine.CellLine}";
        string pepOutName = $"{FileIdentifiers.ChimeraBreakdownTargetDecoy}_{pepLabel}_{cellLine.CellLine}";


        var results = cellLine.Results
            .Where(p => p is MetaMorpheusResult && selector.Contains(p.Condition))
            .SelectMany(p => ((MetaMorpheusResult)p).ChimeraBreakdownFile)
            .ToList();

        var psmChart =
            results.GetChimeraBreakDownStackedColumn_TargetDecoy(ResultType.Psm, cellLine.First().IsTopDown, absolute, out int width);
        string psmOutPath = Path.Combine(cellLine.GetFigureDirectory(), smOutName);
        psmChart.SavePNG(psmOutPath, null, width, GenericPlots.DefaultHeight);
        psmOutPath = Path.Combine(cellLine.FigureDirectory, smOutName);
        psmChart.SavePNG(psmOutPath, null, width, GenericPlots.DefaultHeight);

        var peptideChart =
            results.GetChimeraBreakDownStackedColumn_TargetDecoy(ResultType.Peptide, cellLine.First().IsTopDown, absolute, out width);
        string peptideOutPath = Path.Combine(cellLine.GetFigureDirectory(), pepOutName);
        peptideChart.SavePNG(peptideOutPath, null, width, GenericPlots.DefaultHeight);
        peptideOutPath = Path.Combine(cellLine.FigureDirectory, pepOutName);
        peptideChart.SavePNG(peptideOutPath, null, width, GenericPlots.DefaultHeight);
    }

    #endregion


    /// <summary>
    /// Stacked column: Plots the type of chimeric identifications as a function of the degree of chimericity
    /// </summary>
    /// <param name="cellLine"></param>
    public static void PlotCellLineChimeraBreakdown(this CellLineResults cellLine)
    {
        bool isTopDown = cellLine.First().IsTopDown;
        var selector = isTopDown.ChimeraBreakdownSelector();
        var smLabel = GenericPlots.SpectralMatchLabel(isTopDown);
        var pepLabel = GenericPlots.ResultLabel(isTopDown);
        string smOutName = $"{FileIdentifiers.ChimeraBreakdownComparisonFigure}_{smLabel}_{cellLine.CellLine}";
        string smAreaOutName = $"{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaFigure}_{smLabel}_{cellLine.CellLine}";
        string smAreaRelativeName = $"{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaPercentFigure}_{smLabel}_{cellLine.CellLine}";
        string pepOutName = $"{FileIdentifiers.ChimeraBreakdownComparisonFigure}_{pepLabel}_{cellLine.CellLine}";
        string pepAreaOutName = $"{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaFigure}_{pepLabel}_{cellLine.CellLine}";
        string pepAreaRelativeName = $"{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaPercentFigure}_{pepLabel}_{cellLine.CellLine}";

        // plot aggregated cell line results for specific targeted file from the selector
        var results = cellLine.Where(p => p is IChimeraBreakdownCompatible && selector.Contains(p.Condition))
            .SelectMany(p => ((IChimeraBreakdownCompatible)p).ChimeraBreakdownFile.Results).ToList();

        var psmChart = results.GetChimeraBreakDownStackedColumn(ResultType.Psm, cellLine.First().IsTopDown, out int width); 
        string psmOutPath = Path.Combine(cellLine.GetFigureDirectory(), smOutName);
        psmChart.SavePNG(psmOutPath, null, width, GenericPlots.DefaultHeight);

        var stackedAreaPsmChart = results.GetChimeraBreakDownStackedArea(ResultType.Psm, cellLine.First().IsTopDown, out width);
        string stackedAreaPsmOutPath = Path.Combine(cellLine.GetFigureDirectory(), smAreaOutName);
        stackedAreaPsmChart.SavePNG(stackedAreaPsmOutPath, null, width, GenericPlots.DefaultHeight);

        var statckedAreaPsmChartRelative = results.GetChimeraBreakDownStackedArea(ResultType.Psm, cellLine.First().IsTopDown, out width, true);
        string stackedAreaPsmRelativeOutPath = Path.Combine(cellLine.GetFigureDirectory(), smAreaRelativeName);
        statckedAreaPsmChartRelative.SavePNG(stackedAreaPsmRelativeOutPath, null, width, GenericPlots.DefaultHeight);

        if (results.All(p => p.Type == ResultType.Psm))
            goto IndividualResults;

        var peptideChart = results.GetChimeraBreakDownStackedColumn(ResultType.Peptide, cellLine.First().IsTopDown, out width);
        string peptideOutPath = Path.Combine(cellLine.GetFigureDirectory(), pepOutName);
        peptideChart.SavePNG(peptideOutPath, null, width, GenericPlots.DefaultHeight);

        var stackedAreaPeptideChart = results.GetChimeraBreakDownStackedArea(ResultType.Peptide, cellLine.First().IsTopDown, out width);
        string stackedAreaPeptideOutPath = Path.Combine(cellLine.GetFigureDirectory(), pepAreaOutName);
        stackedAreaPeptideChart.SavePNG(stackedAreaPeptideOutPath, null, width, GenericPlots.DefaultHeight);

        var stackedAreaPeptideChartRelative = results.GetChimeraBreakDownStackedArea(ResultType.Peptide, cellLine.First().IsTopDown, out width, true);
        string stackedAreaPeptideRelativeOutPath = Path.Combine(cellLine.GetFigureDirectory(), pepAreaRelativeName);
        stackedAreaPeptideChartRelative.SavePNG(stackedAreaPeptideRelativeOutPath, null, width, GenericPlots.DefaultHeight);


        IndividualResults:
        // plot individual results for each IChimeraBreakdownCompatible file type
        var compatibleResults = cellLine.Where(m => m is IChimeraBreakdownCompatible)
            .Cast<IChimeraBreakdownCompatible>().ToList();
        foreach (var file in compatibleResults)
        {
            results = file.ChimeraBreakdownFile.Results;

            psmChart = results.GetChimeraBreakDownStackedColumn(ResultType.Psm, cellLine.First().IsTopDown, out width, file.Condition);
            psmOutPath = Path.Combine(file.FigureDirectory, smOutName);
            psmChart.SavePNG(psmOutPath, null, width, GenericPlots.DefaultHeight);


            stackedAreaPsmChart = results.GetChimeraBreakDownStackedArea(ResultType.Psm, cellLine.First().IsTopDown, out width, false, file.Condition);
            stackedAreaPsmOutPath = Path.Combine(file.FigureDirectory, smAreaOutName);
            stackedAreaPsmChart.SavePNG(stackedAreaPsmOutPath, null, width, GenericPlots.DefaultHeight);


            statckedAreaPsmChartRelative = results.GetChimeraBreakDownStackedArea(ResultType.Psm, cellLine.First().IsTopDown, out width, true, file.Condition);
            stackedAreaPsmRelativeOutPath = Path.Combine(file.FigureDirectory, smAreaRelativeName);
            statckedAreaPsmChartRelative.SavePNG(stackedAreaPsmRelativeOutPath, null, width, GenericPlots.DefaultHeight);


            if (results.All(p => p.Type == ResultType.Psm))
                continue;

            peptideChart = results.GetChimeraBreakDownStackedColumn(ResultType.Peptide, cellLine.First().IsTopDown, out width, file.Condition);
            peptideOutPath = Path.Combine(file.FigureDirectory, pepOutName);
            peptideChart.SavePNG(peptideOutPath, null, width, GenericPlots.DefaultHeight);

            stackedAreaPeptideChart = results.GetChimeraBreakDownStackedArea(ResultType.Peptide, cellLine.First().IsTopDown, out width, false, file.Condition);
            stackedAreaPeptideOutPath = Path.Combine(file.FigureDirectory, pepAreaOutName);
            stackedAreaPeptideChart.SavePNG(stackedAreaPeptideOutPath, null, width, GenericPlots.DefaultHeight);

            stackedAreaPeptideChartRelative = results.GetChimeraBreakDownStackedArea(ResultType.Peptide, cellLine.First().IsTopDown, out width, true, file.Condition);
            stackedAreaPeptideRelativeOutPath = Path.Combine(file.FigureDirectory, pepAreaRelativeName);
            stackedAreaPeptideChartRelative.SavePNG(stackedAreaPeptideRelativeOutPath, null, width, GenericPlots.DefaultHeight);
        }
    }

}