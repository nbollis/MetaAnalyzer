using Analyzer.FileTypes.Internal;
using Analyzer.SearchType;
using Proteomics;
using Omics.Modifications;
using ResultAnalyzerUtil;
using UsefulProteomicsDatabases;
using Analyzer.Util;
using CsvHelper.Configuration;
using System.Globalization;
using CsvHelper;
using Readers;

namespace TaskLayer;

public class GptmdSearchResult
{
    public string Condition { get; }
    public string ResultDirectory { get; set; }
    public MetaMorpheusResult SearchTaskResults { get; set; }
    public string GptmdDatabasePath { get; set; }
    private List<Protein>? _gptmdProteins;
    public List<Protein> GptmdProteins
    {
        get
        {
            if (_gptmdProteins == null && GptmdDatabasePath != null)
            {
                _gptmdProteins = ProteinDbLoader.LoadProteinXML(GptmdDatabasePath, true, DecoyType.None, GlobalVariables.AllModsKnown, false, [], out _)
                    .OrderBy(p => p.Accession).ToList();
            }
            return _gptmdProteins;
        }
    }

    public string OriginalDatabasePath { get; set; }
    private List<Protein>? _originalProteins;
    public List<Protein> OriginalProteins
    {
        get
        {
            if (_originalProteins == null && OriginalDatabasePath != null)
            {
                _originalProteins = OriginalDatabasePath.EndsWith(".xml")
                    ? ProteinDbLoader.LoadProteinXML(OriginalDatabasePath, true, DecoyType.None, GlobalVariables.AllModsKnown, false, [], out _)
                    : ProteinDbLoader.LoadProteinFasta(OriginalDatabasePath, true, DecoyType.None, false, out _)
                    .OrderBy(p => p.Accession).ToList();
            }
            return _originalProteins;
        }
    }
    public int ProteinsWithAddedMods { get; private set; }
    public int ProteinsWithAddedModsPsmCount { get; private set; }
    public int ProteinsWithAddedModsPsmContainingModCount { get; private set; }

    public int ModificationsAdded { get; private set; }
    public Dictionary<string, int> ModTypeCounts { get; private set; }
    public Dictionary<int, int> ModsAddedPerProtein { get; private set; }
    public GptmdSearchResult(string resultDirectory)
    {
        ModsAddedPerProtein = new Dictionary<int, int>();
        ResultDirectory = resultDirectory;

        var directoryName = System.IO.Path.GetFileName(resultDirectory);
        Condition = directoryName;
        SearchTaskResults = new(resultDirectory, directoryName, directoryName);

        var gptmdDir = Directory
            .GetDirectories(resultDirectory).FirstOrDefault(path => path.Contains("GPTMD"));

        GptmdDatabasePath = gptmdDir != null
            ? Directory.GetFiles(gptmdDir, "*.xml").FirstOrDefault()
            : null;

        var gptmdProsePath = gptmdDir != null
            ? Directory.GetFiles(gptmdDir, "*Prose.txt").FirstOrDefault()
            : null;

        var gptmdResultsTxtPath = gptmdDir != null
            ? Directory.GetFiles(gptmdDir, "results.txt").FirstOrDefault()
            : null;

        if (!string.IsNullOrEmpty(gptmdResultsTxtPath))
            ParseResultsTxt(gptmdResultsTxtPath);


        if (!string.IsNullOrEmpty(gptmdProsePath))
        {
            var prose = new MetaMorpheusProseFile(gptmdProsePath);
            OriginalDatabasePath = prose.DatabasePaths.FirstOrDefault();
        }
        else
        {
            OriginalDatabasePath = null;
        }
    }

