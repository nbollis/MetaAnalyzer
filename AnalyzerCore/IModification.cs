using Omics.Modifications;
using ResultAnalyzerUtil;
using System.Collections.Concurrent;

namespace AnalyzerCore
{
    public interface IModification
    {
        string Name { get; }
        char ModifiedResidue { get; }
        int NominalMass { get; }
    }

    public interface ILocalizedModification : IModification, IEquatable<ILocalizedModification>
    {
        int OneBasedLocalization { get; }

        public static int GetNominalMass(string modName, char modifiedResidue)
        {
            var mass = modName switch
            {
                var name when name.Contains("Carbamidomethyl") => 57,
                var name when name.Contains("Oxidation") => 16,
                var name when name.Contains("Phospho") => 80,
                var name when name.Contains("Acetyl") => 42,
                var name when name.Contains("Methyl") => 14,
                _ => (int)Math.Round(GetClosestMod(modName, modifiedResidue, GlobalVariables.AllModsKnown).MonoisotopicMass!.Value, 0, MidpointRounding.AwayFromZero)!
            };
            return mass;
        }

        private static readonly ConcurrentDictionary<(string, char), Modification> _modificationCache = new();

        public static Modification GetClosestMod(int uniModId, char modifiedResidue, IList<Modification> allKnownMods)
        {
            var cacheKey = ($"{uniModId}-{modifiedResidue}", modifiedResidue);
            if (_modificationCache.TryGetValue(cacheKey, out var cachedModification))
            {
                return cachedModification;
            }

            var unimodMods = allKnownMods.Where(p => p.ModificationType.Contains("Unimod", StringComparison.InvariantCultureIgnoreCase)).ToList();

            var idMatching = unimodMods.Where(p => p.DatabaseReference.First().Value.First() == $"{uniModId}").ToHashSet();
            var motifMatching = unimodMods.Where(p => p.IdWithMotif.Contains($" on {modifiedResidue}") || p.IdWithMotif.Contains(" on X")).ToHashSet();

            if (idMatching.Count == 1)
            {
                cachedModification = idMatching.First();
            }
            else if (idMatching.Count > 1)
            {
                var intersect = idMatching.Intersect(motifMatching).ToList();
                if (intersect.Count == 1)
                {
                    cachedModification = intersect.First();
                }
                else if (intersect.Count > 1)
                {
                    var exactMatch = intersect.FirstOrDefault(p => p.IdWithMotif.Contains($" on {modifiedResidue}"));
                    if (exactMatch is not null)
                        cachedModification = exactMatch;
                    else
                    {
                        var ambiguousMatch = intersect.FirstOrDefault(p => p.IdWithMotif.Contains(" on X"));
                        if (ambiguousMatch is not null)
                            cachedModification = ambiguousMatch;
                    }
                }
            }

            _modificationCache[cacheKey] = cachedModification;
            return cachedModification;
        }


        public static Modification GetClosestMod(string name, char modifiedResidue, IList<Modification> allKnownMods)
        {
            var cacheKey = (name, modifiedResidue);
            if (_modificationCache.TryGetValue(cacheKey, out var cachedModification))
            {
                return cachedModification;
            }

            var trimmedName = name.Split(new[] { "-L-" }, StringSplitOptions.None)[0];
            var matching = allKnownMods.Where(p =>
                    p.IdWithMotif.Contains(trimmedName)
                    && (p.IdWithMotif.Contains($" on {modifiedResidue}") || p.IdWithMotif.Contains(" on X")))
                .ToList();

            switch (matching.Count)
            {
                // if exact match by name with no ambiguity, return it
                case 1:
                    cachedModification = matching[0];
                    break;
                // if none matched by name alone
                case < 1:
                {
                    var motifMatching = allKnownMods.Where(p =>
                        p.IdWithMotif.Contains($" on {modifiedResidue}") || p.IdWithMotif.Contains(" on X"));
                    matching.AddRange(motifMatching);
                    break;
                }
            }

            // if multiple match by name and motif, but all have the same name, return the one with the correct motif 
            if (matching.Count > 1 && matching.DistinctBy(p => p.OriginalId).Count() == 1) 
            {
                var exactMatch = matching.FirstOrDefault(p => p.IdWithMotif.Contains($" on {modifiedResidue}"));
                if (exactMatch is not null)
                    cachedModification = exactMatch;
                else
                {
                    var ambiguousMatch = matching.FirstOrDefault(p => p.IdWithMotif.Contains(" on X"));
                    if (ambiguousMatch is not null)
                        cachedModification = ambiguousMatch;
                }
            }

            // if nothing above worked, Calculate overlap score by substring overlap and select the modification with the highest score
            if (cachedModification is null)
            {
                // Calculate overlap score and select the modification with the highest score
                cachedModification = matching.MaxBy(mod => GetOverlapScore(mod.IdWithMotif, trimmedName))!;
            }

            _modificationCache[cacheKey] = cachedModification;
            return cachedModification;
        }

