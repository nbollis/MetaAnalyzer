using CsvHelper.Configuration.Attributes;
using CsvHelper.Configuration;
using Readers;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using CsvHelper;
using MzLibUtil;
using ResultAnalyzerUtil.CsvConverters;
using System.IO.MemoryMappedFiles;

namespace RadicalFragmentation;

public class PrecursorFragmentMassSet : IEquatable<PrecursorFragmentMassSet>
{
    public static CsvConfiguration CsvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        MissingFieldFound = null,
        Delimiter = ",",
        HasHeaderRecord = true,
        IgnoreBlankLines = true,
        TrimOptions = TrimOptions.Trim,
        BadDataFound = null
    };

    [Name("Accession")]
    public string Accession { get; set; }

    [Name("Full Sequence")]
    public string FullSequence { get; set; }
    [Name("PrecursorMass")]
    public double PrecursorMass { get; set; }

    [Name("FragmentMasses")]
    [TypeConverter(typeof(SemiColonDelimitedToDoubleSortedListConverter))]
    public List<double> FragmentMasses { get; set; }

    [Name("FragmentCount")]
    public int FragmentCount { get; set; }

    [Optional] public int? CysteineCount { get; set; }
    [NotMapped] private HashSet<double> _fragmentMassesHashSet;
    [NotMapped] public HashSet<double> FragmentMassesHashSet => _fragmentMassesHashSet ??= new HashSet<double>(FragmentMasses);

    public PrecursorFragmentMassSet(double precursorMass, string accession, List<double> fragmentMasses, string fullSequence)
    {
        PrecursorMass = precursorMass;
        Accession = accession;
        FragmentMasses = fragmentMasses.OrderBy(p => p).ToList();
        FragmentCount = fragmentMasses.Count;
        FullSequence = fullSequence;
    }

    public PrecursorFragmentMassSet()
    {
    }

    public bool Equals(PrecursorFragmentMassSet? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        var toReturn = FullSequence.Equals(other.FullSequence)
                       && Accession == other.Accession
                       && FragmentMasses.SequenceEqual(other.FragmentMasses)
                       && FragmentCount == other.FragmentCount;
        return toReturn;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((PrecursorFragmentMassSet)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PrecursorMass, Accession, FragmentMasses, FragmentCount);
    }
}


public class PrecursorFragmentMassFile
    : ResultFile<PrecursorFragmentMassSet>, IMemoryMapped<PrecursorFragmentMassSet>
{
    private MemoryMappedFile? _memoryMappedFile;
    private MemoryMappedViewAccessor? _accessor;
    public int Count { get; private set; }

    public PrecursorFragmentMassFile(string filePath) : base(filePath)
    {
        InitializeMemoryMappedFile(filePath);
    }

    public PrecursorFragmentMassFile() : base()
    {
        // Ensure non-nullable fields are initialized
        _memoryMappedFile = null;
        _accessor = null;
    }

    private void InitializeMemoryMappedFile(string filePath)
    {
        // Create or open the memory-mapped file
        _memoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, "PrecursorFragmentMassFile");
        _accessor = _memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

        // Calculate total file size
        Count = (int)new FileInfo(filePath).Length;
    }


    public IEnumerable<PrecursorFragmentMassSet> ReadChunks(long offset, int chunkSize)
    {
        if (_accessor == null)
        {
            throw new InvalidOperationException("MemoryMappedViewAccessor is not initialized.");
        }

        long size = Math.Min(chunkSize, Count - offset);
        using var stream = new UnmanagedMemoryStream(_accessor.SafeMemoryMappedViewHandle, offset, size, FileAccess.Read);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, PrecursorFragmentMassSet.CsvConfiguration);

        foreach (var record in csv.GetRecords<PrecursorFragmentMassSet>())
        {
            yield return record;
        }
    }


    public IEnumerable<PrecursorFragmentMassSet> ReadRange(long startOffset, double minMass, double maxMass, Tolerance tolerance)
    {
        if (_accessor == null)
        {
            throw new InvalidOperationException("MemoryMappedViewAccessor is not initialized.");
        }

        using var stream = new UnmanagedMemoryStream(_accessor.SafeMemoryMappedViewHandle, startOffset, Count - startOffset, FileAccess.Read);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, PrecursorFragmentMassSet.CsvConfiguration);

        foreach (var record in csv.GetRecords<PrecursorFragmentMassSet>())
        {
            if (tolerance.Within(record.PrecursorMass, minMass) || tolerance.Within(record.PrecursorMass, maxMass))
            {
                yield return record;
            }
            else if (record.PrecursorMass > maxMass)
            {
                break; // Stop reading once beyond the range
            }
        }
    }


    // TODO: Fix to allow static reading from memory mapped file
    public override void LoadResults()
    {
        //if (_accessor == null)
        //{
        //    throw new InvalidOperationException("MemoryMappedViewAccessor is not initialized.");
        //}

        // Read data from the memory-mapped file
        // using var stream = new UnmanagedMemoryStream(_accessor.SafeMemoryMappedViewHandle, 0, _accessor.Capacity+1, FileAccess.Read);
        //using var reader = new StreamReader(stream);
        using var reader = new StreamReader(FilePath);
        using var csv = new CsvReader(reader, PrecursorFragmentMassSet.CsvConfiguration);
        Results = csv.GetRecords<PrecursorFragmentMassSet>().ToList();
    }

    public override void WriteResults(string outputPath)
    { 
        var csv = new CsvWriter(new StreamWriter(outputPath), PrecursorFragmentMassSet.CsvConfiguration);

        csv.WriteHeader<PrecursorFragmentMassSet>();
        foreach (var result in Results)
        {
            csv.NextRecord();
            csv.WriteRecord(result);
        }

        csv.Dispose();
    }

    public override SupportedFileType FileType { get; }
    public override Software Software { get; set; }

    // Dispose the memory-mapped file and accessor
    public void Dispose()
    {
        _accessor?.Dispose();
        _memoryMappedFile?.Dispose();
    }
}