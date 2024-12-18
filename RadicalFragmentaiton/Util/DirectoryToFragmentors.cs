using RadicalFragmentation.Processing;
using System.Diagnostics;

namespace RadicalFragmentation
{
    public static class DirectoryToFragmentExplorers
    {
        public static IEnumerable<RadicalFragmentationExplorer> GetFragmentExplorersFromDirectory(string databasePath, string directoryPath)
        {
            foreach (var potentialResultDir in Directory.GetDirectories(directoryPath))
            {
                if (potentialResultDir.Contains("IndexedFragments") || potentialResultDir.Contains("Figure") || Path.GetFileNameWithoutExtension(potentialResultDir).StartsWith('_'))
                    continue;

                var explorerType = ParseTypeFromDirName(potentialResultDir);
                var missedMonoCount = potentialResultDir.Contains("MissedMono")
                    ? int.Parse(Path.GetFileNameWithoutExtension(potentialResultDir).Split('_')[1].Replace("MissedMonos", ""))
                    : 0;

                var files = Directory.GetFiles(potentialResultDir);
                var grouped = files
                    .Where(p => !p.Contains("_0.csv") || !p.Contains("temp"))
                    .GroupBy(p => string.Join('_', Path.GetFileNameWithoutExtension(p).Split('_')[..4]))
                    .Where(p => p.Count() == 2)
                    .ToList();

                foreach (var pairedResults in grouped)
                {
                    var info = ParseFileName(pairedResults.First());
                    switch (explorerType)
                    {
                        case FragmentExplorerType.Tryptophan:
                            yield return new TryptophanFragmentationExplorer(databasePath, info.Mods, info.Label, info.AmbigLevel, directoryPath, missedMonoCount);
                            break;
                        case FragmentExplorerType.Cysteine:
                            yield return new CysteineFragmentationExplorer(databasePath, info.Mods, info.Label, info.AmbigLevel, int.MaxValue, directoryPath, missedMonoCount);
                            break;
                        case FragmentExplorerType.ETD:
                            yield return new EtdFragmentationExplorer(databasePath, info.Mods, info.Label, info.AmbigLevel, directoryPath, missedMonoCount);
                            break;
                        default:
                            Debugger.Break();
                            break;
                    }
                }
            }
        }

        private static RadFragInfo ParseFileName(string fileName)
        {
            var splits = Path.GetFileNameWithoutExtension(fileName).Split('_');
            var label = splits[0];
            var mods = int.Parse(splits[1].Replace("Mods", ""));
            var ambigLevel = int.Parse(splits[3].Replace("Level(", "").Replace(")Ambiguity", ""));
            return new RadFragInfo { Label = label, Mods = mods, AmbigLevel = ambigLevel };
        }

        private static FragmentExplorerType ParseTypeFromDirName(string directoryPath)
        {
           if (directoryPath.Contains(FragmentExplorerType.ETD.ToString()))
                return FragmentExplorerType.ETD;
           else if (directoryPath.Contains(FragmentExplorerType.Cysteine.ToString()))
                return FragmentExplorerType.Cysteine;
           else if (directoryPath.Contains(FragmentExplorerType.Tryptophan.ToString()))
                return FragmentExplorerType.Tryptophan;
           else
                throw new ArgumentException();
        }

    }

    internal class RadFragInfo
    {
        internal int Mods;
        internal string Label;
        internal int AmbigLevel;
    }
}
