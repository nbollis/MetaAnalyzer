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
using Proteomics.ProteolyticDigestion;
using MathNet.Numerics;
using MathNet.Numerics.RootFinding;

namespace TaskLayer;

public class GptmdSearchResult
{
    private readonly bool ReportAmbiguous = true;

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
                        .OrderBy(p => p.Accession).ToList()
                    : ProteinDbLoader.LoadProteinFasta(OriginalDatabasePath, true, DecoyType.None, false, out _)
                        .OrderBy(p => p.Accession).ToList();
            }
            return _originalProteins;
        }
    }

    // number of proteins that had a mod added to it
    public int ProteinsWithAddedMods { get; private set; }

    // psm centric count of proteins that had a mod added.
    public int ProteinsWithAddedModsPsmCount { get; private set; }

    // Protein centric count of psms that contain an added mod. 
    public int ProteinsWithAddedModsPsmContainingModCount { get; private set; }

    // Total number of added mods found on psms
    public int AddedModsOnPsmCount { get; private set; }

    // result text => mods added
    public int ModificationsAdded { get; private set; }

    // result text => types added
    public Dictionary<string, int> ModTypeCounts { get; private set; }

    // mods added per protein histogram 
    public Dictionary<int, int> ModsAddedPerProtein { get; private set; }

    // the added mod to psm conversion where key is percent of mods added and value is count of proteins
    public Dictionary<double, int> ModConversionHistogram { get; } 

    // distribution of integer scores for target psms
    public Dictionary<int, int> PsmTargetScoreDistribution { get; private set; }
    // distribution of integer scores for decoy psms
    public Dictionary<int, int> PsmDecoyScoreDistribution { get; private set; }
    public GptmdSearchResult(string resultDirectory, bool reportAmbiguous = true)
    {
        ResultDirectory = resultDirectory;
        ReportAmbiguous = reportAmbiguous;
        ModsAddedPerProtein = new Dictionary<int, int>();
        ModsAddedPerProtein = new Dictionary<int, int>();
        PsmTargetScoreDistribution = new Dictionary<int, int>();
        PsmDecoyScoreDistribution = new Dictionary<int, int>();
        ModConversionHistogram = Enumerable.Range(0, 100).Select(p => p / 100.0).ToDictionary(p => p, p => 0);

        var directoryName = System.IO.Path.GetFileName(resultDirectory);
        Condition = directoryName;
        SearchTaskResults = new(resultDirectory, directoryName, directoryName);

        var gptmdDir = Directory
            .GetDirectories(resultDirectory).FirstOrDefault(path => Path.GetFileName(path).Contains("GPTMD", StringComparison.InvariantCultureIgnoreCase));

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
        // Cache AllPsms to avoid multiple enumerations
        var allPsms = SearchTaskResults.AllPsms;

        // Use a single pass to separate targets and decoys
        var targets = new List<PsmFromTsv>(allPsms.Count);
        var decoys = new List<PsmFromTsv>(allPsms.Count);

        foreach (var psm in allPsms)
        {
            int tCount = 0, dCount = 0;
            foreach (var c in psm.DecoyContamTarget)
            {
                if (c == 'T') tCount++;
                else if (c == 'D') dCount++;
            }
            if (tCount > dCount)
                targets.Add(psm);
            else if (dCount > tCount)
                decoys.Add(psm);
        }

        foreach (var score in targets.Select(p => (int)p.Score))
        {
            if (PsmTargetScoreDistribution.TryGetValue(score, out int count))
                PsmTargetScoreDistribution[score] = count + 1;
            else
                PsmTargetScoreDistribution[score] = 1;
        }

        foreach (var score in decoys.Select(p => (int)p.Score))
        {
            if (PsmDecoyScoreDistribution.TryGetValue(score, out int count))
                PsmDecoyScoreDistribution[score] = count + 1;
            else
                PsmDecoyScoreDistribution[score] = 1;
        }

        var filteredPsms = SearchTaskResults.AllPsms.Where(p => p.PassesConfidenceFilter());
        if (!ReportAmbiguous)
            filteredPsms = filteredPsms.Where(p => p.AmbiguityLevel == "1");
        var psmList = filteredPsms.ToList();

        // assume all original proteins are in the gptmd database
        // because we sort both by accession, we assume the indexes are equivalent. 
        for (var index = 0; index < OriginalProteins.Count; index++)
        {
            var protein = OriginalProteins[index];
            var gptmdProtein = GptmdProteins[index];

            var relevantPsms = psmList
                .Where(p => p.ProteinAccession.Contains(protein.Accession));
            ProteinComparison(protein, gptmdProtein, relevantPsms);
        }
    }

    public void ProteinComparison(Protein original, Protein gptmd, IEnumerable<PsmFromTsv> relevantPsms)
    {
        if (original.Accession != gptmd.Accession)
            throw new ArgumentException("Proteins do not match by accession.");

        // flatten and retain residue information:
        var originalModsFlat = original.OneBasedPossibleLocalizedModifications
            .SelectMany(kvp => kvp.Value.Select(mod => new FlatMod(kvp.Key, mod)))
            .Distinct()
            .ToList();

        var gptmdModsFlat = gptmd.OneBasedPossibleLocalizedModifications
            .SelectMany(kvp => kvp.Value.Select(mod => new FlatMod(kvp.Key, mod)))
            .Distinct()
            .ToList();

        // Update histogram of mods added per protein
        int modsAdded = gptmdModsFlat.Count - originalModsFlat.Count;
        ModsAddedPerProtein[modsAdded] = ModsAddedPerProtein.TryGetValue(modsAdded, out var count) ? count + 1 : 1;
        if (modsAdded == 0)
            return;

        ProteinsWithAddedMods++;

        // Parse search results to see if the added mods made it to the psms
        // Identify added modifications
        var addedMods = gptmdModsFlat.Except(originalModsFlat).ToList();
        var thinningModsAdded = new HashSet<FlatMod>(addedMods);
        bool foundAdded = false;

        foreach (var relevantPsm in relevantPsms)
        {
            ProteinsWithAddedModsPsmCount++;
            var accessionSplits = relevantPsm.Accession.Split('|');
            var fullSeqSplits = relevantPsm.FullSequence.Split('|');

            if (accessionSplits.Length == 1 && fullSeqSplits.Length == 1)
            {
                var pep = new PeptideWithSetModifications(relevantPsm.FullSequence, GlobalVariables.AllModsKnownDictionary, 0, null, gptmd);
                var pepMods = pep.AllModsOneIsNterminus
                    .Select(p => new FlatMod(p.Key, p.Value));
                foreach (var mod in pepMods)
                {
                    if (!addedMods.Contains(mod)) continue;
                    foundAdded = true;
                    AddedModsOnPsmCount++;

                    thinningModsAdded.Remove(mod);
                }
            }
            else if (accessionSplits.Length == 1)
            {
                foreach (var seq in fullSeqSplits.Distinct())
                {
                    var pep = new PeptideWithSetModifications(seq, GlobalVariables.AllModsKnownDictionary, 0, null, gptmd);
                    var pepMods = pep.AllModsOneIsNterminus
                        .Select(p => new FlatMod(p.Key, p.Value));
                    foreach (var mod in pepMods)
                    {
                        if (!addedMods.Contains(mod)) continue;
                        foundAdded = true;
                        AddedModsOnPsmCount++;
                        thinningModsAdded.Remove(mod);
                    }
                }
            }
            else if (fullSeqSplits.Length == 1)
            {
                var pep = new PeptideWithSetModifications(relevantPsm.FullSequence, GlobalVariables.AllModsKnownDictionary, 0, null, gptmd);
                var pepMods = pep.AllModsOneIsNterminus
                    .Select(p => new FlatMod(p.Key, p.Value));
                foreach(var mod in pepMods)
                {
                    if (addedMods.Contains(mod))
                    {
                        foundAdded = true;
                        AddedModsOnPsmCount++;
                        thinningModsAdded.Remove(mod);
                    }
                }
            }
            else if (accessionSplits.Length == fullSeqSplits.Length)
            {
                foreach(var seq in fullSeqSplits.Distinct())
                {
                    var pep = new PeptideWithSetModifications(seq, GlobalVariables.AllModsKnownDictionary, 0, null, gptmd);
                    var pepMods = pep.AllModsOneIsNterminus
                        .Select(p => new FlatMod(p.Key, p.Value));
                    foreach (var mod in pepMods)
                    {
                        if (!addedMods.Contains(mod)) continue;
                        foundAdded = true;
                        AddedModsOnPsmCount++;
                        thinningModsAdded.Remove(mod);
                    }
                }
            }
            else
            {

            }
            
        }

        if (foundAdded)
            ProteinsWithAddedModsPsmContainingModCount++;

        var modConversion = (1 - thinningModsAdded.Count / (double)modsAdded).Round(2);
        if (!ModConversionHistogram.TryAdd(modConversion, 1))
            ModConversionHistogram[modConversion]++;
    }

    public GptmdSearchRecord ToRecord()
    {
        var searchRecord = SearchTaskResults.GetBulkResultCountComparisonFile().First();
        return new GptmdSearchRecord
        {
            Condition = Condition,
            ConsiderAmbiguous = ReportAmbiguous,
            PsmCount = searchRecord.OnePercentPsmCount,
            PeptideCount = searchRecord.OnePercentPeptideCount,
            ProteinCount = searchRecord.OnePercentProteinGroupCount,
            UnambiguousPsmCount = searchRecord.OnePercentUnambiguousPsmCount,
            UnambiguousPeptideCount = searchRecord.OnePercentUnambiguousPeptideCount,
            ModsAdded = ModificationsAdded,
            ProteinsWithAddedMods = ProteinsWithAddedMods,
            ProteinsWithAddedModsPsmCount = ProteinsWithAddedModsPsmCount,
            ProteinsWithAddedModsPsmContainingModCount = ProteinsWithAddedModsPsmContainingModCount,
            AddedModsOnPsmCount = AddedModsOnPsmCount,
            ModTypeCounts = ModTypeCounts ?? new Dictionary<string, int>(),
            ModsAddedPerProtein = ModsAddedPerProtein ?? new Dictionary<int, int>(),
            ModConversionHistogram = ModConversionHistogram ?? new Dictionary<double, int>(),
            PsmTargetScoreDistribution = PsmTargetScoreDistribution ?? new Dictionary<int, int>(),
            PsmDecoyScoreDistribution = PsmDecoyScoreDistribution ?? new Dictionary<int, int>()
        };
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
    public bool ConsiderAmbiguous { get; set; }
    public int PsmCount { get; set; }
    public int PeptideCount { get; set; }
    public int ProteinCount { get; set; }
    public int UnambiguousPsmCount { get; set; }
    public int UnambiguousPeptideCount { get; set; }
    public int ModsAdded { get; set; }
    public int ProteinsWithAddedMods { get; set; }
    public int ProteinsWithAddedModsPsmCount { get; set; }
    public int ProteinsWithAddedModsPsmContainingModCount { get; set; }
    public int AddedModsOnPsmCount { get; set; }
    public Dictionary<string, int> ModTypeCounts { get; set; }
    public Dictionary<int, int> ModsAddedPerProtein { get; set; }
    public Dictionary<double, int> ModConversionHistogram { get; set; }
    public Dictionary<int, int> PsmTargetScoreDistribution { get; set; }
    public Dictionary<int, int> PsmDecoyScoreDistribution { get; set; }
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

        var targetScores = Results
            .SelectMany(r => r.PsmTargetScoreDistribution?.Keys ?? Enumerable.Empty<int>())
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var decoyScores = Results
            .SelectMany(r => r.PsmDecoyScoreDistribution?.Keys ?? Enumerable.Empty<int>())
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        using var writer = new StreamWriter(outputPath);
        using var csv = new CsvWriter(writer, CsvConfig);

        // Write header
        csv.WriteField("Condition");
        csv.WriteField("ConsiderAmbiguous");
        csv.WriteField("PsmCount");
        csv.WriteField("PeptideCount");
        csv.WriteField("ProteinCount");
        csv.WriteField("UnambiguousPsmCount");
        csv.WriteField("UnambiguousPeptideCount");
        csv.WriteField("ModsAdded");
        csv.WriteField("ProteinsWithAddedMods");
        csv.WriteField("ProteinsWithAddedModsPsmCount");
        csv.WriteField("ProteinsWithAddedModsPsmContainingModCount");
        csv.WriteField("AddedModsOnPsmCount");
        foreach (var modType in allModTypes)
            csv.WriteField($"ModType-{modType}");
        foreach (var modsAdded in allModsAdded)
            csv.WriteField($"ModsAdded-{modsAdded}");
        foreach (var modConversion in Enumerable.Range(0, 100).Select(p => p / 100.0))
            csv.WriteField($"ModConversion-{modConversion:F2}");
        foreach (var score in targetScores)
            csv.WriteField($"PsmTargetScore-{score}");
        foreach (var score in decoyScores)
            csv.WriteField($"PsmDecoyScore-{score}");
        csv.NextRecord();

        // Write records
        foreach (var record in Results)
        {
            csv.WriteField(record.Condition);
            csv.WriteField(record.ConsiderAmbiguous);
            csv.WriteField(record.PsmCount);
            csv.WriteField(record.PeptideCount);
            csv.WriteField(record.ProteinCount);
            csv.WriteField(record.UnambiguousPsmCount);
            csv.WriteField(record.UnambiguousPeptideCount);
            csv.WriteField(record.ModsAdded);
            csv.WriteField(record.ProteinsWithAddedMods);
            csv.WriteField(record.ProteinsWithAddedModsPsmCount);
            csv.WriteField(record.ProteinsWithAddedModsPsmContainingModCount);
            csv.WriteField(record.AddedModsOnPsmCount);

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

            foreach (var modConversion in Enumerable.Range(0, 100).Select(p => p / 100.0))
            {
                int value = 0;
                if (record.ModConversionHistogram != null && record.ModConversionHistogram.TryGetValue(modConversion, out int v))
                    value = v;
                csv.WriteField(value);
            }

            foreach (var score in targetScores)
            {
                int value = 0;
                if (record.PsmTargetScoreDistribution != null && record.PsmTargetScoreDistribution.TryGetValue(score, out int v))
                    value = v;
                csv.WriteField(value);
            }

            foreach (var score in decoyScores)
            {
                int value = 0;
                if (record.PsmDecoyScoreDistribution != null && record.PsmDecoyScoreDistribution.TryGetValue(score, out int v))
                    value = v;
                csv.WriteField(value);
            }

            csv.NextRecord();
        }
    }
}

