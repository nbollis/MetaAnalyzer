using System.Collections;
using System.Diagnostics;
using Analyzer.FileTypes.External;
using Analyzer.FileTypes.Internal;
using Analyzer.Interfaces;
using Analyzer.Plotting.Util;
using Analyzer.Util;
using Chemistry;
using Easy.Common.Extensions;
using MassSpectrometry;
using Plotting.Util;
using Proteomics.PSM;
using Readers;
using ResultAnalyzerUtil;
using Ms1Feature = Analyzer.FileTypes.External.Ms1Feature;
using Ms1FeatureFile = Analyzer.FileTypes.External.Ms1FeatureFile;

namespace Analyzer.SearchType;

public class CellLineResults : IEnumerable<SingleRunResults>, IDisposable
{
    public string DirectoryPath { get; set; }
    public bool Override { get; set; } = false;
    public string SearchResultsDirectoryPath { get; set; }
    public string CellLine { get; set; }
    public List<SingleRunResults> Results { get; set; }

    public SingleRunResults this[int index] => Results[index];
    public string DatasetName { get; set; }

    private string[] _dataFilePaths;

    public string FigureDirectory { get; }


    public CellLineResults(string directoryPath)
    {
        DirectoryPath = directoryPath;
        SearchResultsDirectoryPath = Path.Combine(DirectoryPath, "SearchResults"); /*directoryPath*/;
        CellLine = Path.GetFileName(DirectoryPath);
        Results = new List<SingleRunResults>();
        FigureDirectory = Path.Combine(DirectoryPath, "Figures");
        if (!Directory.Exists(FigureDirectory))
            Directory.CreateDirectory(FigureDirectory);

        if (directoryPath.Contains("TopDown") || directoryPath.Contains("PEP"))
        {
            var calibAveragedDir = Path.Combine(@"B:\RawSpectraFiles\JurkatTopDown\CalibratedAveraged");
            _dataFilePaths = Directory.GetFiles(calibAveragedDir, "*.mzML", SearchOption.AllDirectories);
        }
        else
        {
            var man11Directory = @"B:\RawSpectraFiles\Mann_11cell_lines";
            var cellLineDirectory = Directory.GetDirectories(man11Directory).First(p => p.Contains(CellLine));
            var caliAvgDirectory = Directory.GetDirectories(cellLineDirectory).First(p =>
                p.Contains("calibratedaveraged", StringComparison.InvariantCultureIgnoreCase));
            _dataFilePaths = Directory.GetFiles(caliAvgDirectory, "*.mzML", SearchOption.AllDirectories);
        }
        
        foreach (var directory in Directory.GetDirectories(SearchResultsDirectoryPath).Where(p => !p.Contains("maxquant") && !p.StartsWith("XX")))
        {
            if (Directory.GetFiles(directory, "meta.bin", SearchOption.AllDirectories).Any()
                && !Directory.GetFiles(directory, "combined_peptide.tsv").Any())
                continue; // fragger currently running
            if (Directory.GetFiles(directory, "*.psmtsv", SearchOption.AllDirectories).Any())
            {
                var files = Directory.GetFiles(directory, "*.psmtsv", SearchOption.AllDirectories);
                if (!files.Any(p => p.Contains("AllProteoforms") || p.Contains("AllPSMs")) && !files.Any(p => p.Contains("AllProteinGroups")))
                    continue;
                if (directory.Contains("Fragger") && Directory.GetDirectories(directory).Count(p => !p.Contains("Figures")) > 2)
                {
                    var directories = Directory.GetDirectories(directory);
                    foreach (var dir in directories.Where(p => !p.Contains("Figures")))
                    {
                        files = Directory.GetFiles(dir, "*.psmtsv", SearchOption.AllDirectories);
                        if (!files.Any(p => p.Contains("AllProteoforms") || p.Contains("AllPSMs")) &&
                            !files.Any(p => p.Contains("AllProteinGroups")))
                            continue;
                        if (dir.Contains("NoChimera"))
                            Results.Add(new MetaMorpheusResult(dir) { DataFilePaths = _dataFilePaths });
                        else if (dir.Contains("WithChimera"))
                            Results.Add(new MetaMorpheusResult(dir) { DataFilePaths = _dataFilePaths });
                        else
                            Debugger.Break();
                    }
                }
                else
                    Results.Add(new MetaMorpheusResult(directory) { DataFilePaths = _dataFilePaths });
            }
            else if (Directory.GetFiles(directory, "*IcTda.tsv", SearchOption.AllDirectories).Any())
            {
                if (Directory.GetFiles(directory, "*IcTda.tsv", SearchOption.AllDirectories).Count() is 20 or 10 or 43) // short circuit fi searching is not yet finishedes from parsing
                    Results.Add(new MsPathFinderTResults(directory));
            }
            else if (Directory.GetFiles(directory, "*.fp-manifest", SearchOption.AllDirectories).Any())
                Results.Add(new MsFraggerResult(directory));
            else if (Directory.GetFiles(directory, "*.tdReport").Any())
                if (Directory.GetFiles(directory, "*.txt").Length == 4)
                    Results.Add(new ProteomeDiscovererResult(directory));
        }
    }

