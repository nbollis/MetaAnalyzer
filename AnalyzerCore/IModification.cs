using Omics.Modifications;
using System.Text;
using System.Xml.Linq;

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

        public static int GetNominalMass(string modName)
        {
            var mass = modName switch
            {
                var name when name.Contains("Carbamidomethyl") => 57,
                var name when name.Contains("Oxidation") => 16,
                var name when name.Contains("Phospho") => 80,
                var name when name.Contains("Acetyl") => 42,
                var name when name.Contains("Methyl") => 14,
                _ => throw new ArgumentOutOfRangeException()
            };
            return mass;
        }
    }

    public static class ModificationExtensions
    {
        public static string GetMetaMorpheusFullSequenceString(this ILocalizedModification mod, IList<Modification> allKnownMods)
        {
            var nameMatching = allKnownMods.Where(p =>
                        p.IdWithMotif.Contains(mod.Name.Replace("-L-lysine", ""))
                        && (p.IdWithMotif.Contains($" on {mod.ModifiedResidue}") || p.IdWithMotif.Contains(" on X"))
                    /*&& !p.OriginalId.Contains("DTT")*/)
                .ToArray();

            Modification modToReturn;
            if (nameMatching.Length != 1) // if multiple match by name, go with lowest uniprot reference number
            {
                int lowestRef = int.MaxValue;
                Modification? lowestReference = null;
                foreach (var modification in nameMatching)
                {
                    if (modification.DatabaseReference.TryGetValue("Unimod", out var values))
                    {
                        foreach (var value in values)
                        {
                            if (int.TryParse(value, out var result) && result < lowestRef)
                            {
                                lowestRef = result;
                                lowestReference = modification;
                            }
                        }
                    }
                }

                if (lowestReference is null)
                    modToReturn = nameMatching.MinBy(p => p.IdWithMotif.Length)!;
                else
                    modToReturn = lowestReference;
            }
            else
                modToReturn = nameMatching[0];

            string category = modToReturn.ModificationType switch
            {
                "Unimod" when modToReturn.OriginalId.Contains("Carbamido") => "Common Fixed",
                "Unimod" when modToReturn.OriginalId.Contains("Oxidation") => "Common Variable",
                "Unimod" when modToReturn.OriginalId.Contains("Phosphoryl") => "Common Biological",
                "Unimod" when modToReturn.OriginalId.Contains("Acetyl") => "Common Biological",
                "Unimod" when modToReturn.OriginalId.Contains("Methy") => "Common Biological",
                _ => modToReturn.ModificationType
            };

            return $"[{category}:{modToReturn.OriginalId} on {modToReturn.Target}]";
        }
    }
}
