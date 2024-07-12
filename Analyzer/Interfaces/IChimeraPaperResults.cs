using Analyzer.FileTypes.Internal;

namespace Analyzer.Interfaces;

public interface IChimeraPaperResults
{
    string DirectoryPath { get;  }
    string DatasetName { get;  }
    string Condition { get; }
    string FigureDirectory { get; }
    bool IsTopDown { get; }
    BulkResultCountComparisonFile BulkResultCountComparisonFile { get; }
    ChimeraCountingFile ChimeraPsmFile { get; }
    BulkResultCountComparisonFile? IndividualFileComparisonFile { get; }

    BulkResultCountComparisonFile? GetIndividualFileComparison(string path = null);
    ChimeraCountingFile CountChimericPsms();
    BulkResultCountComparisonFile GetBulkResultCountComparisonFile(string path = null);
}

public interface IChimeraBreakdownCompatible : IChimeraPaperResults
{
    ChimeraBreakdownFile ChimeraBreakdownFile { get; }
    ChimeraBreakdownFile GetChimeraBreakdownFile();
}

public interface IChimeraPeptideCounter : IChimeraPaperResults
{
    public ChimeraCountingFile ChimeraPeptideFile { get; }
    ChimeraCountingFile CountChimericPeptides();
}

public interface IMultiFilterChecker : IChimeraPaperResults
{
    BulkResultCountComparisonMultipleFilteringTypesFile? BulkResultCountComparisonMultipleFilteringTypesFile { get; }
    BulkResultCountComparisonMultipleFilteringTypesFile GetBulkResultCountComparisonMultipleFilteringTypesFile();

    BulkResultCountComparisonMultipleFilteringTypesFile? IndividualFileResultCountingMultipleFilteringTypesFile { get; }
    BulkResultCountComparisonMultipleFilteringTypesFile GetIndividualFileResultCountingMultipleFilteringTypesFile();
}

public interface IRetentionTimePredictionAnalysis
{
    public string[] IndividualFilePeptidePaths { get; }
    public string CalibratedRetentionTimeFilePath { get; }
    public RetentionTimePredictionFile RetentionTimePredictionFile { get; }
    public RetentionTimePredictionFile CreateRetentionTimePredictionFile();
}