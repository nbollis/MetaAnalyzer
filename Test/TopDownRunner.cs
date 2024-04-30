using Plotly.NET.CSharp;
using Plotly.NET.ImageExport;
using Readers;
using ResultAnalyzer.Plotting;
using ResultAnalyzer.ResultType;

namespace Test
{
    internal class TopDownRunner
    {
        internal static string DirectoryPath = @"B:\Users\Nic\Chimeras\TopDown_Analysis";
        internal static bool RunOnAll = false;
        internal static bool Override = false;
        private static AllResults? _allResults;
        internal static AllResults AllResults => _allResults ??= new AllResults(DirectoryPath, Directory.GetDirectories(DirectoryPath)
            .Where(p => !p.Contains("Figures") && RunOnAll || p.Contains("Jurkat"))
            .Select(datasetDirectory => new CellLineResults(datasetDirectory)).ToList());

        [Test]
        public static void AppendQValueCounts()
        {
            var results = AllResults
                .First(p => p.CellLine == "Jurkat")
                .First(p => p.Condition == "MetaMorpheus");

            var proteoforms = SpectrumMatchTsvReader.ReadPsmTsv(results._peptidePath, out _);
            var qValueFilteredProteoforms = proteoforms.Where(p => p.QValue <= 0.01).ToList();
            var pepQValueFilteredProteoforms = proteoforms.Where(p => p.PEP_QValue <= 0.01).ToList();
            var psms = SpectrumMatchTsvReader.ReadPsmTsv(results._psmPath, out _);
            var qValueFiltered = psms.Where(p => p.QValue <= 0.01).ToList();
            var pepQValueFiltered = psms.Where(p => p.PEP_QValue <= 0.01).ToList();

            // Absolute
            //qValueFilteredProteoforms.ChimeraTargetDecoyChart(true, ResultAnalyzer.FileTypes.Internal.ChimeraBreakdownType.Peptide, "QValue", out int width)
            //    .SavePNG(Path.Combine(results.DirectoryPath, "Figures", "ChimeraTargetDecoy_Proteoforms_QValue_Absolute"), null, width, 600);
            //pepQValueFilteredProteoforms.ChimeraTargetDecoyChart(true, ResultAnalyzer.FileTypes.Internal.ChimeraBreakdownType.Peptide, "PEP QValue", out width)
            //    .SavePNG(Path.Combine(results.DirectoryPath, "Figures", "ChimeraTargetDecoy_Proteoforms_PEPQValue_Absolute"), null, width, 600);

            //qValueFiltered.ChimeraTargetDecoyChart(true, ResultAnalyzer.FileTypes.Internal.ChimeraBreakdownType.Psm, "QValue", out width)
            //    .SavePNG(Path.Combine(results.DirectoryPath, "Figures", "ChimeraTargetDecoy_PrSMs_QValue_Absolute"), null, width, 600);
            //pepQValueFiltered.ChimeraTargetDecoyChart(true, ResultAnalyzer.FileTypes.Internal.ChimeraBreakdownType.Psm, "PEP QValue", out width)
            //    .SavePNG(Path.Combine(results.DirectoryPath, "Figures", "ChimeraTargetDecoy_PrSMs_PEPQValue_Absolute"), null, width, 600);

            // Percent
            qValueFilteredProteoforms.ChimeraTargetDecoyChart(true, ResultAnalyzer.FileTypes.Internal.ChimeraBreakdownType.Peptide, "QValue", out int width)
                .SavePNG(Path.Combine(results.DirectoryPath, "Figures", "ChimeraTargetDecoy_Proteoforms_QValue_Percent"), null, width, 600);
            pepQValueFilteredProteoforms.ChimeraTargetDecoyChart(true, ResultAnalyzer.FileTypes.Internal.ChimeraBreakdownType.Peptide, "PEP QValue", out width)
                .SavePNG(Path.Combine(results.DirectoryPath, "Figures", "ChimeraTargetDecoy_Proteoforms_PEPQValue_Percent"), null, width, 600);

            qValueFiltered.ChimeraTargetDecoyChart(true, ResultAnalyzer.FileTypes.Internal.ChimeraBreakdownType.Psm, "QValue", out width)
                .SavePNG(Path.Combine(results.DirectoryPath, "Figures", "ChimeraTargetDecoy_PrSMs_QValue_Percent"), null, width, 600);
            pepQValueFiltered.ChimeraTargetDecoyChart(true, ResultAnalyzer.FileTypes.Internal.ChimeraBreakdownType.Psm, "PEP QValue", out width)
                .SavePNG(Path.Combine(results.DirectoryPath, "Figures", "ChimeraTargetDecoy_PrSMs_PEPQValue_Percent"), null, width, 600);


        }





        [Test]
        public static void RunAllParsing()
        {
            foreach (var cellLine in AllResults)
            {
                foreach (var result in cellLine)
                {
                    if (result is MsPathFinderTResults)
                        result.Override = true;
                    result.IndividualFileComparison();
                    result.GetBulkResultCountComparisonFile();
                    result.CountChimericPsms();
                    if (result is MetaMorpheusResult mm)
                        mm.CountChimericPeptides();
                    result.Override = false;
                }

                cellLine.Override = true;
                cellLine.IndividualFileComparison();
                cellLine.GetBulkResultCountComparisonFile();
                cellLine.CountChimericPsms();
                cellLine.CountChimericPeptides();
                cellLine.Override = false;
            }

            AllResults.Override = true;
            AllResults.IndividualFileComparison();
            AllResults.GetBulkResultCountComparisonFile();
            AllResults.CountChimericPsms();
            AllResults.CountChimericPeptides();
        }

        [Test]
        public static void PlotAllFigures()
        {
            foreach (var cellLine in AllResults)
            {
                //cellLine.PlotIndividualFileResults();
                cellLine.PlotCellLineChimeraBreakdown();
                cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
            }

            AllResults.PlotBulkResultChimeraBreakDown();
            AllResults.PlotBulkResultChimeraBreakDown_TargetDecoy();
            AllResults.PlotInternalMMComparison();
            AllResults.PlotBulkResultComparison();
            AllResults.PlotStackedIndividualFileComparison();
        }

        [Test]
        public static void MsPathTDatasetInfoGenerator()
        {
            foreach (MsPathFinderTResults mspt in AllResults.SelectMany(p => p.Results)
                .Where(p => p is MsPathFinderTResults mspt && mspt.IndividualFileResults.Count is 20 or 43))
            {
                mspt.CreateDatasetInfoFile();
            }
        }
    }
}
