using Analyzer.Util;
using Analyzer.Util.TypeConverters;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Readers;

namespace Analyzer.FileTypes.Internal
{
    public class ChimericSpectrumSummary
    {
        public static CsvConfiguration CsvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true,
        };

        // Identifiers
        public string Dataset { get; set; }
        public string FileName { get; set; }
        public string Condition { get; set; }
        public string Type { get; set; }

        // Scan Information
        public int Ms1ScanNumber { get; set; }
        public int Ms2ScanNumber { get; set; }
        public double IsolationMz { get; set; }
        public double IdPerSpectrum { get; set; }
        public double RetentionTime { get; set; }

        // Identification Information
        public double PrecursorMz { get; set; }
        public int PrecursorCharge { get; set; }
        public double PrecursorMass { get; set; }
        public double PEP_QValue { get; set; }
        public double PrecursorFractionalIntensity { get; set; }
        [Optional] public double FragmentFractionalIntensity { get; set; }
        public bool IsChimeric { get; set; }
        public bool IsDecoy { get; set; }
        public bool IsParent { get; set; }
        public bool IsUniqueForm { get; set; }
        public bool IsUniqueProtein { get; set; }
        public bool IsDuplicate { get; set; }

        // Deconovlution Information
        public int PossibleFeatureCount { get; set; }
        [Optional] public double FeatureIntensity { get; set; }

    }

    public class ChimericSpectrumSummaryFile : ResultFile<ChimericSpectrumSummary>, IResultFile
    {
        public ChimericSpectrumSummaryFile(string filePath) : base(filePath, Software.MetaMorpheus) { }

        public ChimericSpectrumSummaryFile() : base() { }

        public override void LoadResults()
        {
            using var csv = new CsvReader(new System.IO.StreamReader(FilePath), ChimericSpectrumSummary.CsvConfiguration);

            Results = csv.GetRecords<ChimericSpectrumSummary>().ToList();
        }

        public override void WriteResults(string outputPath)
        {
            if (!Results.Any())
                return;

            using (var csv = new CsvWriter(new StreamWriter(File.Create(outputPath)), ChimericSpectrumSummary.CsvConfiguration))
            {
                csv.WriteHeader<ChimericSpectrumSummary>();
                foreach (var result in Results)
                {
                    csv.NextRecord();
                    csv.WriteRecord(result);
                }
            }
        }

        public override SupportedFileType FileType { get; }
        public override Software Software { get; set; }


        public IEnumerable<ChimeraBreakdownRecord> ToChimeraBreakDownRecords()
        {
            foreach (var chimeraGroup in Results.GroupBy(p => p,
                         new CustomComparer<ChimericSpectrumSummary>(p => p.Ms2ScanNumber, p => p.FileName, p => p.Type)))
            {

                if (Enum.Parse<ResultType>(chimeraGroup.First().Type) is ResultType.Psm)
                {
                    yield return new ChimeraBreakdownRecord()
                    {
                        Dataset = chimeraGroup.First().Dataset,
                        FileName = chimeraGroup.First().FileName,
                        Condition = chimeraGroup.First().Condition,
                        Ms2ScanNumber = chimeraGroup.First().Ms2ScanNumber,
                        Type = Enum.Parse<ResultType>(chimeraGroup.First().Type),
                        IsolationMz = chimeraGroup.First().IsolationMz,
                        IdsPerSpectra = chimeraGroup.Count(),
                        Parent = chimeraGroup.Count(p => p.IsParent),
                        DecoyCount = chimeraGroup.Count(p => p.IsDecoy),
                        TargetCount = chimeraGroup.Count(p => !p.IsDecoy),
                        UniqueForms = chimeraGroup.Count(p => p.IsUniqueForm),
                        UniqueProteins = chimeraGroup.Count(p => p.IsUniqueProtein),
                        DuplicateCount = chimeraGroup.Count(p => p.IsDuplicate),
                        PsmCharges = chimeraGroup.Select(p => p.PrecursorCharge).ToArray(),
                        PsmMasses = chimeraGroup.Select(p => p.PrecursorMass).ToArray(),
                    };
                }
                else if (Enum.Parse<ResultType>(chimeraGroup.First().Type) is ResultType.Peptide)
                {
                    yield return new ChimeraBreakdownRecord()
                    {
                        Dataset = chimeraGroup.First().Dataset,
                        FileName = chimeraGroup.First().FileName,
                        Condition = chimeraGroup.First().Condition,
                        Ms2ScanNumber = chimeraGroup.First().Ms2ScanNumber,
                        Type = Enum.Parse<ResultType>(chimeraGroup.First().Type),
                        IsolationMz = chimeraGroup.First().IsolationMz,
                        IdsPerSpectra = chimeraGroup.Count(),
                        Parent = chimeraGroup.Count(p => p.IsParent),
                        DecoyCount = chimeraGroup.Count(p => p.IsDecoy),
                        TargetCount = chimeraGroup.Count(p => !p.IsDecoy),
                        UniqueForms = chimeraGroup.Count(p => p.IsUniqueForm),
                        UniqueProteins = chimeraGroup.Count(p => p.IsUniqueProtein),
                        DuplicateCount = chimeraGroup.Count(p => p.IsDuplicate),
                        PeptideCharges = chimeraGroup.Select(p => p.PrecursorCharge).ToArray(),
                        PeptideMasses = chimeraGroup.Select(p => p.PrecursorMass).ToArray(),
                    };
                }
            }
        }

        public IEnumerable<MaximumChimeraEstimation> ToMaximumChimeraEstimations()
        {
            throw new NotImplementedException();
        }
    }

}
