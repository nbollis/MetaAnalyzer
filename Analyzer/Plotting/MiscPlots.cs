using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Analyzer.FileTypes.Internal;
using Analyzer.Interfaces;
using Analyzer.Plotting.ComparativePlots;
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

public static class MiscPlots
{
    

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
            .Where(p => false.GetSingleResultSelector(cellLine.CellLine).Contains(p.Condition))
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
            .Where(p => false.GetSingleResultSelector(cellLine.CellLine).Contains(p.Condition))
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





   


    

}