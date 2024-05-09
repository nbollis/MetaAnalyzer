using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Readers;

namespace Analyzer.FileTypes.External
{
    public class ProteomeDiscovererInputFileRecord
    {
        public static CsvConfiguration CsvConfiguration => new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            Delimiter = "\t",
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null,
            MissingFieldFound = null,
            HeaderValidated = null,
        };

        [Name("File ID")]
        public string FileID { get; set; }

        [Name("File Name")]
        public string FileName { get; set; }

        [Name("Creation Date")]
        public DateTime CreationDate { get; set; }

        [Name("Instrument Name")]
        public string InstrumentName { get; set; }

        [Name("Software Revision")]
        public string SoftwareRevision { get; set; }

        [Name("Max. Mass [Da]")]
        public double MaxMass { get; set; }

    }

    public class ProteomeDiscovererInputFileFile : ResultFile<ProteomeDiscovererInputFileRecord>, IResultFile
    {
        public ProteomeDiscovererInputFileFile(string filePath) : base(filePath)
        {
        }

        public ProteomeDiscovererInputFileFile()
        {
        }
        public override SupportedFileType FileType => SupportedFileType.Tsv_FlashDeconv;
        public override Software Software { get; set; }

        public override void LoadResults()
        {
            using var csv = new CsvReader(new StreamReader(FilePath), ProteomeDiscovererInputFileRecord.CsvConfiguration);
            Results = csv.GetRecords<ProteomeDiscovererInputFileRecord>().ToList();
        }

        public override void WriteResults(string outputPath)
        {
            using var csv = new CsvWriter(new StreamWriter(File.Create(outputPath)), ProteomeDiscovererInputFileRecord.CsvConfiguration);

            csv.WriteHeader<ProteomeDiscovererInputFileRecord>();
            foreach (var result in Results)
            {
                csv.NextRecord();
                csv.WriteRecord(result);
            }
        }
    }
}
