using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Interfaces;
using Analyzer.Plotting.ComparativePlots;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;

namespace TaskLayer.ChimeraAnalysis
{
    public class RunSpecificChimeraPaperProcess : BaseResultAnalyzerTask
    {
        public override MyTask MyTask => MyTask.RunSpecificChimeraPaperProcess;
        protected override ChimeraPaperAnalysisParameters Parameters { get; }

        public RunSpecificChimeraPaperProcess(ChimeraPaperAnalysisParameters parameters)
        {
            Parameters = parameters;
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
                        singleRunResult.Override = true;
                        singleRunResult.GetBulkResultCountComparisonFile();
                        singleRunResult.Override = false;
                    }

                    if (singleRunResult is MsPathFinderTResults mspt)
                    {

                    }

                    if (singleRunResult is IChimeraBreakdownCompatible cbc)
                    {

                    }
                }

                cellLine.Override = Parameters.Override;
                cellLine.GetBulkResultCountComparisonFile();
                cellLine.PlotIndividualFileResults(ResultType.Psm);
                cellLine.PlotIndividualFileResults(ResultType.Peptide);
                cellLine.PlotIndividualFileResults(ResultType.Protein);
                cellLine.Override = false;
            }

            allResults.Override = Parameters.Override;
            allResults.GetBulkResultCountComparisonFile();
            allResults.Override = false;
            allResults.PlotBulkResultComparisons();
            allResults.PlotInternalMMComparison();
            allResults.PlotStackedIndividualFileComparison(ResultType.Psm);
            allResults.PlotStackedIndividualFileComparison(ResultType.Peptide);
            allResults.PlotStackedIndividualFileComparison(ResultType.Protein);
        }

     
    }
}
