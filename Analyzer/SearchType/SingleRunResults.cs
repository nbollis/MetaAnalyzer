using Analyzer.FileTypes.Internal;
using Analyzer.Interfaces;
using Analyzer.Util;

namespace Analyzer.SearchType
{
    public abstract class SingleRunResults : IChimeraPaperResults, IDisposable
    {
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

        public SingleRunResults(string directoryPath)
        {
            DirectoryPath = directoryPath;
            FigureDirectory = Path.Combine(DirectoryPath, "Figures");
            if (!Directory.Exists(FigureDirectory))
                Directory.CreateDirectory(FigureDirectory);
            if (DirectoryPath.Contains("Task"))
            {
                DatasetName =
                    Path.GetFileName(
                        Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DirectoryPath))));
                Condition = Path.GetFileName(Path.GetDirectoryName(DirectoryPath)) +
                            Path.GetFileName(DirectoryPath).Split('-')[1];
            }
            else
            {
                Condition = Path.GetFileName(DirectoryPath);
                DatasetName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(DirectoryPath)));
            }
        }

        public abstract BulkResultCountComparisonFile? GetIndividualFileComparison(string path = null);
        public abstract ChimeraCountingFile CountChimericPsms();
        public abstract BulkResultCountComparisonFile GetBulkResultCountComparisonFile(string path = null);
        

        public override string ToString()
        {
            return $"{DatasetName}_{Condition}";
        }

        public void Dispose()
        {
            _individualFileComparison = null;
            _chimeraPsmFile = null;
            _bulkResultCountComparisonFile = null;
        }
    }
}
