using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Readers;

namespace Analyzer.FileTypes.Internal
{
    public class RetentionTimePredictionEntry
    {
        public static CsvConfiguration CsvConfiguration = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            Delimiter = "\t",
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
            BadDataFound = null,
            HeaderValidated = null,
            MissingFieldFound = null,
            ReadingExceptionOccurred = null,
            ShouldSkipRecord = (record) => false,
        };

        public string FileNameWithoutExtension { get; set; }
        public double ScanNumber { get; set; }
        public double PrecursorScanNumber { get; set; }
        public bool IsChimeric { get; set; }
        public double RetentionTime { get; set; }
        public string BaseSequence { get; set; }
        public string FullSequence { get; set; }
        public double QValue { get; set; }
        public double PEP_QValue { get; set; }
        public double PEP { get; set; }
        public double SpectralAngle { get; set; }
        public double SSRCalcPrediction { get; set; }
        [Optional] public double ChronologerPrediction { get; set; }
        public string PeptideModSeq { get; set; }


        [Ignore] private double? _percentHI;
        /// <summary>
        /// The Percent ACN as translated by the linear gradient from the elution time of the identification
        /// </summary>
        [Ignore] public double PercentHI => _percentHI ??= 22.4 / 200.0 * RetentionTime + 1.8;

        [Ignore] private double? _deltaChronologer;
        /// <summary>
        /// Difference between the predicted elution ACN from Chronologer and the actual elution ACN
        /// </summary>
        [Ignore] public double DeltaChronologerHI => _deltaChronologer ??= PercentHI - ChronologerPrediction;


        [Ignore] private double? _chronologerToRetentionTime;

        /// <summary>
        /// The difference between the Chronologer prediction translated to RT and the actual retention time
        /// </summary>
        [Ignore] public double ChronologerToRetentionTime =>
            _chronologerToRetentionTime ??= (ChronologerPrediction - 1.8) * 200 / 22.4;

        [Ignore] private double? _deltaChronologerRT;

        /// <summary>
        /// The difference between the Chronologer prediction translated to RT and the actual retention time
        /// </summary>
        [Ignore] public double DeltaChronologerRT => _deltaChronologerRT ??= RetentionTime - ChronologerToRetentionTime;



        [Ignore] private double? _deltaSSRCalc;
        [Ignore] public double DeltaSSRCalc => _deltaSSRCalc ??= SSRCalcPrediction - RetentionTime;

        public RetentionTimePredictionEntry(string fileNameWithoutExtension, double scanNumber,
            double precursorScanNumber, double retentionTime, string baseSequence, string fullSequence, string peptideModSeq,
            double qValue, double pepQValue, double pep, double spectralAngle, bool isChimeric)
        {
            FileNameWithoutExtension = fileNameWithoutExtension;
            ScanNumber = scanNumber;
            PrecursorScanNumber = precursorScanNumber;
            RetentionTime = retentionTime;
            BaseSequence = baseSequence;
            FullSequence = fullSequence;
            PeptideModSeq = peptideModSeq;
            QValue = qValue;
            PEP_QValue = pepQValue;
            IsChimeric = isChimeric;
            PEP = pep;
            SpectralAngle = spectralAngle;
        }

        public RetentionTimePredictionEntry()
        {
        }
    }


    public class RetentionTimePredictionFile : ResultFile<RetentionTimePredictionEntry>, IResultFile
    {
        public override void LoadResults()
        {
            using var csv = new CsvReader(new StreamReader(FilePath), RetentionTimePredictionEntry.CsvConfiguration);
            Results = csv.GetRecords<RetentionTimePredictionEntry>().ToList();
        }

        public override void WriteResults(string outputPath)
        {
            if (!CanRead(outputPath))
                outputPath += FileType.GetFileExtension();

            using var csv = new CsvWriter(new StreamWriter(File.Create(outputPath)), RetentionTimePredictionEntry.CsvConfiguration);

            csv.WriteHeader<RetentionTimePredictionEntry>();
            foreach (var result in Results)
            {
                csv.NextRecord();
                csv.WriteRecord(result);
            }
        }

        public override SupportedFileType FileType => SupportedFileType.Tsv_FlashDeconv;
        public override Software Software { get; set; }
    }
}
