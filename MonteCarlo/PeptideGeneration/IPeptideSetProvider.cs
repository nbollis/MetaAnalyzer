using Omics;
using Omics.Digestion;
using Omics.Modifications;
using UsefulProteomicsDatabases;

namespace MonteCarlo;

public interface IPeptideSetProvider
{
    int Count { get; }
    int PeptidesPerIteration { get; protected set; }
    IEnumerable<IBioPolymerWithSetMods> GetPeptides();

    void ConfigurePeptidesPerIteration(int totalIterations, int maxPeptidesPerIteration)
    {
        // Maximize peptides per iteration while ensuring we don't exceed the total iterations
        PeptidesPerIteration = Math.Min(Math.Max(1, Count / totalIterations), maxPeptidesPerIteration);
    }
}

public enum PeptideSetProviderType
{
    BottomUpFromDatabase,
    TopDownFromDatabase,
}

public static class PeptideSetFactory
{
    public static IPeptideSetProvider GetPeptideSetProvider(PeptideSetProviderType provider, string databasePath, int maxToReturn, DecoyType decoyType,
       IDigestionParams? customDigestionParams = null, List<Modification>? variableMods = null, List<Modification>? fixedMods = null)
    {
        return provider switch
        {
            PeptideSetProviderType.BottomUpFromDatabase => new PeptideFromDatabaseSetProvider(databasePath, maxToReturn, decoyType, customDigestionParams, variableMods, fixedMods ),
            PeptideSetProviderType.TopDownFromDatabase => new ProteinFromDatabaseSetProvider(databasePath, maxToReturn, decoyType, variableMods, fixedMods),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };
    }
}
