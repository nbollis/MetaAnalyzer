using Analyzer.Plotting.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Interfaces;
using Analyzer.Plotting;
using Analyzer.Plotting.ComparativePlots;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.SearchType;
using Analyzer.Util;
using Calibrator;
using pepXML.Generated;
using Plotly.NET;
using Plotly.NET.ImageExport;
using TaskLayer.CMD;
using System.Security.Cryptography;
using Analyzer.FileTypes.Internal;
using Easy.Common.Extensions;

namespace TaskLayer.ChimeraAnalysis
{
    public class InternalMetaMorpheusAnalysisTask : BaseResultAnalyzerTask
    {
        #region FilePaths

        // Paths for setup
        internal static string MetaMorpheusLocation { get; set; }


        public static string UniprotHumanProteomeAndReviewedXml =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";

        public static string Mann11DataFileDirectory =>
            @"B:\RawSpectraFiles\Mann_11cell_lines";

        public static string Mann11OutputDirectory =>
            @"B:\Users\Nic\Chimeras\InternalMMAnalysis\Mann_11cell_lines";


        // Bottom Up Tomls
        public static string GptmdNoChimerasMann11 =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\GPTMD_NoChimeras.toml";

        public static string GptmdWithChimerasMann11 =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\GPTMD_WithChimeras.toml";

        public static string SearchNoChimerasMann11 =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\Search_NoChimeras.toml";

        public static string SearchWithChimerasMann11 =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\Search_WithChimeras.toml";

        public static string SearchNoChimerasMann11_BuildLibrary =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\Search_NoChimeras_BuildLibrary.toml";

        public static string SearchWithChimerasMann11_BuildLibrary =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\Search_WithChimeras_BuildLibrary.toml";


        // Top-Down Tomls
        public static string Search_BuildChimericLibraryJurkatTd =>
            @"";

        public static string Search_BuildNonChimericLibraryJurkatTd =>
            @"";

        public static string GptmdNoChimerasJurkatTd =>
            @"";

        public static string GptmdWithChimerasJurkatTd =>
            @"";

        public static string SearchNoChimerasJurkatTd =>
            @"";

        public static string SearchWithChimerasJurkatTd =>
            @"";


        #endregion

        public static string Version => "105";
        private static string NonChimericDescriptor => "MetaMorpheusNoChimeras";
        private static string ChimericDescriptor => "MetaMorpheusWithChimeras";
        private static string BulkFigureDirectory { get; set; }

        public override MyTask MyTask => MyTask.InternalMetaMorpheusAnalysis;
        public override InternalMetaMorpheusAnalysisParameters Parameters { get; }

        public InternalMetaMorpheusAnalysisTask(InternalMetaMorpheusAnalysisParameters parameters)
        {
            Parameters = parameters;
            MetaMorpheusLocation = parameters.MetaMorpheusPath;

            if (!Directory.Exists(parameters.OutputDirectory))
                Directory.CreateDirectory(parameters.OutputDirectory);
            BulkFigureDirectory = Path.Combine(parameters.OutputDirectory, "Figures");
            BulkResultComparisonFilePath = Path.Combine(parameters.OutputDirectory, "BulkResultComparisonFile.csv");
            if (!Directory.Exists(BulkFigureDirectory))
                Directory.CreateDirectory(BulkFigureDirectory);
        }

        protected override void RunSpecific() => RunSpecificAsync(Parameters).Wait();

