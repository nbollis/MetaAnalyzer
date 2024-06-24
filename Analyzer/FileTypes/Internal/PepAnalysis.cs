using Analyzer.Util;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Readers;

namespace Analyzer.FileTypes.Internal
{

    public class PepAnalysisForPercolatorFile : ResultFile<PepAnalysis>, IResultFile
    {
        public PepAnalysisForPercolatorFile(string filePath) :base(filePath, Software.MetaMorpheus) { }

        public PepAnalysisForPercolatorFile() : base() { }

        public override void LoadResults()
        {
            using var csv = new CsvReader(new System.IO.StreamReader(FilePath), PepAnalysis.CsvConfiguration);
      
            Results = csv.GetRecords<PepAnalysis>().ToList();
        }

        public override void WriteResults(string outputPath)
        {
            throw new NotImplementedException();
        }

        public override SupportedFileType FileType { get; }
        public override Software Software { get; set; }
    }


    public class PepAnalysis
    {

        public static CsvConfiguration CsvConfiguration = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            Delimiter = "\t",
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
            ShouldSkipRecord = arg => !arg.Row.ValidateMyColumn(),
        };


        public int SpecId { get; set; }
        public int Label { get; set; }

        [Name("ScanNr")]
        public int ScanNumber { get; set; }

        [Name("TotalMatchingFragmentCount")]
        [Optional]
        public double MatchedIonCount { get; set; }

        [Name("Intensity")]
        [Optional]
        public double Intensity { get; set; }

        [Name("PrecursorChargeDiffToMode")]
        [Optional]
        public double PrecursorChargeDifference { get; set; }

        [Name("DeltaScore")]
        [Optional]
        public double DeltaScore { get; set; }

        [Name("Notch")]
        [Optional]
        public double Notch { get; set; }

        [Name("PsmCount")]
        [Optional]
        public double PsmCount { get; set; }

        [Name("ModsCount")]
        [Optional]
        public double ModCount { get; set; }

        [Name("AbsoluteAverageFragmentMassErrorFromMedian")]
        [Optional]
        public double FragmentMassError { get; set; }

        [Optional]
        public double MissedCleavages { get; set; }

        [Name("Ambiguity")]
        [Optional]
        public double AmbiguityLevel { get; set; }

        [Name("LongestFragmentIonSeries")]
        [Optional]
        public double LongestIonSeries { get; set; }

        [Name("ComplementaryIonCount")]
        [Optional]
        public double ComplementaryIonCount { get; set; }

        [Optional]
        [Name("HydrophobicityZScore")]
        public double HydrophobicityZScore { get; set; }

        [Optional]
        [Name("IsVariantPeptide")]
        public double IsVariantPeptide { get; set; }

        [Optional]
        [Name("IsDeadEnd")]
        public double IsDeadEnd { get; set; }

        [Optional]
        [Name("IsLoop")]
        public double IsLoop { get; set; }

        [Name("SpectralAngle")]
        [Optional]
        public double SpectralAngle { get; set; }

        [Name("HasSpectralAngle")]
        [Optional]
        public double HasSpectralAngle { get; set; }

        [Name("PeaksInPrecursorEnvelope")]
        [Optional]
        public double PeaksInPrecursorEnvelope { get; set; }

        [Name("PrecursorEnvelopeScore")]
        [Optional]
        public double PrecursorEnvelopeScore { get; set; }

        [Name("ChimeraDecoyRatio")]
        [Optional]
        public double ChimeraDecoyRatio { get; set; }

        [Name("ChimeraCount")]
        [Optional]
        public double ChimeraCount { get; set; }

        [Name("Peptide")]
        [Optional]
        public string Peptide { get; set; }

        [Name("Proteins")]
        [Optional]
        public string Proteins { get; set; }

        [Optional]
        public double MostAbundantPrecursorPeakIntensity { get; set; }

        [Ignore] public bool IsDecoy => Label == -1;
    }
}
