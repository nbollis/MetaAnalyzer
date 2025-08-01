﻿using Plotting.RadicalFragmentation;
using RadicalFragmentation;
using ResultAnalyzerUtil;

namespace Test
{
    internal class JustRunStuff
    {
        static string DatabasePath = @"D:\Projects\RadicalFragmentation\FragmentAnalysis\Databases\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";
        static string DirectoryPath = @"D:\Projects\RadicalFragmentation\FragmentAnalysis\SeventhIteration";
        //static string ProcessedResultsDirectory = @"B:\Users\Nic\RadicalFragmentation_Greedy";

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
        public static void HorseTest()
        {
            string horseCytoAccession = "P00004";
            string directoryPath = @"D:\Projects\RadicalFragmentation\FragmentAnalysis\Databases";
            string reviewedName = "uniprotkb_horseReviewed_taxonomy_id_9796_AND_reviewed_2025_02_04.xml";
            string fullName = "uniprotkb_horse_taxonomy_id_9796_2025_02_04.xml";

            //var reviewedPath = Path.Combine(directoryPath, reviewedName);
            var fullPath = Path.Combine(directoryPath, fullName);

            //var reviewed = ProteinDbLoader.LoadProteinXML(reviewedPath, true, DecoyType.None, GlobalVariables.AllModsKnown, false, [], out var unknownMods);
            //var full = ProteinDbLoader.LoadProteinXML(fullPath, true, DecoyType.None, GlobalVariables.AllModsKnown, false, [], out var unknownMods2);
            

            var explorers = DirectoryToFragmentExplorers.GetFragmentExplorersFromDirectory(DatabasePath, DirectoryPath)
                .ToList();

            var horse = explorers.Where(p => p is { Tolerance: 10, MissedMonoIsotopics: 0, Species: "Horse" })
                .ToList();

            var horseRecords = horse.SelectMany(p => p.FindNumberOfFragmentsNeededToDifferentiate().Where(r => r.Accession == horseCytoAccession)).ToList();

            var cytoFilePath = Path.Combine(DirectoryPath, "HorseCytoC.csv");
            var cytoFile = new FragmentsToDistinguishFile(cytoFilePath)
            {
                Results = horseRecords
            };
            cytoFile.WriteResults(cytoFilePath);

        }