    public CellLineResults(string directorypath, List<SingleRunResults> results)
    {
        DirectoryPath = directorypath;
        SearchResultsDirectoryPath = Path.Combine(DirectoryPath);
        CellLine = Path.GetFileName(DirectoryPath);
        Results = results;

        FigureDirectory = Path.Combine(DirectoryPath, "Figures");
        if (!Directory.Exists(FigureDirectory))
            Directory.CreateDirectory(FigureDirectory);
    }

    private string _chimeraCountingPath => Path.Combine(DirectoryPath, $"{CellLine}_PSM_{FileIdentifiers.ChimeraCountingFile}");
    private ChimeraCountingFile? _chimeraCountingFile;
    public ChimeraCountingFile ChimeraCountingFile => _chimeraCountingFile ??= CountChimericPsms();

    public ChimeraCountingFile CountChimericPsms()
    {
        if (!Override && File.Exists(_chimeraCountingPath))
        {
            var result = new ChimeraCountingFile(_chimeraCountingPath);
            if (result.Results.DistinctBy(p => p.Software).Count() == Results.Count)
                return result;
        }

        List<ChimeraCountingResult> results = new List<ChimeraCountingResult>();
        foreach (var result in Results)
        {
            results.AddRange(result.ChimeraPsmFile.Results);
        }

        var chimeraCountingFile = new ChimeraCountingFile(_chimeraCountingPath) { Results = results };
        chimeraCountingFile.WriteResults(_chimeraCountingPath);
        return chimeraCountingFile;
    }

    private string _chimeraPeptidePath => Path.Combine(DirectoryPath, $"{CellLine}_Peptide_{FileIdentifiers.ChimeraCountingFile}");
    private ChimeraCountingFile? _chimeraPeptideFile;
    public ChimeraCountingFile ChimeraPeptideFile => _chimeraPeptideFile ??= CountChimericPeptides();
    public ChimeraCountingFile CountChimericPeptides()
    {
        if (!Override && File.Exists(_chimeraPeptidePath))
        {
            var result = new ChimeraCountingFile(_chimeraPeptidePath);
            if (result.Results.DistinctBy(p => p.Software).Count() == Results.Count)
                return result;
        }

        List<ChimeraCountingResult> results = new List<ChimeraCountingResult>();
        foreach (var bulkResult in Results.Where(p => p is IChimeraPeptideCounter))
        {
            var result = (IChimeraPeptideCounter)bulkResult;
            results.AddRange(result.ChimeraPeptideFile.Results);
        }

        var chimeraPeptideFile = new ChimeraCountingFile(_chimeraPeptidePath) { Results = results };
        chimeraPeptideFile.WriteResults(_chimeraPeptidePath);
        return chimeraPeptideFile;
    }

