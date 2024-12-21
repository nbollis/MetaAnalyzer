using Plotly.NET.CSharp;
using Plotting.RadicalFragmentation;
using RadicalFragmentation;
using ResultAnalyzerUtil;

namespace Test
{
    internal class JustRunStuff
    {
        static string DatabasePath = @"D:\Projects\RadicalFragmentation\FragmentAnalysis\Databases\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";
        static string DirectoryPath = @"D:\Projects\RadicalFragmentation\FragmentAnalysis\SeventhIteration";

        [Test]
        public static void GeneratePlotsOnSecondIteration()
        {
            var explorers = DirectoryToFragmentExplorers.GetFragmentExplorersFromDirectory(DatabasePath, DirectoryPath)
                .ToList();

            var mainExplorers = explorers.Where(p => p is { Tolerance: 10, MissedMonoIsotopics: 1 or 0 })
                .ToList();

            //foreach (var groupedExplorers in mainExplorers
            //    //.Where(p => p.AnalysisType.Equals("ETD"))
            //   // .Where(p => p.NumberOfMods < 2)
            //    .GroupBy(p => (p.AnalysisType, p.MissedMonoIsotopics)))
            //{
            //    groupedExplorers.ToList().CreatePlots();
            //}

            foreach (var groupedExplorers in mainExplorers
                         //.Where(p => p.AnalysisType.Equals("ETD"))
                         //.Where(p => p.NumberOfMods < 2)
                         .GroupBy(p => p.AnalysisType))
            {
                groupedExplorers.ToList().CreateMissedMonoCombinedCumulativeFragCountPlot();
            }
        }

        [Test]
        public static void CreateSummaryRecords()
        {
            var explorers = DirectoryToFragmentExplorers.GetFragmentExplorersFromDirectory(DatabasePath, DirectoryPath)
              //  .Where(p => p.NumberOfMods < 2)
                .ToList();


            var fragNeededOutPath = Path.Combine(DirectoryPath, $"{FileIdentifiers.FragNeededSummary}.csv");
            var fragNeededSummary = new FragmentsNeededFile()
            {
                Results = explorers.SelectMany(p => p.ToFragmentsNeededSummaryRecords()).ToList()
            };
            fragNeededSummary.WriteResults(fragNeededOutPath);

            var precursorCompetitionOutPath = Path.Combine(DirectoryPath, $"{FileIdentifiers.PrecursorCompetitionSummary}.csv");
            var precursorCompetitionSummary = new PrecursorCompetitionFile()
            {
                Results = explorers.SelectMany(p => p.ToPrecursorCompetitionSummaryRecords()).ToList()
            };
            precursorCompetitionSummary.WriteResults(precursorCompetitionOutPath);
        }

        [Test]
        public static void UseSummaryRecords()
        {
            var directoryPath = @"D:\Projects\RadicalFragmentation\FragmentAnalysis\SeventhIteration\ATestFigures";
            var fragNeededOutPath = Path.Combine(DirectoryPath, $"{FileIdentifiers.FragNeededSummary}.csv");
            var fragNeededSummary = new FragmentsNeededFile(fragNeededOutPath);
            fragNeededSummary.LoadResults();

            int[] missedMonos = new[] { 0/*, 1, 2, 3*/ };
            double[] tolerances = new[] {10.0/*,20,50,100*/ };
            string[] types = new[] { "ETD", "Tryptophan", "Cysteine" };
            int[] ambig = new[] { 1, 2 };


            foreach (var type in types)
                foreach (var amb in ambig)
                {
                    var summary = fragNeededSummary.Results
                        .Where(p => p.FragmentationType == type)
                        .Where(p => p.AmbiguityLevel == amb)
                        .ToList();

                    summary.GetToleranceCumulativeChart(type, amb, 0).Show();
                    summary.GetToleranceFragmentsNeededHist(type, amb, 0).Show();

                    summary.WriteMissedMonoHistogram(directoryPath, type, amb, 10);
                    summary.WriteMissedMonoHistogram(directoryPath, type, amb, 10, false);
                    summary.WriteMissedMonoCumulativeChart(directoryPath, type, amb, 10);
                    foreach (var missedMono in missedMonos)
                    {
                        var innerPath = Path.Combine(directoryPath, $"{missedMono} Missed Mono");
                        var innerSummary= summary.Where(p => p.MissedMonoisotopics == missedMono)
                            .ToList();

                        var label = SummaryPlots.GetLabel(type, missedMono, 10);
                        innerSummary.WriteMinFragmentsNeededHist(innerPath, type, amb, 10, missedMono);
                        innerSummary.WriteCumulativeFragmentsNeededChart(innerPath, type, amb, 10, missedMono, true);
                        innerSummary.WriteHybridFragmentNeededChart(innerPath, type, amb, 10, missedMono);
                    }
                }
        }           
    }
}
