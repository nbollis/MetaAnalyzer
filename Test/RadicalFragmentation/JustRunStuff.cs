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
              //  .Where(p => p.NumberOfMods < 2)
                .ToList();

            var fragNeededOutPath = Path.Combine(DirectoryPath, $"{FileIdentifiers.FragNeededSummary}.csv");
            var fragNeededSummary = new FragmentsNeededFile(fragNeededOutPath);
            List<FragmentsNeededSummary> currentResults = new();
            if (File.Exists(fragNeededOutPath))
            {
                fragNeededSummary.LoadResults();
                currentResults = fragNeededSummary.Results;
            }

            var precursorCompetitionOutPath = Path.Combine(DirectoryPath, $"{FileIdentifiers.PrecursorCompetitionSummary}.csv");
            var precursorCompetitionSummary = new PrecursorCompetitionFile(precursorCompetitionOutPath);
            List<PrecursorCompetitionSummary> precursorCurrentResults = new();
            if (File.Exists(precursorCompetitionOutPath))
            {
                precursorCompetitionSummary.LoadResults();
                precursorCurrentResults = precursorCompetitionSummary.Results;
            }


            foreach (var explorer in explorers)
            {
                if (!currentResults.Any(p => p.FragmentationType == explorer.AnalysisType
                                             && p.AmbiguityLevel == explorer.AmbiguityLevel
                                             && p.MissedMonoisotopics == explorer.MissedMonoIsotopics
                                             && p.NumberOfMods == explorer.NumberOfMods
                                             && p.PpmTolerance == explorer.Tolerance))
                {
                    var summary = explorer.ToFragmentsNeededSummaryRecords();
                    currentResults.AddRange(summary);
                }

                if (!precursorCurrentResults.Any(p => p.FragmentationType == explorer.AnalysisType
                                                      && p.AmbiguityLevel == explorer.AmbiguityLevel
                                                      && p.MissedMonoisotopics == explorer.MissedMonoIsotopics
                                                      && p.NumberOfMods == explorer.NumberOfMods
                                                      && p.PpmTolerance == explorer.Tolerance))
                {
                    var summary = explorer.ToPrecursorCompetitionSummaryRecords();
                    precursorCurrentResults.AddRange(summary);
                }
            }

            currentResults = currentResults.OrderBy(p => p.FragmentationType)
                .ThenBy(p => p.AmbiguityLevel)
                .ThenBy(p => p.MissedMonoisotopics)
                .ThenBy(p => p.NumberOfMods)
                .ThenBy(p => p.PpmTolerance)
                .ToList();
            fragNeededSummary.Results = currentResults;
            fragNeededSummary.WriteResults(fragNeededOutPath);

            precursorCurrentResults = precursorCurrentResults.OrderBy(p => p.FragmentationType)
                .ThenBy(p => p.AmbiguityLevel)
                .ThenBy(p => p.MissedMonoisotopics)
                .ThenBy(p => p.NumberOfMods)
                .ThenBy(p => p.PpmTolerance)
                .ToList();
            precursorCompetitionSummary.Results = precursorCurrentResults;
            precursorCompetitionSummary.WriteResults(precursorCompetitionOutPath);
        }

        [Test]
        public static void UseSummaryRecordsForAllFigures()
        {
            var baseDirPath = @"D:\Projects\RadicalFragmentation\FragmentAnalysis\SeventhIteration";
            var directoryPath = @"D:\Projects\RadicalFragmentation\FragmentAnalysis\SeventhIteration\Figures_All";
            //baseDirPath = DirectoryPath;
            var fragNeededOutPath = Path.Combine(baseDirPath, $"{FileIdentifiers.FragNeededSummary}.csv");
            var fragNeededSummary = new FragmentsNeededFile(fragNeededOutPath);
            fragNeededSummary.LoadResults();

            int maxMods = 6;
            int[] missedMonos = new[] { 0, 1, 2, 3 };
            double[] tolerances = new[] { 10.0, 20, 50, 100 };
            string[] types = new[] { "ETD", "Tryptophan", "Cysteine" };
            int[] ambig = new[] { 1, 2 };

            var resultsToUse = fragNeededSummary.Results
                .Where(p => p.NumberOfMods <= maxMods)
                .ToList();

            foreach (var type in types)
                foreach (var amb in ambig)
                {
                    var summary = resultsToUse
                        .Where(p => p.FragmentationType == type)
                        .Where(p => p.AmbiguityLevel == amb)
                        .ToList();

                    summary.WriteToleranceFragmentsNeededHistogram(directoryPath, type, amb, 0);
                    //summary.WriteToleranceFragmentsNeededHistogram(directoryPath, type, amb, 0, false);
                    summary.WriteToleranceCumulativeLine(directoryPath, type, amb, 0);

                    summary.WriteMissedMonoFragmentsNeededHistogram(directoryPath, type, amb, 10);
                    //summary.WriteMissedMonoFragmentsNeededHistogram(directoryPath, type, amb, 10, false);
                    summary.WriteMissedMonoCumulativeLine(directoryPath, type, amb, 10);
                    foreach (var missedMono in missedMonos)
                    {
                        var innerPath = Path.Combine(directoryPath, $"{missedMono} Missed Mono");
                        var innerSummary = summary.Where(p => p.MissedMonoisotopics == missedMono)
                            .ToList();

                        innerSummary.WriteFragmentsNeededHistogram(innerPath, type, amb, 10, missedMono);
                        innerSummary.WriteCumulativeFragmentsNeededLine(innerPath, type, amb, 10, missedMono, true);
                        innerSummary.WriteHybridFragmentNeeded(innerPath, type, amb, 10, missedMono);
                    }

                    foreach (var tolerance in tolerances)
                    {
                        if (tolerance == 10)
                            continue;

                        var innerPath = Path.Combine(directoryPath, $"{tolerance} ppm");
                        var innerSummary = summary.Where(p => p.PpmTolerance == tolerance)
                            .ToList();

                        innerSummary.WriteFragmentsNeededHistogram(innerPath, type, amb, tolerance);
                        innerSummary.WriteCumulativeFragmentsNeededLine(innerPath, type, amb, tolerance, 0, true);
                        innerSummary.WriteHybridFragmentNeeded(innerPath, type, amb, tolerance, 0);
                    }
                }
        }
    }
}
