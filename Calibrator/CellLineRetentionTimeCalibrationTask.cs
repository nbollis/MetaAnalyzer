using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Plotting;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Plotly.NET;
using Plotly.NET.CSharp;
using GenericChartExtensions = Plotly.NET.CSharp.GenericChartExtensions;
using Chart = Plotly.NET.CSharp.Chart;
using TaskLayer;
using TaskLayer.ChimeraAnalysis;

namespace Calibrator
{
    public class CellLineRetentionTimeCalibrationTask : BaseResultAnalyzerTask
    {
        public override MyTask MyTask => MyTask.RetentionTimeAlignment;
        public override CellLineAnalysisParameters Parameters { get; }

        public CellLineRetentionTimeCalibrationTask(CellLineAnalysisParameters parameters)
        {
            Parameters = parameters;
        }

        protected override void RunSpecific()
        {
            var potentialRuns = Parameters.CellLine
                .Where(p => Parameters.CellLine.GetSingleResultSelector().Contains(p.Condition)).ToList();
            if (!potentialRuns.Any())
                throw new ArgumentException($"Cell Line does not contain result in single run selector");
            if (potentialRuns.Count() > 1) 
                throw new ArgumentException("Cell Line contains multiple results in single run selector");

            var run = potentialRuns.First();

            if (run is not MetaMorpheusResult mm)
                throw new ArgumentException("Selected run is not from MetaMorpheus");

            if (!mm.IndividualFileResults.Any())
                throw new ArgumentException("Selected run does not contain any individual file results");

            Log($"Running Retention Time Calibration");
            var peptideFilePaths = mm.IndividualFileResults.Select(p => p.PeptidePath).ToList();
            var calibrator = new CalibratorClass(peptideFilePaths);
            calibrator.Calibrate();

            Log("Writing Retention Time Calibration");
            var outPath = Path.Combine(Parameters.CellLine.DirectoryPath, "AdjustedRetentionTimes.tsv");
            calibrator.WriteFile(outPath);


            Log("Parsing out original and adjusted retention times");
            var results = calibrator.FileLoggers.First().FileWiseCalibrations;
            var dataFromOriginalPredictions = mm.RetentionTimePredictionFile.Where(p => p.ChronologerPrediction != 0 && p.PeptideModSeq != "")
                .Select(p => (p.FullSequence, p.IsChimeric, p.ChronologerToRetentionTime, p.RetentionTime, p.FileNameWithoutExtension))
                .ToList();
            var dataFromAdjustedRetentionTimes = dataFromOriginalPredictions.Select(p =>
            {
                var peptide = results[p.FullSequence];
                (string FileName, double RetentionTime)? fileSpecific = peptide.FirstOrDefault(m => m.fileName == p.FileNameWithoutExtension);
                if (fileSpecific == default((string, double)))
                    Debugger.Break();

                return (p.FullSequence, p.IsChimeric, p.ChronologerToRetentionTime, fileSpecific.Value.RetentionTime);
            })
                .ToArray();

            Log("Making Pretty Pictures");

            var originalRetentionTimeKDE = Chart.Combine(new[]
            {
                GenericPlots.KernelDensityPlot(
                    dataFromOriginalPredictions.Where(p => p.IsChimeric)
                        .Select(p => p.RetentionTime - p.ChronologerToRetentionTime).ToList(),
                    "Chimeric", "Retention Time Error", "Density"),
                GenericPlots.KernelDensityPlot(
                    dataFromOriginalPredictions.Where(p => !p.IsChimeric)
                        .Select(p => p.RetentionTime - p.ChronologerToRetentionTime).ToList(),
                    "Non-Chimeric", "Retention Time Error", "Density")
            });

            var adjustedRetetionTimeKDE = Chart.Combine(new[]
            {
                GenericPlots.KernelDensityPlot(
                    dataFromAdjustedRetentionTimes.Where(p => p.IsChimeric)
                        .Select(p => p.RetentionTime - p.ChronologerToRetentionTime).ToList(),
                    "Chimeric", "Retention Time Error", "Density"),
                GenericPlots.KernelDensityPlot(
                    dataFromAdjustedRetentionTimes.Where(p => !p.IsChimeric)
                        .Select(p => p.RetentionTime - p.ChronologerToRetentionTime).ToList(),
                    "Non-Chimeric", "Retention Time Error", "Density")
            });

            var kde = Chart.Grid(new[] { originalRetentionTimeKDE, adjustedRetetionTimeKDE }, 2, 1)
                .WithTitle($"{Parameters.CellLine.CellLine} 1% Peptides Chronolger Delta Kernel Density");
            var outName = $"RetentionTimeCalibration_{Parameters.CellLine.CellLine}_KDE";
            kde.SaveInCellLineOnly(Parameters.CellLine, outName, 1200, 600);


            var originalRetentionTimeScatterPlot = Chart.Combine(new[]
            {
                Chart2D.Chart.Scatter<double, double, string>(
                    MarkerColor: "Chimeric".ConvertConditionToColor(),
                    X: dataFromOriginalPredictions.Where(p => p.IsChimeric).Select(p => p.RetentionTime).ToArray(),
                    Y: dataFromOriginalPredictions.Where(p => p.IsChimeric).Select(p => p.ChronologerToRetentionTime).ToArray())
                    .WithXAxisStyle( Title.init("RetentionTime"))
                    .WithYAxisStyle(Title.init("Chronologer Prediction")),
                Chart2D.Chart.Scatter<double, double, string>(
                    MarkerColor: "Non-Chimeric".ConvertConditionToColor(),
                    X: dataFromOriginalPredictions.Where(p => !p.IsChimeric).Select(p => p.RetentionTime).ToArray(),
                    Y: dataFromOriginalPredictions.Where(p => !p.IsChimeric).Select(p => p.ChronologerToRetentionTime).ToArray())
                    .WithXAxisStyle(Title.init("RetentionTime"))
                    .WithYAxisStyle(Title.init("Chronologer Prediction"))
            });

            var adjustedRetentionTimeScatterPlot = Chart.Combine(new[]
            {
                Chart2D.Chart.Scatter<double, double, string>(
                        MarkerColor: "Chimeric".ConvertConditionToColor(),
                        X: dataFromAdjustedRetentionTimes.Where(p => p.IsChimeric).Select(p => p.RetentionTime)
                            .ToArray(),
                        Y: dataFromAdjustedRetentionTimes.Where(p => p.IsChimeric)
                            .Select(p => p.ChronologerToRetentionTime).ToArray())
                    .WithXAxisStyle(Title.init("RetentionTime"))
                    .WithYAxisStyle(Title.init("Chronologer Prediction")),
                Chart2D.Chart.Scatter<double, double, string>(
                        MarkerColor: "Non-Chimeric".ConvertConditionToColor(),
                        X: dataFromAdjustedRetentionTimes.Where(p => !p.IsChimeric).Select(p => p.RetentionTime)
                            .ToArray(),
                        Y: dataFromAdjustedRetentionTimes.Where(p => !p.IsChimeric)
                            .Select(p => p.ChronologerToRetentionTime).ToArray())
                    .WithXAxisStyle(Title.init("RetentionTime"))
                    .WithYAxisStyle(Title.init("Chronologer Prediction"))
            });

            var scatter = Chart.Grid(new[] { originalRetentionTimeScatterPlot, adjustedRetentionTimeScatterPlot }, 2, 1)
                .WithTitle($"{Parameters.CellLine.CellLine} 1% Peptides Chronolger Delta Scatter")
                .WithXAxisStyle(Title.init("RetentionTime"))
                .WithYAxisStyle(Title.init("Chronologer Prediction"));
            outName = $"RetentionTimeCalibration_{Parameters.CellLine.CellLine}_Scatter";
            scatter.SaveInCellLineOnly(Parameters.CellLine, outName, 1200, 600);


            var originalRetentionTimeHistogram = Chart.Combine(new[]
            {
                GenericPlots.Histogram(
                    dataFromOriginalPredictions.Where(p => p.IsChimeric)
                        .Select(p => p.RetentionTime - p.ChronologerToRetentionTime).ToList(),
                    "Chimeric", "Retention Time Error", "Count"),
                GenericPlots.Histogram(
                    dataFromOriginalPredictions.Where(p => !p.IsChimeric)
                        .Select(p => p.RetentionTime - p.ChronologerToRetentionTime).ToList(),
                    "Non-Chimeric", "Retention Time Error", "Count")
            });

            var adjustedRetentionTimeHistogram = Chart.Combine(new[]
            {
                GenericPlots.Histogram(
                    dataFromAdjustedRetentionTimes.Where(p => p.IsChimeric)
                        .Select(p => p.RetentionTime - p.ChronologerToRetentionTime).ToList(),
                    "Chimeric", "Retention Time Error", "Count"),
                GenericPlots.Histogram(
                    dataFromAdjustedRetentionTimes.Where(p => !p.IsChimeric)
                        .Select(p => p.RetentionTime - p.ChronologerToRetentionTime).ToList(),
                    "Non-Chimeric", "Retention Time Error", "Count")
            });

            var histogram = Chart.Grid(new[] { originalRetentionTimeHistogram, adjustedRetentionTimeHistogram }, 2, 1)
                .WithTitle($"{Parameters.CellLine.CellLine} 1% Peptides Chronolger Delta Histogram");
            outName = $"RetentionTimeCalibration_{Parameters.CellLine.CellLine}_Histogram";
            histogram.SaveInCellLineOnly(Parameters.CellLine, outName, 1200, 600);
        }
    }
}
