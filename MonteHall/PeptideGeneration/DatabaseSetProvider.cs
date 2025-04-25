using Omics;
using ResultAnalyzerUtil;
using UsefulProteomicsDatabases;

namespace MonteCarlo;

public abstract class DatabaseSetProvider : IPeptideSetProvider
{
    protected readonly string DatabaseFilePath;
    protected readonly int MaxToReturn;
    protected readonly DecoyType DecoyType;

    protected DatabaseSetProvider(string databaseFilePath, int maxToReturn, DecoyType decoyType)
    {
        DatabaseFilePath = databaseFilePath;
        MaxToReturn = maxToReturn;
        DecoyType = decoyType;
    }

    public abstract IEnumerable<IBioPolymerWithSetMods> GetPeptides();

    public IEnumerable<IBioPolymer> GetBioPolymers()
    {
        bool generateTargets = DecoyType == DecoyType.None;
        if (DatabaseFilePath.EndsWith(".xml"))
        {
            return ProteinDbLoader.LoadProteinXML(DatabaseFilePath, generateTargets, DecoyType, GlobalVariables.AllModsKnown, false, [], out _);
        }
        else if (DatabaseFilePath.EndsWith(".fasta"))
        {
            return ProteinDbLoader.LoadProteinFasta(DatabaseFilePath, generateTargets, DecoyType, false, out _);
        }
        else
        {
            throw new ArgumentException("Unsupported database file format.");
        }
    }

}
