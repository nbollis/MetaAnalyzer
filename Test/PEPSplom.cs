using Analyzer.FileTypes.Internal;
using Analyzer.Plotting;
using Analyzer.Plotting.ComparativePlots;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.SearchType;
using Analyzer.Util;
using Plotly.NET;

namespace Test
{
    internal class PEPSplom
    {
        internal static string TestPath = @"B:\Users\Nic\Chimeras\PEPTesting\AllPSMs_FormattedForPercolator.tab";

        internal static string Small_TopDown_ChimeraPath =
            @"B:\Users\Nic\Chimeras\PEPTesting\Small_ChimeraIncorporation\AllPSMs_FormattedForPercolator.tab";


        [Test]
        public static void TestSplom()
        {
            var pepAnalysis = new PepAnalysisForPercolatorFile(Small_TopDown_ChimeraPath);
            pepAnalysis.LoadResults();
            var allResults = pepAnalysis.Results;

            var pepEvaluationPlot = new PepEvaluationPlot(allResults);
            pepEvaluationPlot.ShowChart();
        }



        [Test]
        public static void PlotAsCellLine()
        {
            string dirPath = @"B:\Users\Nic\Chimeras\PEPTesting";
            string figPath = @"B:\Users\Nic\Chimeras\PEPTesting\Figures";
            var cellLine = new CellLineResults(dirPath);
            foreach (MetaMorpheusResult result in cellLine)
            {
                result.CountChimericPsms();
                result.CountChimericPeptides();
                result.GetBulkResultCountComparisonFile();
                result.GetIndividualFileComparison();
                //result.PlotPepFeaturesScatterGrid();
                //result.ExportCombinedChimeraTargetDecoyExploration(figPath,
                //    new KeyValuePair<string, string>(result.Condition, result.Condition));

            }
            cellLine.PlotIndividualFileResults();
            cellLine.GetBulkResultCountComparisonFile();
            cellLine.CountChimericPsms();
            cellLine.CountChimericPeptides();
        }

        [Test]
        public static void TopDownBottomUpNormalizationComparison()
        {
            string helaNoNormPath =
                @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\Hela\SearchResults\MetaMorpheus_NoNormalization";
            string helaNormPath =
                @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\Hela\SearchResults\MetaMorpheusWithLibrary";
            string jurkatNormPath =
                @"B:\Users\Nic\Chimeras\PEPTesting\SearchResults\Full_ChimeraIncorporation";
            string jurkatNoNormpath = 
                @"B:\Users\Nic\Chimeras\PEPTesting\SearchResults\Full_ChimeraIncorporation_NoNormalization";
            string originalMMPath = 
                @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\SearchResults\MetaMorpheus";
            string jurkatNoNormRemove2 =
                @"B:\Users\Nic\Chimeras\PEPTesting\SearchResults\Jurkat_NoNorm_Remove2";
            string jurkatNormRemove2 =
                @"B:\Users\Nic\Chimeras\PEPTesting\SearchResults\Jurkat_Norm_Remove2";

            List<(MetaMorpheusResult, string)> results = new()
            {
                //(new MetaMorpheusResult(helaNoNormPath), "Hela No Norm"),
                //(new MetaMorpheusResult(helaNormPath), "Hela Norm"),
                (new MetaMorpheusResult(jurkatNormPath), "Jurkat Norm"),
                (new MetaMorpheusResult(jurkatNoNormRemove2), "Jurkat No Norm-2"),
                (new MetaMorpheusResult(jurkatNormRemove2), "Jurkat Norm-2"),
                (new MetaMorpheusResult(jurkatNoNormpath), "Jurkat No Norm"),
                (new MetaMorpheusResult(originalMMPath), "Original Jurkat"),

            };

            results.ForEach(p => p.Item1.GetIndividualFileComparison());
            var temp = results.Select(p => p.Item1.IndividualFileComparisonFile).ToList();
            
            var chart = GenericPlots.IndividualFileResultBarChart(temp,
                out int width, out int height, "PEP Exploration", true, ResultType.Psm);
            chart.Show();

            string figPath = @"B:\Users\Nic\Chimeras\PEPTesting\Figures\BUTDNormComparison";
            foreach (var result in results)
            {
                result.Item1.PlotPepFeaturesScatterGrid(result.Item2);
                result.Item1.ExportCombinedChimeraTargetDecoyExploration(figPath, result.Item2);
            }

        }
    }
}
