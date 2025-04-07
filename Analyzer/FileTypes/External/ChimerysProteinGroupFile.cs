using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Easy.Common.Extensions;
using Readers;


namespace Analyzer.FileTypes.External;
public class ChimerysProteinGroupFile : ResultFile<ChimerysProteinGroup>, IResultFile
{
    public static CsvConfiguration CsvConfiguration => new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        Encoding = Encoding.UTF8,
        HasHeaderRecord = true,
        Delimiter = "\t",
        IgnoreBlankLines = true,
        TrimOptions = TrimOptions.Trim
    };

    public override SupportedFileType FileType { get; }
    public override Software Software { get; set; } = Software.Unspecified;

    public ChimerysProteinGroupFile() : base() { Software = Software.Unspecified; }
    public ChimerysProteinGroupFile(string path) : base(path, Software.Unspecified) { }

    public override void LoadResults()
    {
        using var csv = new CsvReader(new StreamReader(FilePath), CsvConfiguration);
        var headers = File.ReadLines(FilePath).First().Split('\t');
        var results = new List<ChimerysProteinGroup>();

        // wide file (condensed same IDs from different files)
        if (headers.Count(p => p.Contains("Q_VALUE")) > 2)
        {
            var headerDicts = new Dictionary<string, Dictionary<string, int>>
            {
                { "PROTEIN_IDS", headers.Where(p => p.Contains(":PROTEIN_IDS")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "FASTA_HEADERS", headers.Where(p => p.Contains(":FASTA_HEADERS")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "GENE_NAMES", headers.Where(p => p.Contains(":GENE_NAMES")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "PROTEIN_IDENTIFIERS", headers.Where(p => p.Contains(":PROTEIN_IDENTIFIERS")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "TAXONOMY_IDS", headers.Where(p => p.Contains(":TAXONOMY_IDS")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "ORGANISMS", headers.Where(p => p.Contains(":ORGANISMS")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "PSM_IDS", headers.Where(p => p.Contains("PSM_IDS")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "PRECURSOR_IDS", headers.Where(p => p.Contains("PRECURSOR_IDS")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "MODIFIED_PEPTIDE_IDS", headers.Where(p => p.Contains("MODIFIED_PEPTIDE_IDS")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "PEPTIDE_IDS", headers.Where(p => p.Contains("PEPTIDE_IDS")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "COUNT_PSMS", headers.Where(p => p.Contains("COUNT_PSMS")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "COUNT_PRECURSORS", headers.Where(p => p.Contains("COUNT_PRECURSORS")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "COUNT_MODIFIED_PEPTIDES", headers.Where(p => p.Contains("COUNT_MODIFIED_PEPTIDES")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "COUNT_PEPTIDES", headers.Where(p => p.Contains("COUNT_PEPTIDES")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "QUANTIFICATION", headers.Where(p => p.Contains("QUANTIFICATION")).ToDictionary(p => p, p => headers.IndexOf(p)) }
            };

            var rawFileNames = headerDicts["PROTEIN_IDS"].Keys.Select(p => p.Split(':')[0]).ToArray();
            var sampleNames = headerDicts["PROTEIN_IDS"].Keys.Select(p => p.Split(':')[1]).ToArray();
            var allValues = headerDicts.ToDictionary(
                dict => dict.Key,
                dict => dict.Value.Select(p => csv.GetField(p.Value)).ToArray()
            );

            bool readHeader = false;
            while (csv.Read())
            {
                if (readHeader == false)
                {
                    csv.ReadHeader();
                    readHeader = true;
                    continue;
                }
                
                var record = csv.GetRecord<ChimerysProteinGroup>();
                if (record is null)
                    continue;

                var nonNullFileEntries = allValues
                    .Select((m, i) =>
                    {
                        var peptideCount = (int?)(int.TryParse(allValues["COUNT_PEPTIDES"][i], out var result) ? result : null);
                        var psmCount = (int?)(int.TryParse(allValues["COUNT_PSMS"][i], out var psmResult) ? psmResult : null);

                        return new { PeptideCount = peptideCount, PsmCount = psmCount, Index = i };
                    })
                    .Where(p => p.PeptideCount != null)
                    .OrderByDescending(p => p.PeptideCount)
                    .ThenByDescending(p => p.PsmCount)
                    .ToList();
                var nonNullIndexes = nonNullFileEntries.Select(p => p.Index).ToList();
                var bestIndex = nonNullFileEntries.First().Index;

                foreach (var indexWhereIdentified in nonNullIndexes)
                {
                    var intermediate = new ChimerysFileSpecificProteinGroupInfo
                    {
                        RawFileName = rawFileNames[indexWhereIdentified],
                        SampleName = sampleNames[indexWhereIdentified],
                        ProteinIds = allValues["PROTEIN_IDS"][indexWhereIdentified].Split(';').Select(int.Parse).ToArray(),
                        FastaHeaders = allValues["FASTA_HEADERS"][indexWhereIdentified].Split(';').ToArray(),
                        GeneNames = allValues["GENE_NAMES"][indexWhereIdentified].Split(';').ToArray(),
                        ProteinIdentifiers = allValues["PROTEIN_IDENTIFIERS"][indexWhereIdentified].Split(';').ToArray(),
                        TaxonomyIds = allValues["TAXONOMY_IDS"][indexWhereIdentified].Split(';').ToArray(),
                        Organisms = allValues["ORGANISMS"][indexWhereIdentified].Split(';').ToArray(),
                        PsmIds = allValues["PSM_IDS"][indexWhereIdentified].Split(';').Select(long.Parse).ToArray(),
                        PrecursorIds = allValues["PRECURSOR_IDS"][indexWhereIdentified].Split(';').Select(long.Parse).ToArray(),
                        ModifiedPeptideIds = allValues["MODIFIED_PEPTIDE_IDS"][indexWhereIdentified].Split(';').Select(int.Parse).ToArray(),
                        PeptideIds = allValues["PEPTIDE_IDS"][indexWhereIdentified].Split(';').Select(int.Parse).ToArray(),
                        CountPsms = int.Parse(allValues["COUNT_PSMS"][indexWhereIdentified]),
                        CountPrecursors = int.Parse(allValues["COUNT_PRECURSORS"][indexWhereIdentified]),
                        CountModifiedPeptides = int.Parse(allValues["COUNT_MODIFIED_PEPTIDES"][indexWhereIdentified]),
                        CountPeptides = int.Parse(allValues["COUNT_PEPTIDES"][indexWhereIdentified]),
                        Quantification = double.Parse(allValues["QUANTIFICATION"][indexWhereIdentified]),
                    };
                    record.FileSpecificInformation.Add(intermediate);

                    // if this result is best by q value across all files, set the base properties.
                    if (indexWhereIdentified != bestIndex) continue;

                    record.RawFileName = intermediate.RawFileName;
                    record.SampleName = intermediate.SampleName;
                    record.ProteinIds = intermediate.ProteinIds;
                    record.FastaHeaders = intermediate.FastaHeaders;
                    record.GeneNames = intermediate.GeneNames;
                    record.ProteinIdentifiers = intermediate.ProteinIdentifiers;
                    record.TaxonomyIds = intermediate.TaxonomyIds;
                    record.Organisms = intermediate.Organisms;
                    record.PsmIds = intermediate.PsmIds;
                    record.PrecursorIds = intermediate.PrecursorIds;
                    record.ModifiedPeptideIds = intermediate.ModifiedPeptideIds;
                    record.PeptideIds = intermediate.PeptideIds;
                    record.CountPsms = intermediate.CountPsms;
                    record.CountPrecursors = intermediate.CountPrecursors;
                    record.CountModifiedPeptides = intermediate.CountModifiedPeptides;
                    record.CountPeptides = intermediate.CountPeptides;
                    record.Quantification = intermediate.Quantification;
                }
                results.Add(record);
            }
        }
        else // Long file (each ID from each file gets its own row)
        {
            results = csv.GetRecords<ChimerysProteinGroup>().ToList();
        }
        Results = results;
    }

    public override void WriteResults(string outputPath)
    {
        using var csv = new CsvWriter(new StreamWriter(File.Create(outputPath)), CsvConfiguration);

        csv.WriteHeader<ChimerysProteinGroup>();
        foreach (var result in Results.SelectMany(p => p.ToLongFormat()))
        {
            csv.NextRecord();
            csv.WriteRecord(result);
        }
    }
}