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

            foreach (var groupedExplorers in mainExplorers
                //.Where(p => p.AnalysisType.Equals("ETD"))
               // .Where(p => p.NumberOfMods < 2)
                .GroupBy(p => (p.AnalysisType, p.MissedMonoIsotopics)))
            {
                groupedExplorers.ToList().CreatePlots();
            }

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
                .Where(p => p.NumberOfMods < 2)
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
    }
}
