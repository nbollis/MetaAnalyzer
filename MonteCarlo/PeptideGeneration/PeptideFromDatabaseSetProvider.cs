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

        // Initialize the queue and then scramble its order
        Random rand = new Random();
        ScrambledBioPolymersList = new();
        foreach (var bioPolymer in GetAllPeptides().OrderBy(_ => rand.Next()))
        {
            ScrambledBioPolymersList.Add(bioPolymer);
        }
    }

    public override IEnumerable<IBioPolymerWithSetMods> GetAllPeptides()
    {
        foreach (var bioPolymer in GetBioPolymers())
        {
            var peptideList = bioPolymer.Digest(DigestionParams, FixedMods, VariableMods);
            foreach (var peptide in peptideList)
            {
                yield return peptide;
            }
        }
    }
}
