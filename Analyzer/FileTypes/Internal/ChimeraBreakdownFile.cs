﻿using System.Globalization;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Readers;
using ResultAnalyzerUtil;

namespace Analyzer.FileTypes.Internal
{
   

    public class ChimeraBreakdownRecord
    {
        public static CsvConfiguration CsvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true,
        };

        // identifiers
        public string Dataset { get; set; }
        public string FileName { get; set; }
        public string Condition { get; set; }
        public int Ms2ScanNumber { get; set; }
        public ResultType Type { get; set; }

        // results
        [Optional] public double IsolationMz { get; set; } = -1;
        public int IdsPerSpectra { get; set; }
        public int Parent { get; set; } = 1;
        public int UniqueForms { get; set; }
        public int UniqueProteins { get; set; }
        [Optional] public int TargetCount { get; set; }
        [Optional] public int DecoyCount { get; set; }
        [Optional] public int DuplicateCount { get; set; }

        [Optional]
        [TypeConverter(typeof(SemiColonDelimitedToDoubleArrayConverter.SemiColonDelimitedToIntegerArrayConverter ))]
        public int[] PsmCharges { get; set; }

        [Optional]
        [TypeConverter(typeof(SemiColonDelimitedToDoubleArrayConverter))]
        public double[] PsmMasses { get; set; }

        [Optional]
        [TypeConverter(typeof(SemiColonDelimitedToDoubleArrayConverter.SemiColonDelimitedToIntegerArrayConverter ))]
        public int[] PeptideCharges { get; set; }

        [Optional]
        [TypeConverter(typeof(SemiColonDelimitedToDoubleArrayConverter))]
        public double[] PeptideMasses { get; set; }

        public ChimeraBreakdownRecord()
        {
        }
    }

    public class ChimeraBreakdownFile : ResultFile<ChimeraBreakdownRecord>, IResultFile
    {

        public ChimeraBreakdownFile(string filePath) : base(filePath)
        {
        }

        public ChimeraBreakdownFile()
        {
        }

        public override void LoadResults()
        {
            using var csv = new CsvHelper.CsvReader(new System.IO.StreamReader(FilePath), ChimeraBreakdownRecord.CsvConfiguration);
            Results = csv.GetRecords<ChimeraBreakdownRecord>().ToList();
        }

        public override void WriteResults(string outputPath)
        {
            using (var csv = new CsvHelper.CsvWriter(new System.IO.StreamWriter(outputPath),
                       ChimeraBreakdownRecord.CsvConfiguration))
            {
                csv.WriteHeader<ChimeraBreakdownRecord>();
                foreach (var result in Results)
                {
                    csv.NextRecord();
                    csv.WriteRecord(result);
                }
            }
            Thread.Sleep(1000);
        }

        public override string ToString()
        {
            var result = Results.First();
            return $"{result.Dataset}_{result.Condition}";
        }

        public override SupportedFileType FileType { get; }
        public override Software Software { get; set; }
    }
}
