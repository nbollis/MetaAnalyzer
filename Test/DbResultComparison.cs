using Analyzer.Plotting;
using ResultAnalyzerUtil;
using ResultAnalyzerUtil.CommandLine;
using System.Diagnostics;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;
using TaskLayer;
using TaskLayer.ChimeraAnalysis;
using Analyzer.SearchType;
using Analyzer.FileTypes.Internal;

namespace Test
{
    [TestFixture]
    public static class DbResultComparison
    {
        //public static string AllDirPath = @"D:\Projects\MassCentricGptmdUpdate\BigGptmdComparison";
        public static string AllDirPath = @"D:\Projects\MassCentricGptmdUpdate\BU_DecoyModAnalysis_0MinScore";
        public static string OneDirPath = @"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\GptmdSearch_111";

        public static IEnumerable<string> GetAllRelevantDirectories() => Directory.GetDirectories(AllDirPath);
        /*.Where(p => p.Contains("GptmdSearch")*/


        [Test]
        public static void TESTNAME()
        {
            var dirPath = @"D:\Projects\Chimeras\Revisions\EvoSep_MSV000095360\IsolatedRuns";

            var mmResults = new List<MetaMorpheusResult>
            {
                ////new MetaMorpheusResult(@"D:\Projects\Chimeras\Revisions\EvoSep_MSV000095360\FirstBigRun_JustSearch", "EvoSep", "JustSearch"),
                //new MetaMorpheusResult(@"D:\Projects\Chimeras\Revisions\EvoSep_MSV000095360\FirstBigRun", "EvoSep", "FirstRun")
            };

            foreach (var dir in Directory.GetDirectories(dirPath))
            {
                if (dir.Contains("Figures"))
                    continue;
                var mmResult = new MetaMorpheusResult(dir);
                mmResults.Add(mmResult);
            }

            var cellLine = new CellLineResults(dirPath, mmResults.Cast<SingleRunResults>().ToList());

            // Generate the Individual Run Plots and analyses for each MetaMorpheusResult
            foreach (var mmResult in mmResults)
            {
                var indFile = mmResult.GetIndividualFileComparison();
                AnalyzerGenericPlots.IndividualFileResultBarChart([indFile], out int width, out int height, mmResult.DatasetName, mmResult.IsTopDown)
                    .SaveInRunResultOnly(mmResult, $"IndividualFileComparison_{mmResult.DatasetName}_{mmResult.Condition}", width, height);


                var bulkFile = mmResult.GetBulkResultCountComparisonFile();
                AnalyzerGenericPlots.BulkResultBarChart(bulkFile.Results, mmResult.IsTopDown)
                    .SaveInRunResultOnly(mmResult, $"BulkResultComparison_{mmResult.DatasetName}_{mmResult.Condition}_Psm", 1000, 600);
                AnalyzerGenericPlots.BulkResultBarChart(bulkFile.Results, mmResult.IsTopDown, ResultType.Peptide)
                    .SaveInRunResultOnly(mmResult, $"BulkResultComparison_{mmResult.DatasetName}_{mmResult.Condition}_Peptide", 1000, 600);


                var psmCounting = mmResult.CountChimericPsms();
                var peptideCounting = mmResult.CountChimericPeptides();
                var proteinCounting = mmResult.CountProteins();
                ExternalComparisonTask.PlotProteinCountingCharts(proteinCounting.Results, false, mmResult.FigureDirectory);

                var sw = Stopwatch.StartNew();
                _ = mmResult.GetChimeraBreakdownFile();
                sw.Stop();

                // if it takes less than one minute to get the breakdown, and we are not overriding, do not plot
                if (sw.Elapsed.Minutes >= 1)
                {
                    mmResult.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Peptide);
                    mmResult.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Psm);
                    mmResult.PlotChimeraBreakDownStackedColumn(ResultType.Psm);
                    mmResult.PlotChimeraBreakDownStackedColumn(ResultType.Peptide);
                }

                var parameters = new SingleRunAnalysisParameters(mmResult, false, false);
                var summaryTask = new SingleRunChimericSpectrumSummaryTask(parameters);
                summaryTask.Run().Wait();
            }