    private void ParseResultsTxt(string resultsTxtPath)
    {
        if (!File.Exists(resultsTxtPath))
            return;

        var lines = File.ReadAllLines(resultsTxtPath);
        ModTypeCounts = new Dictionary<string, int>();
        bool inModSection = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("Modifications added:"))
            {
                if (int.TryParse(line.Split(':')[1].Trim(), out int modsAdded))
                    ModificationsAdded = modsAdded;
            }
            else if (line.StartsWith("Mods types and counts:"))
            {
                inModSection = true;
                continue;
            }
            else if (inModSection)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("-") || !line.Contains('\t'))
                {
                    inModSection = false;
                    continue;
                }
                var parts = line.Trim().Split('\t');
                if (parts.Length == 2 && int.TryParse(parts[1], out int count))
                {
                    ModTypeCounts[parts[0]] = count;
                }
            }
        }
    }

    /// <summary>
    /// Compares the modifications added by Gptmd to the original proteins.
    /// </summary>
    public void ParseModifications()
    {
        int gptmdIndex = 0;
        int gptmdCount = GptmdProteins.Count;
        ModsAddedPerProtein = new Dictionary<int, int>();
        var filteredPsms = SearchTaskResults.AllPsms.Where(p => p.PassesConfidenceFilter()).ToList();

        foreach (var protein in OriginalProteins)
        {
            // Advance gptmdIndex to the matching accession or until we pass it
            while (gptmdIndex < gptmdCount && string.Compare(GptmdProteins[gptmdIndex].Accession, protein.Accession, StringComparison.Ordinal) < 0)
            {
                gptmdIndex++;
            }

            var relevantPsms = filteredPsms
                .Where(p => p.ProteinAccession.Contains(protein.Accession))
                .ToList();

            ProteinComparison(protein, GptmdProteins[gptmdIndex], relevantPsms);
        }
    }

    public void ProteinComparison(Protein original, Protein gptmd, List<PsmFromTsv> relevantPsms)
    {
        if (original.Accession != gptmd.Accession)
            throw new ArgumentException("Proteins do not match by accession.");

        // flatten and retain residue information:
        var originalModsFlat = original.OneBasedPossibleLocalizedModifications
            .SelectMany(kvp => kvp.Value.Select(mod => new FlatMod(kvp.Key, mod)))
            .ToList();

        var gptmdModsFlat = gptmd.OneBasedPossibleLocalizedModifications
            .SelectMany(kvp => kvp.Value.Select(mod => new FlatMod(kvp.Key,mod)))
            .ToList();

        // Update histogram of mods added per protein
        int modsAdded = gptmdModsFlat.Count - originalModsFlat.Count;
        if (!ModsAddedPerProtein.TryAdd(modsAdded, 1))
            ModsAddedPerProtein[modsAdded]++;

        // Parse search results to see if the added mods made it to the psms
        var addedMods = gptmdModsFlat.Except(originalModsFlat)
            .ToList();
        var temp = originalModsFlat.Except(gptmdModsFlat)
            .ToList();

        if (addedMods.Count == 0)
            return;

        ProteinsWithAddedMods++;
        ProteinsWithAddedModsPsmCount += relevantPsms.Count;

        foreach (var relevantPsm in relevantPsms)
        {
            //var mods = relevantPsm.
            
        }
    }
}

public class FlatMod(int position, Modification mod) : IEquatable<FlatMod>
{
    public int Position { get; init; } = position;
    public Modification Mod { get; init; } = mod;
    public bool Equals(FlatMod? other)
    {
        if (other is null) return false;
        return Position == other.Position && Mod.IdWithMotif == other.Mod.IdWithMotif;
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(Position, Mod.IdWithMotif);
    }

    public override string ToString() => $"{Position}-{Mod.IdWithMotif}";
}

public class GptmdSearchRecord
{
    public string Condition { get; set; }
    public int ModsAdded { get; set; }
    public int PsmCount { get; set; }
    public int PeptideCount { get; set; }
    public int ProteinCount { get; set; }
    public int UnambiguousPsmCount { get; set; }
    public int UnambiguousPeptideCount { get; set; }
    public Dictionary<string, int> ModTypeCounts { get; set; }
    public Dictionary<int, int> ModsAddedPerProtein { get; set; }
}

public class GptmdSearchResultFile : ResultFile<GptmdSearchRecord>
{
    public override SupportedFileType FileType { get; }
    public override Software Software { get; set; }

    public static CsvConfiguration CsvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        Delimiter = ",",
        HasHeaderRecord = true,
    };

    public override void LoadResults()
    {
        throw new NotImplementedException();
    }

    public override void WriteResults(string outputPath)
    {
        // Gather all unique keys for columns
        var allModTypes = Results
            .SelectMany(r => r.ModTypeCounts?.Keys ?? Enumerable.Empty<string>())
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var allModsAdded = Results
            .SelectMany(r => r.ModsAddedPerProtein?.Keys ?? Enumerable.Empty<int>())
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        using var writer = new StreamWriter(outputPath);
        using var csv = new CsvWriter(writer, CsvConfig);

        // Write header
        csv.WriteField("Condition");
        csv.WriteField("ModsAdded");
        csv.WriteField("PsmCount");
        csv.WriteField("PeptideCount");
        csv.WriteField("ProteinCount");
        foreach (var modType in allModTypes)
            csv.WriteField($"ModType-{modType}");
        foreach (var modsAdded in allModsAdded)
            csv.WriteField($"ModsAdded-{modsAdded}");
        csv.NextRecord();

        // Write records
        foreach (var record in Results)
        {
            csv.WriteField(record.Condition);
            csv.WriteField(record.ModsAdded);
            csv.WriteField(record.PsmCount);
            csv.WriteField(record.PeptideCount);
            csv.WriteField(record.ProteinCount);

            foreach (var modType in allModTypes)
            {
                int value = 0;
                if (record.ModTypeCounts != null && record.ModTypeCounts.TryGetValue(modType, out int v))
                    value = v;
                csv.WriteField(value);
            }

            foreach (var modsAdded in allModsAdded)
            {
                int value = 0;
                if (record.ModsAddedPerProtein != null && record.ModsAddedPerProtein.TryGetValue(modsAdded, out int v))
                    value = v;
                csv.WriteField(value);
            }

            csv.NextRecord();
        }
    }
}