        /// <summary>
        /// Calculates the overlap score between the modification ID with motif and the trimmed name.
        /// The score represents the length of the longest common substring between the two strings.
        /// </summary>
        /// <param name="idWithMotif">The modification ID with motif.</param>
        /// <param name="trimmedName">The trimmed name of the modification.</param>
        /// <returns>The overlap score, which is the length of the longest common substring.</returns>
        private static int GetOverlapScore(string idWithMotif, string trimmedName)
        {
            int overlapScore = 0;
            for (int i = 0; i < idWithMotif.Length; i++)
            {
                for (int j = 0; j < trimmedName.Length; j++)
                {
                    int k = 0;
                    while (i + k < idWithMotif.Length && j + k < trimmedName.Length && idWithMotif[i + k] == trimmedName[j + k])
                    {
                        k++;
                    }
                    overlapScore = Math.Max(overlapScore, k);
                }
            }
            return overlapScore;
        }
    }

    public static class ModificationExtensions
    {
        public static Modification GetClosestMod(this ILocalizedModification modToMatch, IList<Modification> allKnownMods)
        {
            return ILocalizedModification.GetClosestMod(modToMatch.Name, modToMatch.ModifiedResidue, allKnownMods);
        }

        public static string GetMetaMorpheusFullSequenceString(this ILocalizedModification mod, IList<Modification> allKnownMods)
        {
            var mmMod = mod.GetClosestMod(allKnownMods);

            string category = mmMod.ModificationType switch
            {
                "Unimod" when mmMod.OriginalId.Contains("Carbamido") => "Common Fixed",
                "Unimod" when mmMod.OriginalId.Contains("Oxidation") => "Common Variable",
                "Unimod" when mmMod.OriginalId.Contains("Phosphoryl") => "Common Biological",
                "Unimod" when mmMod.OriginalId.Contains("Acetyl") => "Common Biological",
                "Unimod" when mmMod.OriginalId.Contains("Methy") => "Common Biological",
                _ => mmMod.ModificationType
            };

            return $"[{category}:{mmMod.OriginalId} on {mmMod.Target}]";
        }

        public static string GetMetaMorpheusFullSequenceString(this Modification mmMod, IList<Modification> allKnownMods)
        {
            string category = mmMod.ModificationType switch
            {
                "Unimod" when mmMod.OriginalId.Contains("Carbamido") => "Common Fixed",
                "Unimod" when mmMod.OriginalId.Contains("Oxidation") => "Common Variable",
                "Unimod" when mmMod.OriginalId.Contains("Phospho") => "Common Biological",
                "Unimod" when mmMod.OriginalId.Contains("Acetyl") => "Common Biological",
                "Unimod" when mmMod.OriginalId.Contains("Methy") => "Common Biological",
                _ => mmMod.ModificationType
            };

            return $"[{category}:{mmMod.OriginalId} on {mmMod.Target}]";
        }
    }
}
