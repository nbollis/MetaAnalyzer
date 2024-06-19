﻿using Analyzer.SearchType;
using Analyzer.Util;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
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
                foreach (var specificRunDirectory in Directory.GetDirectories(DirectoryPath).Where(p => !p.Contains("Figures")))
                {
                    var name = Path.GetFileName(specificRunDirectory);
                    var runDirectories = specificRunDirectory.GetDirectories();

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
                    singleRunResults.Override = true;
                    var mm = (MetaMorpheusResult)singleRunResults;
                    mm.GetBulkResultCountComparisonMultipleFilteringTypesFile();
                    //mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score);
                    //mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score);
                    singleRunResults.Override = false;
                }

                groupRun.Override = true;
                groupRun.GetBulkResultCountComparisonMultipleFilteringTypesFile();
                groupRun.Override = false;

                //groupRun.GetIndividualFileComparison();
                //groupRun.PlotIndividualFileResults(ResultType.Psm, null, false);
                //groupRun.PlotIndividualFileResults(ResultType.Peptide, null, false);
                //groupRun.PlotIndividualFileResults(ResultType.Protein, null, false);
            }

            AllResults.Override = true;
            AllResults.GetBulkResultCountComparisonMultipleFilteringTypesFile();
            AllResults.Override = false;


            AllResults.PlotBulkResultsDifferentFilteringTypePlotsForPullRequests();

            //AllResults.PlotStackedIndividualFileComparison(ResultType.Psm, false);
            //AllResults.PlotStackedIndividualFileComparison(ResultType.Peptide, false);
            //AllResults.PlotStackedIndividualFileComparison(ResultType.Protein, false);
        }

        [Test]
        public static void PlotChart()
        {
            AllResults.PlotBulkResultsDifferentFilteringTypePlotsForPullRequests();
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