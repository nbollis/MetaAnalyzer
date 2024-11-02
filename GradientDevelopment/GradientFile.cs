using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Readers;

namespace GradientDevelopment
{
    public class GradientPoint
    {
        public static CsvConfiguration CsvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
        };

        public GradientPoint() { }

        public GradientPoint(double time, double percentB)
        {
            Time = time;
            PercentB = percentB;
        }

        public double Time { get; set; }
        [Name("%B", "PercentB")]
        public double PercentB { get; set; }
    }

    public class Gradient : ResultFile<GradientPoint>, IResultFile
    {
        public (double, double)[] GetGradient() => Results.Select(p => (p.Time, p.PercentB)).ToArray();
        public Gradient(string path) : base(path) { } // Add constructor to set FilePath

        public override void LoadResults()
        {
            using var csv = new CsvReader(new StreamReader(FilePath), GradientPoint.CsvConfiguration);
            Results = csv.GetRecords<GradientPoint>().ToList();
        }

        public override void WriteResults(string outputPath)
        {
            using var csv = new CsvWriter(new StreamWriter(outputPath), GradientPoint.CsvConfiguration);

            csv.WriteHeader<GradientPoint>();
            foreach (var result in Results)
            {
                csv.NextRecord();
                csv.WriteRecord(result);
            }
        }


        public override SupportedFileType FileType { get; }
        public override Software Software { get; set; }
    }
}