        private static async Task RunSpecificAsync(InternalMetaMorpheusAnalysisParameters parameters)
        {
            var processes = BuildProcesses(parameters);
            await RunProcesses(processes);

            // Parse Results Together
            Dictionary<string, List<string>> cellLineDict = new();
            foreach (var cellLineDirectory in Directory.GetDirectories(parameters.OutputDirectory)
                         .Where(p => !p.Contains("Generate") && !p.Contains("Figure")))
            {
                cellLineDict.Add(cellLineDirectory, new());
                foreach (var runDirectory in Directory.GetDirectories(cellLineDirectory).Where(p => !p.Contains("Figure") && p.Contains(Version)))
                    cellLineDict[cellLineDirectory].Add(runDirectory);
            }

            //// Run MM Task basic processing 
            //int degreesOfParallelism = (int)(MaxWeight / 0.25);
            //Parallel.ForEach(cellLineDict.SelectMany(p => p.Value),
            //    new ParallelOptions() { MaxDegreeOfParallelism = Math.Max(degreesOfParallelism, 1) },
            //    singleRunPath =>
            //    {
            //        var mmResult = new MetaMorpheusResult(singleRunPath);
            //        Log($"Processing {mmResult.DatasetName} {mmResult.Condition}", 1);

            //        Log($"Tabulating Result Counts: {mmResult.DatasetName} {mmResult.Condition}", 2);
            //        _ = mmResult.GetIndividualFileComparison();
            //        _ = mmResult.GetBulkResultCountComparisonFile();

            //        Log($"Counting Chimeric Psms/Peptides: {mmResult.DatasetName} {mmResult.Condition}", 2);
            //        mmResult.CountChimericPsms();
            //        mmResult.CountChimericPeptides();

            //        Log($"Running Chimera Breakdown Analysis: {mmResult.DatasetName} {mmResult.Condition}", 2);
            //        var sw = Stopwatch.StartNew();
            //        _ = mmResult.GetChimeraBreakdownFile();
            //        sw.Stop();

            //        // if it takes less than one minute to get the breakdown, and we are not overriding, do not plot
            //        if (sw.Elapsed.Minutes < 1 && !parameters.Override)
            //            return;

            //        mmResult.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Psm);
            //        mmResult.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Peptide);
            //    });

            //Log($"Running Chimeric Spectrum Summaries", 0);
            //foreach (var cellLineDictEntry in cellLineDict)
            //{
            //    var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);
            //    Log($"Processing Cell Line {cellLine}", 1);
            //    List<CmdProcess> summaryTasks = new();
            //    foreach (var singleRunPath in cellLineDictEntry.Value)
            //    {
            //        var summaryParams =
            //            new SingleRunAnalysisParameters(singleRunPath, parameters.Override, false);
            //        var summaryTask = new SingleRunChimericSpectrumSummaryTask(summaryParams);
            //        summaryTasks.Add(new ResultAnalyzerTaskToCmdProcessAdaptor(summaryTask, "Chimeric Spectrum Summary", 0.5,
            //            singleRunPath));
            //    }

            //    try
            //    {
            //        await RunProcesses(summaryTasks);
            //    }
            //    catch (Exception e)
            //    {
            //        Warn($"Error Running Chimeric Spectrum Summary for {cellLine}: {e.Message}");
            //    }
            //}

            //Log($"Running Retention Time Plots", 0);
            //foreach (var cellLineDictEntry in cellLineDict)
            //{
            //    var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);

            //    List<CmdProcess> summaryTasks = new();
            //    Log($"Processing Cell Line {cellLine}", 1);
            //    foreach (var singleRunPath in cellLineDictEntry.Value)
            //    {
            //        if (singleRunPath.Contains(NonChimericDescriptor))
            //            continue;

            //        foreach (var distribPlotTypes in Enum.GetValues<DistributionPlotTypes>())
            //        {
            //            var summaryParams =
            //                new SingleRunAnalysisParameters(singleRunPath, parameters.Override, false, distribPlotTypes);
            //            var summaryTask = new SingleRunChimeraRetentionTimeDistribution(summaryParams);
            //            summaryTasks.Add(new ResultAnalyzerTaskToCmdProcessAdaptor(summaryTask, "Retention Time Plots", 0.25,
            //                singleRunPath));
            //        }
            //    }

            //    try
            //    {
            //        await RunProcesses(summaryTasks);
            //    }
            //    catch (Exception e)
            //    {
            //        Warn($"Error Running Retention Time Plots for {cellLine}: {e.Message}");
            //    }
            //}

            //Log($"Running Spectral Angle Comparisons", 0);
            //foreach (var cellLineDictEntry in cellLineDict)
            //{
            //    var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);

            //    List<CmdProcess> summaryTasks = new();
            //    Log($"Processing Cell Line {cellLine}", 1);
            //    foreach (var singleRunPath in cellLineDictEntry.Value)
            //    {
            //        if (singleRunPath.Contains(NonChimericDescriptor))
            //            continue;
            //        foreach (var distribPlotTypes in Enum.GetValues<DistributionPlotTypes>())
            //        {
            //            var summaryParams =
            //                new SingleRunAnalysisParameters(singleRunPath, parameters.Override, false, distribPlotTypes);
            //            var summaryTask = new SingleRunSpectralAngleComparisonTask(summaryParams);
            //            summaryTasks.Add(new ResultAnalyzerTaskToCmdProcessAdaptor(summaryTask, "Spectral Angle Comparisons", 0.25,
            //                singleRunPath));
            //        }
            //    }

            //    try
            //    {
            //        await RunProcesses(summaryTasks);
            //    }
            //    catch (Exception e)
            //    {
            //        Warn($"Error Running Spectral Angle Comparisons for {cellLine}: {e.Message}");
            //    }
            //}

            // TODO: Change retention time alignment to operate on the grouped runs
            //Log($"Running Retention Time Alignment", 0);
            //foreach (var cellLineDictEntry in cellLineDict)
            //{
            //    var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);

            //    List<CmdProcess> summaryTasks = new();
            //    Log($"Processing Cell Line {cellLine}", 1);
            //    foreach (var singleRunPath in cellLineDictEntry.Value)
            //    {
            //        if (singleRunPath.Contains(NonChimericDescriptor))
            //            continue;
            //        var summaryParams =
            //            new SingleRunAnalysisParameters(singleRunPath, parameters.Override, false);
            //        var summaryTask = new SingleRunRetentionTimeCalibrationTask(summaryParams);
            //        summaryTasks.Add(new ResultAnalyzerTaskToCmdProcessAdaptor(summaryTask, "Retention Time Alignment", 0.25,
            //            singleRunPath));
            //    }

            //    try
            //    {
            //        await RunProcesses(summaryTasks);
            //    }
            //    catch (Exception e)
            //    {
            //        Warn($"Error Running Retention Time Alignment for {cellLine}: {e.Message}");
            //    }
            //}



            var resultsForInternalComparison = cellLineDict.SelectMany(p => p.Value)
                .Where(p => !p.Contains($"WithChimeras_{Version}_NonChimericLibrary"))
                .Select(p => new MetaMorpheusResult(p))
                .ToList();
            //GetResultCountFile(resultsForInternalComparison);
            Log($"Plotting Bulk Internal Comparison", 0);
            PlotCellLineBarCharts(resultsForInternalComparison);
            PlotChimeraBreakdownBarChart(resultsForInternalComparison);
            PlotPossibleFeatures(resultsForInternalComparison);
            PlotFractionalIntensityPlots(resultsForInternalComparison);
            
        }

