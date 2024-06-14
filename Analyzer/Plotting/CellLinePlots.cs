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

    

    #region Chronologer Exploration

    

  

    
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