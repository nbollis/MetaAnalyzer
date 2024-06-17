using Analyzer.SearchType;
using Analyzer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Plotting.ComparativePlots;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;

namespace Test
{
    internal class PepTestingRunner
    {
        internal static string DirectoryPath = @"B:\Users\Nic\PEPTesting";

        private static AllResults? _allResults;

        public static AllResults AllResults
        {
            get
            {
                List<CellLineResults> differentRunResults = new();
                foreach (var specificRunDirectory in Directory.GetDirectories(DirectoryPath).Where(p => !p.Contains("Figures")).Take(2))
                {
                    var runDirectories = specificRunDirectory.GetDirectories();

                    var semiSpecificDir = runDirectories.First(p => p.Contains("Semispecific"));
                    var semiSpecific = new MetaMorpheusResult(semiSpecificDir, null, "Semi-Specific");

                    var nonspecificDir = runDirectories.First(p => p.Contains("Nonspecific"));
                    var nonSpecific = new MetaMorpheusResult(nonspecificDir, null, "Non-Specific");

                    var modernDir = runDirectories.First(p => p.Contains("Modern"));
                    var modern = new MetaMorpheusResult(modernDir, null, "Modern");


                    var classicDir = runDirectories.First(p => p.Contains("Classic"));
                    var classicInitialDir = classicDir.GetDirectories().First(p => p.Contains("Task1"));
                    var classicIntial = new MetaMorpheusResult(classicInitialDir, null, "Classic - Initial");
                    var classicPostCalibDir = classicDir.GetDirectories().First(p => p.Contains("Task3"));
                    var classicPostCalib =
                        new MetaMorpheusResult(classicPostCalibDir, null, "Classic - Post Calibration");
                    var classicPostGptmdDir = classicDir.GetDirectories().First(p => p.Contains("Task5"));
                    var classicPostGptmd = new MetaMorpheusResult(classicPostGptmdDir, null, "Classic - Post GPTMD");


                    var topDownDir = runDirectories.First(p => p.Contains("TopDown"));
                    var tdInitialDir = topDownDir.GetDirectories().First(p => p.Contains("Task1"));
                    var tdInitial = new MetaMorpheusResult(tdInitialDir, null, "TopDown - Initial");
                    var tdPostCalibDir = topDownDir.GetDirectories().First(p => p.Contains("Task3"));
                    var tdPostCalib = new MetaMorpheusResult(tdPostCalibDir, null, "TopDown - Post Calibration");
                    var tdPostAveragingDir = topDownDir.GetDirectories().First(p => p.Contains("Task5"));
                    var tdPostAveraging = new MetaMorpheusResult(tdPostAveragingDir, null, "TopDown - Post Averaging");
                    var tdPostGPTMDDir = topDownDir.GetDirectories().First(p => p.Contains("Task7"));
                    var tdPostGPTMD = new MetaMorpheusResult(tdPostGPTMDDir, null, "TopDown - Post GPTMD");

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

                var results = new AllResults(DirectoryPath, differentRunResults);
                return _allResults ??= results;
            }
        }

        [Test]
        public static void FirstGo()
        {
            
            foreach (var groupRun in AllResults)
            {
                foreach (var singleRunResults in groupRun)
                {
                    var mm = (MetaMorpheusResult)singleRunResults;
                    mm.GetBulkResultCountComparisonMultipleFilteringTypesFile();
                    mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score);
                    mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score);
                }

                groupRun.GetBulkResultCountComparisonMultipleFilteringTypesFile();
                groupRun.GetIndividualFileComparison();
                groupRun.PlotIndividualFileResults(ResultType.Psm, null, false);
                groupRun.PlotIndividualFileResults(ResultType.Peptide, null, false);
                groupRun.PlotIndividualFileResults(ResultType.Protein, null, false);
            }

            AllResults.PlotStackedIndividualFileComparison(ResultType.Psm, false);
            AllResults.PlotStackedIndividualFileComparison(ResultType.Peptide, false);
            AllResults.PlotStackedIndividualFileComparison(ResultType.Protein, false);
        }



        [Test]
        public static void TargetDecoyCurveTestRunner()
        {
            string dirpath = @"B:\Users\Nic\Chimeras\Testing";
            List<MetaMorpheusResult> mmResults = dirpath.GetDirectories().Select(mmDir => new MetaMorpheusResult(mmDir)).ToList();

            foreach (var result in mmResults)
            {
                result.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, true);
                result.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, false);
                result.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, true);
                result.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, false);
            }
        }
    }
}