    private string _chimeraBreakdownFilePath => Path.Combine(DirectoryPath, $"{CellLine}_{FileIdentifiers.ChimeraBreakdownComparison}");
    private ChimeraBreakdownFile? _chimeraBreakdownFile;
    public ChimeraBreakdownFile ChimeraBreakdownFile => _chimeraBreakdownFile ??= GetChimeraBreakdownFile();

    public ChimeraBreakdownFile GetChimeraBreakdownFile()
    {
        if (!Override && File.Exists(_chimeraBreakdownFilePath))
            return new ChimeraBreakdownFile(_chimeraBreakdownFilePath);
        
        List<ChimeraBreakdownRecord> results = new List<ChimeraBreakdownRecord>();
        string[]? selector;
        try
        {
            selector = this.GetSingleResultSelector();
        }
        catch
        {
            selector = null;
        }
        foreach (var singleRunResult in Results.Where(p => p is MetaMorpheusResult))
        {
            var result = (MetaMorpheusResult)singleRunResult;
            if (selector is not null && !selector.Contains(result.Condition))
                continue;
            results.AddRange(result.ChimeraBreakdownFile.Results);
        }

        var chimeraBreakdownFile = new ChimeraBreakdownFile(_chimeraBreakdownFilePath) { Results = results };
        chimeraBreakdownFile.WriteResults(_chimeraBreakdownFilePath);
        return chimeraBreakdownFile;
    }

    private string _bulkResultCountComparisonPath => Path.Combine(DirectoryPath, $"{CellLine}_{FileIdentifiers.BottomUpResultComparison}");
    private BulkResultCountComparisonFile? _bulkResultCountComparisonFile;
    public BulkResultCountComparisonFile BulkResultCountComparisonFile => _bulkResultCountComparisonFile ??= GetBulkResultCountComparisonFile();
    public BulkResultCountComparisonFile GetBulkResultCountComparisonFile()
    {
        if (!Override && File.Exists(_bulkResultCountComparisonPath))
        {
            var result = new BulkResultCountComparisonFile(_bulkResultCountComparisonPath);
            if (result.Results.DistinctBy(p => p.Condition).Count() == Results.Count)
                return result;
        }

        List<BulkResultCountComparison> results = new List<BulkResultCountComparison>();
        foreach (var result in Results)
        {
            results.AddRange(result.BulkResultCountComparisonFile.Results);
        }

        var bulkResultCountComparisonFile = new BulkResultCountComparisonFile(_bulkResultCountComparisonPath) { Results = results };
        bulkResultCountComparisonFile.WriteResults(_bulkResultCountComparisonPath);
        return bulkResultCountComparisonFile;
    }

    private string _individualFilePath => Path.Combine(DirectoryPath, $"{CellLine}_{FileIdentifiers.IndividualFileComparison}");
    private BulkResultCountComparisonFile? _individualFileComparison;
    public BulkResultCountComparisonFile IndividualFileComparisonFile => _individualFileComparison ??= GetIndividualFileComparison();
    public BulkResultCountComparisonFile GetIndividualFileComparison()
    {
        if (!Override && File.Exists(_individualFilePath))
        {
            var result = new BulkResultCountComparisonFile(_individualFilePath);
            if (result.Results.DistinctBy(p => p.Condition).Count() == Results.Count)
                return result;
        }

        List<BulkResultCountComparison> results = new List<BulkResultCountComparison>();
        foreach (var result in Results.Where(p => p.IndividualFileComparisonFile != null))
        {
            results.AddRange(result.IndividualFileComparisonFile.Results);
        }

        var individualFileComparison = new BulkResultCountComparisonFile(_individualFilePath) { Results = results };
        individualFileComparison.WriteResults(_individualFilePath);
        return individualFileComparison;
    }

