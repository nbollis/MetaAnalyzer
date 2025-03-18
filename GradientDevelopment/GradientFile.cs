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
            PercentBSolvent = percentB;
        }

        public double Time { get; set; }
        [Name("%B", "PercentB")]
        public double PercentBSolvent { get; set; }
    }

    public class Gradient : ResultFile<GradientPoint>, IResultFile
    {
        public string Name { get; init; }
        public (double, double)[] GetGradient() => Results.Select(p => (p.Time, p.PercentBSolvent)).ToArray();
        public Gradient(string path) : base(path) 
        {
            Name = Path.GetFileNameWithoutExtension(path);
        } // Add constructor to set FilePath

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
