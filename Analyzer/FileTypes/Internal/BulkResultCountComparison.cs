using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Readers;

namespace Analyzer.FileTypes.Internal
{
    public class BulkResultCountComparison : IEquatable<BulkResultCountComparison>
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
        public int PeptideCount { get; set; }
        public int ProteinGroupCount { get; set; }
        public int OnePercentPsmCount { get; set; }
        public int OnePercentPeptideCount { get; set; }
        public int OnePercentProteinGroupCount { get; set; }
        [Optional] public int OnePercentUnambiguousPsmCount { get; set; }
        [Optional] public int OnePercentUnambiguousPeptideCount { get; set; }


        // addition operator to sum all integer components
        public static BulkResultCountComparison operator +(BulkResultCountComparison a, BulkResultCountComparison b)
        {
            if (a.DatasetName != b.DatasetName)
                throw new System.ArgumentException("Datasets must be the same to add counts");
            
            if (a.Condition != b.Condition)
                throw new System.ArgumentException("Conditions must be the same to add counts");

            return new BulkResultCountComparison
            {
                DatasetName = a.DatasetName,
                FileName = "Summed",
                Condition = a.Condition,
                PsmCount = a.PsmCount + b.PsmCount,
                PeptideCount = a.PeptideCount + b.PeptideCount,
                ProteinGroupCount = a.ProteinGroupCount + b.ProteinGroupCount,
                OnePercentPsmCount = a.OnePercentPsmCount + b.OnePercentPsmCount,
                OnePercentPeptideCount = a.OnePercentPeptideCount + b.OnePercentPeptideCount,
                OnePercentProteinGroupCount = a.OnePercentProteinGroupCount + b.OnePercentProteinGroupCount,
                OnePercentUnambiguousPsmCount = a.OnePercentUnambiguousPsmCount + b.OnePercentUnambiguousPsmCount,
                OnePercentUnambiguousPeptideCount = a.OnePercentUnambiguousPeptideCount + b.OnePercentUnambiguousPeptideCount
            };
        }

        public bool Equals(BulkResultCountComparison? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return DatasetName == other.DatasetName && FileName == other.FileName && Condition == other.Condition;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BulkResultCountComparison)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DatasetName, FileName, Condition);
        }
    }


    public class BulkResultCountComparisonFile : ResultFile<BulkResultCountComparison>, IResultFile
    {
        public override void LoadResults()
        {
            using (var csv = new CsvReader(new StreamReader(FilePath), BulkResultCountComparison.CsvConfig))
            {
                Results = csv.GetRecords<BulkResultCountComparison>().ToList();
            }
        }

        public override void WriteResults(string outputPath)
        {
            using var csv = new CsvWriter(new StreamWriter(outputPath), BulkResultCountComparison.CsvConfig);

            csv.WriteHeader<BulkResultCountComparison>();
            foreach (var result in Results)
            {
                csv.NextRecord();
                csv.WriteRecord(result);
            }
        }

        public BulkResultCountComparisonFile(string filePath) : base(filePath)
        {
        }

        public BulkResultCountComparisonFile() : base()
        {
        }
        public override SupportedFileType FileType { get; }
        public override Software Software { get; set; }

        public override string ToString()
        {
            var result = Results.First();
            return $"{result.DatasetName}_{result.Condition}";
        }
    }
}