        #region MetaMorpheus search running

        static List<CmdProcess> BuildProcesses(InternalMetaMorpheusAnalysisParameters parameters)
        {
            List<CmdProcess> toReturn = new();
            var directoryPath = parameters.InputDirectoryPath;
            var dataDirectoryPath = parameters.SpectraFileDirectory;
            var dataset = Path.GetFileNameWithoutExtension(directoryPath);
            var isTopDown = !directoryPath.Contains("Mann");
            var manager = new DatasetFileManager(dataset, isTopDown, directoryPath, dataDirectoryPath);

            if (!Directory.Exists(parameters.OutputDirectory))
                Directory.CreateDirectory(parameters.OutputDirectory);

            // Generate Libraries
            var replicateDict = manager.CellLines.SelectMany(p => p.Replicates)
                .GroupBy(p => p.Replicate)
                .ToDictionary(p => p.Key,
                    p => p.SelectMany(m => m.CalibratedAveragedFilePaths.Values).ToArray());

            var reps = replicateDict.Keys.ToArray();
            for (int i = 1; i < replicateDict.Count + 1; i++)
            {
                var specToRun = replicateDict.Where(p => p.Key != i)
                    .SelectMany(m => m.Value).ToArray();
                string descriptor = string.Join('+', reps.Where(p => p != i));

                // Generate Library Processes
                string chimericOutPath = Path.Combine(parameters.OutputDirectory,
                    $"GenerateChimericLibrary_{Version}_{descriptor}");
                string gptmd = GetGptmdPath(true, isTopDown);
                string search = GetSearchPath(true, isTopDown, true);
                var chimLibPrcess = new InternalMetaMorpheusCmdProcess(specToRun, parameters.DatabasePath, gptmd, search, chimericOutPath,
                    $"Generating Chimeric Library for {descriptor} in {dataset}", 0.5, MetaMorpheusLocation);
                toReturn.Add(chimLibPrcess);

                string nonChimericOutPath = Path.Combine(parameters.OutputDirectory,
                    $"GenerateNonChimericLibrary_{Version}_{descriptor}");
                gptmd = GetGptmdPath(false, isTopDown);
                search = GetSearchPath(false, isTopDown, true);
                var nonChimLibProcess = new InternalMetaMorpheusCmdProcess(specToRun, parameters.DatabasePath, gptmd, search,
                    nonChimericOutPath,
                    $"Generating Non-Chimeric Library for {descriptor} in {dataset}", 0.5, MetaMorpheusLocation);
                toReturn.Add(nonChimLibProcess);

                foreach (var cellLine in manager.CellLines)
                {
                    string cellLineDir = Path.Combine(parameters.OutputDirectory, cellLine.CellLine);
                    if (!Directory.Exists(cellLineDir))
                        Directory.CreateDirectory(cellLineDir);

                    var spec = cellLine.Replicates
                        .Where(p => p.Replicate == i)
                        .SelectMany(m => m.CalibratedAveragedFilePaths.Values)
                        .ToArray();

                    // Search Chimericly with chim lib
                    string chimWithChimOutPath = Path.Combine(cellLineDir,
                        $"{ChimericDescriptor}_{Version}_ChimericLibrary_Rep{i}");
                    gptmd = GetGptmdPath(true, isTopDown);
                    search = GetSearchPath(true, isTopDown, false);
                    var individualProcess = new InternalMetaMorpheusCmdProcess(spec, parameters.DatabasePath, gptmd, search,
                        chimWithChimOutPath,
                        $"Searching with Chimeric Library for Replicate {i} in {cellLine.CellLine}", 0.25,
                        MetaMorpheusLocation, $"{cellLine.CellLine} rep {i}");
                    individualProcess.DependsOn(chimLibPrcess);
                    toReturn.Add(individualProcess);

                    // Search Chimerically with NonChimeric Lib
                    string chimWithNonChimOutPath = Path.Combine(cellLineDir,
                        $"{ChimericDescriptor}_{Version}_NonChimericLibrary_Rep{i}");
                    gptmd = GetGptmdPath(true, isTopDown);
                    search = GetSearchPath(true, isTopDown, false);
                    individualProcess = new InternalMetaMorpheusCmdProcess(spec, parameters.DatabasePath, gptmd, search,
                        chimWithNonChimOutPath,
                        $"Searching Chimeric with Non-Chimeric Library for Replicate {i} in {cellLine.CellLine}", 0.25,
                        MetaMorpheusLocation, $"{cellLine.CellLine} rep {i}");
                    individualProcess.DependsOn(nonChimLibProcess);
                    toReturn.Add(individualProcess);

                    // Search NonChimerically with NonChimeric Lib
                    string nonChimWithNonChimOutPath = Path.Combine(cellLineDir,
                        $"{NonChimericDescriptor}_{Version}_NonChimericLibrary_Rep{i}");
                    gptmd = GetGptmdPath(false, isTopDown);
                    search = GetSearchPath(false, isTopDown, false);
                    individualProcess = new InternalMetaMorpheusCmdProcess(spec, parameters.DatabasePath, gptmd, search,
                        nonChimWithNonChimOutPath,
                        $"Searching Non-Chimeric with Non-Chimeric Library for Replicate {i} in {cellLine.CellLine}", 0.25,
                        MetaMorpheusLocation);
                    individualProcess.DependsOn(nonChimLibProcess);
                    toReturn.Add(individualProcess);
                }

            }

            return toReturn;
        }

