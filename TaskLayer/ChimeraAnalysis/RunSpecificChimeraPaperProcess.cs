using Analyzer.Interfaces;
using Analyzer.SearchType;

namespace TaskLayer.ChimeraAnalysis
{
    public class RunSpecificChimeraPaperProcess : BaseResultAnalyzerTask
    {
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
                    if (singleRunResult is MetaMorpheusResult mm)
                    {

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
