using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.FileTypes.Internal;
using Plotly.NET.CSharp;
using Proteomics.PSM;

namespace Analyzer.Plotting
{
    public class PepEvaluationPlot
    {

        public List<PepAnalysis> AllResults { get; set; }


        public PepEvaluationPlot(List<PepAnalysis> allResults)
        {
            AllResults = allResults;
        }

        public PepEvaluationPlot(string filePath)
        {
            var pepAnalysis = new PepAnalysisForPercolatorFile(filePath);
            pepAnalysis.LoadResults();
            AllResults = pepAnalysis.Results;
        }






        public void GenerateChart()
        {
            
        }
    }
}