        [Test]
        public static void CreateSummaryRecords()
        {
            var explorers = DirectoryToFragmentExplorers.GetFragmentExplorersFromDirectory(DatabasePath, DirectoryPath)
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

        [Test] // Use this one to generate ALL figures
        public static void UseSummaryRecordsForAllFigures()
        {
            var baseDirPath = @"B:\Users\Nic\RadicalFragmentation_Greedy";
            var directoryPath = @"D:\Projects\RadicalFragmentation\FragmentAnalysis\SeventhIteration\Figures_All";
            //var directoryPath = @"D:\Projects\RadicalFragmentation\FragmentAnalysis\SeventhIteration\Figures_3MaxMods";
            baseDirPath = DirectoryPath;

            int maxMods = 6;
            int[] missedMonos = new[] { 0, 1, 2, 3 };
            double[] tolerances = new[] { 10.0, 20, 50, 100 };
            string[] types = new[] { "ETD", "Tryptophan", "Cysteine" };
            int[] ambig = new[] { 1, 2 };

            #region Frag Needed Setup

            var fragNeededOutPath = Path.Combine(baseDirPath, $"{FileIdentifiers.FragNeededSummary}.csv");
            var fragNeededSummaryFile = new FragmentsNeededFile(fragNeededOutPath);
            fragNeededSummaryFile.LoadResults();


            var fragNeededResultsToUse = fragNeededSummaryFile.Results
                .Where(p => p.NumberOfMods <= maxMods)
                .ToList();

            #endregion

            #region Precursor Competition Setup

            var precCompOutPath = Path.Combine(baseDirPath, $"{FileIdentifiers.PrecursorCompetitionSummary}.csv");
            var precCompSummaryFile = new PrecursorCompetitionFile(precCompOutPath);
            precCompSummaryFile.LoadResults();

            var precCompResultsToUse = precCompSummaryFile.Results
                .Where(p => p.NumberOfMods <= maxMods)
                .ToList();

            #endregion

            #region Explorer Extra Stuff

            var explorers = DirectoryToFragmentExplorers.GetFragmentExplorersFromDirectory(DatabasePath, baseDirPath)
                .Where(p => p.NumberOfMods <= maxMods)
                .ToList();

            #endregion

            bool createdPrecursorCompetitionPlots = false;
            foreach (var type in types)
            {
                // create unique fragment histogram once per type
                var uniqueFragmentRecords = explorers
                    .Where(p => p.AnalysisType == type && p is { AmbiguityLevel: 1, MissedMonoIsotopics: 0, Tolerance: 10 })
                    .SelectMany(p => p.FragmentHistogramFile)
                    .ToList();
                uniqueFragmentRecords.WriteUniqueFragmentPlot(directoryPath, type);

                foreach (var amb in ambig)
                {
                    var fragNeededSummary = fragNeededResultsToUse
                        .Where(p => p.FragmentationType == type)
                        .Where(p => p.AmbiguityLevel == amb)
                        .ToList();

                    var precCompSummary = precCompResultsToUse
                        .Where(p => p.FragmentationType == type)
                        .Where(p => p.AmbiguityLevel == amb)
                        .ToList();

                    fragNeededSummary.WriteToleranceFragmentsNeededHistogram(directoryPath, type, amb, 0);
                    fragNeededSummary.WriteToleranceCumulativeLine(directoryPath, type, amb, 0);

                    fragNeededSummary.WriteMissedMonoFragmentsNeededHistogram(directoryPath, type, amb, 10);
                    fragNeededSummary.WriteMissedMonoCumulativeLine(directoryPath, type, amb, 10);

                    // only need one per ambiguity level
                    if (!createdPrecursorCompetitionPlots && type == "Tryptophan")
                    {
                        precCompSummary.WriteTolerancePrecursorCompetitionPlot(directoryPath, type, amb, 0);
                        precCompSummary.WriteMissedMonoPrecursorCompetitionPlot(directoryPath, type, amb, 10);
                        precCompSummary.WritePrecursorCompetitionPlot(directoryPath, type, amb, 10, 0);
                        if (amb == 2)
                            createdPrecursorCompetitionPlots = true;
                    }

                    foreach (var missedMono in missedMonos)
                    {
                        var innerPath = Path.Combine(directoryPath, $"{missedMono} Missed Mono");
                        var innerSummary = fragNeededSummary.Where(p => p.MissedMonoisotopics == missedMono)
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
                        var innerSummary = fragNeededSummary.Where(p => p.PpmTolerance == tolerance)
                            .ToList();

                        innerSummary.WriteFragmentsNeededHistogram(innerPath, type, amb, tolerance, 0);
                        innerSummary.WriteCumulativeFragmentsNeededLine(innerPath, type, amb, tolerance, 0, true);
                        innerSummary.WriteHybridFragmentNeeded(innerPath, type, amb, tolerance, 0);
                    }
                }
            }
        }



        [Test]
        public static void ReorderIndexFilesByPrecursorMass()
        {
            var indexFiles = Directory.GetFiles(DirectoryPath, "*_FragmentIndexFile.csv", SearchOption.AllDirectories)
                .OrderBy(p => p)
                .ToList();
            var newDirectoryPath = Path.Combine(DirectoryPath, "IndexFiles2");
            if (!Directory.Exists(newDirectoryPath))
                Directory.CreateDirectory(newDirectoryPath);

            foreach (var filePath in indexFiles)
            {
                // Write the ordered results to a new file in the new directory
                string dir = Path.GetDirectoryName(filePath)!; 
                string type = dir.Split(Path.DirectorySeparatorChar).Last();

                var newFilePath = Path.Combine(newDirectoryPath, type, Path.GetFileName(filePath));
                dir = Path.GetDirectoryName(newFilePath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                else if (File.Exists(newFilePath))
                    continue;


                var precursorFragmentMassFile = new PrecursorFragmentMassFile(filePath);

                // Load the data
                precursorFragmentMassFile.LoadResults();

                // Order by PrecursorMass
                var orderedResults = precursorFragmentMassFile.Results.OrderBy(p => p.PrecursorMass).ToList();

                precursorFragmentMassFile.Results = orderedResults;
                precursorFragmentMassFile.WriteResults(newFilePath);

                precursorFragmentMassFile.Dispose();
                Console.WriteLine($"Reordered and wrote file: {newFilePath}");
            }
        }
    }
}
