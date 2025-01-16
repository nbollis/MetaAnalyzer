using System.Numerics;
using Analyzer;
using Analyzer.Interfaces;
using Analyzer.Plotting;
using Analyzer.Plotting.ComparativePlots;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Easy.Common.Extensions;
using Plotly.NET;
using ResultAnalyzerUtil;
using TaskLayer.ChimeraAnalysis;
using UsefulProteomicsDatabases;

namespace Test.ChimeraPaper
{
    internal class BottomUpRunner
    {
        internal static string DirectoryPath = @"B:\Users\Nic\Chimeras\Mann_11cell_analysis";
        internal static bool RunOnAll = true;
        internal static bool Override = false;

        internal static AllResults? _allResults;
        internal static AllResults AllResults => _allResults ??= new AllResults(DirectoryPath, Directory.GetDirectories(DirectoryPath)
                   .Where(p => !p.Contains("Figures") && !p.Contains("ProcessedResults") && !p.Contains("Prosight") && RunOnAll || p.Contains("Hela"))
                   .Select(datasetDirectory => new CellLineResults(datasetDirectory)).ToList());


        internal static List<MsFraggerResult> MsFraggerResults => new AllResults(DirectoryPath, Directory.GetDirectories(DirectoryPath)
            .Where(p => !p.Contains("Figures") && !p.Contains("ProcessedResults") && !p.Contains("Prosight"))
            .Select(datasetDirectory => new CellLineResults(datasetDirectory)).ToList())
            .SelectMany(cellLine => cellLine.Results.Where(result => result.Condition.Contains("DDA+") && result is MsFraggerResult).Cast<MsFraggerResult>())
            .ToList();

        internal static ProteomeDiscovererResult ChimerysResult => new ProteomeDiscovererResult(
                           @"B:\Users\Nic\Chimeras\Chimerys\Chimerys");


        #region Submission

        internal static string SubmissionDirectoryPath =
            @"D:\Projects\Chimeras\SumbissionDirectory\Mann_11cell_analysis";
        internal static List<MsFraggerResult> DdaPlusResults => new AllResults(SubmissionDirectoryPath,
                Directory.GetDirectories(SubmissionDirectoryPath)
                    .Where(p => !p.Contains("Figures") && !p.Contains("ProcessedResults") && !p.Contains("Prosight"))
                    .Select(datasetDirectory => new CellLineResults(datasetDirectory)).ToList())
            .SelectMany(cellLine => cellLine.Results.Where(result => result.Condition.Contains("DDA+") && result is MsFraggerResult).Cast<MsFraggerResult>())
            .ToList();

        #endregion

        [OneTimeSetUp]
        public static void OneTimeSetup() { Loaders.LoadElements(); }


