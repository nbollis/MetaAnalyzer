using Omics;
using Omics.Digestion;
using Omics.Modifications;
using Proteomics.ProteolyticDigestion;
using UsefulProteomicsDatabases;

namespace MonteCarlo;

public class PeptideFromDatabaseSetProvider : DatabaseSetProvider
{
    protected readonly IDigestionParams DigestionParams;
    protected readonly List<Modification> VariableMods;
    protected readonly List<Modification> FixedMods;

    public PeptideFromDatabaseSetProvider(string databaseFilePath, int maxToReturn, DecoyType decoyType,
        IDigestionParams? digestionParams = null, List<Modification>? fixedMods = null, List<Modification>? variableMods = null)
        : base(databaseFilePath, maxToReturn, decoyType)
    {
        DigestionParams = digestionParams ?? new DigestionParams();
        FixedMods = fixedMods ?? [];
        VariableMods = variableMods ?? [];
    }

    public override IEnumerable<IBioPolymerWithSetMods> GetPeptides()
    {
        foreach (var bioPolymer in GetBioPolymers())
        {
            foreach (var withSetMods in bioPolymer.Digest(DigestionParams, VariableMods, FixedMods))
            {
                yield return withSetMods;
            }
        }
    }
}
