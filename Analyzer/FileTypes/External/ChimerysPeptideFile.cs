using System.Diagnostics;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Easy.Common.Extensions;
using Readers;

namespace Analyzer.FileTypes.External;

public class ChimerysPeptideFile : ResultFile<ChimerysPeptide>, IResultFile
{
    public static CsvConfiguration CsvConfiguration => new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        Encoding = Encoding.UTF8,
        HasHeaderRecord = true,
        Delimiter = "\t",
        IgnoreBlankLines = true,
        TrimOptions = TrimOptions.Trim,
        HeaderValidated = null
    };

    public override SupportedFileType FileType { get; }
    public override Software Software { get; set; } = Software.Unspecified;

    public ChimerysPeptideFile() : base() { Software = Software.Unspecified; }
    public ChimerysPeptideFile(string path) : base(path, Software.Unspecified) { }

    public override void LoadResults()
    {
        using var csv = new CsvReader(new StreamReader(FilePath), CsvConfiguration);
        var headers = File.ReadLines(FilePath).First().Split('\t');
        var results = new List<ChimerysPeptide>();

        // if wide file (condensed same IDs from different files)
        if (headers.Count(p => p.Contains("Q_VALUE")) > 2)
        {
            var headerDicts = new Dictionary<string, Dictionary<string, int>>
            {
                { "Q_VALUE", headers.Where(p => p.Contains(":Q_VALUE")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "SE_SCORE", headers.Where(p => p.Contains(":SE_SCORE")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "PEP", headers.Where(p => p.Contains(":PEP")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "IS_IDENTIFIED_BY_MBR", headers.Where(p => p.Contains("IS_IDENTIFIED_BY_MBR")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "PSM_IDS", headers.Where(p => p.Contains("PSM_IDS")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "PRECURSOR_IDS", headers.Where(p => p.Contains("PRECURSOR_IDS")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "MODIFIED_PEPTIDE_IDS", headers.Where(p => p.Contains("MODIFIED_PEPTIDE_IDS")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "COUNT_PSMS", headers.Where(p => p.Contains("COUNT_PSMS")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "COUNT_PRECURSORS", headers.Where(p => p.Contains("COUNT_PRECURSORS")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "COUNT_MODIFIED_PEPTIDES", headers.Where(p => p.Contains("COUNT_MODIFIED_PEPTIDES")).ToDictionary(p => p, p => headers.IndexOf(p)) },
                { "QUANTIFICATION", headers.Where(p => p.Contains("QUANTIFICATION")).ToDictionary(p => p, p => headers.IndexOf(p)) }
            };

            var rawFileNames = headerDicts["Q_VALUE"].Keys.Select(p => p.Split(':')[0]).ToArray();
            var sampleNames = headerDicts["Q_VALUE"].Keys.Select(p => p.Split(':')[1]).ToArray();

            bool readHeader = false;
            while (csv.Read())
            {
                if (readHeader == false)
                {
                    csv.ReadHeader();
                    readHeader = true;
                    continue;
                }

                ChimerysPeptide? record = csv.GetRecord<ChimerysPeptide>();
                if (record is null)
                    continue;

                var allValues = headerDicts.ToDictionary(
                    dict => dict.Key,
                    dict => dict.Value.Select(p => csv.GetField(p.Value)).ToArray()
                );

                var nonNullQValues = allValues["Q_VALUE"]
                    .Select((m, i) => new { Value = (double?)(double.TryParse(m, out var result) ? result : null), Index = i })
                    .OrderBy(p => p.Value)
                    .Where(p => p.Value != null).ToList();
                var nonNullIndexes = nonNullQValues.Select(p => p.Index).ToList();
                var bestIndex = nonNullQValues.First().Index;

                if (nonNullIndexes.Count == 0)
                    Debugger.Break();

                foreach (var indexWhereIdentified in nonNullIndexes)
                {
                    var intermediate = new ChimerysFileSpecificPeptideInfo
                    {
                        RawFileName = rawFileNames[indexWhereIdentified],
                        SampleName = sampleNames[indexWhereIdentified],
                        QValue = double.Parse(allValues["Q_VALUE"][indexWhereIdentified]),
                        SearchEngineScore = double.Parse(allValues["SE_SCORE"][indexWhereIdentified]),
                        Pep = double.Parse(allValues["PEP"][indexWhereIdentified]),
                        IsIdentifiedByMbr = bool.Parse(allValues["IS_IDENTIFIED_BY_MBR"][indexWhereIdentified]),
                        PsmIds = allValues["PSM_IDS"][indexWhereIdentified].Split(';').Select(long.Parse).ToArray(),
                        PrecursorIds = allValues["PRECURSOR_IDS"][indexWhereIdentified].Split(';').Select(long.Parse).ToArray(),
                        ModifiedPeptideIds = allValues["MODIFIED_PEPTIDE_IDS"][indexWhereIdentified].Split(';').Select(long.Parse).ToArray(),
                        CountPsms = int.Parse(allValues["COUNT_PSMS"][indexWhereIdentified]),
                        CountPrecursors = int.Parse(allValues["COUNT_PRECURSORS"][indexWhereIdentified]),
                        CountModifiedPeptides = int.Parse(allValues["COUNT_MODIFIED_PEPTIDES"][indexWhereIdentified]),
                        Quantification = double.Parse(allValues["QUANTIFICATION"][indexWhereIdentified])
                    };

                    record.FileSpecificInformation.Add(intermediate);

                    // if this result is best by q value across all files, set the base properties. 
                    if (indexWhereIdentified != bestIndex) continue;

                    record.RawFileName = intermediate.RawFileName;
                    record.SampleName = intermediate.SampleName;
                    record.QValue = intermediate.QValue;
                    record.SearchEngineScore = intermediate.SearchEngineScore;
                    record.Pep = intermediate.Pep;
                    record.IsIdentifiedByMbr = intermediate.IsIdentifiedByMbr;
                    record.PsmIds = intermediate.PsmIds;
                    record.PrecursorIds = intermediate.PrecursorIds;
                    record.ModifiedPeptideIds = intermediate.ModifiedPeptideIds;
                    record.CountPsms = intermediate.CountPsms;
                    record.CountPrecursors = intermediate.CountPrecursors;
                    record.CountModifiedPeptides = intermediate.CountModifiedPeptides;
                    record.Quantification = intermediate.Quantification;
                }

                results.Add(record);
            }
        }
        else // Long file (each ID from each file gets its own row)
        {
            results = csv.GetRecords<ChimerysPeptide>().ToList();
        }
        Results = results;
    }

    public override void WriteResults(string outputPath)
    {
        using var csv = new CsvWriter(new StreamWriter(File.Create(outputPath)), CsvConfiguration);

        csv.WriteHeader<ChimerysPeptide>();
        foreach (var result in Results.SelectMany(p => p.ToLongFormat()))
        {
            csv.NextRecord();
            csv.WriteRecord(result);
        }
    }
}