        static string GetGptmdPath(bool isChimeric, bool isTopDown)
        {
            return isTopDown switch
            {
                true => isChimeric ? GptmdWithChimerasJurkatTd : GptmdNoChimerasJurkatTd,
                false => isChimeric ? GptmdWithChimerasMann11 : GptmdNoChimerasMann11
            };
        }

        static string GetSearchPath(bool isChimeric, bool isTopDown, bool buildLibrary)
        {
            if (isTopDown)
            {
                return buildLibrary switch
                {
                    true => isChimeric ? Search_BuildChimericLibraryJurkatTd : Search_BuildNonChimericLibraryJurkatTd,
                    false => isChimeric ? SearchWithChimerasJurkatTd : SearchNoChimerasJurkatTd
                };
            }
            else
            {
                return buildLibrary switch
                {
                    true => isChimeric ? SearchWithChimerasMann11_BuildLibrary : SearchNoChimerasMann11_BuildLibrary,
                    false => isChimeric ? SearchWithChimerasMann11 : SearchNoChimerasMann11
                };
            }
        }

        #endregion

        #region Result File Creation

        private static string BulkResultComparisonFilePath { get; set; }
        private static BulkResultCountComparisonFile? _bulkResultCountComparisonFile;

        internal static BulkResultCountComparisonFile GetResultCountFile(List<MetaMorpheusResult> mmResults)
        {
            Log($"Counting Total Results", 0);
            if (_bulkResultCountComparisonFile != null)
                return _bulkResultCountComparisonFile;
            if (File.Exists(BulkResultComparisonFilePath))
            {
                _bulkResultCountComparisonFile = new BulkResultCountComparisonFile(BulkResultComparisonFilePath);
                _bulkResultCountComparisonFile.LoadResults();
                return _bulkResultCountComparisonFile;
            }

            bool isTopDown = mmResults.First().IsTopDown;
            List<BulkResultCountComparison> allResults = new();
            foreach (var conditionGroup in mmResults.GroupBy(p => p.Condition.ConvertConditionName()))
            {
                string condition = conditionGroup.Key;
                foreach (var cellLineGroup in conditionGroup.GroupBy(p => p.DatasetName))
                {
                    string cellLine = cellLineGroup.Key;

                    if (cellLineGroup.Count() != 3 && !isTopDown)
                        Debugger.Break();
                    if (cellLineGroup.Count() != 2 && isTopDown)
                        Debugger.Break();

                    int psmCount = cellLineGroup.Sum(p =>
                        p.AllPsms.Count(psm => !psm.IsDecoy() && psm.PEP_QValue <= 0.01));
                    int allPsmCount = cellLineGroup.Sum(p => p.AllPsms.Count(psm => !psm.IsDecoy()));

                    int peptideCount = cellLineGroup.SelectMany(p =>
                            p.AllPeptides.Where(peptide => !peptide.IsDecoy() && peptide.PEP_QValue <= 0.01))
                        .DistinctBy(peptide => peptide.FullSequence)
                        .Count();
                    int allPeptideCount = cellLineGroup.SelectMany(p => p.AllPeptides.Where(peptide => !peptide.IsDecoy()))
                        .DistinctBy(peptide => peptide.FullSequence)
                        .Count();

                    List<string> accessions = new();
                    List<string> unfilteredAccessions = new();
                    cellLineGroup.ForEach(group =>
                    {
                        using (var sw = new StreamReader(File.OpenRead(group.ProteinPath)))
                        {
                            var header = sw.ReadLine();
                            var headerSplit = header.Split('\t');
                            var qValueIndex = Array.IndexOf(headerSplit, "Protein QValue");
                            var decoyContamTargetIndex = Array.IndexOf(headerSplit, "Protein Decoy/Contaminant/Target");
                            var accessionIndex = Array.IndexOf(headerSplit, "Protein Accession");
                            
                            while (!sw.EndOfStream)
                            {
                                var line = sw.ReadLine();
                                var values = line.Split('\t');

                                if (values[decoyContamTargetIndex].Contains('D'))
                                    continue;

                                var internalAccessions = values[accessionIndex].Split('|')
                                    .Select(p => p.Trim()).ToArray();
                                unfilteredAccessions.AddRange(internalAccessions);

                                if (double.Parse(values[qValueIndex]) > 0.01)
                                    continue;
                                

                                accessions.AddRange(internalAccessions);
                            }
                        }
                    });

                    var proteinGroupCount = accessions.Distinct().Count();
                    var allProteinGroupCount = unfilteredAccessions.Distinct().Count();

                    var result = new BulkResultCountComparison()
                    {
                        DatasetName = cellLine,
                        Condition = condition,
                        FileName = "All 3 Replicates",
                        OnePercentPeptideCount = peptideCount,
                        OnePercentPsmCount = psmCount,
                        OnePercentProteinGroupCount = proteinGroupCount,
                        PsmCount = allPsmCount,
                        PeptideCount = allPeptideCount,
                        ProteinGroupCount = allProteinGroupCount
                    };

                    allResults.Add(result);
                    cellLineGroup.ForEach(p => p.Dispose());
                }
            }

            var file = new BulkResultCountComparisonFile(BulkResultComparisonFilePath) { Results = allResults };
            file.WriteResults(BulkResultComparisonFilePath);
            return _bulkResultCountComparisonFile = file;
        }