    private string _baseSeqIndividualFilePath => Path.Combine(DirectoryPath, $"{CellLine}_BaseSeq_{FileIdentifiers.IndividualFileComparison}");
    private BulkResultCountComparisonFile? _baseSeqIndividualFileComparison;
    public BulkResultCountComparisonFile BaseSeqIndividualFileComparisonFile => _baseSeqIndividualFileComparison ??= IndividualFileComparisonBaseSeq();

    public BulkResultCountComparisonFile IndividualFileComparisonBaseSeq()
    {
        if (!Override && File.Exists(_baseSeqIndividualFilePath))
        {
            var result = new BulkResultCountComparisonFile(_baseSeqIndividualFilePath);
            if (result.Results.DistinctBy(p => p.Condition).Count() == Results.Count)
                return result;
        }

        List<BulkResultCountComparison> results = new List<BulkResultCountComparison>();
        foreach (var result in Results)
        {
            if (result.BaseSeqIndividualFileComparisonFile != null)
                results.AddRange(result.BaseSeqIndividualFileComparisonFile.Results);
        }

        var individualFileComparison = new BulkResultCountComparisonFile(_baseSeqIndividualFilePath) { Results = results };
        individualFileComparison.WriteResults(_baseSeqIndividualFilePath);
        return individualFileComparison;
    }

    private string _bultResultCountingDifferentFilteringFilePath => Path.Combine(DirectoryPath, $"{CellLine}_{FileIdentifiers.BulkResultComparisonMultipleFilters}");
    private BulkResultCountComparisonMultipleFilteringTypesFile? _bulkResultCountComparisonMultipleFilteringTypesFile;

    public BulkResultCountComparisonMultipleFilteringTypesFile BulkResultCountComparisonMultipleFilteringTypesFile =>
        _bulkResultCountComparisonMultipleFilteringTypesFile ??= GetBulkResultCountComparisonMultipleFilteringTypesFile();

    public BulkResultCountComparisonMultipleFilteringTypesFile GetBulkResultCountComparisonMultipleFilteringTypesFile()
    {
        if (!Override && File.Exists(_bultResultCountingDifferentFilteringFilePath))
        {
            var result = new BulkResultCountComparisonMultipleFilteringTypesFile(_bultResultCountingDifferentFilteringFilePath);
            if (result.Results.DistinctBy(p => p.Condition).Count() == Results.Count)
                return result;
        }

        List<BulkResultCountComparisonMultipleFilteringTypes> results = new List<BulkResultCountComparisonMultipleFilteringTypes>();
        foreach (var bulkResult in Results.Where(p => p is IMultiFilterChecker))
        {
            var result = (IMultiFilterChecker)bulkResult;
            results.AddRange(result.BulkResultCountComparisonMultipleFilteringTypesFile.Results);
        }

        var bulkResultCountComparisonFile = new BulkResultCountComparisonMultipleFilteringTypesFile(_bultResultCountingDifferentFilteringFilePath) { Results = results };
        bulkResultCountComparisonFile.WriteResults(_bultResultCountingDifferentFilteringFilePath);
        return bulkResultCountComparisonFile;
    }


    private string _individualFileResultCountingDifferentFilteringFilePath => Path.Combine(DirectoryPath, $"{CellLine}_{FileIdentifiers.IndividualFileComparisonMultipleFilters}");
    private BulkResultCountComparisonMultipleFilteringTypesFile? _individualFileResultCountingMultipleFilteringTypesFile;
    public BulkResultCountComparisonMultipleFilteringTypesFile IndividualFileResultCountingMultipleFilteringTypesFile =>
        _individualFileResultCountingMultipleFilteringTypesFile ??= GetIndividualFileResultCountingMultipleFilteringTypesFile();

