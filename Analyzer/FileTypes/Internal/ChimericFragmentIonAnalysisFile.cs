using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Readers;

namespace Analyzer.FileTypes.Internal;

public class ChimericFragmentIonAnalysisRecord
{
    public static CsvConfiguration CsvConfiguration = new(CultureInfo.InvariantCulture)
    {
        Delimiter = "\t",
        HasHeaderRecord = true,
        IgnoreBlankLines = true,
        TrimOptions = TrimOptions.Trim,
        BadDataFound = null,
        HeaderValidated = null,
        MissingFieldFound = null,
        ReadingExceptionOccurred = null,
        ShouldSkipRecord = _ => false,
    };

    public string FileNameWithoutExtension { get; set; }
    public double ScanNumber { get; set; }
    public double PrecursorScanNumber { get; set; }
    public int ProteoformCountInSpectrum { get; set; }
    public int ProteoformIndex { get; set; }
    public string BaseSequence { get; set; }
    public string FullSequence { get; set; }
    public double PEP_QValue { get; set; }
    public int TotalMatchedFragmentIons { get; set; }
    public int UniqueMatchedFragmentIons { get; set; }
    public int SharedMatchedFragmentIons { get; set; }
    public double UniqueMatchedFragmentFraction { get; set; }
    public bool ExcludedInternalIons { get; set; }

    public ChimericFragmentIonAnalysisRecord()
    {
    }

    public ChimericFragmentIonAnalysisRecord(PsmFromTsv psm, int proteoformCountInSpectrum, int proteoformIndex,
        int totalMatchedFragmentIons, int uniqueMatchedFragmentIons, bool excludedInternalIons)
    {
        FileNameWithoutExtension = psm.FileNameWithoutExtension;
        ScanNumber = psm.Ms2ScanNumber;
        PrecursorScanNumber = psm.PrecursorScanNum;
        ProteoformCountInSpectrum = proteoformCountInSpectrum;
        ProteoformIndex = proteoformIndex;
        BaseSequence = psm.BaseSequence;
        FullSequence = psm.FullSequence;
        PEP_QValue = psm.PEP_QValue;
        TotalMatchedFragmentIons = totalMatchedFragmentIons;
        UniqueMatchedFragmentIons = uniqueMatchedFragmentIons;
        SharedMatchedFragmentIons = totalMatchedFragmentIons - uniqueMatchedFragmentIons;
        UniqueMatchedFragmentFraction = totalMatchedFragmentIons == 0 ? 0 : (double)uniqueMatchedFragmentIons / totalMatchedFragmentIons;
        ExcludedInternalIons = excludedInternalIons;
    }
}

public class ChimericFragmentIonAnalysisFile : ResultFile<ChimericFragmentIonAnalysisRecord>, IResultFile
{
    public ChimericFragmentIonAnalysisFile(string filePath) : base(filePath, Software.MetaMorpheus)
    {
    }

    public ChimericFragmentIonAnalysisFile() : base()
    {
    }

    public override void LoadResults()
    {
        using var csv = new CsvReader(new StreamReader(FilePath), ChimericFragmentIonAnalysisRecord.CsvConfiguration);
        Results = csv.GetRecords<ChimericFragmentIonAnalysisRecord>().ToList();
    }

    public override void WriteResults(string outputPath)
    {
        using var csv = new CsvWriter(new StreamWriter(File.Create(outputPath)), ChimericFragmentIonAnalysisRecord.CsvConfiguration);
        csv.WriteHeader<ChimericFragmentIonAnalysisRecord>();
        foreach (var result in Results)
        {
            csv.NextRecord();
            csv.WriteRecord(result);
        }
    }

    public override SupportedFileType FileType { get; }
    public override Software Software { get; set; }
}
