using Omics;
using Proteomics.ProteolyticDigestion;
using ResultAnalyzerUtil;
using UsefulProteomicsDatabases;

namespace MonteCarlo;

public abstract class DatabaseSetProvider : IPeptideSetProvider
{
    public int Count => ScrambledBioPolymersQueue.Count;
    public int PeptidesPerIteration { get; set; }
    protected readonly string DatabaseFilePath;
    protected readonly DecoyType DecoyType;
    protected Queue<IBioPolymerWithSetMods> ScrambledBioPolymersQueue;

    protected DatabaseSetProvider(string databaseFilePath, int peptidesPerIteration, DecoyType decoyType)
    {
        DatabaseFilePath = databaseFilePath;
        PeptidesPerIteration = peptidesPerIteration;
        DecoyType = decoyType;
        ScrambledBioPolymersQueue = new();
    }

    public abstract IEnumerable<IBioPolymerWithSetMods> GetAllPeptides();

    public IEnumerable<IBioPolymerWithSetMods> GetPeptides()
    {
        int count = PeptidesPerIteration;
        while (ScrambledBioPolymersQueue.Count > 0 && count > 0)
        {
            yield return ScrambledBioPolymersQueue.Dequeue();
            count--;
        }
    }

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
