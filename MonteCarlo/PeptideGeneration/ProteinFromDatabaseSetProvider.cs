using Omics.Modifications;
using Proteomics.ProteolyticDigestion;
using UsefulProteomicsDatabases;

namespace MonteCarlo;

public class ProteinFromDatabaseSetProvider : PeptideFromDatabaseSetProvider
{
    public ProteinFromDatabaseSetProvider(string databaseFilePath, int maxToReturn, DecoyType decoyType,
        List<Modification>? fixedMods = null, List<Modification>? variableMods = null)
        : base(databaseFilePath, maxToReturn, decoyType, new DigestionParams("top-down"), fixedMods, variableMods)
    {
 
    }
}
