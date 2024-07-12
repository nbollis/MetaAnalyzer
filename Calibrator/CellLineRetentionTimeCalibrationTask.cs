using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Interfaces;
using Analyzer.Plotting;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using MathNet.Numerics;
using Microsoft.FSharp.Core;
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

            if (run is not IRetentionTimePredictionAnalysis rtp)
                throw new ArgumentException("Selected run is not from MetaMorpheus");

            if (!rtp.IndividualFilePeptidePaths.Any())
                throw new ArgumentException("Selected run does not contain any individual file results");

            string retentionTimeAdjustmentFilePath = rtp.CalibratedRetentionTimeFilePath;
            Dictionary<string, List<(string fileName, double retentionTime)>> results;
            if (File.Exists(retentionTimeAdjustmentFilePath))
            {
                if (Parameters.Override)
                {
                    Log("Retention Time Adjustment file found, overriding it");
                    var calibrator = new CalibratorClass(rtp.IndividualFilePeptidePaths);
                    calibrator.Calibrate();
                    Log("Writing Retention Time Calibration");
                    calibrator.WriteFile(retentionTimeAdjustmentFilePath);
                    results = calibrator.FileLoggers.First().FileWiseCalibrations;
                }
                else
                {
                    Log("Retention Time Adjustment file found, loading it in");
                    results = new CalibratedRetentionTimeFile(retentionTimeAdjustmentFilePath).Results.ToDictionary(
                        p => p.FullSequence, p => p.AdjustedRetentionTimes.Select(m => (m.Key, m.Value)).ToList());
                }
            }
            else
            {
                Log($"Running Retention Time Calibration");
                var calibrator = new CalibratorClass(rtp.IndividualFilePeptidePaths);
                calibrator.Calibrate();
                Log("Writing Retention Time Calibration");
                calibrator.WriteFile(retentionTimeAdjustmentFilePath);
                results = calibrator.FileLoggers.First().FileWiseCalibrations;
            }

            Log("Parsing out original and adjusted retention times");
            var dataFromOriginalPredictionsWithFileNames = rtp.RetentionTimePredictionFile.Where(p => p.ChronologerPrediction != 0 && p.PeptideModSeq != "")
                .Select(p => (p.FullSequence, p.IsChimeric, p.ChronologerToRetentionTime, p.RetentionTime, p.FileNameWithoutExtension.ConvertFileName()))
                .ToList();
            int misMatchCount = 0;
            int missingFullSeqCount = 0;
            var dataFromAdjustedRetentionTimes =
                new List<(string FullSequence, bool IsChimeric, double ChronologerToRetentionTime, double RetentionTime)>();
            foreach (var originalPsm in dataFromOriginalPredictionsWithFileNames)
            {
                if (!results.TryGetValue(originalPsm.FullSequence, out var peptide))
                {
                    missingFullSeqCount++;
                    continue;
                }

                (string FileName, double RetentionTime)? fileSpecific = peptide
                    .FirstOrDefault(adjustedPsm => adjustedPsm.fileName.ConvertFileName() == originalPsm.Item5);

                if (fileSpecific == default((string, double)))
                {
                    misMatchCount++;
                    continue;
                }
                dataFromAdjustedRetentionTimes.Add((originalPsm.FullSequence, originalPsm.IsChimeric, originalPsm.ChronologerToRetentionTime, fileSpecific.Value.RetentionTime));
            }

            var dataFromOriginalPredictions = dataFromOriginalPredictionsWithFileNames
                .Select(p => (p.FullSequence, p.IsChimeric, p.ChronologerToRetentionTime, p.RetentionTime))
                .OrderBy(p => p.RetentionTime).ToList();
            dataFromAdjustedRetentionTimes = dataFromAdjustedRetentionTimes.OrderBy(p => p.RetentionTime).ToList();

            

            Log("Making Pretty Pictures");
            GenerateChronologerDeltaRtKde(dataFromOriginalPredictions, dataFromAdjustedRetentionTimes);
            GenerateChronologerDeltaRtHistogram(dataFromOriginalPredictions, dataFromAdjustedRetentionTimes);
            GenerateChronologerVsRtScatter(dataFromOriginalPredictions, dataFromAdjustedRetentionTimes);
            GenerateChronologerShiftPlots(dataFromOriginalPredictions, dataFromAdjustedRetentionTimes);
        }

        private void GenerateChronologerDeltaRtKde(
            List<(string FullSequence, bool IsChimeric, double ChronologerToRetentionTime, double RetentionTime)>
                dataFromOriginalPredictions,
            List<(string FullSequence, bool IsChimeric, double ChronologerToRetentionTime, double RetentionTime)>
                dataFromAdjustedRetentionTimes)
        {
            var originalRetentionTimeKDE = Chart.Combine(new[]
                {
                    GenericPlots.KernelDensityPlot(
                        dataFromOriginalPredictions.Where(p => p.IsChimeric)
                            .Select(p => p.RetentionTime - p.ChronologerToRetentionTime).ToList(),
                        "Chimeric", "Retention Time Error", "Original Density"),
                    GenericPlots.KernelDensityPlot(
                        dataFromOriginalPredictions.Where(p => !p.IsChimeric)
                            .Select(p => p.RetentionTime - p.ChronologerToRetentionTime).ToList(),
                        "Non-Chimeric", "Retention Time Error", "Original Density")
                })
                .WithXAxisStyle(Title.init("Retention Time Error"),
                    new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(-80, 80)));

            var adjustedRetetionTimeKDE = Chart.Combine(new[]
                {
                    GenericPlots.KernelDensityPlot(
                        dataFromAdjustedRetentionTimes.Where(p => p.IsChimeric)
                            .Select(p => p.RetentionTime - p.ChronologerToRetentionTime).ToList(),
                        "Chimeric", "Retention Time Error", "Adjusted Density"),
                    GenericPlots.KernelDensityPlot(
                        dataFromAdjustedRetentionTimes.Where(p => !p.IsChimeric)
                            .Select(p => p.RetentionTime - p.ChronologerToRetentionTime).ToList(),
                        "Non-Chimeric", "Retention Time Error", "Adjusted Density")
                })
                .WithXAxisStyle(Title.init("Retention Time Error"),
                    new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(-80, 80)));

            var kde = Chart.Grid(new[] { originalRetentionTimeKDE, adjustedRetetionTimeKDE }, 1, 2)
                .WithTitle($"{Parameters.CellLine.CellLine} 1% Peptides Chronolger Delta Kernel Density");
            var outName = $"RetentionTimeCalibration_{Parameters.CellLine.CellLine}_KDE";
            kde.SaveInCellLineOnly(Parameters.CellLine, outName, 1200, 600);
        }

        private void GenerateChronologerDeltaRtHistogram(
            List<(string FullSequence, bool IsChimeric, double ChronologerToRetentionTime, double RetentionTime)>
                dataFromOriginalPredictions,
            List<(string FullSequence, bool IsChimeric, double ChronologerToRetentionTime, double RetentionTime)>
                dataFromAdjustedRetentionTimes)
        {
            var originalRetentionTimeHistogram = Chart.Combine(new[]
                {
                    GenericPlots.Histogram(
                        dataFromOriginalPredictions.Where(p => p.IsChimeric)
                            .Select(p => p.RetentionTime - p.ChronologerToRetentionTime).ToList(),
                        "Chimeric", "Retention Time Error", "Original Count"),
                    GenericPlots.Histogram(
                        dataFromOriginalPredictions.Where(p => !p.IsChimeric)
                            .Select(p => p.RetentionTime - p.ChronologerToRetentionTime).ToList(),
                        "Non-Chimeric", "Retention Time Error", "Original Count")
                })
                .WithXAxisStyle(Title.init("Retention Time Error"),
                    new FSharpOption<Tuple<IConvertible, IConvertible>>(
                        new Tuple<IConvertible, IConvertible>(-80, 80)));

            var adjustedRetentionTimeHistogram = Chart.Combine(new[]
                {
                    GenericPlots.Histogram(
                        dataFromAdjustedRetentionTimes.Where(p => p.IsChimeric)
                            .Select(p => p.RetentionTime - p.ChronologerToRetentionTime).ToList(),
                        "Chimeric", "Retention Time Error", "Adjusted Count"),
                    GenericPlots.Histogram(
                        dataFromAdjustedRetentionTimes.Where(p => !p.IsChimeric)
                            .Select(p => p.RetentionTime - p.ChronologerToRetentionTime).ToList(),
                        "Non-Chimeric", "Retention Time Error", "Adjusted Count")
                })
                .WithXAxisStyle(Title.init("Retention Time Error"),
                    new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(-80, 80)));

            var histogram = Chart.Grid(new[] { originalRetentionTimeHistogram, adjustedRetentionTimeHistogram }, 1, 2)
                .WithTitle($"{Parameters.CellLine.CellLine} 1% Peptides Chronolger Delta Histogram");
            var outName = $"RetentionTimeCalibration_{Parameters.CellLine.CellLine}_Histogram";
            histogram.SaveInCellLineOnly(Parameters.CellLine, outName, 1200, 600);
        }

        private void GenerateChronologerVsRtScatter(
            List<(string FullSequence, bool IsChimeric, double ChronologerToRetentionTime, double RetentionTime)>
                dataFromOriginalPredictions,
            List<(string FullSequence, bool IsChimeric, double ChronologerToRetentionTime, double RetentionTime)>
                dataFromAdjustedRetentionTimes)
        {
            var originalRetentionTimeScatterPlot = Chart.Combine(new[]
            {

                Chart2D.Chart.Scatter<double, double, string>(
                    MarkerColor: "Non-Chimeric".ConvertConditionToColor(), Mode: StyleParam.Mode.Markers, Name: "Non-Chimeric",
                    X: dataFromOriginalPredictions.Where(p => !p.IsChimeric).Select(p => p.RetentionTime).ToArray(),
                    Y: dataFromOriginalPredictions.Where(p => !p.IsChimeric).Select(p => p.ChronologerToRetentionTime).ToArray())
                    .WithXAxisStyle(Title.init("Original RetentionTime"))
                    .WithYAxisStyle(Title.init("Chronologer Prediction")),
                Chart2D.Chart.Scatter<double, double, string>( Name: "Chimeric",
                        MarkerColor: "Chimeric".ConvertConditionToColor(), Mode: StyleParam.Mode.Markers,
                        X: dataFromOriginalPredictions.Where(p => p.IsChimeric).Select(p => p.RetentionTime).ToArray(),
                        Y: dataFromOriginalPredictions.Where(p => p.IsChimeric).Select(p => p.ChronologerToRetentionTime).ToArray())
                    .WithXAxisStyle( Title.init("Original RetentionTime"))
                    .WithYAxisStyle(Title.init("Chronologer Prediction"))
            }).WithTitle($"{Parameters.CellLine.CellLine} 1% Peptides Original Chronolger Delta Scatter")
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            var outName = $"RetentionTimeCalibration_{Parameters.CellLine.CellLine}_Scatter_Orignal";
            originalRetentionTimeScatterPlot.SaveInCellLineOnly(Parameters.CellLine, outName, 1200, 600);

            var adjustedRetentionTimeScatterPlot = Chart.Combine(new[]
            {
                Chart2D.Chart.Scatter<double, double, string>( Name: "Non-Chimeric",
                        MarkerColor: "Non-Chimeric".ConvertConditionToColor(), Mode: StyleParam.Mode.Markers,
                        X: dataFromAdjustedRetentionTimes.Where(p => !p.IsChimeric).Select(p => p.RetentionTime)
                            .ToArray(),
                        Y: dataFromAdjustedRetentionTimes.Where(p => !p.IsChimeric)
                            .Select(p => p.ChronologerToRetentionTime).ToArray())
                    .WithXAxisStyle(Title.init("Adjusted RetentionTime"))
                    .WithYAxisStyle(Title.init("Chronologer Prediction")),
                Chart2D.Chart.Scatter<double, double, string>(Name: "Chimeric",
                        MarkerColor: "Chimeric".ConvertConditionToColor(), Mode: StyleParam.Mode.Markers,
                        X: dataFromAdjustedRetentionTimes.Where(p => p.IsChimeric).Select(p => p.RetentionTime)
                            .ToArray(),
                        Y: dataFromAdjustedRetentionTimes.Where(p => p.IsChimeric)
                            .Select(p => p.ChronologerToRetentionTime).ToArray())
                    .WithXAxisStyle(Title.init("Adjusted RetentionTime"))
                    .WithYAxisStyle(Title.init("Chronologer Prediction")),
            }).WithTitle($"{Parameters.CellLine.CellLine} 1% Peptides Adjusted Chronolger Delta Scatter")
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            outName = $"RetentionTimeCalibration_{Parameters.CellLine.CellLine}_Scatter_Adjusted";
            adjustedRetentionTimeScatterPlot.SaveInCellLineOnly(Parameters.CellLine, outName, 1200, 600);
        }

        private void GenerateChronologerShiftPlots(
            List<(string FullSequence, bool IsChimeric, double ChronologerToRetentionTime, double RetentionTime)>
                dataFromOriginalPredictions,
            List<(string FullSequence, bool IsChimeric, double ChronologerToRetentionTime, double RetentionTime)>
                dataFromAdjustedRetentionTimes)
        {
            Dictionary<bool, List<double>> chimericToShiftDictionary = new()
            {
                { true, new List<double>() },
                { false, new List<double>() }
            };

            var adjustedDict = dataFromAdjustedRetentionTimes
                .ToDictionary(p => p.FullSequence, p => p.RetentionTime);
            foreach (var original in dataFromOriginalPredictions)
                if (adjustedDict.TryGetValue(original.FullSequence, out var adjustedValue))
                    chimericToShiftDictionary[original.IsChimeric].Add(original.RetentionTime - adjustedValue);

            var hist = Chart.Combine(new[]
            {
                GenericPlots.Histogram(chimericToShiftDictionary[true], "Chimeric", "Adjustment", "Count"),
                GenericPlots.Histogram(chimericToShiftDictionary[false], "Non-Chimeric", "Adjustment", "Count")
            });
            string outname = $"RetentionTimeCalibration_{Parameters.CellLine.CellLine}_ShiftHistogram";
            hist.SaveInCellLineOnly(Parameters.CellLine, outname, 600, 600);

            var kde = Chart.Combine(new[]
            {
                GenericPlots.KernelDensityPlot(chimericToShiftDictionary[true], "Chimeric", "Adjustment", "Density", 0.5),
                GenericPlots.KernelDensityPlot(chimericToShiftDictionary[false], "Non-Chimeric", "Adjustment", "Density", 0.5)
            });
            outname = $"RetentionTimeCalibration_{Parameters.CellLine.CellLine}_ShiftKDE";
            kde.SaveInCellLineOnly(Parameters.CellLine, outname, 600, 600);
        }

        #region Unused

        private void GenerateChronologerVsRtScatterRemovingIntersectionPoints(
           List<(string FullSequence, bool IsChimeric, double ChronologerToRetentionTime, double RetentionTime)>
               original,
           List<(string FullSequence, bool IsChimeric, double ChronologerToRetentionTime, double RetentionTime)>
               adjusted)
        {
            var dataFromOriginalPredictions = new List<(string FullSequence, bool IsChimeric, double ChronologerToRetentionTime, double RetentionTime)>();
            foreach (var fullSeqGroup in original.GroupBy(p => p.FullSequence))
                if (fullSeqGroup.Any(p => p.IsChimeric) && fullSeqGroup.Any(p => !p.IsChimeric))
                    dataFromOriginalPredictions.AddRange(fullSeqGroup);

            var dataFromAdjustedRetentionTimes = new List<(string FullSequence, bool IsChimeric, double ChronologerToRetentionTime, double RetentionTime)>();
            foreach (var fullSeqGroup in adjusted.GroupBy(p => p.FullSequence))
                if (fullSeqGroup.Any(p => p.IsChimeric) && fullSeqGroup.Any(p => !p.IsChimeric))
                    dataFromAdjustedRetentionTimes.AddRange(fullSeqGroup);

            var originalRetentionTimeScatterPlot = Chart.Combine(new[]
            {

                Chart2D.Chart.Scatter<double, double, string>(
                    MarkerColor: "Non-Chimeric".ConvertConditionToColor(), Mode: StyleParam.Mode.Markers, Name: "Non-Chimeric",
                    X: dataFromOriginalPredictions.Where(p => !p.IsChimeric).Select(p => p.RetentionTime).ToArray(),
                    Y: dataFromOriginalPredictions.Where(p => !p.IsChimeric).Select(p => p.ChronologerToRetentionTime).ToArray())
                    .WithXAxisStyle(Title.init("Original RetentionTime"))
                    .WithYAxisStyle(Title.init("Chronologer Prediction")),
                Chart2D.Chart.Scatter<double, double, string>( Name: "Chimeric",
                        MarkerColor: "Chimeric".ConvertConditionToColor(), Mode: StyleParam.Mode.Markers,
                        X: dataFromOriginalPredictions.Where(p => p.IsChimeric).Select(p => p.RetentionTime).ToArray(),
                        Y: dataFromOriginalPredictions.Where(p => p.IsChimeric).Select(p => p.ChronologerToRetentionTime).ToArray())
                    .WithXAxisStyle( Title.init("Original RetentionTime"))
                    .WithYAxisStyle(Title.init("Chronologer Prediction"))
            }).WithTitle($"{Parameters.CellLine.CellLine} 1% Peptides Original Chronolger Delta Scatter")
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            var outName = $"RetentionTimeCalibration_{Parameters.CellLine.CellLine}_Scatter_RemovedIntersection_Orignal";
            originalRetentionTimeScatterPlot.SaveInCellLineOnly(Parameters.CellLine, outName, 1200, 600);

            var adjustedRetentionTimeScatterPlot = Chart.Combine(new[]
            {
                Chart2D.Chart.Scatter<double, double, string>( Name: "Non-Chimeric",
                        MarkerColor: "Non-Chimeric".ConvertConditionToColor(), Mode: StyleParam.Mode.Markers,
                        X: dataFromAdjustedRetentionTimes.Where(p => !p.IsChimeric).Select(p => p.RetentionTime)
                            .ToArray(),
                        Y: dataFromAdjustedRetentionTimes.Where(p => !p.IsChimeric)
                            .Select(p => p.ChronologerToRetentionTime).ToArray())
                    .WithXAxisStyle(Title.init("Adjusted RetentionTime"))
                    .WithYAxisStyle(Title.init("Chronologer Prediction")),
                Chart2D.Chart.Scatter<double, double, string>(Name: "Chimeric",
                        MarkerColor: "Chimeric".ConvertConditionToColor(), Mode: StyleParam.Mode.Markers,
                        X: dataFromAdjustedRetentionTimes.Where(p => p.IsChimeric).Select(p => p.RetentionTime)
                            .ToArray(),
                        Y: dataFromAdjustedRetentionTimes.Where(p => p.IsChimeric)
                            .Select(p => p.ChronologerToRetentionTime).ToArray())
                    .WithXAxisStyle(Title.init("Adjusted RetentionTime"))
                    .WithYAxisStyle(Title.init("Chronologer Prediction")),
            }).WithTitle($"{Parameters.CellLine.CellLine} 1% Peptides Adjusted Chronolger Delta Scatter")
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            outName = $"RetentionTimeCalibration_{Parameters.CellLine.CellLine}_Scatter_RemovedIntersection_Adjusted";
            adjustedRetentionTimeScatterPlot.SaveInCellLineOnly(Parameters.CellLine, outName, 1200, 600);
        }

        #endregion
    }
}
