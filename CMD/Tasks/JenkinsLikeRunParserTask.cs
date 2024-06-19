using Analyzer.SearchType;
using Analyzer.Util;
using CMD.TaskParameters;
using CMD.Util;
using System.IO;
using Analyzer.Plotting.ComparativePlots;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;

namespace CMD.Tasks;

public class JenkinsLikeRunParserTask : BaseResultAnalyzerTask
{
    public override CommandLineTasks MyTask => CommandLineTasks.JenkinsLikeRunParser;
    protected override JenkinsLikeRunParserTaskParameters Parameters { get; }

    public JenkinsLikeRunParserTask(JenkinsLikeRunParserTaskParameters parameters) : base()
    {
        Parameters = parameters;
    }

    protected override void RunSpecific()
    {
        var allResults = BuildResultsObjects();


        Log("Parsing All Single Search Data");
        foreach (var singleRunResults in allResults.SelectMany(p => p.Select(m => m)))
        {
            singleRunResults.Override = Parameters.Override;
            var mm = (MetaMorpheusResult)singleRunResults;

            mm.GetBulkResultCountComparisonMultipleFilteringTypesFile();
            mm.GetIndividualFileComparison();
            mm.GetIndividualFileResultCountingMultipleFilteringTypesFile();

            mm.Override = false;
        }

        Log("Plotting All Single Search Data");
        foreach (var singleRunResults in allResults.SelectMany(p => p.Select(m => m)))
        {
            var mm = (MetaMorpheusResult)singleRunResults;
            mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score);
            mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score);
            try
            {
                mm.PlotPepFeaturesScatterGrid();
            }
            catch (Exception e)
            {
                Warn($"Could not plot PEP features scatter grid {e.Message}");
            }
        }

        Log("Parsing All Whole Run Data");
        foreach (var groupRun in allResults)
        {
            groupRun.Override = Parameters.Override;
            groupRun.GetBulkResultCountComparisonMultipleFilteringTypesFile();
            groupRun.GetIndividualFileComparison();
            groupRun.GetIndividualFileResultCountingMultipleFilteringTypesFile();
            groupRun.Override = false;
        }

        Log("Plotting All Whole Run Data");
        foreach (var groupRun in allResults)
        {
            groupRun.PlotIndividualFileResults(ResultType.Psm, null, false);
            groupRun.PlotIndividualFileResults(ResultType.Peptide, null, false);
            groupRun.PlotIndividualFileResults(ResultType.Protein, null, false);
        }

        Log("Parsing All Aggregated Data");
        allResults.Override = Parameters.Override;
        allResults.GetBulkResultCountComparisonMultipleFilteringTypesFile();
        allResults.IndividualFileComparison();
        allResults.GetIndividualFileResultCountingMultipleFilteringTypesFile();
        allResults.Override = false;

        Log("Plotting All Aggregated Data");
        allResults.PlotBulkResultsDifferentFilteringTypePlotsForPullRequests();
        allResults.PlotStackedIndividualFileComparison(ResultType.Psm, false);
        allResults.PlotStackedIndividualFileComparison(ResultType.Peptide, false);
        allResults.PlotStackedIndividualFileComparison(ResultType.Protein, false);
    }

    private AllResults BuildResultsObjects()
    {
        Log("Parsing Input Directory");
        List<CellLineResults> differentRunResults = new();
        foreach (var specificRunDirectory in Directory.GetDirectories(Parameters.InputDirectoryPath))
        {
            if (specificRunDirectory.Contains("Figures") || Path.GetFileName(specificRunDirectory).StartsWith("XX"))
                continue;

            var name = Path.GetFileName(specificRunDirectory);
            var runDirectories = specificRunDirectory.GetDirectories();

            // if started running all tasks
            if (runDirectories.Count(p => !p.Contains("Figure")) == 5) // Searches ran during the test
            {
                var last = runDirectories.First(p => p.Contains("TopDown"));
                var topDownDirectories = last.GetDirectories();
                // if started running last task
                if (topDownDirectories.Count(p => !p.Contains("Figure")) is 7 or 8) // number of top-down tasks ran
                {
                    var postGPTMD = topDownDirectories.First(p => p.Contains("Task7"));
                    var files = Directory.GetFiles(postGPTMD, "*.psmtsv", SearchOption.AllDirectories);

                    // if last is still running
                    if (!files.Any(p => p.Contains("AllProteoforms") || p.Contains("AllPSMs")) &&
                        !files.Any(p => p.Contains("AllProteinGroups")))
                        continue;
                }
                else
                    continue;

            }
            else
                continue;

            var semiSpecificDir = runDirectories.First(p => p.Contains("Semispecific"));
            var semiSpecific = new MetaMorpheusResult(semiSpecificDir, name, "Semi-Specific");

            var nonspecificDir = runDirectories.First(p => p.Contains("Nonspecific"));
            var nonSpecific = new MetaMorpheusResult(nonspecificDir, name, "Non-Specific");

            var modernDir = runDirectories.First(p => p.Contains("Modern"));
            var modern = new MetaMorpheusResult(modernDir, name, "Modern");


            var classicDir = runDirectories.First(p => p.Contains("Classic"));
            var classicInitialDir = classicDir.GetDirectories().First(p => p.Contains("Task1"));
            var classicIntial = new MetaMorpheusResult(classicInitialDir, name, "Classic - Initial");
            var classicPostCalibDir = classicDir.GetDirectories().First(p => p.Contains("Task3"));
            var classicPostCalib =
                new MetaMorpheusResult(classicPostCalibDir, name, "Classic - Post Calibration");
            var classicPostGptmdDir = classicDir.GetDirectories().First(p => p.Contains("Task5"));
            var classicPostGptmd = new MetaMorpheusResult(classicPostGptmdDir, name, "Classic - Post GPTMD");


            var topDownDir = runDirectories.First(p => p.Contains("TopDown"));
            var tdInitialDir = topDownDir.GetDirectories().First(p => p.Contains("Task1"));
            var tdInitial = new MetaMorpheusResult(tdInitialDir, name, "TopDown - Initial");
            var tdPostCalibDir = topDownDir.GetDirectories().First(p => p.Contains("Task3"));
            var tdPostCalib = new MetaMorpheusResult(tdPostCalibDir, name, "TopDown - Post Calibration");
            var tdPostAveragingDir = topDownDir.GetDirectories().First(p => p.Contains("Task5"));
            var tdPostAveraging = new MetaMorpheusResult(tdPostAveragingDir, name, "TopDown - Post Averaging");
            var tdPostGPTMDDir = topDownDir.GetDirectories().First(p => p.Contains("Task7"));
            var tdPostGPTMD = new MetaMorpheusResult(tdPostGPTMDDir, name, "TopDown - Post GPTMD");

            var allMMResults = new List<SingleRunResults>()
                    {
                        semiSpecific,
                        nonSpecific,
                        modern,
                        classicIntial,
                        classicPostCalib,
                        classicPostGptmd,
                        tdInitial,
                        tdPostCalib,
                        tdPostAveraging,
                        tdPostGPTMD
                    };

            var run = new CellLineResults(specificRunDirectory, allMMResults);
            differentRunResults.Add(run);
        }

        var results = new AllResults(Parameters.InputDirectoryPath, differentRunResults);
        return results;
    }
}