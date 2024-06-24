using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Interfaces;
using Analyzer.Plotting.ComparativePlots;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;

namespace TaskLayer.ChimeraAnalysis
{
    public class RunSpecificChimeraPaperProcess : BaseResultAnalyzerTask
    {
        public override MyTask MyTask => MyTask.RunSpecificChimeraPaperProcess;
        protected override ChimeraPaperAnalysisParameters Parameters { get; }

        public RunSpecificChimeraPaperProcess(ChimeraPaperAnalysisParameters parameters) : base()
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
                cellLine.Override = false;
            }

            allResults.Override = Parameters.Override;
            allResults.GetBulkResultCountComparisonFile();
            allResults.Override = false;
            allResults.PlotBulkResultComparisons();
        }

     
    }
}
