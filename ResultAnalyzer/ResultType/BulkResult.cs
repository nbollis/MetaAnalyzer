﻿using ResultAnalyzer.FileTypes.Internal;
using ResultAnalyzer.Util;

namespace ResultAnalyzer.ResultType
{
    public abstract class BulkResult
    {
        public string DirectoryPath { get; set; }
        public string DatasetName { get; set; }
        public string Condition { get; set; }
        public bool Override { get; set; } = false;

        public string _psmPath;
        public string _peptidePath;
        protected string _proteinPath;

        public bool IsTopDown = false;
        public string ResultType => IsTopDown ? "Proteoform" : "Peptide";

        protected string _IndividualFilePath => Path.Combine(DirectoryPath, $"{DatasetName}_{Condition}_{FileIdentifiers.IndividualFileComparison}");
        protected BulkResultCountComparisonFile _individualFileComparison;
        public BulkResultCountComparisonFile IndividualFileComparisonFile => _individualFileComparison ??= IndividualFileComparison();

        protected string _chimeraPsmPath => Path.Combine(DirectoryPath,
            $"{DatasetName}_{Condition}_PSM_{FileIdentifiers.ChimeraCountingFile}");
        protected ChimeraCountingFile _chimeraPsmFile;
        public ChimeraCountingFile ChimeraPsmFile => _chimeraPsmFile ??= CountChimericPsms();

        protected string _bulkResultCountComparisonPath => Path.Combine(DirectoryPath,
                       $"{DatasetName}_{Condition}_{FileIdentifiers.BottomUpResultComparison}");
        protected BulkResultCountComparisonFile _bulkResultCountComparisonFile;
        public BulkResultCountComparisonFile BulkResultCountComparisonFile => _bulkResultCountComparisonFile ??= GetBulkResultCountComparisonFile();

        #region Base Sequence Only Filtering

        protected string _baseSeqIndividualFilePath => Path.Combine(DirectoryPath, 
            $"{DatasetName}_{Condition}_BaseSeq_{FileIdentifiers.IndividualFileComparison}");
        protected BulkResultCountComparisonFile _baseSeqIndividualFileComparison;
        public virtual BulkResultCountComparisonFile BaseSeqIndividualFileComparisonFile => _baseSeqIndividualFileComparison ??= IndividualFileComparison(_baseSeqIndividualFilePath);


        protected string _baseSeqBulkResultCountComparisonPath => Path.Combine(DirectoryPath,
                       $"{DatasetName}_{Condition}_BaseSeq_{FileIdentifiers.BottomUpResultComparison}");
        protected BulkResultCountComparisonFile _baseSeqBulkResultCountComparisonFile;
        public BulkResultCountComparisonFile BaseSeqBulkResultCountComparisonFile => _baseSeqBulkResultCountComparisonFile ??= GetBulkResultCountComparisonFile(_baseSeqBulkResultCountComparisonPath);

        #endregion

        public BulkResult(string directoryPath)
        {
            DirectoryPath = directoryPath;
            if (DirectoryPath.Contains("Task"))
            {
                DatasetName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DirectoryPath))));
                Condition = Path.GetFileName(Path.GetDirectoryName(DirectoryPath)) + Path.GetFileName(DirectoryPath).Split('-')[1];
            }
            else
            {
                Condition = Path.GetFileName(DirectoryPath);
                DatasetName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(DirectoryPath)));
            }
        }

        public abstract BulkResultCountComparisonFile IndividualFileComparison(string path = null);
        public abstract ChimeraCountingFile CountChimericPsms();
        public abstract BulkResultCountComparisonFile GetBulkResultCountComparisonFile(string path = null);

        public override string ToString()
        {
            return $"{DatasetName}_{Condition}";
        }
    }
}