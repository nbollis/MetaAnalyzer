using System.Diagnostics;
using Analyzer.Interfaces;
using Analyzer.Plotting;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using ResultAnalyzerUtil;

namespace TaskLayer.ChimeraAnalysis
{
    public class RunSpecificChimeraPaperProcess : BaseResultAnalyzerTask
    {
        public object plottingLock = new object();
        public override MyTask MyTask => MyTask.RunSpecificChimeraPaperProcess;
        public override ChimeraPaperAnalysisParameters Parameters { get; }

        public RunSpecificChimeraPaperProcess(ChimeraPaperAnalysisParameters parameters)
        {
            Parameters = parameters;
            Condition = Path.GetFileNameWithoutExtension(parameters.InputDirectoryPath);
        }

        protected override void RunSpecific()
        {
            var allResults = BuildChimeraPaperResultsObjects();

            foreach (var cellLine in allResults)
            {
                foreach (var singleRunResult in cellLine)
                {
                    if (singleRunResult is MetaMorpheusResult mmResult)
                    {
                        Log($"Processing {mmResult.DatasetName} {mmResult.Condition}", 1);

                        Log($"Tabulating Result Counts: {mmResult.DatasetName} {mmResult.Condition}", 2);
                        var indFile = mmResult.GetIndividualFileComparison();
                        AnalyzerGenericPlots.IndividualFileResultBarChart([indFile], out int width, out int height, mmResult.DatasetName, mmResult.IsTopDown)
                            .SaveInRunResultOnly(mmResult, $"IndividualFileComparison_{mmResult.DatasetName}_{mmResult.Condition}", width, height);


                        var bulkFile = mmResult.GetBulkResultCountComparisonFile();
                        AnalyzerGenericPlots.BulkResultBarChart(bulkFile.Results, mmResult.IsTopDown)
                            .SaveInRunResultOnly(mmResult, $"BulkResultComparison_{mmResult.DatasetName}_{mmResult.Condition}_Psm", 1000, 600);
                        AnalyzerGenericPlots.BulkResultBarChart(bulkFile.Results, mmResult.IsTopDown, ResultType.Peptide)
                            .SaveInRunResultOnly(mmResult, $"BulkResultComparison_{mmResult.DatasetName}_{mmResult.Condition}_Peptide", 1000, 600);


                        Log($"Counting Chimeric Psms/Peptides: {mmResult.DatasetName} {mmResult.Condition}", 2);
                        mmResult.CountChimericPsms();
                        mmResult.CountChimericPeptides();

                        Log($"Running Chimera Breakdown Analysis: {mmResult.DatasetName} {mmResult.Condition}", 2);
                        var sw = Stopwatch.StartNew();
                        _ = mmResult.GetChimeraBreakdownFile();
                        sw.Stop();

                        // if it takes less than one minute to get the breakdown, and we are not overriding, do not plot
                        if (sw.Elapsed.Minutes < 1 && !Parameters.Override)
                            return;
                        lock (plottingLock)
                        {
                            mmResult.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Peptide);
                            mmResult.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Psm);
                            mmResult.PlotChimeraBreakDownStackedColumn(ResultType.Psm);
                            mmResult.PlotChimeraBreakDownStackedColumn(ResultType.Peptide);
                        }


                        Log($"Running Chimeric Spectrum Summaries", 0);
                        var parameters = new SingleRunAnalysisParameters(mmResult, Parameters.Override, false);
                        var summaryTask = new SingleRunChimericSpectrumSummaryTask(parameters);
                        summaryTask.Run().Wait();
                    }

                    if (singleRunResult is MsFraggerResult msf)
                    {

                    }

                    if (singleRunResult is ProteomeDiscovererResult pd)
                    {
                        
                    }

                    if (singleRunResult is MsPathFinderTResults mspt)
                    {

                    }

                    if (singleRunResult is IChimeraBreakdownCompatible cbc)
                    {

                    }

                    if (singleRunResult is IRetentionTimePredictionAnalysis rt)
                    {
                        //singleRunResult.Override = Parameters.Override;
                        //rt.CreateRetentionTimePredictionFile();
                        //singleRunResult.Override = false;
                    }
                }

                //cellLine.Override = Parameters.Override;
                //cellLine.GetBulkResultCountComparisonFile();
                //cellLine.PlotIndividualFileResults(ResultType.Psm);
                //cellLine.PlotIndividualFileResults(ResultType.Peptide);
                //cellLine.PlotIndividualFileResults(ResultType.Protein);
                //cellLine.Override = false;
                cellLine.Dispose();
            }

            //allResults.Override = Parameters.Override;
            //allResults.GetBulkResultCountComparisonFile();
            //allResults.Override = false;
            //allResults.PlotBulkResultComparisons();
            //allResults.PlotInternalMMComparison();
            //allResults.PlotStackedIndividualFileComparison(ResultType.Psm);
            //allResults.PlotStackedIndividualFileComparison(ResultType.Peptide);
            //allResults.PlotStackedIndividualFileComparison(ResultType.Protein);
        }

     
    }
}
