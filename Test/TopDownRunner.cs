using Analyzer.Interfaces;
using Analyzer.Plotting.AggregatePlots;
using Analyzer.Plotting.ComparativePlots;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using Proteomics.PSM;
using UsefulProteomicsDatabases;

namespace Test
{
    internal class TopDownRunner
    {
        internal static string DirectoryPath = @"B:\Users\Nic\Chimeras\TopDown_Analysis";
        internal static bool RunOnAll = true;
        internal static bool Override = false;
        private static AllResults? _allResults;
        internal static AllResults AllResults => _allResults ??= new AllResults(DirectoryPath);

        [OneTimeSetUp]
        public static void OneTimeSetup() { Loaders.LoadElements(); }





        [Test]
        public static void PaperInternalMMParsing()
        {
            string path = @"B:\Users\Nic\Chimeras\InternalMMAnalysis\TopDown_Jurkat\Jurkat";
            var groupedRuns = Directory.GetDirectories(path)
                .Select(p => new MetaMorpheusResult(p))
                .GroupBy(p => p.Condition.ConvertConditionName())
                .ToDictionary(p => p.Key, p => p.ToList());



        }




        [Test]
        public static void RunAllParsing()
        {
            foreach (var cellLine in AllResults)
            {
                foreach (var result in cellLine)
                {
                    //result.Override = true;
                    result.CountChimericPsms();
                    result.GetBulkResultCountComparisonFile();
                    result.GetIndividualFileComparison();
                    if (result is IChimeraBreakdownCompatible cb)
                        cb.GetChimeraBreakdownFile();
                    if (result is IChimeraPeptideCounter pc)
                        pc.CountChimericPeptides();
                    if (result is MetaMorpheusResult mm)
                    {
                        mm.PlotPepFeaturesScatterGrid();
                        mm.ExportCombinedChimeraTargetDecoyExploration(mm.FigureDirectory, mm.Condition);
                        mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, false);
                        mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, true);
                        mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, false);
                        mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, true);
                    }
                    result.Override = false;
                }

                cellLine.Override = true;
                cellLine.GetIndividualFileComparison();
                cellLine.GetBulkResultCountComparisonFile();
                cellLine.CountChimericPsms();
                cellLine.CountChimericPeptides();
                cellLine.Override = false;

                cellLine.PlotIndividualFileResults();
                cellLine.PlotCellLineSpectralSimilarity();
                cellLine.PlotCellLineChimeraBreakdown();
                cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
                cellLine.PlotModificationDistribution(ResultType.Psm, false);
                cellLine.PlotModificationDistribution(ResultType.Peptide, false);

                cellLine.Dispose();
            }

            AllResults.Override = true;
            AllResults.IndividualFileComparison();
            AllResults.GetBulkResultCountComparisonFile();
            AllResults.CountChimericPsms();
            AllResults.CountChimericPeptides();

        }

        [Test]
        public static void GenerateAllFigures()
        {
            //foreach (CellLineResults cellLine in AllResults)
            //{
            //    foreach (var individualResult in cellLine)
            //    {
            //        if (individualResult is not MetaMorpheusResult mm) continue;
            //        //mm.PlotPepFeaturesScatterGrid();
            //        //mm.ExportCombinedChimeraTargetDecoyExploration(mm.FigureDirectory, mm.Condition);
            //    }

            //    cellLine.PlotIndividualFileResults();
            //    cellLine.PlotCellLineSpectralSimilarity();
            //    cellLine.PlotCellLineChimeraBreakdown();
            //    cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
            //}

            //AllResults.PlotInternalMMComparison();
            AllResults.PlotBulkResultComparisons();
            //AllResults.PlotStackedIndividualFileComparison();
            //AllResults.PlotBulkResultChimeraBreakDown();
            //AllResults.PlotBulkResultChimeraBreakDown_TargetDecoy();
        }

        [Test]
        public static void GenerateSpecificFigures()
        {
            var a549 = BottomUpRunner.AllResults.First();
            a549.PlotModificationDistribution();
            a549.PlotModificationDistribution(ResultType.Peptide);
            var jurkat = AllResults.Skip(1).First();
            jurkat.PlotModificationDistribution();
            jurkat.PlotModificationDistribution(ResultType.Peptide);
            //a549.PlotAccuracyByModificationType();
            //a549.PlotChronologerDeltaKernelPDF();
        }


        [Test]
        public static void MsPathTDatasetInfoGenerator()
        {

            var path =
                @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\SearchResults\MsPathFinderTWithMods_15Rep2_Final";
            var result = new MsPathFinderTResults(path);


            foreach (MsPathFinderTResults mspt in AllResults.SelectMany(p => p.Results)
                .Where(p => p is MsPathFinderTResults mspt && mspt.IndividualFileResults.Count is 20 or 43 or 10))
            {
                mspt.CreateDatasetInfoFile();
            }
        }


        [Test]
        public static void DetermineChimericResultsScanPercentForPaper()
        {
            // get the single result selector result file from each cell line
            var data =
                AllResults.SelectMany(p => p.Where(m => p.GetSingleResultSelector().Contains(m.Condition))).ToList();
            data.AddRange(BottomUpRunner.AllResults.SelectMany(p => p.Where(m => p.GetSingleResultSelector().Contains(m.Condition))).ToList());

            var results = data.ToDictionary(p => (p.Condition, p.DatasetName),
                        p => ((MetaMorpheusResult)p).AllPsms
                            .Where(p => p is { DecoyContamTarget: "T", PEP_QValue: <= 0.01 })
                            .ToChimeraGroupedDictionary());


            var percentResults = results.ToDictionary(p => p.Key,
                p => p.Value
                    .OrderBy(n => n.Key)
                    .ToDictionary(
                    m => m.Key, 
                    m => m.Value.Count / (double)p.Value.Sum(p => p.Value.Count) * 100));

            var singles = percentResults.Select(p => (p.Key.Item1, p.Key.Item2, p.Value[1])).ToList();
            
            var topDown = singles.Take(AllResults.Count());
            var bottomUp = singles.Skip(AllResults.Count());

            var topDownAveragePercent = topDown.Average(p => p.Item3);
            var bottomUpAveragePercent = bottomUp.Average(p => p.Item3);
        }





        [Test]
        public static void RunProteinCountingAndProForma()
        {
            List<string> errors = new();
            foreach (var cellLine in AllResults)
            {
                foreach (var result in cellLine)
                {
                    try
                    {
                        result.CountProteins();
                    }
                    catch (Exception e)
                    {
                        errors.Add($"Counting: {result.Condition} {result.DatasetName} {e.Message}");
                    }

                    try
                    {
                        result.ToPsmProformaFile();
                    }
                    catch (Exception e)
                    {
                        errors.Add($"ProForma: {result.Condition} {result.DatasetName} {e.Message}");
                    }
                }
            }

            errors.ForEach(Console.WriteLine);
        }

    

        [Test]
        public static void IsabellaData()
        {

            AllResults.PlotBulkResultComparisons();
        }


    }
}