    public BulkResultCountComparisonMultipleFilteringTypesFile GetIndividualFileResultCountingMultipleFilteringTypesFile()
    {
        if (!Override && File.Exists(_individualFileResultCountingDifferentFilteringFilePath))
        {
            var result = new BulkResultCountComparisonMultipleFilteringTypesFile(_individualFileResultCountingDifferentFilteringFilePath);
            if (result.Results.DistinctBy(p => p.Condition).Count() == Results.Count)
                return result;
        }

        List<BulkResultCountComparisonMultipleFilteringTypes> results = new List<BulkResultCountComparisonMultipleFilteringTypes>();
        foreach (var bulkResult in Results.Where(p => p is IMultiFilterChecker))
        {
            var result = (IMultiFilterChecker)bulkResult;
            if (result.IndividualFileResultCountingMultipleFilteringTypesFile is null)
                continue;
            results.AddRange(result.IndividualFileResultCountingMultipleFilteringTypesFile.Results);
        }

        var bulkResultCountComparisonFile = new BulkResultCountComparisonMultipleFilteringTypesFile(_individualFileResultCountingDifferentFilteringFilePath) { Results = results };
        bulkResultCountComparisonFile.WriteResults(_individualFileResultCountingDifferentFilteringFilePath);
        return bulkResultCountComparisonFile;
    }




    private string _maximumChimeraEstimateFilePath =>
        Path.Combine(DirectoryPath, $"{CellLine}_{FileIdentifiers.MaximumChimeraEstimate}");
    private MaximumChimeraEstimationFile? _maximumChimeraEstimationFile;
    public MaximumChimeraEstimationFile? MaximumChimeraEstimationFile => _maximumChimeraEstimationFile ??= GetMaximumChimeraEstimationFile();

    private string _maximumChimeraEstimateCalibAveragedFilePath =>
        Path.Combine(DirectoryPath, $"{CellLine}_{FileIdentifiers.MaximumChimeraEstimateCalibAveraged}");
    private MaximumChimeraEstimationFile? _maximumChimeraEstimationCalibAveragedFile;
    public MaximumChimeraEstimationFile? MaximumChimeraEstimationCalibAveragedFile => _maximumChimeraEstimationCalibAveragedFile ??= GetMaximumChimeraEstimationFile(false);