        [Test]
        public static void PlotAllFigures()
        {


            foreach (CellLineResults cellLine in AllResults)
            {
                cellLine.PlotIndividualFileResults(ResultType.Psm);
                //cellLine.PlotIndividualFileResults(ResultType.Peptide);
                //cellLine.PlotIndividualFileResults(ResultType.Protein);
                //cellLine.PlotCellLineRetentionTimePredictions();
                //cellLine.PlotCellLineSpectralSimilarity();
                //cellLine.PlotCellLineChimeraBreakdown();
                //cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
                //cellLine.PlotChronologerDeltaKernelPDF();
                //cellLine.PlotChronologerVsPercentHi();
                foreach (var individualResult in cellLine
                             .Where(p => cellLine.GetSingleResultSelector().Contains(p.Condition)))
                {
                    if (individualResult is not MetaMorpheusResult mm) continue;

                    mm.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Psm);
                    mm.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Peptide);

                    //mm.PlotPepFeaturesScatterGrid();
                    //mm.PlotTargetDecoyCurves();
                    //mm.ExportCombinedChimeraTargetDecoyExploration(mm.FigureDirectory, mm.Condition);
                }
                //cellLine.Dispose(); 
                cellLine.PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Psm);
                cellLine.PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Peptide);
            }

            //AllResults.PlotInternalMMComparison();
            //AllResults.PlotBulkResultComparisons();
            //AllResults.PlotStackedIndividualFileComparison();
            //AllResults.PlotBulkResultChimeraBreakDown();
            //AllResults.PlotStackedSpectralSimilarity();
            //AllResults.PlotAggregatedSpectralSimilarity();
            //AllResults.PlotBulkResultChimeraBreakDown();
            //AllResults.PlotBulkResultChimeraBreakDown_TargetDecoy();
            //AllResults.PlotChronologerVsPercentHi();
            //AllResults.PlotBulkChronologerDeltaPlotKernalPDF();
            //AllResults.PlotGridChronologerDeltaPlotKernalPDF();

            AllResults.PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Psm);
            AllResults.PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Peptide);
            TopDownRunner.AllResults.PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Psm);
            TopDownRunner.AllResults.PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Peptide);
            TopDownRunner.AllResults.First().PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Psm);
            TopDownRunner.AllResults.First().PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Peptide);
            TopDownRunner.AllResults.Skip(1).First().PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Psm);
            TopDownRunner.AllResults.Skip(1).First().PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Peptide);
        }
        [Test]
        public static void RunAllParsing()
        {
            // Got to K562
            foreach (CellLineResults cellLine in AllResults)
            {
                foreach (var result in cellLine)
                {
                    //if (result is MetaMorpheusResult)
                    //    continue;
                    //result.Override = true;
                    result.GetIndividualFileComparison();
                    result.GetBulkResultCountComparisonFile();
                    result.CountChimericPsms();

                    if (result is IChimeraBreakdownCompatible cb)
                        cb.GetChimeraBreakdownFile();
                    if (result is IChimeraPeptideCounter pc)
                        pc.CountChimericPeptides();
                    if (result is MetaMorpheusResult mm)
                    {
                        mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, false);
                        mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, true);
                        mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, false);
                        mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, true);
                    }

                    //result.Override = false;
                }

                try
                {
                    cellLine.PlotModificationDistribution(ResultType.Psm, false);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                try
                {
                    cellLine.PlotModificationDistribution(ResultType.Peptide, false);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                cellLine.GetMaximumChimeraEstimationFile();
                cellLine.GetChimeraBreakdownFile();

                cellLine.Override = true;
                cellLine.GetIndividualFileComparison();
                cellLine.GetBulkResultCountComparisonFile();
                cellLine.CountChimericPsms();
                cellLine.CountChimericPeptides();
                cellLine.Override = false;


                cellLine.PlotModificationDistribution(ResultType.Psm, false);
                cellLine.PlotModificationDistribution(ResultType.Peptide, false);
                cellLine.Dispose();
            }

            AllResults.Override = true;
            AllResults.GetBulkResultCountComparisonFile();
            AllResults.IndividualFileComparison();
            AllResults.CountChimericPsms();
            AllResults.CountChimericPeptides();
            AllResults.GetChimeraBreakdownFile();
            AllResults.Override = false;
        }





        [Test]
        public static void WeekendRunner()
        {
            //RunProformaForAllInMann11ResultDirectory();
            //RunProformaShit_TempChimerys();
            //RunProformaShit_SumbissionDirectory();
            RunProteinCounting();
        }

        [Test]
        public static void RunProformaForAllInMann11ResultDirectory()
        {
            var conditionGroupedResult = AllResults.SelectMany(p => p.Results)
                .GroupBy(p => p.Condition)
                .Where(p => p.Count() == 11)
                .ToDictionary(p => p.Key, p => p.ToList());

            var bigResultPath = @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\ProcessedResults";
            if (!Directory.Exists(bigResultPath))
                Directory.CreateDirectory(bigResultPath);

            foreach (var group in conditionGroupedResult)
            {
                if (group.Value.First() is MetaMorpheusResult mm && !mm.IndividualFileResults.Any())
                    continue;

                foreach (var result in group.Value)
                {
                    result.Override = true;
                    //if (result is MsFraggerResult)
                    result.ToPsmProformaFile();
                    result.Override = false;
                    result.Dispose();
                }

                var allRecords = group.Value.SelectMany(p => p.ToPsmProformaFile().Results).ToList();
                var proforomaFileName = Path.Combine(bigResultPath, group.Key + "_PSM_" + FileIdentifiers.ProformaFile);
                var newFile = new ProformaFile(proforomaFileName)
                {
                    Results = allRecords
                };
                newFile.WriteResults(proforomaFileName);

                group.Value.ForEach(p => p.Dispose());
            }
        }

        [Test]
        public static void RunProformaShit_SumbissionDirectory()
        {
            string resultDirectoryPath = @"D:\Projects\Chimeras\SumbissionDirectory\Mann_11cell_analysis";
            var allResults = new AllResults(resultDirectoryPath, Directory.GetDirectories(resultDirectoryPath)
                           .Where(p => !p.Contains("Figures") && !p.Contains("Prosight") && !p.Contains("ProcessedResults"))
                           .Select(datasetDirectory => new CellLineResults(datasetDirectory)).ToList());

            foreach (var cellLine in allResults)
            {
                foreach (var result in cellLine.Where(m => m is MsFraggerResult))
                {
                    result.Override = true;
                    result.ToPsmProformaFile();
                    result.Override = false;
                    result.Dispose();
                }
                cellLine.Dispose();
            }

            var bigResultPath = @"D:\Projects\Chimeras\SumbissionDirectory\Mann_11cell_analysis\ProcessedResults";

            var temp = allResults.SelectMany(p => p.Results)
                .GroupBy(p => p.Condition).ToDictionary(p => p.Key, p => p.ToList());

            foreach (var condition in temp)
            {
                var proforomaFileName = Path.Combine(bigResultPath, condition.Key + "_PSM_" + FileIdentifiers.ProformaFile);
                var records = new List<ProformaRecord>();
                foreach (var result in condition.Value)
                {
                    records.AddRange(result.ToPsmProformaFile().Results);
                }

                var newFile = new ProformaFile(proforomaFileName)
                {
                    Results = records
                };

                newFile.WriteResults(proforomaFileName);
                condition.Value.ForEach(p => p.Dispose());
            }
        }

        [Test]
        public static void RunProformaShit_TempChimerys()
        {
            var dirPath = ExternalComparisonTask.Mann11OutputDirectory;
            List<SingleRunResults> results = new();
            foreach (var cellLineDir in Directory.GetDirectories(dirPath).Where(p =>
                             !p.Contains("Figures") && !p.Contains("Genera") && !p.Contains("Prosight") && !p.Contains("ProcessedResults")))
            {
                foreach (var indRunDir in Directory.GetDirectories(cellLineDir))
                {
                    if (indRunDir.Contains("Figure"))
                        continue;

                    SingleRunResults result;
                    if (indRunDir.Contains("MetaM"))
                        result = new MetaMorpheusResult(indRunDir);
                    else
                        result = new ProteomeDiscovererResult(indRunDir);
                    results.Add(result);
                }
            }

            foreach (var result in results)
            {
                if (result is MetaMorpheusResult)
                    continue;

                result.Override = true;
                result.GetIndividualFileComparison();
                result.GetBulkResultCountComparisonFile();
                result.ToPsmProformaFile();
            }
        }

        [Test]
        public static void RunProteinCounting()
        {
            // Mann 11 Directory
            var conditionGroupedResult = AllResults.SelectMany(p => p.Results)
                .GroupBy(p => p.Condition)
                .Where(p => p.Count() == 11)
                .ToDictionary(p => p.Key, p => p.ToList());

            var bigResultPath = @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\ProcessedResults";
            if (!Directory.Exists(bigResultPath))
                Directory.CreateDirectory(bigResultPath);

            foreach (var group in conditionGroupedResult)
            {
                if (group.Value.First() is MetaMorpheusResult mm && !mm.IndividualFileResults.Any())
                    continue;
                var finalResultOutPath = Path.Combine(bigResultPath, group.Key + "_" + FileIdentifiers.ProteinCountingFile);

                // Remove this to override
                //if (File.Exists(finalResultOutPath))
                //    continue;

                foreach (var result in group.Value)
                {
                    if (result is not MetaMorpheusResult)
                        continue;
                    result.Override = true;
                    result.CountProteins();
                    result.Override = false;
                    result.Dispose();
                }

                var finalResult = group.Value.Select(p => p.CountProteins())
                    .Aggregate((a, b) => a + b);
                finalResult.WriteResults(finalResultOutPath);

                group.Value.ForEach(p => p.Dispose());
            }

            // Chimerys
            var pspd = new ProteomeDiscovererResult(
                @"B:\Users\Nic\Chimeras\Chimerys\Chimerys");
            pspd.Override = true;
            pspd.CountProteins();
        }



        [Test]
        public static void TestProteinCountPlots()
        {
            // Mann 11 Directory
            var conditionGroupedResult = AllResults.SelectMany(p => p.Results)
                .GroupBy(p => p.Condition)
                .Where(p => p.Count() == 11)
                .ToDictionary(p => p.Key, p => p.ToList());

            var bigResultPath = @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\ProcessedResults";
            if (!Directory.Exists(bigResultPath))
                Directory.CreateDirectory(bigResultPath);
            var mm = conditionGroupedResult.Where(p => p.Key.Contains("IndividualFilesFraggerEquivalentWithChimeras"))
                .SelectMany(m => m.Value);
            List<SingleRunResults> runResults = new()
            {
                ChimerysResult,
                MsFraggerResults,
                mm
            };

            var records = runResults.SelectMany(p => p.CountProteins())
                .Where(p => p is { UniqueFullSequences: > 1, UniqueBaseSequences: > 1 }).ToList();
            records.GetProteinCountPlotsStacked(ProteinCountPlots.ProteinCountPlotTypes.BaseSequenceCount).Show();
            records.GetProteinCountPlotsStacked(ProteinCountPlots.ProteinCountPlotTypes.FullSequenceCount).Show();
            records.GetProteinCountPlotsStacked(ProteinCountPlots.ProteinCountPlotTypes.SequenceCoverage).Show();

        }

        [Test]
        public static void TestModificationPlots()
        {
            // Mann 11 Directory
            var conditionGroupedResult = AllResults.SelectMany(p => p.Results)
                .GroupBy(p => p.Condition)
                .Where(p => p.Count() == 11)
                .ToDictionary(p => p.Key, p => p.ToList());

            var bigResultPath = @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\ProcessedResults";
            if (!Directory.Exists(bigResultPath))
                Directory.CreateDirectory(bigResultPath);
            var mm = conditionGroupedResult.Where(p => p.Key.Contains("IndividualFilesFraggerEquivalentWithChimeras"))
                .SelectMany(m => m.Value);
            var mm2 = conditionGroupedResult.Where(p => p.Key.Contains("MetaMorpheusWithLibrary"))
                .SelectMany(m => m.Value);
            List<SingleRunResults> runResults = new()
            {
                ChimerysResult,
                MsFraggerResults.Where(p => p.Condition != "ReviewdDatabase_MsFraggerDDA+"),
                mm, mm2
            };
            runResults.GetModificationDistribution().Show();
            runResults.GetModificationDistribution(ResultType.Peptide).Show();


        }










        static double LogFactorial(int n)
        {
            if (n < 1)
                return -1;

            var logFactorial = Enumerable.Range(1, n).Select(p => Math.Log(p)).Aggregate((a, b) => a + b);
            return logFactorial;
        }

        private static ulong GetFactorial(ulong n)
        {
            if (n == 0)
            {
                return 1;
            }
            return n * GetFactorial(n - 1);
        }

        private static double GetFactorial(double n)
        {
            if (n == 0)
            {
                return 1;
            }
            return n * GetFactorial(n - 1);
        }

        private static int GetFactorial(int n)
        {
            if (n == 0)
            {
                return 1;
            }
            return n * GetFactorial(n - 1);
        }

        private static BigInteger GetFactorial(BigInteger n)
        {
            if (n == 0)
            {
                return 1;
            }
            return n * GetFactorial(n - 1);
        }

        [Test]
        public static void TestFactorial()
        {
            int lastValue = 0;
            for (int i = 1; i < 10000; i++)
            {
                var val = LogFactorial(i);
                if (lastValue > val)
                {
                    Console.WriteLine($"Log Factorial Maxed Out at {i}");
                    break;
                }
                lastValue = (int)val;
            }

            ulong lastValue2 = 0;
            for (ulong i = 1; i < 1000; i++)
            {
                var val = GetFactorial(i);
                if (lastValue2 > val)
                {
                    Console.WriteLine($"Ulong Factorial Maxed Out at {i}");
                    break;
                }
                lastValue2 = val;
            }

            double lastValue3 = 0;
            for (double i = 1; i < 1000; i++)
            {
                var val = GetFactorial(i);
                if (lastValue3 > val || !val.IsFinite())
                {
                    Console.WriteLine($"Double Factorial Maxed Out at {i}");
                    break;
                }
                lastValue3 = val;
            }

            int lastValue4 = 0;
            for (int i = 1; i < 1000; i++)
            {
                var val = GetFactorial(i);
                if (lastValue4 > val)
                {
                    Console.WriteLine($"Int Factorial Maxed Out at {i}");
                    break;
                }
                lastValue4 = val;
            }

            BigInteger lastValue5 = 0;
            for (BigInteger i = 1; i < 10000; i++)
            {
                var val = GetFactorial(i);
                if (lastValue5 > val)
                {
                    Console.WriteLine($"BigInteger Factorial Maxed Out at {i}");
                    break;
                }
                lastValue5 = val;
            }
        }
    }
}
