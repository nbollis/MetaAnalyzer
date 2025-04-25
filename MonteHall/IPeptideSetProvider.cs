using Omics;
using Omics.Digestion;
using Omics.Modifications;
using Proteomics.ProteolyticDigestion;
using ResultAnalyzerUtil;
using UsefulProteomicsDatabases;

namespace MonteCarlo;

public interface IPeptideSetProvider
{
    IEnumerable<IBioPolymerWithSetMods> GetPeptides();
}

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

public class ProteinFromDatabaseSetProvider : PeptideFromDatabaseSetProvider
{
    public ProteinFromDatabaseSetProvider(string databaseFilePath, int maxToReturn, DecoyType decoyType,
        List<Modification>? fixedMods = null, List<Modification>? variableMods = null)
        : base(databaseFilePath, maxToReturn, decoyType, new DigestionParams("top-down"), fixedMods, variableMods)
    {
 
    }
}