    public MaximumChimeraEstimationFile? GetMaximumChimeraEstimationFile(bool useRawFiles = true)
    {
        if (!Override)
        {
            switch (useRawFiles)
            {
                case true when File.Exists(_maximumChimeraEstimateFilePath):
                {
                    var result = new MaximumChimeraEstimationFile(_maximumChimeraEstimateFilePath);
                    return result.Results.Count > 0 ? result : null;
                }
                case false when File.Exists(_maximumChimeraEstimateCalibAveragedFilePath):
                {
                    var result = new MaximumChimeraEstimationFile(_maximumChimeraEstimateCalibAveragedFilePath);
                    return result.Results.Count > 0 ? result : null;
                }
            }
        }

        var deconDirectory = Directory.GetDirectories(DirectoryPath).FirstOrDefault(p => p.Contains("Decon"));
        if (deconDirectory is null)
            return null;

        string metaMorpheusCondition;
        string otherCondition;
        List<(string, string)> rawFileDeconFile = new List<(string, string)>();
        if (Results.First().IsTopDown)
        {
            return null;
        }
        else
        {
            List<string> massSpecFiles = useRawFiles
                ? Directory.GetFiles(Path.Combine(@"B:\RawSpectraFiles\Mann_11cell_lines", CellLine), "*.raw",
                    SearchOption.AllDirectories).ToList()
                : Directory.GetFiles(
                    Path.Combine(@"B:\RawSpectraFiles\Mann_11cell_lines", CellLine, "CalibratedAveraged"), "*.mzML",
                    SearchOption.AllDirectories).ToList();


            if (massSpecFiles.Count != 18)
                throw new Exception("Not all raw files found");

            string deconDir = useRawFiles ? "FlashDeconv" : "CalibAveragedFlashDeconv";
            if (!Directory.Exists(deconDir))
                return null;
            var deconFiles = Directory.GetFiles(Path.Combine(deconDirectory, deconDir), "*ms1.feature", SearchOption.AllDirectories);

            foreach (var deconFile in deconFiles)
            {
                var massSpecFile = massSpecFiles.FirstOrDefault(p =>
                    deconFile.Contains(
                        Path.GetFileNameWithoutExtension(p.Replace("-calib", "").Replace("-averaged", ""))));
                if (massSpecFile is null)
                    continue;
                rawFileDeconFile.Add((massSpecFile, deconFile));
            }

            metaMorpheusCondition = false.GetSingleResultSelector(CellLine).First();
            otherCondition = "ReviewdDatabaseNoPhospho_MsFraggerDDA+";
        }

        var mmRun = Results.First(p => p.Condition == metaMorpheusCondition) as MetaMorpheusResult;
        var fragRun = Results.First(p => p.Condition == otherCondition) as MsFraggerResult;


        List<(Ms1Feature, double Mz)> features = new();
        List<MaximumChimeraEstimation> results = new List<MaximumChimeraEstimation>();
        foreach (var deconRun in rawFileDeconFile)
        {
            Ms1FeatureFile deconFile = new Ms1FeatureFile(deconRun.Item2);
            MsDataFile dataFile = FileReader.ReadFile<MsDataFileToResultFileAdapter>(deconRun.Item1).LoadAllStaticData();
            string fileName = Path.GetFileNameWithoutExtension(dataFile.FilePath).ConvertFileName();
            MetaMorpheusIndividualFileResult? mmResult = mmRun?.IndividualFileResults.FirstOrDefault(p =>
                p.FileName.Contains(Path.GetFileNameWithoutExtension(dataFile.FilePath).Replace("-calib", "")
                    .Replace("-averaged", ""))) ?? null;
            MsFraggerIndividualFileResult? fragResult =
                fragRun?.IndividualFileResults.FirstOrDefault(p => p.DirectoryPath.Contains(fileName)) ?? null;

            if (mmResult is null && fragResult is null)
                continue;

            deconFile.ForEach(p =>
            {
                p.RetentionTimeBegin /= 60;
                p.RetentionTimeEnd /=60;
                p.RetentionTimeApex /=60;
            });

            foreach (var scan in dataFile.Scans)
            {
                if (scan.MsnOrder is 1 or > 2)
                    continue;

                var isolationRange = scan.IsolationRange;
                if (isolationRange is null)
                    continue;

                var result = new MaximumChimeraEstimation()
                {
                    CellLine = CellLine,
                    FileName = fileName,
                    Ms2ScanNumber = scan.OneBasedScanNumber
                };

                features.Clear();
                foreach (var rtMatchingFeature in deconFile.Where(feature => feature.RetentionTimeBegin <= scan.RetentionTime && feature.RetentionTimeEnd >= scan.RetentionTime))
                    for (int i = rtMatchingFeature.ChargeStateMin; i < rtMatchingFeature.ChargeStateMax; i++)
                        if (isolationRange.Contains(rtMatchingFeature.Mass.ToMz(i)))
                        {
                            result.PossibleFeatureCount++;
                            features.Add((rtMatchingFeature, rtMatchingFeature.Mass.ToMz(i)));
                        }
                if (result.PossibleFeatureCount == 0) continue;

                // Find RT of the precursor scan, if that fails, use the ms2 scan
                double retentionTime;
                if (scan.OneBasedPrecursorScanNumber is null)
                    retentionTime = scan.RetentionTime;
                else
                {
                    var precursorScan = dataFile.GetOneBasedScan(scan.OneBasedPrecursorScanNumber.Value);
                    retentionTime = precursorScan.RetentionTime;
                }

                if (mmResult is not null)
                {
                    var mmPsms = mmResult.AllPsms.Where(p => !p.IsDecoy() && p.Ms2ScanNumber == scan.OneBasedScanNumber)
                        .ToArray();
                    var mmPeps = mmResult.AllPeptides.Where(p => !p.IsDecoy() && p.Ms2ScanNumber == scan.OneBasedScanNumber)
                        .ToArray();


                    // find the feature which has the closest m/z to each mmResult. If it is within 50 ppm, then add that to the result.RetentionTimeShift
                    List<double> shifts = new();
                    List<double> onePercentShifts = new();
                    foreach (var psm in mmPsms)
                    {
                        // we know they are all targets due to above where statement
                        var feature = features.MinBy(p => Math.Abs(p.Item2 - psm.PrecursorMz));
                        if (!(Math.Abs((feature.Item2 - psm.PrecursorMz) / psm.PrecursorMz * 1e6) <= 50)) continue;
                        var shift = retentionTime - feature.Item1.RetentionTimeApex;
                        shifts.Add(shift);
                        if (psm.PEP_QValue <= 0.01)
                            onePercentShifts.Add(shift);
                    }

                    result.PsmCount_MetaMorpheus = mmPsms.Length;
                    result.OnePercentPsmCount_MetaMorpheus = mmPsms.Count(p => p.PEP_QValue <= 0.01);
                    result.RetentionTimeShift_MetaMorpheus_PSMs = shifts.ToArray();
                    result.OnePercentRetentionTimeShift_MetaMorpheus_PSMs = onePercentShifts.ToArray();

                    shifts = new();
                    onePercentShifts = new();
                    foreach (var pep in mmPeps)
                    {
                        var feature = features.MinBy(p => Math.Abs(p.Item2 - pep.PrecursorMz));
                        if (!(Math.Abs((feature.Item2 - pep.PrecursorMz) / pep.PrecursorMz * 1e6) <= 50)) continue;
                        var shift = retentionTime - feature.Item1.RetentionTimeApex;
                        shifts.Add(shift);
                        if (pep.PEP_QValue <= 0.01)
                            onePercentShifts.Add(shift);
                    }

                    result.PeptideCount_MetaMorpheus = mmPeps.Length;
                    result.OnePercentPeptideCount_MetaMorpheus = mmPeps.Count(p => p.PEP_QValue <= 0.01);
                    result.RetentionTimeShift_MetaMorpheus_Peptides = shifts.ToArray();
                    result.OnePercentRetentionTimeShift_MetaMorpheus_Peptides = onePercentShifts.ToArray();
                }

                if (fragResult is not null)
                {
                    var fraggerResults = fragResult.PsmFile.Where(p => p.OneBasedScanNumber == scan.OneBasedScanNumber )
                        .ToArray();
                    List<double> fraggerShifts = new();
                    List<double> onePercentFragShifts = new();
                    foreach (var frag in fraggerResults)
                    {
                        var feature = features.MinBy(p => Math.Abs(p.Item2 - frag.CalculatedMz));
                        if (!(Math.Abs((feature.Item2 - frag.CalculatedMz) / frag.CalculatedMz * 1e6) <= 50)) continue;
                        var shift = retentionTime - feature.Item1.RetentionTimeApex;
                        fraggerShifts.Add(shift);
                        if (frag.PeptideProphetProbability >= 0.99)
                            onePercentFragShifts.Add(shift);
                    }

                    result.PsmCount_Fragger = fraggerResults.Length;
                    result.OnePercentPsmCount_Fragger = fraggerResults.Count(p => p.PeptideProphetProbability >= 0.99);
                    result.RetentionTimeShift_Fragger_PSMs = fraggerShifts.ToArray();
                    result.OnePercentRetentionTimeShift_Fragger_PSMs = onePercentFragShifts.ToArray();
                }

                results.Add(result);
            }
        }

        string outPath = useRawFiles ? _maximumChimeraEstimateFilePath : _maximumChimeraEstimateCalibAveragedFilePath;
        var maxChimeraEstimationFile = new MaximumChimeraEstimationFile(outPath) { Results = results };
        maxChimeraEstimationFile.WriteResults(outPath);
        return maxChimeraEstimationFile;
    }