            var allSummaryRecords = new List<ChimericSpectrumSummary>();
            foreach (var mmResult in mmResults)
            {
                var summaryFile = mmResult.GetChimericSpectrumSummaryFile();
                allSummaryRecords.AddRange(summaryFile.Results);
            }
            SingleRunChimericSpectrumSummaryTask.GetIntensityPlot(allSummaryRecords, ResultType.Psm, DistributionPlotTypes.ViolinPlot, false, true)
                .SaveInCellLineOnly(cellLine, $"ChimericSpectrumSummary_Psm_Intensity_Log_ViolinPlot", 1000, 600);
            SingleRunChimericSpectrumSummaryTask.GetIntensityPlot(allSummaryRecords, ResultType.Peptide, DistributionPlotTypes.ViolinPlot, false, true)
                .SaveInCellLineOnly(cellLine, $"ChimericSpectrumSummary_Peptide_Intensity_Log_ViolinPlot", 1000, 600);
            SingleRunChimericSpectrumSummaryTask.GetIntensityPlot(allSummaryRecords, ResultType.Psm, DistributionPlotTypes.ViolinPlot, false, false)
                .SaveInCellLineOnly(cellLine, $"ChimericSpectrumSummary_Psm_Intensity_ViolinPlot", 1000, 600);
            SingleRunChimericSpectrumSummaryTask.GetIntensityPlot(allSummaryRecords, ResultType.Peptide, DistributionPlotTypes.ViolinPlot, false, false)
                .SaveInCellLineOnly(cellLine, $"ChimericSpectrumSummary_Peptide_Intensity_ViolinPlot", 1000, 600);

            SingleRunChimericSpectrumSummaryTask.GetIntensityPlot(allSummaryRecords, ResultType.Psm, DistributionPlotTypes.BoxPlot, false, true)
                .SaveInCellLineOnly(cellLine, $"ChimericSpectrumSummary_Psm_Intensity_Log_BoxPlot", 1000, 600);
            SingleRunChimericSpectrumSummaryTask.GetIntensityPlot(allSummaryRecords, ResultType.Peptide, DistributionPlotTypes.BoxPlot, false, true)
                .SaveInCellLineOnly(cellLine, $"ChimericSpectrumSummary_Peptide_Intensity_Log_BoxPlot", 1000, 600);
            SingleRunChimericSpectrumSummaryTask.GetIntensityPlot(allSummaryRecords, ResultType.Psm, DistributionPlotTypes.BoxPlot, false, false)
                .SaveInCellLineOnly(cellLine, $"ChimericSpectrumSummary_Psm_Intensity_BoxPlot", 1000, 600);
            SingleRunChimericSpectrumSummaryTask.GetIntensityPlot(allSummaryRecords, ResultType.Peptide, DistributionPlotTypes.BoxPlot, false, false)
                .SaveInCellLineOnly(cellLine, $"ChimericSpectrumSummary_Peptide_Intensity_BoxPlot", 1000, 600);


            // Protein Counting: Aggregate
            List<ProteinCountingRecord> allProteinCountingRecords = new List<ProteinCountingRecord>();
            foreach (var mmResult in mmResults)
            {
                var proteinCounting = mmResult.CountProteins();
                allProteinCountingRecords.AddRange(proteinCounting.Results);
            }

            var allOutPath = Path.Combine(dirPath, "ProteinCountingComparison_All.csv");
            var allFile = new ProteinCountingFile { Results = allProteinCountingRecords };
            allFile.WriteResults(allOutPath);

