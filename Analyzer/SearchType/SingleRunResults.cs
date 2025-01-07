using Analyzer.FileTypes.Internal;
using Analyzer.Interfaces;
using ResultAnalyzerUtil;
using ResultAnalyzerUtil.CommandLine;

namespace Analyzer.SearchType
{
    public abstract class SingleRunResults : IChimeraPaperResults, IDisposable
    {
        #region Command Line

        public static event EventHandler<StringEventArgs>? LogHandler;
        public static event EventHandler<StringEventArgs>? WarnHandler;
        public static event EventHandler<StringEventArgs>? CrashHandler; 
        protected void Log(string message, int nestLayer = 1)
        {
            string added = string.Join("", Enumerable.Repeat("\t", nestLayer));
            LogHandler?.Invoke(this, new StringEventArgs(added + message));
        }

        protected static void Warn(string v, int nestLayer = 1)
        {
            string added = string.Join("", Enumerable.Repeat("\t", nestLayer));
            WarnHandler?.Invoke(null, new StringEventArgs($"{added}Error (Nonfatal): {v}"));
        }

        protected static void ReportCrash(string message, int nestLayer = 1)
        {
            string added = string.Join("", Enumerable.Repeat("\t", nestLayer));
            CrashHandler?.Invoke(null, new StringEventArgs($"{added}Error (Fatal): {message}"));
        }

        #endregion

        public string DirectoryPath { get; set; }
        public string DatasetName { get; set; }
        public string Condition { get; set; }
        public string FigureDirectory { get; }
        public bool Override { get; set; } = false;

        public string PsmPath { get; init; }
        public string PeptidePath { get; init; }
        public string ProteinPath { get; init; }

        public bool IsTopDown { get; protected set; } = false;
    
        public string ResultType => IsTopDown ? "Proteoform" : "Peptide";

        protected string _IndividualFilePath => Path.Combine(DirectoryPath,
            $"{DatasetName}_{Condition}_{FileIdentifiers.IndividualFileComparison}");

        protected BulkResultCountComparisonFile? _individualFileComparison;

        public BulkResultCountComparisonFile? IndividualFileComparisonFile =>
            _individualFileComparison ??= GetIndividualFileComparison();

        protected string _chimeraPsmPath => Path.Combine(DirectoryPath,
            $"{DatasetName}_{Condition}_PSM_{FileIdentifiers.ChimeraCountingFile}");

        protected ChimeraCountingFile? _chimeraPsmFile;
        public ChimeraCountingFile ChimeraPsmFile => _chimeraPsmFile ??= CountChimericPsms();

        protected string _bulkResultCountComparisonPath => Path.Combine(DirectoryPath,
            $"{DatasetName}_{Condition}_{FileIdentifiers.BottomUpResultComparison}");

        protected BulkResultCountComparisonFile? _bulkResultCountComparisonFile;

        public BulkResultCountComparisonFile BulkResultCountComparisonFile =>
            _bulkResultCountComparisonFile ??= GetBulkResultCountComparisonFile();
        protected string _proformaPsmFilePath => Path.Combine(DirectoryPath, $"{DatasetName}_{Condition}_PSMs_{FileIdentifiers.ProformaFile}");
        protected ProformaFile? _proformaPsmFile;

        protected string _proteinCountingFilePath => Path.Combine(DirectoryPath, $"{DatasetName}_{Condition}_Proteins_{FileIdentifiers.ProteinCountingFile}");
        protected ProteinCountingFile? _proteinCountingFile;

        #region Base Sequence Only Filtering

        protected string _baseSeqIndividualFilePath => Path.Combine(DirectoryPath,
            $"{DatasetName}_{Condition}_BaseSeq_{FileIdentifiers.IndividualFileComparison}");

        protected BulkResultCountComparisonFile _baseSeqIndividualFileComparison;

        public virtual BulkResultCountComparisonFile BaseSeqIndividualFileComparisonFile =>
            _baseSeqIndividualFileComparison ??= GetIndividualFileComparison(_baseSeqIndividualFilePath);


        protected string _baseSeqBulkResultCountComparisonPath => Path.Combine(DirectoryPath,
            $"{DatasetName}_{Condition}_BaseSeq_{FileIdentifiers.BottomUpResultComparison}");

        protected BulkResultCountComparisonFile _baseSeqBulkResultCountComparisonFile;

        public BulkResultCountComparisonFile BaseSeqBulkResultCountComparisonFile =>
            _baseSeqBulkResultCountComparisonFile ??=
                GetBulkResultCountComparisonFile(_baseSeqBulkResultCountComparisonPath);

        #endregion

        protected SingleRunResults(string directoryPath, string? datasetName = null, string? condition = null)
        {
            DirectoryPath = directoryPath;
            FigureDirectory = Path.Combine(DirectoryPath, "Figures");
            if (!Directory.Exists(FigureDirectory))
                Directory.CreateDirectory(FigureDirectory);

            if (datasetName is null)
                if (DirectoryPath.Contains("Task"))
                    DatasetName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DirectoryPath))));
                else if (DirectoryPath.Contains("ChimericLibrary_") || DirectoryPath.Contains("ExternalMM"))
                    DatasetName = Path.GetFileName(Path.GetDirectoryName(DirectoryPath));
                else
                    DatasetName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(DirectoryPath)));
            else
                DatasetName = datasetName;

            if (condition is null)
                if (DirectoryPath.Contains("Task"))
                    Condition = Path.GetFileName(Path.GetDirectoryName(DirectoryPath)) + Path.GetFileName(DirectoryPath).Split('-')[1];
                else
                    Condition = Path.GetFileName(DirectoryPath);
            else
                Condition = condition;
        }

        public abstract BulkResultCountComparisonFile? GetIndividualFileComparison(string path = null);
        public abstract ChimeraCountingFile CountChimericPsms();
        public abstract BulkResultCountComparisonFile GetBulkResultCountComparisonFile(string path = null);
        public abstract ProformaFile ToPsmProformaFile();
        public abstract ProteinCountingFile CountProteins();
        public override string ToString()
        {
            return $"{DatasetName}_{Condition}";
        }

        public void Dispose()
        {
            _individualFileComparison = null;
            _chimeraPsmFile = null;
            _bulkResultCountComparisonFile = null;
            _proformaPsmFile = null;
            _proteinCountingFile = null;
        }
    }
}
