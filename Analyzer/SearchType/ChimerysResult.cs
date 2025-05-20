using Analyzer.FileTypes.External;
using Analyzer.FileTypes.Internal;
using Chemistry;
using Plotting.Util;
using Proteomics;
using UsefulProteomicsDatabases;

namespace Analyzer.SearchType;

public class ChimerysResult : SingleRunResults
{

    private ChimerysResultDirectory _chimerysResultDirectory;

    public ChimerysResultDirectory ChimerysResultDirectory
    {
        get => _chimerysResultDirectory;
        set
        {
            _chimerysResultDirectory = value;
            DirectoryPath = value.DirectoryPath;
        }
    }

    public ChimerysResult(string directoryPath, string? datasetName = null, string? condition = null) : base(directoryPath, datasetName, condition)
    {
        IsTopDown = false;
        ChimerysResultDirectory = new(directoryPath);
        PsmPath = ChimerysResultDirectory.PsmPath!;
        PeptidePath = ChimerysResultDirectory.PeptidePath!;
        ProteinPath = ChimerysResultDirectory.ProteinGroupPath!;
    }

    public override BulkResultCountComparisonFile? GetIndividualFileComparison(string path = null)
    {
        if (!Override && File.Exists(_IndividualFilePath))
            return new BulkResultCountComparisonFile(_IndividualFilePath);


        // set up result dictionary
        var results = ChimerysResultDirectory.PsmFile
            .Select(p => p.RawFileName.ConvertFileName()).Distinct()
            .ToDictionary(fileID => fileID,
                fileID => new BulkResultCountComparison()
                {
                    DatasetName = DatasetName,
                    Condition = Condition,
                    FileName = fileID,
                });

        var targetPeptides = ChimerysResultDirectory.PeptideFile!
            .Where(p => !p.IsDecoy)
            .SelectMany(p => p.ToLongFormat())
            .ToList();

        var targetProteinGroups = ChimerysResultDirectory.ProteinGroupFile!
            .Where(p => !p.IsDecoy)
            .SelectMany(p => p.ToLongFormat())
            .ToList();

        foreach (var fileGroupedPsms in ChimerysResultDirectory.PsmFile.GroupBy(p => p.RawFileName.ConvertFileName()))
        {
            var psms = fileGroupedPsms.Where(p => !p.IsDecoy).ToList();
            var onePercentPsms = psms.Count(p => p.PassesConfidenceFilter);
            var onePercentPEPPsms = psms.Count(p => p.SecondaryConfidenceMetric <= 0.01);

            var peptides = targetPeptides.Where(p => p.RawFileName.ConvertFileName() == fileGroupedPsms.Key).ToList();
            var onePercentPeptideCount = peptides.Count(p => p.QValue <= 0.01);
            var onePercentPEPPeptides = peptides.Count(p => p.Pep >= 0.01);

            var proteinGroups = targetProteinGroups.Where(p => p.RawFileName.ConvertFileName() == fileGroupedPsms.Key).ToList();
            var onePercentProteins = proteinGroups.Count(p => p.GlobalQValue <= 0.01);

            results[fileGroupedPsms.Key].PsmCount = psms.Count;
            results[fileGroupedPsms.Key].PeptideCount = peptides.Count;
            results[fileGroupedPsms.Key].ProteinGroupCount = proteinGroups.Count;
            results[fileGroupedPsms.Key].OnePercentPsmCount = onePercentPsms;
            results[fileGroupedPsms.Key].OnePercentPeptideCount = onePercentPeptideCount;
            results[fileGroupedPsms.Key].OnePercentProteinGroupCount = onePercentProteins;
            results[fileGroupedPsms.Key].OnePercentSecondary_PsmCount = onePercentPEPPsms;
            results[fileGroupedPsms.Key].OnePercentSecondary_PeptideCount = onePercentPEPPeptides;
        }

        var bulkResultComparisonFile = new BulkResultCountComparisonFile(_IndividualFilePath)
        {
            Results = results.Values.ToList()
        };
        bulkResultComparisonFile.WriteResults(_IndividualFilePath);
        return bulkResultComparisonFile;
    }

    public override ChimeraCountingFile CountChimericPsms()
    {
        if (File.Exists(_chimeraPsmPath))
            return new ChimeraCountingFile(_chimeraPsmPath);

        Dictionary<int, int> allPsmsCount = new();
        Dictionary<int, int> onePercentPsmCount = new();
        foreach (var individualFilePsms in ChimerysResultDirectory.PsmFile
            .GroupBy(p => p.RawFileName.ConvertFileName()))
        {
            var targets = individualFilePsms.Where(p => !p.IsDecoy).ToList();

            foreach (var chimeraGroup in targets.GroupBy(p => p.OneBasedScanNumber))
            {
                if (allPsmsCount.ContainsKey(chimeraGroup.Key))
                    allPsmsCount[chimeraGroup.Key] += chimeraGroup.Count();
                else
                    allPsmsCount.Add(chimeraGroup.Key, chimeraGroup.Count());
            }

            foreach (var onePercentChimeraGroup in targets.Where(p => p.PassesConfidenceFilter)
                .GroupBy(p => p.OneBasedScanNumber))
            {
                if (onePercentPsmCount.ContainsKey(onePercentChimeraGroup.Key))
                    onePercentPsmCount[onePercentChimeraGroup.Key] += onePercentChimeraGroup.Count();
                else
                    onePercentPsmCount.Add(onePercentChimeraGroup.Key, onePercentChimeraGroup.Count());
            }
        }


        var results = allPsmsCount.Keys.Select(count => new ChimeraCountingResult(count, allPsmsCount[count],
            onePercentPsmCount.GetValueOrDefault(count, 0), DatasetName, Condition)).ToList();

        var chimeraCountingFile = new ChimeraCountingFile(_chimeraPsmPath) { Results = results };
        chimeraCountingFile.WriteResults(_chimeraPsmPath);
        return chimeraCountingFile;
    }

