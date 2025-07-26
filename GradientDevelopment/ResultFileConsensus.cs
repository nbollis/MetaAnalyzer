using Readers;

namespace GradientDevelopment
{
    public static class ResultFileConsensus
    {
        public static Dictionary<string, List<ResultFileConsensusRecord>> ParsedConsensusDictionary { get; private set; }

        static ResultFileConsensus()
        {
            ParsedConsensusDictionary = new Dictionary<string, List<ResultFileConsensusRecord>>();
        }

        /// <summary>
        /// Gets the consensus IDs from the provided spectral matches.
        /// </summary>
        /// <param name="spectralMatches">The list of spectral matches.</param>
        /// <param name="toReturn">The number of consensus records to return.</param>
        /// <returns>A list of consensus records.</returns>
        public static List<ResultFileConsensusRecord> GetConsensusIds(List<OsmFromTsv> spectralMatches, int toReturn)
        {
            List<ResultFileConsensusRecord> records = new();
            foreach (var fileNameGroup in spectralMatches.GroupBy(p => p.FileNameWithoutExtension))
            {
                var fileName = fileNameGroup.Key;
                if (ParsedConsensusDictionary.TryGetValue(fileName, out var consensusRecords))
                {
                    records.AddRange(consensusRecords);
                    continue;
                }

                // Open file and set up
                var distinctFileNames = spectralMatches.Select(p => p.FileNameWithoutExtension)
                    .Distinct()
                    .ToArray();

                // Find the Id's with the lowest q values represented in at least 80% portion of the file names
                var lowestQValueIds = spectralMatches
                    .GroupBy(p => p.FullSequence)
                    .Select(g => new
                    {
                        FullSequence = g.Key,
                        MinQValue = g.Min(p => p.QValue),
                        FileCount = g.Select(p => p.FileNameWithoutExtension).Distinct().Count(),
                        OSMs = g.ToList()
                    })
                    .Where(p => p.FileCount >= distinctFileNames.Length * 0.8)
                    .OrderByDescending(p => p.FileCount)
                    .ThenBy(p => p.OSMs.Average(m => m.QValue))
                    .Take(toReturn)
                    .ToList();

                // Create consensus records
                var consensusRecordsList = lowestQValueIds.Select(p => new ResultFileConsensusRecord
                {
                    FullSequence = p.FullSequence,
                    MinQValue = p.MinQValue,
                    FileCount = p.FileCount,
                    MaxFileCount = distinctFileNames.Length,
                    AverageRT = p.OSMs.WeightedAverage(z => z.RetentionTime, z => 1 - z.QValue),
                    SpectralMatchesByFileName = p.OSMs.Cast<SpectrumMatchFromTsv>().GroupBy(p => p.FileNameWithoutExtension)
                        .ToDictionary(q => q.Key, q => q.OrderBy(n => n.RetentionTime).ToList())
                }).ToList();

                // Add to dictionary
                records.AddRange(consensusRecordsList);
                ParsedConsensusDictionary[fileName] = consensusRecordsList;
            }

            return records;
        }

        public static double WeightedAverage(this IEnumerable<OsmFromTsv> source, Func<OsmFromTsv, double> valueSelector, Func<OsmFromTsv, double> weightSelector)
        {
            double weightedValueSum = source.Sum(x => valueSelector(x) * weightSelector(x));
            double weightSum = source.Sum(weightSelector);
            return weightedValueSum / weightSum;
        }
    }

    public class ResultFileConsensusRecord
    {
        public string FullSequence { get; set; }
        public double MinQValue { get; set; }
        public int FileCount { get; set; }
        public int MaxFileCount { get; set; }
        public double AverageRT { get; set; }
        public Dictionary<string, List<SpectrumMatchFromTsv>> SpectralMatchesByFileName { get; set; }
    }

    
    
}
