using Omics;
using ResultAnalyzerUtil;
using UsefulProteomicsDatabases;

namespace MonteCarlo;

public abstract class DatabaseSetProvider : IPeptideSetProvider
{
    public int Count => ScrambledBioPolymersList.Count;
    public int PeptidesPerIteration { get; set; }
    protected readonly string DatabaseFilePath;
    protected readonly DecoyType DecoyType;
    protected CircularLinkedList<IBioPolymerWithSetMods> ScrambledBioPolymersList;

    protected DatabaseSetProvider(string databaseFilePath, int peptidesPerIteration, DecoyType decoyType)
    {
        DatabaseFilePath = databaseFilePath;
        PeptidesPerIteration = peptidesPerIteration;
        DecoyType = decoyType;
        ScrambledBioPolymersList = new CircularLinkedList<IBioPolymerWithSetMods>();
    }

    public abstract IEnumerable<IBioPolymerWithSetMods> GetAllPeptides();

    public IEnumerable<IBioPolymerWithSetMods> GetPeptides()
    {
        int count = PeptidesPerIteration;
        while (count > 0)
        {
            yield return ScrambledBioPolymersList.GetNext();
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

public class CircularLinkedList<T>
{
    private LinkedList<T> _list = new();
    private LinkedListNode<T>? _current;

    public void Add(T item)
    {
        _list.AddLast(item);
    }

    public T GetNext()
    {
        if (_list.Count == 0)
        {
            throw new InvalidOperationException("The list is empty.");
        }

        _current = _current?.Next ?? _list.First;
        return _current!.Value;
    }

    public int Count => _list.Count;
}
