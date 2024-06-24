using CsvHelper.Configuration;
using System.Globalization;
using Readers;
using Analyzer.Util.TypeConverters;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace Analyzer.FileTypes.Internal
{
    public class MaximumChimeraEstimation
    {
        public static CsvConfiguration CsvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true,
        };

        public string FileName { get; set; }
        public string CellLine { get; set; }
        public int Ms2ScanNumber { get; set; }

        public int PossibleFeatureCount { get; set; }
        public int PsmCount_MetaMorpheus { get; set; } 
        public int OnePercentPsmCount_MetaMorpheus { get; set; }
        public int PsmCount_Fragger { get; set; }
        public int OnePercentPsmCount_Fragger { get; set; }

        public int PeptideCount_MetaMorpheus { get; set; }
        public int OnePercentPeptideCount_MetaMorpheus { get; set; }

        [TypeConverter(typeof(SemiColonDelimitedToDoubleArrayConverter))]
        public double[] RetentionTimeShift_MetaMorpheus_PSMs { get; set; }

        [TypeConverter(typeof(SemiColonDelimitedToDoubleArrayConverter))]
        public double[] RetentionTimeShift_Fragger_PSMs { get; set; }

        [TypeConverter(typeof(SemiColonDelimitedToDoubleArrayConverter))]
        public double[] OnePercentRetentionTimeShift_MetaMorpheus_PSMs { get; set; }

        [TypeConverter(typeof(SemiColonDelimitedToDoubleArrayConverter))]
        public double[] OnePercentRetentionTimeShift_Fragger_PSMs { get; set; }
        
        [TypeConverter(typeof(SemiColonDelimitedToDoubleArrayConverter))]
        public double[] RetentionTimeShift_MetaMorpheus_Peptides { get; set; }
        
        [TypeConverter(typeof(SemiColonDelimitedToDoubleArrayConverter))]
        public double[] OnePercentRetentionTimeShift_MetaMorpheus_Peptides { get; set; }



        [Ignore] public bool IsChimeric => PsmCount_MetaMorpheus > 1;

        public MaximumChimeraEstimation()
        {
            RetentionTimeShift_MetaMorpheus_PSMs = Array.Empty<double>();
            RetentionTimeShift_Fragger_PSMs = Array.Empty<double>();
            OnePercentRetentionTimeShift_MetaMorpheus_PSMs = Array.Empty<double>();
            OnePercentRetentionTimeShift_Fragger_PSMs = Array.Empty<double>();
            RetentionTimeShift_MetaMorpheus_Peptides = Array.Empty<double>();
            OnePercentRetentionTimeShift_MetaMorpheus_Peptides = Array.Empty<double>();
        }
    }

    public class MaximumChimeraEstimationFile : ResultFile<MaximumChimeraEstimation>, IResultFile
    {
        public MaximumChimeraEstimationFile(string filePath) : base(filePath)
        {
        }

        public MaximumChimeraEstimationFile()
        {
        }

        public override void LoadResults()
        {
            using (var csv = new CsvReader(new StreamReader(FilePath), MaximumChimeraEstimation.CsvConfiguration))
            {
                Results = csv.GetRecords<MaximumChimeraEstimation>().ToList();
            }
        }

        public override void WriteResults(string outputPath)
        {
            using (var csv = new CsvWriter(new StreamWriter(File.Create(outputPath)), MaximumChimeraEstimation.CsvConfiguration))
            {
                csv.WriteHeader<MaximumChimeraEstimation>();
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