    public override BulkResultCountComparisonFile GetBulkResultCountComparisonFile(string? path = null)
    {
        path ??= _bulkResultCountComparisonPath;
        if (!Override && File.Exists(_bulkResultCountComparisonPath))
            return new BulkResultCountComparisonFile(_bulkResultCountComparisonPath);

        var psms = ChimerysResultDirectory.PsmFile!.Where(p => !p.IsDecoy).ToList();
        var peptides = ChimerysResultDirectory.PeptideFile!.Where(p => !p.IsDecoy).ToList();
        var proteinGroups = ChimerysResultDirectory.ProteinGroupFile!.Where(p => !p.IsDecoy).ToList();

        int psmsCount = psms.Count;
        int peptidesCount = peptides.Count;
        int proteinsGroupCount = proteinGroups.Count;

        int onePercentPsmCount = psms.Count(p => p.PassesConfidenceFilter);
        int onePercentPeptideCount = peptides.Count(p => p.PassesConfidenceFilter);
        int onePercentProteinGroupCount = proteinGroups.Count(p => p.PassesConfidenceFilter);

        int onePercentSecondaryPsmCount = psms.Count(p => p.SecondaryConfidenceMetric <= 0.01);
        int onePercentSecondaryPeptideCount = peptides.Count(p => p.Pep >= 0.99);

        int onePercentUnambiguousPsmCount = psms.Count(p => p is { PassesConfidenceFilter: true, IsAmbiguous: false });
        int onePercentUnambiguousPeptideCount = peptides.Count(p => p is { PassesConfidenceFilter: true, IsAmbiguous: false });

        var bulkResultCountComparison = new BulkResultCountComparison
        {
            DatasetName = DatasetName,
            Condition = Condition,
            FileName = "Combined",
            PsmCount = psmsCount,
            PeptideCount = peptidesCount,
            ProteinGroupCount = proteinsGroupCount,
            OnePercentPsmCount = onePercentPsmCount,
            OnePercentPeptideCount = onePercentPeptideCount,
            OnePercentProteinGroupCount = onePercentProteinGroupCount,
            OnePercentUnambiguousPsmCount = onePercentUnambiguousPsmCount,
            OnePercentUnambiguousPeptideCount = onePercentUnambiguousPeptideCount,
            OnePercentSecondary_PsmCount = onePercentSecondaryPsmCount,
            OnePercentSecondary_PeptideCount = onePercentSecondaryPeptideCount
        };

        var bulkComparisonFile = new BulkResultCountComparisonFile(path)
        {
            Results = new List<BulkResultCountComparison> { bulkResultCountComparison }
        };
        bulkComparisonFile.WriteResults(path);
        return bulkComparisonFile;
    }

    public override ProformaFile ToPsmProformaFile()
    {
        if (File.Exists(_proformaPsmFilePath) && !Override)
            return _proformaPsmFile ??= new ProformaFile(_proformaPsmFilePath);
        string condition = Condition.ConvertConditionName();

        List<ProformaRecord> records = new();
        foreach (var psm in ChimerysResultDirectory.PsmFile!.Where(p => p is { IsDecoy: false, PassesConfidenceFilter: true }))
        {
            int modMass = 0;
            if (psm.ModifiedSequence != psm.BaseSequence)
            {
                modMass += psm.OneBasedModificationDictionary.Sum(p => (int)p.Value.MonoisotopicMass!.RoundedDouble(0)!);
            }

            var record = new ProformaRecord()
            {
                Condition = condition,
                FileName = psm.RawFileName.ConvertFileName(),
                BaseSequence = psm.BaseSequence,
                ModificationMass = modMass,
                PrecursorCharge = psm.PrecursorCharge,
                ProteinAccession = psm.ProteinAccession,
                ScanNumber = psm.OneBasedScanNumber,
                FullSequence = psm.FullSequence
            };

            records.Add(record);
        }

        var proformaFile = new ProformaFile(_proformaPsmFilePath) { Results = records };
        proformaFile.WriteResults(_proformaPsmFilePath);
        return _proformaPsmFile = proformaFile;
    }

    public override ProteinCountingFile CountProteins()
    {
        if (File.Exists(_proteinCountingFilePath) && !Override)
            return _proteinCountingFile ??= new ProteinCountingFile(_proteinCountingFilePath);


        string dbPath = @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\UP000005640_reviewed.fasta";
        List<Protein> proteins = ProteinDbLoader.LoadProteinFasta(dbPath, true, DecoyType.None, false, out _);

        var psms = ChimerysResultDirectory.PsmFile!.Where(m => m is { IsDecoy: false, PassesConfidenceFilter: true }).Cast<ISpectralMatch>().ToList();
        var records = ProteinCountingRecord.GetRecords(psms, proteins, Condition);
        var proteinCountingFile = new ProteinCountingFile(_proteinCountingFilePath) { Results = records };
        proteinCountingFile.WriteResults(_proteinCountingFilePath);
        return _proteinCountingFile = proteinCountingFile;
    }
}