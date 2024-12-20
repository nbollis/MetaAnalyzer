using RadicalFragmentation.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadicalFragmentation
{
    public static class Extensions
    {
        public static IEnumerable<FragmentsNeededSummary> ToFragmentsNeededSummaryRecords(this RadicalFragmentationExplorer explorer)
        {
            var records = explorer.MinFragmentNeededFile.Results;
            var first = records.First();
            var type = first.AnalysisType;
            var species = first.Species;
            var ambiguityLevel = first.AmbiguityLevel;
            var missedMonos = explorer.MissedMonoIsotopics;
            var ppmTolerance = explorer.Tolerance;
            var modCount = explorer.NumberOfMods;

            var groupedRecords = records.GroupBy(r => r.FragmentCountNeededToDifferentiate)
                .ToDictionary(g => g.Key, g => g.Count());

            var minFragmentsNeeded = groupedRecords.Keys.Min();
            var maxFragmentsNeeded = groupedRecords.Keys.Max();

            var summary = Enumerable.Range(minFragmentsNeeded, maxFragmentsNeeded - minFragmentsNeeded + 1)
                .Select(fragmentsNeeded => new FragmentsNeededSummary
                {
                    FragmentationType = type,
                    Species = species,
                    AmbiguityLevel = ambiguityLevel,
                    MissedMonoisotopics = missedMonos,
                    NumberOfMods = modCount,
                    PpmTolerance = ppmTolerance,
                    Count = groupedRecords.GetValueOrDefault(fragmentsNeeded, 0),
                    FragmentsNeeded = fragmentsNeeded
                })
                .OrderBy(s => s.FragmentsNeeded)
                .ToList();

            return summary;
        }

        public static IEnumerable<PrecursorCompetitionSummary> ToPrecursorCompetitionSummaryRecords(this RadicalFragmentationExplorer explorer)
        {
            var records = explorer.MinFragmentNeededFile.Results;

            var first = records.First();
            var type = first.AnalysisType;
            var species = first.Species;
            var ambiguityLevel = first.AmbiguityLevel;
            var missedMonos = explorer.MissedMonoIsotopics;
            var ppmTolerance = explorer.Tolerance;
            var modCount = explorer.NumberOfMods;

            var groupedRecords = records.GroupBy(r => r.NumberInPrecursorGroup)
                .ToDictionary(g => g.Key, g => g.Count());

            var minPrecursorsInGroup = groupedRecords.Keys.Min();
            var maxPrecursorsInGroup = groupedRecords.Keys.Max();

            var summary = Enumerable.Range(minPrecursorsInGroup, maxPrecursorsInGroup - minPrecursorsInGroup + 1)
                .Select(precursorsInGroup => new PrecursorCompetitionSummary
                {
                    FragmentationType = type,
                    Species = species,
                    AmbiguityLevel = ambiguityLevel,
                    MissedMonoisotopics = missedMonos,
                    NumberOfMods = modCount,
                    PpmTolerance = ppmTolerance,
                    Count = groupedRecords.GetValueOrDefault(precursorsInGroup, 0),
                    PrecursorsInGroup = precursorsInGroup
                })
                .OrderBy(s => s.PrecursorsInGroup)
                .ToList();
            return summary;
        }
    }
}
