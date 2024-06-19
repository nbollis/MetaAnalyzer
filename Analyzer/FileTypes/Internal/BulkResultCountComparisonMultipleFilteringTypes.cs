using CsvHelper.Configuration;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Readers;

namespace Analyzer.FileTypes.Internal
{
    public class BulkResultCountComparisonMultipleFilteringTypes
    {
        public static CsvConfiguration CsvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",",
            BadDataFound = null,
            MissingFieldFound = null
        };

        public string DatasetName { get; set; }
        [Optional] public string FileName { get; set; }
        public string Condition { get; set; }

        public int PsmCount { get; set; }
        public int ProteoformCount { get; set; }
        public int ProteinGroupCount { get; set; }
        [Optional] public int PsmCountDecoys { get; set; }
        [Optional] public int ProteoformCountDecoys { get; set; }
        [Optional] public int ProteinGroupCountDecoys { get; set; }
            
        public int PsmCount_QValue { get; set; }
        public int ProteoformCount_QValue { get; set; }
        public int ProteinGroupCount_QValue { get; set; }
        [Optional] public int PsmCountDecoys_QValue { get; set; }
        [Optional] public int ProteoformCountDecoys_QValue { get; set; }
        [Optional] public int ProteinGroupCountDecoys_QValue { get; set; }

             
        public int PsmCount_PepQValue { get; set; }
        public int ProteoformCount_PepQValue { get; set; }
        public int ProteinGroupCount_PepQValue { get; set; }
        [Optional] public int PsmCountDecoys_PepQValue { get; set; }
        [Optional] public int ProteoformCountDecoys_PepQValue { get; set; }
             
        public int PsmCount_ResultFile { get; set; }
        public int ProteoformCount_ResultFile { get; set; }
        public int ProteinGroupCount_ResultFile { get; set; }
    }

    public class BulkResultCountComparisonMultipleFilteringTypesFile : ResultFile<BulkResultCountComparisonMultipleFilteringTypes>, IResultFile
    {
        public BulkResultCountComparisonMultipleFilteringTypesFile() { }
        public BulkResultCountComparisonMultipleFilteringTypesFile(string filePath) : base(filePath) { }
        public override SupportedFileType FileType { get; }
        public override Software Software { get; set; }

        public override void LoadResults()
        {
            using var csv = new CsvHelper.CsvReader(new System.IO.StreamReader(FilePath), BulkResultCountComparisonMultipleFilteringTypes.CsvConfig);
            Results = csv.GetRecords<BulkResultCountComparisonMultipleFilteringTypes>().ToList();
        }

        public override void WriteResults(string outputPath)
        {
            using var csv = new CsvWriter(new StreamWriter(outputPath), BulkResultCountComparisonMultipleFilteringTypes.CsvConfig);

            csv.WriteHeader<BulkResultCountComparisonMultipleFilteringTypes>();
            foreach (var result in Results)
            {
                csv.NextRecord();
                csv.WriteRecord(result);
            }
        }
    }
}