            ExternalComparisonTask.PlotProteinCountingCharts(allProteinCountingRecords, false, cellLine.FigureDirectory);
        }









        [Test]
        public static void CompareDbResults()
        {
            var gptmdSearch = new GptmdSearchResult(OneDirPath, false);

            gptmdSearch.ParseModifications();
            var record = gptmdSearch.ToRecord();
            
            var outPath = Path.Combine(OneDirPath, "DbResultComparison_NoAmbig.csv");

            var file = new GptmdSearchResultFile();
            file.Results = [record];
            file.WriteResults(outPath);
        }

        [Test]
        public static void RunAll()
        {
            var allRecords = new List<GptmdSearchRecord>();
            foreach (var dir in GetAllRelevantDirectories())
            {
                var gptmdSearch = new GptmdSearchResult(dir);
                gptmdSearch.ParseModifications();
                var record = gptmdSearch.ToRecord();
                allRecords.Add(record);
                var outPath = Path.Combine(dir, "DbResultComparison.csv");
                new GptmdSearchResultFile{ Results = [record] }.WriteResults(outPath);

                gptmdSearch = new GptmdSearchResult(dir, false);
                gptmdSearch.ParseModifications();
                record = gptmdSearch.ToRecord();
                allRecords.Add(record);
                outPath = Path.Combine(dir, "DbResultComparison_NoAmbig.csv");
                new GptmdSearchResultFile { Results = [record] }.WriteResults(outPath);
            }

            var allOutPath = Path.Combine(AllDirPath, "DbResultComparison_All.csv");
            var allFile = new GptmdSearchResultFile { Results = allRecords };
            allFile.WriteResults(allOutPath);
        }


        [Test]
        public static void gptmdUpdateTest_TD()
        {
            string dirPath = @"D:\Projects\MassCentricGptmdUpdate";
            var resultDir = Path.Combine(dirPath, "DecoyModAnalysis2_0MinScore");
            if (!Directory.Exists(resultDir))
                Directory.CreateDirectory(resultDir);

            List<(string, string)> gptmdTomls = new()
            {
                ("Update_DecoyMods_BiDirectional", Path.Combine(dirPath, "Tomls", "GPTMD_DecoyMods_BiDirectional.toml")),
                ("Update_DecoyMods_FlankingIons", Path.Combine(dirPath, "Tomls", "GPTMD_DecoyMods_Flanking.toml")),
                ("Update_DecoyMods_ScoreImproveAndFlankingIons", Path.Combine(dirPath, "Tomls", "GPTMD_DecoyMods_ScoreImproveAndFlanking.toml")),
                ("Update_DecoyMods_ScoreImproved", Path.Combine(dirPath, "Tomls", "GPTMD_DecoyMods_ScoreImprove.toml")),
                ("Update_DecoyMods_UniDirectional", Path.Combine(dirPath, "Tomls", "GPTMD_DecoyMods_UniDirectional.toml")),
                ("Update_DecoyMods", Path.Combine(dirPath, "Tomls", "GPTMD_DecoyMods_Update.toml")),
            };
            string[] spectraFiles = new[]
            {
                @"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-17-20_jurkat_td_rep2_fract1-averaged.mzML",
                @"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-17-20_jurkat_td_rep2_fract2-calib-averaged.mzML",
                @"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-17-20_jurkat_td_rep2_fract3-calib-averaged.mzML",
                @"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-17-20_jurkat_td_rep2_fract4-calib-averaged.mzML",
                @"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-18-20_jurkat_td_rep2_fract10-calib-averaged.mzML",
                @"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-18-20_jurkat_td_rep2_fract5-calib-averaged.mzML",
                @"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-18-20_jurkat_td_rep2_fract6-calib-averaged.mzML",
                @"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-18-20_jurkat_td_rep2_fract7-calib-averaged.mzML",
                @"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-18-20_jurkat_td_rep2_fract8-calib-averaged.mzML",
                @"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-18-20_jurkat_td_rep2_fract9-calib-averaged.mzML"
            };

            string startingdbPath = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";
            string tomlpath =
                @"D:\Projects\MassCentricGptmdUpdate\Tomls\TopDownSearchTask_WithInternal.toml";

            List<CmdProcess> processes = new List<CmdProcess>();
            foreach (var indConditionDir in gptmdTomls)
            {
                var name = indConditionDir.Item1;
                var gptmdTomlPath = indConditionDir.Item2;
                var outDir = Path.Combine(resultDir, name);

                var search = new MetaMorpheusGptmdSearchCmdProcess(spectraFiles, [startingdbPath], gptmdTomlPath, tomlpath, outDir, "", 1, @"C:\Users\Nic\source\repos\MetaMorpheus\MetaMorpheus\CMD\bin\Release\net8.0");
                processes.Add(search);
            }

            var controlGptmdPath = Path.Combine(dirPath, "Tomls", "GPTMD_DecoyMods_Release.toml");
            var controlOutDir = Path.Combine(resultDir, "Release_DecoyMods");
            var controlSearch = new MetaMorpheusGptmdSearchCmdProcess(spectraFiles, [startingdbPath], controlGptmdPath, tomlpath, controlOutDir, "", 1, @"C:\Program Files\MetaMorpheus");
            processes.Add(controlSearch);

            new TaskManager(3).RunProcesses(processes).Wait();
        }
        
        [Test]
        public static void gptmdUpdateTest_BU()
        {
            string dirPath = @"D:\Projects\MassCentricGptmdUpdate";
            string resultDir = Path.Combine(dirPath, "BU_DecoyModAnalysis_0MinScore");
            if (!Directory.Exists(resultDir))
                Directory.CreateDirectory(resultDir);

            List<(string, string)> gptmdTomls = new()
            {
                ("Update_DecoyMods_BiDirectional", Path.Combine(dirPath, "Tomls", "GPTMD_BU_DecoyMods_BiDirectional.toml")),
                ("Update_DecoyMods_FlankingIons", Path.Combine(dirPath, "Tomls", "GPTMD_BU_DecoyMods_Flanking.toml")),
                ("Update_DecoyMods_ScoreImproveAndFlankingIons", Path.Combine(dirPath, "Tomls", "GPTMD_BU_DecoyMods_ScoreImproveAndFlanking.toml")),
                ("Update_DecoyMods_ScoreImproved", Path.Combine(dirPath, "Tomls", "GPTMD_BU_DecoyMods_ScoreImprove.toml")),
                ("Update_DecoyMods_UniDirectional", Path.Combine(dirPath, "Tomls", "GPTMD_BU_DecoyMods_UniDirectional.toml")),
                ("Update_DecoyMods", Path.Combine(dirPath, "Tomls", "GPTMD_BU_DecoyMods_Update.toml")),
            };

            string[] spectraFiles = new[]
            {
                // GAMG_1
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_1\Task1AveragingTask\20100609_Velos1_TaGe_SA_GAMG_1-calib-averaged.mzML",
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_1\Task1AveragingTask\20100609_Velos1_TaGe_SA_GAMG_2-calib-averaged.mzML",
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_1\Task1AveragingTask\20100609_Velos1_TaGe_SA_GAMG_3-calib-averaged.mzML",
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_1\Task1AveragingTask\20100609_Velos1_TaGe_SA_GAMG_4-calib-averaged.mzML",
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_1\Task1AveragingTask\20100609_Velos1_TaGe_SA_GAMG_5-calib-averaged.mzML",
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_1\Task1AveragingTask\20100609_Velos1_TaGe_SA_GAMG_6-calib-averaged.mzML",

                // GAMG_2
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_2\Task1AveragingTask\20100723_Velos1_TaGe_SA_Gamg_1-calib-averaged.mzML",
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_2\Task1AveragingTask\20100723_Velos1_TaGe_SA_Gamg_2-calib-averaged.mzML",
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_2\Task1AveragingTask\20100723_Velos1_TaGe_SA_Gamg_3-calib-averaged.mzML",
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_2\Task1AveragingTask\20100723_Velos1_TaGe_SA_Gamg_4-calib-averaged.mzML",
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_2\Task1AveragingTask\20100723_Velos1_TaGe_SA_Gamg_5-calib-averaged.mzML",
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_2\Task1AveragingTask\20100723_Velos1_TaGe_SA_Gamg_6-calib-averaged.mzML",

                // GAMG_3
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_3\Task1AveragingTask\20101227_Velos1_TaGe_SA_GAMG1-calib-averaged.mzML",
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_3\Task1AveragingTask\20101227_Velos1_TaGe_SA_GAMG2-calib-averaged.mzML",
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_3\Task1AveragingTask\20101227_Velos1_TaGe_SA_GAMG3-calib-averaged.mzML",
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_3\Task1AveragingTask\20101227_Velos1_TaGe_SA_GAMG4-calib-averaged.mzML",
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_3\Task1AveragingTask\20101227_Velos1_TaGe_SA_GAMG5-calib-averaged.mzML",
                @"B:\RawSpectraFiles\Mann_11cell_lines\GAMG\107_CalibratedAveraged-GAMG_3\Task1AveragingTask\20101227_Velos1_TaGe_SA_GAMG6-calib-averaged.mzML"
            };

            string startingdbPath = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";
            string tomlpath =
                "\"B:\\Users\\Nic\\Chimeras\\Mann_11cell_analysis\\GAMG\\SearchResults\\MetaMorpheusFraggerEquivalent\\Task Settings\\Task3-WithChimerasconfig.toml\"";   
            
            List<CmdProcess> processes = new List<CmdProcess>();
            foreach (var indConditionDir in gptmdTomls)
            {
                var name = indConditionDir.Item1;
                var gptmdTomlPath = indConditionDir.Item2;
                var outDir = Path.Combine(resultDir, name);

                var search = new MetaMorpheusGptmdSearchCmdProcess(spectraFiles, [startingdbPath], gptmdTomlPath, tomlpath, outDir, "", 1, @"C:\Users\Nic\source\repos\MetaMorpheus\MetaMorpheus\CMD\bin\Release\net8.0");
                processes.Add(search);
            }

            var controlGptmdPath = Path.Combine(dirPath, "Tomls", "GPTMD_BU_DecoyMods_Release.toml");
            var controlOutDir = Path.Combine(resultDir, "Release_DecoyMods");
            var controlSearch = new MetaMorpheusGptmdSearchCmdProcess(spectraFiles, [startingdbPath], controlGptmdPath, tomlpath, controlOutDir, "", 1, @"C:\Program Files\MetaMorpheus");
            processes.Add(controlSearch);

            new TaskManager(3).RunProcesses(processes).Wait();
        }
    }
}
