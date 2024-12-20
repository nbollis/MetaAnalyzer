using Plotting.RadicalFragmentation;
using RadicalFragmentation;

namespace Test
{
    internal class JustRunStuff
    {
        [Test]
        public static void GeneratePlotsOnSecondIteration()
        {
            string dbPath = @"D:\Projects\RadicalFragmentation\FragmentAnalysis\Databases\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";
            string dirPath = @"D:\Projects\RadicalFragmentation\FragmentAnalysis\SeventhIteration";
            var explorers = DirectoryToFragmentExplorers.GetFragmentExplorersFromDirectory(dbPath, dirPath);


            foreach (var groupedExplorers in explorers
                //.Where(p => p.AnalysisType.Equals("ETD"))
                .Where(p => p.NumberOfMods < 2)
                .GroupBy(p => (p.AnalysisType, p.MissedMonoIsotopics)))
            {
                groupedExplorers.ToList().CreatePlots();
            }

            foreach (var groupedExplorers in explorers
                         //.Where(p => p.AnalysisType.Equals("ETD"))
                         .Where(p => p.NumberOfMods < 2)
                         .GroupBy(p => p.AnalysisType))
            {
                groupedExplorers.ToList().CreateMissedMonoCombinedCumulativeFragCountPlot();
            }
        }
    }
}
