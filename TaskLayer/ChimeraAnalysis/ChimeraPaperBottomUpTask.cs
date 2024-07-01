using Analyzer.SearchType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Plotting.Util;
using Analyzer.Interfaces;
using Analyzer.Plotting.AggregatePlots;
using Analyzer.Plotting.ComparativePlots;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Util;

namespace TaskLayer.ChimeraAnalysis
{
    public class ChimeraPaperBottomUpTask : BaseResultAnalyzerTask
    {
        public override MyTask MyTask => MyTask.ChimeraPaperBottomUp;
        protected override ChimeraPaperAnalysisParameters Parameters { get; }

        public ChimeraPaperBottomUpTask(ChimeraPaperAnalysisParameters parameters)
        {
            Parameters = parameters;
        }

        protected override void RunSpecific()
        {
            var allResults = BuildChimeraPaperResultsObjects();

            foreach (var cellLine in allResults)
            {
                Log($"Starting Processing of {cellLine.CellLine}");
                foreach (var singleRunResult in cellLine)
                {
                    Log($"Processing {singleRunResult.Condition}", 2);

                    if (Parameters.CountChimericResults)
                    {
                        singleRunResult.Override = Parameters.Override;
                        Log("Counting Chimeric Identifications", 3);

                        singleRunResult.CountChimericPsms();
                        if (singleRunResult is IChimeraPeptideCounter pc)
                            pc.CountChimericPeptides();
                    }

                    if (Parameters.RunResultCounting)
                    {
                        singleRunResult.Override = Parameters.Override;
                        Log("Counting All Search Results", 3);

                        singleRunResult.GetIndividualFileComparison();
                        singleRunResult.GetBulkResultCountComparisonFile();
                    }

                    if (Parameters.RunFdrAnalysis)
                    {
                        singleRunResult.Override = Parameters.Override;
                        Log("Running FDR Analysis", 3);

                        if (singleRunResult is MetaMorpheusResult mm)
                        {
                            mm.Override = false;
                            mm.PlotPepFeaturesScatterGrid();
                            mm.ExportCombinedChimeraTargetDecoyExploration(mm.FigureDirectory, mm.Condition);
                            mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, false);
                            mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, true);
                            mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, false);
                            mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, true);
                        }
                    }

                    if (Parameters.RunChimeraBreakdown)
                    {
                        singleRunResult.Override = Parameters.Override;
                        Log("Running Chimera Breakdown", 3);

                        if (singleRunResult is IChimeraBreakdownCompatible cb)
                            cb.GetChimeraBreakdownFile();
                    }

                    singleRunResult.Override = false;
                }

                if (Parameters.CountChimericResults)
                {
                    cellLine.Override = Parameters.Override;
                    Log("Counting Cell Line Chimeric Identifications", 2);

                    cellLine.CountChimericPsms();
                    cellLine.CountChimericPeptides();
                }

                if (Parameters.RunResultCounting)
                {
                    cellLine.Override = Parameters.Override;
                    Log("Counting All Cell Line Search Results", 2);

                    cellLine.GetBulkResultCountComparisonFile();
                    cellLine.GetIndividualFileComparison();

                    cellLine.Override = false;
                    cellLine.PlotIndividualFileResults(ResultType.Psm);
                    cellLine.PlotIndividualFileResults(ResultType.Peptide);
                    cellLine.PlotIndividualFileResults(ResultType.Protein);
                }

                if (Parameters.RunFdrAnalysis)
                {
                    cellLine.Override = Parameters.Override;
                    Log("Running Cell Line FDR Analysis", 2);

                    cellLine.Override = false;
                    cellLine.PlotCellLineSpectralSimilarity();
                    cellLine.PlotCellLineRetentionTimePredictions();
                    cellLine.PlotChronologerDeltaKernelPDF();
                    cellLine.PlotChronologerVsPercentHi();
                    cellLine.PlotCellLineRetentionTimeVsChronologerPredictionBubble();
                    try
                    {
                        cellLine.PlotAverageRetentionTimeShiftPlotKernelPdf();
                        cellLine.PlotAverageRetentionTimeShiftPlotHistogram();
                    }
                    catch
                    {
                    }
                }

                if (Parameters.RunChimeraBreakdown)
                {
                    cellLine.Override = Parameters.Override;
                    Log("Running Cell Line Chimera Breakdown", 2);

                    cellLine.GetChimeraBreakdownFile();

                    cellLine.Override = false;
                    cellLine.PlotCellLineChimeraBreakdown();
                    cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
                    cellLine.PlotChimeraBreakdownByMassAndCharge();
                }

                if (Parameters.RunModificationAnalysis)
                {
                    cellLine.Override = Parameters.Override;
                    Log("Running Modification Analysis", 2);

                    cellLine.PlotModificationDistribution();
                }
            }


            Log("Running Bulk Result Analysis");
            if (Parameters.CountChimericResults)
            {
                allResults.Override = Parameters.Override;
                Log("Counting Bulk Results Chimeric Identifications");

                allResults.CountChimericPsms();
                allResults.CountChimericPeptides();
            }

            if (Parameters.RunResultCounting)
            {
                allResults.Override = Parameters.Override;
                Log("Counting All Bulk Results Search Results");

                allResults.PlotInternalMMComparison();
                allResults.IndividualFileComparison();
                allResults.GetBulkResultCountComparisonFile();

                allResults.Override = false;
                allResults.PlotBulkResultComparisons();
                allResults.PlotStackedIndividualFileComparison();
            }

            if (Parameters.RunFdrAnalysis)
            {
                allResults.Override = Parameters.Override;
                Log("Running Bulk Results FDR Analysis");

                allResults.Override = false;
                allResults.PlotGridChronologerDeltaPlotKernalPDF();
                allResults.PlotBulkChronologerDeltaPlotKernalPDF();
            }

            if (Parameters.RunChimeraBreakdown)
            {
                allResults.Override = Parameters.Override;
                Log("Running Bulk Results Chimera Breakdown");

                allResults.GetChimeraBreakdownFile();

                allResults.Override = false;
                allResults.PlotBulkResultChimeraBreakDown();
                allResults.PlotBulkResultChimeraBreakDown_TargetDecoy();
            }
        }
    }
}
