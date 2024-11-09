using GradientDevelopment.Temporary;

namespace GradientDevelopment
{
    public static class ResultFileConsensus
    {
        public static Dictionary<string, List<ResultFileConsensusRecord>> ParsedConsensusDictionary { get; private set; }

        static ResultFileConsensus()
        {
            ParsedConsensusDictionary = new Dictionary<string, List<ResultFileConsensusRecord>>();
        }

        public static List<ResultFileConsensusRecord> GetConsensusIds(string osmPath)
        {
            if (ParsedConsensusDictionary.TryGetValue(osmPath, out var consensusRecords))
                return consensusRecords;

            // Open file and set up
            var spectralMatches = SpectrumMatchTsvReader.ReadOsmTsv(osmPath, out var warnings);
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
                .OrderBy(p => p.MinQValue)
                .ThenByDescending(p => p.FileCount)
                .ToList();

            // Create consensus records
            var consensusRecordsList = lowestQValueIds.Select(p => new ResultFileConsensusRecord
            {
                FullSequence = p.FullSequence,
                MinQValue = p.MinQValue,
                FileCount = p.FileCount,
                MaxFileCount = distinctFileNames.Length,
                AverageRT = p.OSMs.WeightedAverage(z => z.RetentionTime!.Value, z => 1 - z.QValue),
                SpectralMatchesByFileName = p.OSMs.Cast<SpectrumMatchFromTsv>().GroupBy(p => p.FileNameWithoutExtension)
                    .ToDictionary(q => q.Key, q => q.OrderBy(n => n.RetentionTime).ToList())
            }).ToList();

            // Add to dictionary
            ParsedConsensusDictionary[osmPath] = consensusRecordsList;

            return consensusRecordsList;
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
