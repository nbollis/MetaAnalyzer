using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Interfaces;
using Analyzer.Plotting.AggregatePlots;
using Analyzer.Plotting.ComparativePlots;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;

namespace TaskLayer.ChimeraAnalysis
{
    public class ChimeraPaperTopDownTask : BaseResultAnalyzerTask
    {
        public override MyTask MyTask => MyTask.ChimeraPaperTopDown;
        protected override ChimeraPaperAnalysisParameters Parameters { get; }

        public ChimeraPaperTopDownTask(ChimeraPaperAnalysisParameters parameters) : base()
        {
            Parameters = parameters;
        }

        protected override void RunSpecific()
        {
            var allResults = BuildResultsObjects();

            foreach (var cellLine in allResults)
            {
                Log($"Starting Processing of {cellLine.CellLine}");
                foreach (var singleRunResult in cellLine)
                {
                    Log($"Processing {singleRunResult.Condition}", 2);

                    singleRunResult.Override = Parameters.Override;
                    Log("Counting Chimeric Identifications", 3);
                    if (Parameters.CountChimericResults)
                    {
                        singleRunResult.CountChimericPsms();
                        if (singleRunResult is IChimeraPeptideCounter pc)
                            pc.CountChimericPeptides();
                    }

                    singleRunResult.Override = Parameters.Override;
                    Log("Counting All Search Results", 3);
                    if (Parameters.RunResultCounting)
                    {
                        singleRunResult.GetIndividualFileComparison();
                        singleRunResult.GetBulkResultCountComparisonFile();
                    }

                    singleRunResult.Override = Parameters.Override;
                    Log("Running FDR Analysis", 3);
                    if (Parameters.RunFdrAnalysis)
                    {
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

                    singleRunResult.Override = Parameters.Override;
                    Log("Running Chimera Breakdown", 3);
                    if (Parameters.RunChimeraBreakdown)
                    {
                        if (singleRunResult is IChimeraBreakdownCompatible cb)
                            cb.GetChimeraBreakdownFile();
                    }
                    
                    singleRunResult.Override = false;
                }

                cellLine.Override = Parameters.Override;
                Log("Counting Cell Line Chimeric Identifications", 2);
                if (Parameters.CountChimericResults)
                {
                    cellLine.CountChimericPsms();
                    cellLine.CountChimericPeptides();
                }

                cellLine.Override = Parameters.Override;
                Log("Counting All Cell Line Search Results", 2);
                if (Parameters.RunResultCounting)
                {
                    cellLine.GetBulkResultCountComparisonFile();
                    cellLine.GetIndividualFileComparison();

                    cellLine.Override = false;
                    cellLine.PlotIndividualFileResults();
                }

                cellLine.Override = Parameters.Override;
                Log("Running Cell Line FDR Analysis", 2);
                if (Parameters.RunFdrAnalysis)
                {
                    cellLine.Override = false;
                    cellLine.PlotCellLineSpectralSimilarity();
                }

                cellLine.Override = Parameters.Override;
                Log("Running Cell Line Chimera Breakdown", 2);
                if (Parameters.RunChimeraBreakdown)
                {
                    cellLine.GetChimeraBreakdownFile();

                    cellLine.Override = false;
                    cellLine.PlotCellLineChimeraBreakdown();
                    cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
                    cellLine.PlotChimeraBreakdownByMassAndCharge();
                }
            }


            Log("Running Bulk Result Analysis");
            allResults.Override = Parameters.Override;
            Log("Counting Cell Line Chimeric Identifications");
            if (Parameters.CountChimericResults)
            {
                allResults.CountChimericPsms();
                allResults.CountChimericPeptides();
            }

            allResults.Override = Parameters.Override;
            Log("Counting All Cell Line Search Results");
            if (Parameters.RunResultCounting)
            {
                allResults.IndividualFileComparison();
                allResults.GetBulkResultCountComparisonFile();

                allResults.Override = false;
                allResults.PlotBulkResultComparisons();
                allResults.PlotStackedIndividualFileComparison();
            }

            allResults.Override = Parameters.Override;
            Log("Running Cell Line FDR Analysis");
            if (Parameters.RunFdrAnalysis)
            {
                
            }

            allResults.Override = Parameters.Override;
            Log("Running Cell Line Chimera Breakdown");
            if (Parameters.RunChimeraBreakdown)
            {
                allResults.GetChimeraBreakdownFile();

                allResults.Override = false;
                allResults.PlotBulkResultChimeraBreakDown();
                allResults.PlotBulkResultChimeraBreakDown_TargetDecoy();
            }

        }

        private AllResults BuildResultsObjects()
        {
            var allResults = new AllResults(Parameters.InputDirectoryPath);
            if (Parameters.RunOnAll)
                return allResults;

            var cellLines = new List<CellLineResults>();
            foreach (var cellLine in allResults)
            {
                var selector = cellLine.GetAllSelectors();

                var runResults = new List<SingleRunResults>();
                foreach (var singleRunResult in cellLine)
                {
                    if (selector.Contains(singleRunResult.Condition))
                        runResults.Add(singleRunResult);
                }
                cellLines.Add(new CellLineResults(cellLine.DirectoryPath, runResults));
            }

            allResults = new AllResults(Parameters.InputDirectoryPath, cellLines);
            return allResults;
        }
    }
}