        #endregion

        #region Plotting

        static void PlotCellLineBarCharts(List<MetaMorpheusResult> results)
        {
            Log($"Cell line bar charts", 1);
            bool isTopDown = results.First().IsTopDown;
            var resultsToPlot = GetResultCountFile(results)
                .ToList();

            var labels = results.Select(p => p.DatasetName).Distinct().ConvertConditionNames().ToList();
            List<GenericChart.GenericChart> psmCharts = new();
            List<GenericChart.GenericChart> peptideCharts = new();
            List<GenericChart.GenericChart> proteinCharts = new();
            foreach (var condition in results.Select(p => p.Condition).ConvertConditionNames().Distinct())
            {
                var conditionSpecificResults = resultsToPlot
                    .Where(p => p.Condition.ConvertConditionName() == condition)
                    .ToList();

                //(string CellLine, int OnePercentPsmCount, int OnePercentPeptideCount, int OnePercentProteinGroupCount)[] extractedCounts = conditionSpecificResults
                //    .GroupBy(p => p.DatasetName)
                //    .Select(p => (p.Key, p.Sum(m => m.OnePercentPsmCount),
                //            p.Sum(m => m.OnePercentPeptideCount), p.Sum(m => m.OnePercentProteinGroupCount)))
                //        .ToArray();


                psmCharts.Add(Chart2D.Chart.Column<int, string, string, int, int>(
                    conditionSpecificResults.Select(m => m.OnePercentPsmCount), labels, null, condition,
                    MarkerColor: condition.ConvertConditionToColor(),
                    MultiText: conditionSpecificResults.Select(m => m.OnePercentPsmCount).Select(p => p.ToString()).ToArray()));

                peptideCharts.Add(Chart2D.Chart.Column<int, string, string, int, int>(
                    conditionSpecificResults.Select(m => m.OnePercentPeptideCount), labels, null, condition,
                    MarkerColor: condition.ConvertConditionToColor(),
                    MultiText: conditionSpecificResults.Select(m => m.OnePercentPeptideCount).Select(p => p.ToString()).ToArray()));

                proteinCharts.Add(Chart2D.Chart.Column<int, string, string, int, int>(
                    conditionSpecificResults.Select(m => m.OnePercentProteinGroupCount), labels, null, condition,
                    MarkerColor: condition.ConvertConditionToColor(),
                    MultiText: conditionSpecificResults.Select(m => m.OnePercentProteinGroupCount).Select(p => p.ToString()).ToArray()));
            }


            var psmPlot = Chart.Combine(psmCharts).WithTitle($"1% FDR {Labels.GetLabel(isTopDown, ResultType.Psm)}")
                .WithXAxisStyle(Title.init("Cell Line"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            //var psmPlot = GenericPlots.BulkResultBarChart(resultsToPlot, isTopDown, ResultType.Psm);
            var psmOutName = Path.Combine(BulkFigureDirectory, $"{Version}_InternalComparison_Psm");
            psmPlot.SaveJPG(psmOutName, null, 1000, 800);

            var peptidePlot = Chart.Combine(peptideCharts).WithTitle($"1% FDR {Labels.GetLabel(isTopDown, ResultType.Peptide)}")
                .WithXAxisStyle(Title.init("Cell Line"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            //var peptidePlot = GenericPlots.BulkResultBarChart(resultsToPlot, isTopDown, ResultType.Peptide);
            var peptideOutName = Path.Combine(BulkFigureDirectory, $"{Version}_InternalComparison_Peptide");
            peptidePlot.SaveJPG(peptideOutName, null, 1000, 800);

            var proteinPlot = Chart.Combine(proteinCharts).WithTitle($"1% FDR {Labels.GetLabel(isTopDown, ResultType.Protein)}")
                .WithXAxisStyle(Title.init("Cell Line"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            //var proteinPlot = GenericPlots.BulkResultBarChart(resultsToPlot, isTopDown, ResultType.Protein);
            var proteinOutName = Path.Combine(BulkFigureDirectory, $"{Version}_InternalComparison_Protein");
            proteinPlot.SaveJPG(proteinOutName, null, 1000, 800);
        }

        static void PlotChimeraBreakdownBarChart(List<MetaMorpheusResult> results)
        {
            Log($"Chimera Breakdown", 1);
            bool isTopDown = results.First().IsTopDown;
            var resultsToPlot = results.SelectMany(p => p.ChimeraBreakdownFile)
                .ToList();

            var psmPlot = resultsToPlot.GetChimeraBreakdownStackedColumn_Scaled(ResultType.Psm, isTopDown);
            var psmOutPath = Path.Combine(BulkFigureDirectory, $"{Version}_ChimeraBreakdown_Psm");
            psmPlot.SaveJPG(psmOutPath, null, 1000, 800);

            var peptidePlot = resultsToPlot.GetChimeraBreakdownStackedColumn_Scaled(ResultType.Peptide, isTopDown);
            var peptideOutPath = Path.Combine(BulkFigureDirectory, $"{Version}_ChimeraBreakdown_Peptide");
            peptidePlot.SaveJPG(peptideOutPath, null, 1000, 800);
        }

        static void PlotPossibleFeatures(List<MetaMorpheusResult> results)
        {
            Log($"Possible Features", 1);
            var noIdString = "No ID";
            bool isTopDown = results.First().IsTopDown;
            var resultsToPlot = results.SelectMany(p => p.ChimericSpectrumSummaryFile)
                .ToList();



            var resultType = ResultType.Psm;
            var records = resultsToPlot.Where(p => p.Type == resultType.ToString() && p.PossibleFeatureCount != 0).ToList();
            var chimeric = records.Where(p => p.IsChimeric && p.Type != noIdString).ToList();
            var nonChimeric = records.Where(p => !p.IsChimeric && p.Type != noIdString).ToList();

            var chimericKde = GenericPlots.KernelDensityPlot(chimeric.Select(p =>
                (double)p.PossibleFeatureCount).ToList(), "Chimeric ID", "Features per Isolation Window", "Density", 0.5);
            var nonChimericKde = GenericPlots.KernelDensityPlot(nonChimeric.Select(p =>
                (double)p.PossibleFeatureCount).ToList(), "Non-Chimeric ID", "Features per Isolation Window", "Density", 0.5);
            var kde = Chart.Combine(new List<GenericChart.GenericChart> { chimericKde, nonChimericKde })
                .WithTitle($"1% {Labels.GetLabel(isTopDown, resultType)} Detected Features Per MS2 Isolation Window");
            var outname = $"{Version}_SpectrumSummary_FeatureCount_{Labels.GetLabel(isTopDown, resultType)}_KernelDensity";
            kde.SaveJPG(Path.Combine(BulkFigureDirectory, outname), null, 800, 600);

            (double, double)? minMax = isTopDown ? (0.0, 50.0) : (0.0, 15.0);
            var chimericHist = GenericPlots.Histogram(chimeric.Select(p => 
                (double)p.PossibleFeatureCount).ToList(), "Chimeric ID", "Features per Isolation Window", "Number of Spectra", false, minMax);
            var nonChimericHist = GenericPlots.Histogram(nonChimeric.Select(p => 
                (double)p.PossibleFeatureCount).ToList(), "Non-Chimeric ID", "Features per Isolation Window", "Number of Spectra", false, minMax);
            var hist = Chart.Combine(new List<GenericChart.GenericChart> { chimericHist, nonChimericHist })
                .WithTitle($"1% {Labels.GetLabel(isTopDown, resultType)} Detected Features Per MS2 Isolation Window");
            outname = $"{Version}_SpectrumSummary_FeatureCount_{Labels.GetLabel(isTopDown, resultType)}_Histogram";
            hist.SaveJPG(Path.Combine(BulkFigureDirectory, outname), null, 800, 600);

            



            resultType = ResultType.Peptide;
            records = resultsToPlot.Where(p => p.Type == resultType.ToString() && p.PossibleFeatureCount != 0).ToList();
            chimeric = records.Where(p => p.IsChimeric && p.Type != noIdString).ToList();
            nonChimeric = records.Where(p => !p.IsChimeric && p.Type != noIdString).ToList();

            minMax = null;
            chimericKde = GenericPlots.KernelDensityPlot(chimeric.Select(p =>
                (double)p.PossibleFeatureCount).ToList(), "Chimeric ID", "Features per Isolation Window", "Density", 0.5);
            nonChimericKde = GenericPlots.KernelDensityPlot(nonChimeric.Select(p =>
                (double)p.PossibleFeatureCount).ToList(), "Non-Chimeric ID", "Features per Isolation Window", "Density", 0.5);
            kde = Chart.Combine(new List<GenericChart.GenericChart> { chimericKde, nonChimericKde })
                .WithTitle($"1% {Labels.GetLabel(isTopDown, resultType)} Detected Features Per MS2 Isolation Window");
            outname = $"{Version}_SpectrumSummary_FeatureCount_{Labels.GetLabel(isTopDown, resultType)}_KernelDensity";
            kde.SaveJPG(Path.Combine(BulkFigureDirectory, outname), null, 800, 600);

            chimericHist = GenericPlots.Histogram(chimeric.Select(p =>
                    (double)p.PossibleFeatureCount).ToList(), "Chimeric ID", "Features per Isolation Window", "Number of Spectra", false, minMax);
            nonChimericHist = GenericPlots.Histogram(nonChimeric.Select(p =>
                    (double)p.PossibleFeatureCount).ToList(), "Non-Chimeric ID", "Features per Isolation Window", "Number of Spectra", false, minMax);
            hist = Chart.Combine(new List<GenericChart.GenericChart> { chimericHist, nonChimericHist })
                .WithTitle($"1% {Labels.GetLabel(isTopDown, resultType)} Detected Features Per MS2 Isolation Window");
            outname = $"{Version}_SpectrumSummary_FeatureCount_{Labels.GetLabel(isTopDown, resultType)}_Histogram";
            hist.SaveJPG(Path.Combine(BulkFigureDirectory, outname), null, 800, 600);

            
        }

        static void PlotFractionalIntensityPlots(List<MetaMorpheusResult> results)
        {
            Log($"Fractional Intensity", 1);
            bool isTopDown = results.First().IsTopDown;
            var resultsToPlot = results.SelectMany(p => p.ChimericSpectrumSummaryFile)
                .ToList();
            GenerateFractionalIntensityPlots(ResultType.Psm, resultsToPlot, true, true, isTopDown);
            GenerateFractionalIntensityPlots(ResultType.Psm, resultsToPlot, true, false, isTopDown);
            GenerateFractionalIntensityPlots(ResultType.Peptide, resultsToPlot, true, true, isTopDown);
            GenerateFractionalIntensityPlots(ResultType.Peptide, resultsToPlot, true, false, isTopDown);

            GenerateFractionalIntensityPlots(ResultType.Psm, resultsToPlot, false, false, isTopDown);
            GenerateFractionalIntensityPlots(ResultType.Psm, resultsToPlot, false, true, isTopDown);
            GenerateFractionalIntensityPlots(ResultType.Peptide, resultsToPlot, false, false, isTopDown);
            GenerateFractionalIntensityPlots(ResultType.Peptide, resultsToPlot, false, true, isTopDown);
        }

        static void GenerateFractionalIntensityPlots(ResultType resultType,
            List<ChimericSpectrumSummary> summaryRecords, bool isPrecursor, bool sumPrecursor, bool isTopDown = false)
        {
            var records = summaryRecords.Where(p => p.Type == resultType.ToString()).ToList();
            var chimeric = records.Where(p => p.IsChimeric).ToList();
            var nonChimeric = records.Where(p => !p.IsChimeric).ToList();

            var chimericFractionalIntensity = sumPrecursor
                ? isPrecursor
                    ? chimeric.GroupBy(p => p,
                            new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber,
                                result => result.FileName))
                        .ToDictionary(p => p.Key, p => p.Select(b => b.PrecursorFractionalIntensity).Sum())
                        .Values.ToList()
                    : chimeric.GroupBy(p => p,
                            new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber,
                                result => result.FileName))
                        .ToDictionary(p => p.Key, p => p.Select(b => b.FragmentFractionalIntensity).Sum())
                        .Values.ToList()
                : chimeric.Select(p => isPrecursor ? p.PrecursorFractionalIntensity : p.FragmentFractionalIntensity)
                    .ToList();
            var nonChimericFractionalIntensity = sumPrecursor
                ? isPrecursor
                    ? nonChimeric.GroupBy(p => p,
                            new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber,
                                result => result.FileName))
                        .ToDictionary(p => p.Key, p => p.Select(b => b.PrecursorFractionalIntensity).Sum())
                        .Values.ToList()
                    : nonChimeric.GroupBy(p => p,
                            new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber,
                                result => result.FileName))
                        .ToDictionary(p => p.Key, p => p.Select(b => b.FragmentFractionalIntensity).Sum())
                        .Values.ToList()
                : nonChimeric.Select(p => isPrecursor ? p.PrecursorFractionalIntensity : p.FragmentFractionalIntensity)
                    .ToList();

            (double, double) minMax = isTopDown switch
            {
                true when sumPrecursor && isPrecursor => (0.0, 1.0),
                true when sumPrecursor && !isPrecursor => (0.0, 0.8),
                true when !sumPrecursor && isPrecursor => (0.0, 0.3),
                true when !sumPrecursor && !isPrecursor => (0.0, 0.25),

                false when sumPrecursor && isPrecursor => (0.0, 1.0),
                false when sumPrecursor && !isPrecursor => (0.0, 0.6),
                false when !sumPrecursor && isPrecursor => (0.0, 1.0),
                false when !sumPrecursor && !isPrecursor => (0.0, 0.6),

                _ => (0.0, 1.0)
            };


            var label = /*isPrecursor ? sumPrecursor ?*/ "Percent Identified Intensity" /*: "Precursor ID Fractional Intensity" : "Fragment Fractional Intensity"*/;
            var titleEnd = sumPrecursor
                ? isPrecursor ? "Per Isolation Window" : "Per MS2"
                : "Per ID";
            var outPrecursor = isPrecursor ? "Precursor" : "Fragment";
            var outType = sumPrecursor ? "Summed" : "Independent";

            var chimericHist = GenericPlots.Histogram(chimericFractionalIntensity, "Chimeric ID", label, "Number of Spectra", false, minMax);
            var nonChimericHist = GenericPlots.Histogram(nonChimericFractionalIntensity, "Non-Chimeric ID", label, "Number of Spectra", false, minMax);
            var hist = Chart.Combine(new List<GenericChart.GenericChart> { chimericHist, nonChimericHist })
                .WithTitle($"1% {Labels.GetLabel(isTopDown, resultType)} Identified {outPrecursor} Intensity {titleEnd}")
                .WithAxisAnchor(Y: 1);
            var outName = $"SpectrumSummary_{outPrecursor}FractionalIntensity_{outType}_{Labels.GetLabel(isTopDown, resultType)}_Histogram";
            //hist.SaveInRunResultOnly(RunResult, outName);

            var chimericKde = GenericPlots.KernelDensityPlot(chimericFractionalIntensity, "Chimeric ID", label, "Density", 0.04);
            var nonChimericKde = GenericPlots.KernelDensityPlot(nonChimericFractionalIntensity, "Non-Chimeric ID", label, "Density", 0.04);
            var kde = Chart.Combine(new List<GenericChart.GenericChart> { chimericKde, nonChimericKde })
                .WithTitle($"1% {Labels.GetLabel(isTopDown, resultType)} Identified {outPrecursor} Intensity {titleEnd}")
                .WithLayout(PlotlyBase.DefaultLayout)
                .WithAxisAnchor(Y: 2);
            outName = $"SpectrumSummary_{outPrecursor}FractionalIntensity_{outType}_{Labels.GetLabel(isTopDown, resultType)}_KernelDensity";
            //kde.SaveInRunResultOnly(RunResult, outName);


            var combinedHistKde = Chart.Combine(new[]
                {
                    kde.WithLineStyle(Dash: StyleParam.DrawingStyle.Dot).WithMarkerStyle(Opacity: 0.8),
                    hist
                })
                .WithYAxisStyle(Title.init("Number of Spectra"), Side: StyleParam.Side.Left,
                    Id: StyleParam.SubPlotId.NewYAxis(1))
                .WithYAxisStyle(Title.init("Density"), Side: StyleParam.Side.Right,
                    Id: StyleParam.SubPlotId.NewYAxis(2),
                    Overlaying: StyleParam.LinearAxisId.NewY(1))
                .WithLayout(PlotlyBase.DefaultLayoutNoLegend);
            outName =
                $"{Version}_SpectrumSummary_{outPrecursor}FractionalIntensity_{outType}_{Labels.GetLabel(isTopDown, resultType)}_Combined";
            combinedHistKde.SaveJPG(Path.Combine(BulkFigureDirectory, outName), null, 600, 500);
        }

        #endregion


    }

}