    public void Dispose()
    {
        foreach (var result in Results)
            result.Dispose();
        _chimeraBreakdownFile = null;
        _chimeraPeptideFile = null;
        _chimeraCountingFile = null;
        _individualFileComparison = null;
        _bulkResultCountComparisonFile = null;
        _baseSeqIndividualFileComparison = null;
    }

    public void FileComparisonDifferentTypes(string outPath)
    {
        var sw = new StreamWriter(outPath);
        sw.WriteLine("DatasetName,FileName,Condition,Peptides,Base Sequence,Full Sequence,1% Peptides, 1% Base Sequence, 1% Full Sequence, 1% No Chimeras");
        foreach (var result in Results)
        {
            int mmPeptides,
                mmPeptidesBaseSeq,
                mmPeptidesFullSeq,
                fraggerPeptides,
                fraggerPeptidesBaseSeq,
                fraggerPeptidesFullSeq,
                fraggerPeptidesOnePercent,
                fraggerPeptidesOnePercentBaseSeq,
                fraggerPeptidesOnePercentFullSeq;
            string file;

            if (result is MsFraggerResult frag)
            {
                foreach (var individualFile in frag.IndividualFileResults)
                {
                    file = Path.GetFileNameWithoutExtension((string)individualFile.PsmFile.First().FileNameWithoutExtension);
                    var peptides = individualFile.PeptideFile;
                    fraggerPeptides = peptides.Count();
                    fraggerPeptidesBaseSeq = peptides.DistinctBy(p => p.BaseSequence).Count();

                    fraggerPeptidesFullSeq = peptides.GroupBy(p => p,
                        CustomComparerExtensions.MsFraggerPeptideDistinctComparer).Count();

                    var onePercentPeptides = peptides.Where(p => p.Probability >= 0.99).ToList();
                    fraggerPeptidesOnePercent = onePercentPeptides.Count();
                    fraggerPeptidesOnePercentBaseSeq = onePercentPeptides.DistinctBy(p => p.BaseSequence).Count();
                    fraggerPeptidesOnePercentFullSeq = onePercentPeptides.GroupBy(p => p,
                        CustomComparerExtensions.MsFraggerPeptideDistinctComparer).Count();

                    sw.WriteLine(
                        $"MsFragger,{file},{frag.Condition},{fraggerPeptides},{fraggerPeptidesBaseSeq},{fraggerPeptidesFullSeq},{fraggerPeptidesOnePercent},{fraggerPeptidesOnePercentBaseSeq},{fraggerPeptidesOnePercentFullSeq}");
                }
            }
            else if (result is MetaMorpheusResult mm)
            {
                var indFileDir =
                    Directory.GetDirectories(mm.DirectoryPath, "Individual File Results", SearchOption.AllDirectories);
                if (indFileDir.Length == 0)
                    continue;

                var indFileDirectory = indFileDir.First();
                foreach (var peptideFile in Directory.GetFiles(indFileDirectory, "*Peptides.psmtsv"))
                {
                    var peptides = SpectrumMatchTsvReader.ReadPsmTsv(peptideFile, out _)
                        .Where(p => p.DecoyContamTarget == "T" && p.PEP_QValue <= 0.01);
                    file = peptides.First().FileNameWithoutExtension.Split('-')[0];
                    mmPeptides = peptides.Count();
                    mmPeptidesBaseSeq = peptides.DistinctBy(p => p.BaseSeq).Count();
                    mmPeptidesFullSeq = peptides.GroupBy(p => p.FullSequence).Count();
                    var mmNoChimeraCount = peptides.DistinctBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer).Count();
                    sw.WriteLine(
                        $"MetaMorpheus,{file},{mm.Condition},{mmPeptides},{mmPeptidesBaseSeq},{mmPeptidesFullSeq},{mmPeptides},{mmPeptidesBaseSeq},{mmPeptidesFullSeq},{mmNoChimeraCount}");
                }
            }
        }
        sw.Dispose();
    }

    public IEnumerator<SingleRunResults> GetEnumerator()
    {
        return Results.GetEnumerator();
    }

    public override string ToString() => CellLine;
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}