using CsvHelper;
using CsvHelper.Configuration;
using Readers;
using System.Globalization;
using CsvHelper.Configuration.Attributes;

namespace GradientDevelopment
{
    public class ExtractedInformation
    {
        public static CsvConfiguration CsvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = "\t",
            HasHeaderRecord = true,
            HeaderValidated = null,
            MissingFieldFound = null
        };

        public int Ms2ScansCollected { get; set; }
        public int PrecursorsFragmented { get; set; }
        public int OSMCount { get; set; }
        public int OligoCount { get; set; }



        [Name("DataFileName")]
        public string DataFileName { get; set; }

        [Name("MobilePhaseB")]
        public string MobilePhaseB { get; set; }

        [Name("GradientName")]
        public string GradientName { get; set; }

        [Name("Tic")]
        [TypeConverter(typeof(AnonymousDoubleArrayToStringConverter))]
        public (double, double)[] Tic { get; set; }

        [Name("Gradient")]
        [TypeConverter(typeof(AnonymousDoubleArrayToStringConverter))]
        public (double, double)[] Gradient { get; set; }

        [Name("Ids")]
        [TypeConverter(typeof(AnonymousDoubleArrayToStringConverter))]
        public (double, double)[] Ids { get; set; }

        [Name("FivePercentIds")]
        [TypeConverter(typeof(AnonymousDoubleArrayToStringConverter))]
        public (double, double)[] FivePercentIds { get; set; }

        public ExtractedInformation(string dataFileName, string mobilePhaseB, string gradientName,
            (double, double)[] tic, (double, double)[] gradient, (double, double)[] ids, (double, double)[] fivePercentIds,
            int ms2ScansCollected, int precursorsFragmented, int osMCount, int oligoCount)
        {
            DataFileName = dataFileName;
            MobilePhaseB = mobilePhaseB;
            GradientName = gradientName;
            Tic = tic;
            Gradient = gradient;
            Ids = ids;
            FivePercentIds = fivePercentIds;
            Ms2ScansCollected = ms2ScansCollected;
            PrecursorsFragmented = precursorsFragmented;
            OSMCount = osMCount;
            OligoCount = oligoCount;
        }

        public ExtractedInformation()
        {
        }
    }

    public class ExtractedInformationFile : ResultFile<ExtractedInformation>, IResultFile
    {
        public ExtractedInformationFile(string path) : base(path) { }

        public override void LoadResults()
        {
            using var csv = new CsvReader(new StreamReader(FilePath), ExtractedInformation.CsvConfiguration);
            Results = csv.GetRecords<ExtractedInformation>().ToList();
        }

        public override void WriteResults(string outputPath)
        {
            using (var csv = new CsvWriter(new StreamWriter(outputPath), ExtractedInformation.CsvConfiguration))
            {
                csv.WriteHeader<ExtractedInformation>();
                foreach (var result in Results)
                {
                    csv.NextRecord();
                    csv.WriteRecord(result);
                }
            }
        }

        public override SupportedFileType FileType { get; }
        public override Software Software { get; set; }
    }
}
