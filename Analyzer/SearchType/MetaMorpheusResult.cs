using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using Analyzer.FileTypes.Internal;
using Analyzer.Interfaces;
using Analyzer.Plotting.Util;
using Analyzer.Util;
using Chemistry;
using Easy.Common.Extensions;
using MassSpectrometry;
using Proteomics.ProteolyticDigestion;
using Proteomics.PSM;
using Proteomics.RetentionTimePrediction;
using Readers;
using ThermoFisher.CommonCore.Data.Business;

namespace Analyzer.SearchType
{
    public class MetaMorpheusResult : SingleRunResults, IChimeraBreakdownCompatible, IChimeraPeptideCounter, IDisposable, IMultiFilterChecker, IRetentionTimePredictionAnalysis
    {
        #region Results

        private List<PsmFromTsv>? allPsms;
        public List<PsmFromTsv> AllPsms => allPsms ??= SpectrumMatchTsvReader.ReadPsmTsv(PsmPath, out _);

        
        private List<PsmFromTsv>? allPeptides;
        public List<PsmFromTsv> AllPeptides => allPeptides ??= SpectrumMatchTsvReader.ReadPsmTsv(PeptidePath, out _);


        public List<MetaMorpheusIndividualFileResult> IndividualFileResults { get; set; }


        private string _searchResultsTextPath;
        public MetaMorpheusProseFile ProseFile { get; init; }

        #endregion

        public override BulkResultCountComparisonFile BaseSeqIndividualFileComparisonFile => _baseSeqIndividualFileComparison ??= CountIndividualFilesForFengChaoComparison();
        public string[] DataFilePaths { get; set; } // set by CellLineResults constructor
        public MetaMorpheusResult(string directoryPath, string? datasetName = null, string? condition = null) : base(directoryPath, datasetName, condition)
        {
            PsmPath = Directory.GetFiles(directoryPath, "*PSMs.psmtsv", SearchOption.AllDirectories).First();
            PeptidePath = Directory.GetFiles(directoryPath, "*Peptides.psmtsv", SearchOption.AllDirectories).FirstOrDefault();
            if (PeptidePath is null)
            {
                IsTopDown = true;
                PeptidePath = Directory.GetFiles(directoryPath, "*Proteoforms.psmtsv", SearchOption.AllDirectories).First();
                PeptidePath = Directory.GetFiles(directoryPath, "*Proteoforms.psmtsv", SearchOption.AllDirectories).First();
            }
            ProteinPath = Directory.GetFiles(directoryPath, "*ProteinGroups.tsv", SearchOption.AllDirectories).First();

            var searchDir =
                Directory.GetDirectories(directoryPath, "*SearchTask", SearchOption.AllDirectories).FirstOrDefault() ??
                directoryPath;
            _searchResultsTextPath = Directory.GetFiles(searchDir, "results.txt", SearchOption.AllDirectories).FirstOrDefault();
            _individualFileComparison = null;
            _chimeraPsmFile = null;
            var prosePath = Directory.GetFiles(searchDir, "*Prose.txt", SearchOption.AllDirectories).FirstOrDefault();
            if (prosePath is not null)
            {
                ProseFile = new MetaMorpheusProseFile(prosePath);
                DataFilePaths = ProseFile.SpectraFilePaths;
            }

            IndividualFileResults = new();
            var indFileDir = Directory.GetDirectories(DirectoryPath, "Individual File Results", SearchOption.AllDirectories);
            if (indFileDir.Length == 0) return;
            foreach (var individualFile in Directory.GetFiles(indFileDir.First(), "*tsv")
                         .Where(p => !p.Contains("Percolator") && !p.Contains("Quantified")).GroupBy(p =>
                             Path.GetFileNameWithoutExtension(p).Replace("-calib", "").Replace("-averaged", "")
                                 .Replace("_Peptides", "").Replace("_PSMs", "").Replace("_ProteinGroups", "")
                                 .Replace("_Proteoforms", ""))
                         .ToDictionary(p => p.Key, p => p.ToList()))
            {
                string psm = individualFile.Value.First(p => Path.GetFileNameWithoutExtension(p).Contains("PSM"));
                string peptide = individualFile.Value.First(p => Path.GetFileNameWithoutExtension(p).Contains("Peptide") || Path.GetFileNameWithoutExtension(p).Contains("Proteoform"));
                string? protein = individualFile.Value.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).Contains("Protein"));

                IndividualFileResults.Add(new MetaMorpheusIndividualFileResult(individualFile.Key, psm, peptide, protein));
            }

            
        }

        public override BulkResultCountComparisonFile? GetIndividualFileComparison(string path = null)
        {
            path ??= _IndividualFilePath;
            if (!Override && File.Exists(path))
                return new BulkResultCountComparisonFile(path);
            switch (IndividualFileResults.Count)
            {
                case 0 when File.Exists(path):
                    return new BulkResultCountComparisonFile(path);
                case 0:
                    return null;
            }


            var results = new List<BulkResultCountComparison>();
            foreach (var individualFile in IndividualFileResults)
            {
                var spectralmatches = individualFile.AllPsms.Where(p => p.DecoyContamTarget == "T").ToList();
                var peptides = individualFile.AllPeptides
                    .Where(p => p.DecoyContamTarget == "T")
                    .ToList();

                int count = 0;
                int onePercentCount = 0;

                if (individualFile.ProteinPath is not null)
                {
                    using (var sw = new StreamReader(File.OpenRead(individualFile.ProteinPath)))
                    {
                        var header = sw.ReadLine();
                        var headerSplit = header.Split('\t');
                        var qValueIndex = Array.IndexOf(headerSplit, "Protein QValue");


                        while (!sw.EndOfStream)
                        {
                            var line = sw.ReadLine();
                            var values = line.Split('\t');
                            count++;
                            if (double.Parse(values[qValueIndex]) <= 0.01)
                                onePercentCount++;
                        }
                    }
                }
                
                int psmCount = spectralmatches.Count;
                int onePercentPsmCount = spectralmatches.Count(p => p.PEP_QValue <= 0.01);
                int peptideCount = peptides.Count;
                int onePercentPeptideCount = peptides.Count(p => p.PEP_QValue <= 0.01);

                results.Add(new BulkResultCountComparison()
                {
                    DatasetName = DatasetName,
                    Condition = Condition,
                    FileName = individualFile.FileName,
                    PsmCount = psmCount,
                    PeptideCount = peptideCount,
                    ProteinGroupCount = count,
                    OnePercentPsmCount = onePercentPsmCount,
                    OnePercentPeptideCount = onePercentPeptideCount,
                    OnePercentProteinGroupCount = onePercentCount
                });
            }

            var bulkComparisonFile = new BulkResultCountComparisonFile(path)
            {
                Results = results
            };
            bulkComparisonFile.WriteResults(path);
            return bulkComparisonFile;
        }

        public override ChimeraCountingFile CountChimericPsms()
        {
            if (File.Exists(_chimeraPsmPath))
                return new ChimeraCountingFile(_chimeraPsmPath);

            var psms = AllPsms.Where(p => p.DecoyContamTarget == "T").ToList();
            var allPsmCounts = psms.ToChimeraGroupedDictionary()
                .ToDictionary(p => p.Key, p => p.Value.Count());
            var onePercentFdrPsmCounts = psms.Where(p => p.PEP_QValue <= 0.01)
                .ToChimeraGroupedDictionary()
                .ToDictionary(p => p.Key, p => p.Value.Count());
            var results = allPsmCounts.Keys.Select(count => new ChimeraCountingResult(count, allPsmCounts[count],
                onePercentFdrPsmCounts.TryGetValue(count, out var psmCount) ? psmCount : 0, DatasetName, Condition)).ToList();

            var chimeraCountingFile = new ChimeraCountingFile(_chimeraPsmPath) { Results = results };
            chimeraCountingFile.WriteResults(_chimeraPsmPath);
            return chimeraCountingFile;
        }

        private string _chimeraPeptidePath => Path.Combine(DirectoryPath, $"{DatasetName}_{Condition}_{ResultType}_{FileIdentifiers.ChimeraCountingFile}");
        private ChimeraCountingFile? _chimeraPeptideFile;
        public ChimeraCountingFile ChimeraPeptideFile => _chimeraPeptideFile ??= CountChimericPeptides();
        public ChimeraCountingFile CountChimericPeptides()
        {
            if (!Override && File.Exists(_chimeraPeptidePath))
                return new ChimeraCountingFile(_chimeraPeptidePath);

            var peptides = AllPeptides.Where(p => p.DecoyContamTarget == "T").ToList();
            var allPeptideCounts = peptides.ToChimeraGroupedDictionary()
                .ToDictionary(p => p.Key, p => p.Value.Count());
            var onePercentFdrPeptideCounts = peptides.Where(p => p.PEP_QValue <= 0.01)
                .ToChimeraGroupedDictionary()
                .ToDictionary(p => p.Key, p => p.Value.Count());
            var results = allPeptideCounts.Keys.Select(count => new ChimeraCountingResult(count, allPeptideCounts[count],
                               onePercentFdrPeptideCounts.TryGetValue(count, out var peptideCount) ? peptideCount : 0, DatasetName, Condition)).ToList();

            var chimeraCountingFile = new ChimeraCountingFile(_chimeraPeptidePath) { Results = results };
            chimeraCountingFile.WriteResults(_chimeraPeptidePath);
            return chimeraCountingFile;
        }
        public BulkResultCountComparisonFile CountIndividualFilesForFengChaoComparison()
        {
            if (!Override && File.Exists(_baseSeqIndividualFilePath))
                return new BulkResultCountComparisonFile(_baseSeqIndividualFilePath);

            var indFileDir =
                Directory.GetDirectories(DirectoryPath, "Individual File Results", SearchOption.AllDirectories);
            if (indFileDir.Length == 0)
                return null;
            
            var indFileDirectory = indFileDir.First();
                
            var fileNames = Directory.GetFiles(indFileDirectory, "*tsv");
            List<BulkResultCountComparison> results = new List<BulkResultCountComparison>();
            foreach (var individualFile in fileNames.GroupBy(p => Path.GetFileNameWithoutExtension(p).Split('-')[0])
                         .ToDictionary(p => p.Key, p => p.ToList()))
            {
                string psm = individualFile.Value.First(p => p.Contains("PSM"));
                string peptide = individualFile.Value.First(p => p.Contains("Peptide"));
                string protein = individualFile.Value.First(p => p.Contains("Protein"));

                var spectralmatches = AllPsms
                    .Where(p => p.DecoyContamTarget == "T").ToList();
                var peptides = SpectrumMatchTsvReader.ReadPsmTsv(peptide, out _)
                    .Where(p => p.DecoyContamTarget == "T")
                    .DistinctBy(p => p.BaseSeq).ToList();

                int count = 0;
                int onePercentCount = 0;
                using (var sw = new StreamReader(File.OpenRead(protein)))
                {
                    var header = sw.ReadLine();
                    var headerSplit = header.Split('\t');
                    var qValueIndex = Array.IndexOf(headerSplit, "Protein QValue");
                    

                    while (!sw.EndOfStream)
                    {
                        var line = sw.ReadLine();
                        var values = line.Split('\t');
                        count++;
                        if (double.Parse(values[qValueIndex]) <= 0.01)
                            onePercentCount++;
                    }
                }

                int psmCount = spectralmatches.Count;
                int onePercentPsmCount = spectralmatches.Count(p => p.PEP_QValue <= 0.01);
                int peptideCount = peptides.Count;
                int onePercentPeptideCount = peptides.Count(p => p.PEP_QValue <= 0.01);
                int onePercentPeptideCountQ = peptides.Count(p => p.QValue <= 0.01);

                results.Add( new BulkResultCountComparison()
                {
                    DatasetName = DatasetName,
                    Condition = Condition,
                    FileName = individualFile.Key,
                    PsmCount = psmCount,
                    PeptideCount = peptideCount,
                    ProteinGroupCount = count,
                    OnePercentPsmCount = onePercentPsmCount,
                    OnePercentPeptideCount = onePercentPeptideCountQ,
                    OnePercentProteinGroupCount = onePercentCount
                });
            }
            var bulkComparisonFile = new BulkResultCountComparisonFile(_baseSeqIndividualFilePath)
            {
                Results = results
            };
            bulkComparisonFile.WriteResults(_baseSeqIndividualFilePath);
            return bulkComparisonFile;
        }

        public override BulkResultCountComparisonFile GetBulkResultCountComparisonFile(string? path = null)
        {
            path ??= _bulkResultCountComparisonPath;
            if (!Override && File.Exists(path))
                return new BulkResultCountComparisonFile(path);

            var psms = path.Contains("BaseS") ?
                AllPsms.Where(p => p.DecoyContamTarget == "T").DistinctBy(p => p.BaseSeq).ToList() 
                : AllPsms.Where(p => p.DecoyContamTarget == "T").ToList();
            var peptides = path.Contains("BaseS") ?
                AllPeptides.Where(p => p.DecoyContamTarget == "T").DistinctBy(p => p.BaseSeq).ToList()
                : AllPeptides.Where(p => p.DecoyContamTarget == "T").ToList();

            int psmsCount = psms.Count;
            int peptidesCount = peptides.Count;
            int onePercentPsmCount = psms.Count(p => p.PEP_QValue <= 0.01);
            int onePercentPeptideCount = peptides.Count(p => p.PEP_QValue <= 0.01);
            int onePercentUnambiguousPsmCount = psms.Count(p => p.PEP_QValue <= 0.01 && p.AmbiguityLevel == "1");
            int onePercentUnambiguousPeptideCount = peptides.Count(p => p.PEP_QValue <= 0.01 && p.AmbiguityLevel == "1");


            int proteingCount = 0;
            int onePercentProteinCount = 0;

            using (var sw = new StreamReader(File.OpenRead(ProteinPath)))
            {
                var header = sw.ReadLine();
                var headerSplit = header.Split('\t');
                var qValueIndex = Array.IndexOf(headerSplit, "Protein QValue");
                int count = 0;
                int onePercentCount = 0;

                while (!sw.EndOfStream)
                {
                    var line = sw.ReadLine();
                    var values = line.Split('\t');
                    proteingCount++;
                    if (double.Parse(values[qValueIndex]) <= 0.01)
                        onePercentProteinCount++;
                }
            }

            var bulkResultCountComparison = new BulkResultCountComparison
            {
                DatasetName = DatasetName,
                Condition = Condition,
                FileName = "Combined",
                PsmCount = psmsCount,
                PeptideCount = peptidesCount,
                ProteinGroupCount = proteingCount,
                OnePercentPsmCount = onePercentPsmCount,
                OnePercentPeptideCount = onePercentPeptideCount,
                OnePercentProteinGroupCount = onePercentProteinCount,
                OnePercentUnambiguousPsmCount = onePercentUnambiguousPsmCount,
                OnePercentUnambiguousPeptideCount = onePercentUnambiguousPeptideCount
            };

            var bulkComparisonFile = new BulkResultCountComparisonFile(path)
            {
                Results = new List<BulkResultCountComparison> { bulkResultCountComparison }
            };
            bulkComparisonFile.WriteResults(path);
            return bulkComparisonFile;
        }

        private string _chimeraBreakDownPath => Path.Combine(DirectoryPath, $"{DatasetName}_{Condition}_{FileIdentifiers.ChimeraBreakdownComparison}");
        private ChimeraBreakdownFile? _chimeraBreakdownFile;
        public ChimeraBreakdownFile ChimeraBreakdownFile => _chimeraBreakdownFile ??= GetChimeraBreakdownFile();
        public ChimeraBreakdownFile GetChimeraBreakdownFile()
        {
            if (!Override && File.Exists(_chimeraBreakDownPath))
            {
                var breakdownFile = new ChimeraBreakdownFile(_chimeraBreakDownPath);
                if (breakdownFile.Any(p => p.PsmCharges.IsNotNullOrEmpty()))
                    return breakdownFile;
                AppendChargesAndMassesToBreakdownFile(breakdownFile);
                breakdownFile.WriteResults(_chimeraBreakDownPath);
                return breakdownFile;
            }

            bool useIsolation;
            List<ChimeraBreakdownRecord> chimeraBreakDownRecords = new();
            // PSMs or PrSMs
            foreach (var fileGroup in AllPsms
                         .Where(p => p.PEP_QValue <= 0.01)
                         .GroupBy(p => p.FileNameWithoutExtension))
            {
                useIsolation = true;
                MsDataFile dataFile = null;
                var dataFilePath = DataFilePaths.FirstOrDefault(p => p.Contains(fileGroup.Key, StringComparison.InvariantCultureIgnoreCase));
                if (dataFilePath == null)
                    useIsolation = false;
                else
                {
                    try
                    {
                        dataFile = MsDataFileReader.GetDataFile(dataFilePath);
                        dataFile.InitiateDynamicConnection();
                    }
                    catch
                    {
                        useIsolation = false;
                    }
                }
                foreach (var chimeraGroup in fileGroup.GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer)
                             .Select(p => p.ToArray()))
                {
                    var record = new ChimeraBreakdownRecord()
                    {
                        Dataset = DatasetName,
                        FileName = chimeraGroup.First().FileNameWithoutExtension.Replace("-calib", "").Replace("-averaged", ""),
                        Condition = Condition,
                        Ms2ScanNumber = chimeraGroup.First().Ms2ScanNumber,
                        Type = Util.ResultType.Psm,
                        IdsPerSpectra = chimeraGroup.Length,
                        TargetCount = chimeraGroup.Count(p => p.DecoyContamTarget == "T"),
                        DecoyCount = chimeraGroup.Count(p => p.DecoyContamTarget == "D"),
                        PsmCharges = chimeraGroup.Select(p => p.PrecursorCharge).ToArray(),
                        PsmMasses = chimeraGroup.Select(p => p.PrecursorMass).ToArray()
                    };

                    PsmFromTsv parent = null;
                    if (chimeraGroup.Length != 1)
                    {
                        PsmFromTsv[] orderedChimeras;
                        if (useIsolation) // use the precursor with the closest mz to the isolation mz
                        {
                            var ms2Scan = dataFile.GetOneBasedScanFromDynamicConnection(chimeraGroup.First().Ms2ScanNumber);
                            var isolationMz = ms2Scan.IsolationMz;
                            if (isolationMz is null) // if this fails, order by score
                                orderedChimeras = chimeraGroup
                                    .Where(p => p.DecoyContamTarget == "T")
                                    .OrderByDescending(p => p.Score)
                                    .ThenBy(p => Math.Abs(double.Parse(p.MassDiffDa)))
                                    .ToArray();
                            else
                                orderedChimeras = chimeraGroup
                                    .Where(p => p.DecoyContamTarget == "T")
                                    .OrderBy(p => Math.Abs(p.PrecursorMz - (double)isolationMz))
                                    .ThenByDescending(p => p.Score)
                                    .ToArray();
                            record.IsolationMz = isolationMz ?? -1;
                        }
                        else // use the precursor with the highest score
                        {
                            orderedChimeras = chimeraGroup
                                .Where(p => p.DecoyContamTarget == "T")
                                .OrderByDescending(p => p.Score)
                                .ThenBy(p => Math.Abs(double.Parse(p.MassDiffDa.Split('|')[0])))
                                .ToArray();
                        }

                        foreach (var chimericPsm in orderedChimeras)
                            if (parent is null)
                                parent = chimericPsm;
                            else if (parent.FullSequence == chimericPsm.FullSequence)
                                record.DuplicateCount++;
                            else if (parent.Accession == chimericPsm.Accession)
                                record.UniqueForms++;
                            else
                                record.UniqueProteins++;
                    }
                    chimeraBreakDownRecords.Add(record);
                }
                if (useIsolation)
                    dataFile.CloseDynamicConnection();
            }


            // Peptides or Proteoforms
            foreach (var fileGroup in AllPeptides
                         .Where(p => p.PEP_QValue <= 0.01)
                         .GroupBy(p => p.FileNameWithoutExtension))
            {
                useIsolation = true;
                MsDataFile dataFile = null;
                var dataFilePath = DataFilePaths.FirstOrDefault(p => p.Contains(fileGroup.Key));
                if (dataFilePath == null)
                    useIsolation = false;
                else
                {
                    try
                    {
                        dataFile = MsDataFileReader.GetDataFile(dataFilePath);
                        dataFile.InitiateDynamicConnection();
                    }
                    catch
                    {
                        useIsolation = false;
                    }
                }
                foreach (var chimeraGroup in fileGroup.GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer)
                             .Select(p => p.ToArray()))
                {
                    var record = new ChimeraBreakdownRecord()
                    {
                        Dataset = DatasetName,
                        FileName = chimeraGroup.First().FileNameWithoutExtension.Replace("-calib", "").Replace("-averaged", ""),
                        Condition = Condition,
                        Ms2ScanNumber = chimeraGroup.First().Ms2ScanNumber,
                        Type = Util.ResultType.Peptide,
                        IdsPerSpectra = chimeraGroup.Length,
                        TargetCount = chimeraGroup.Count(p => p.DecoyContamTarget == "T"),
                        DecoyCount = chimeraGroup.Count(p => p.DecoyContamTarget == "D"),
                        PeptideCharges = chimeraGroup.Select(p => p.PrecursorCharge).ToArray(),
                        PeptideMasses = chimeraGroup.Select(p => p.PrecursorMass).ToArray()
                    };

                    PsmFromTsv parent = null;
                    if (chimeraGroup.Length != 1)
                    {
                        PsmFromTsv[] orderedChimeras;
                        if (useIsolation) // use the precursor with the closest mz to the isolation mz
                        {
                            var ms2Scan = dataFile.GetOneBasedScanFromDynamicConnection(chimeraGroup.First().Ms2ScanNumber);
                            var isolationMz = ms2Scan.IsolationMz;
                            if (isolationMz is null) // if this fails, order by score
                                orderedChimeras = chimeraGroup
                                    .Where(p => p.DecoyContamTarget == "T")
                                    .OrderByDescending(p => p.Score)
                                    .ThenBy(p => Math.Abs(double.Parse(p.MassDiffDa))).ToArray();
                            else
                                orderedChimeras = chimeraGroup
                                    .Where(p => p.DecoyContamTarget == "T")
                                    .OrderBy(p => Math.Abs(p.PrecursorMz - (double)isolationMz))
                                    .ThenByDescending(p => p.Score).ToArray();
                            record.IsolationMz = isolationMz ?? -1;
                        }
                        else // use the precursor with the highest score
                        {
                            orderedChimeras = chimeraGroup
                                .Where(p => p.DecoyContamTarget == "T")
                                .OrderByDescending(p => p.Score)
                                .ThenBy(p => Math.Abs(double.Parse(p.MassDiffDa))).ToArray();
                        }

                        foreach (var chimericPsm in orderedChimeras)
                            if (parent is null)
                                parent = chimericPsm;
                            else if (parent.FullSequence == chimericPsm.FullSequence)
                                record.DuplicateCount++;
                            else if (parent.Accession == chimericPsm.Accession)
                                record.UniqueForms++;
                            else
                                record.UniqueProteins++;
                    }
                    chimeraBreakDownRecords.Add(record);
                }
                if (useIsolation)
                    dataFile.CloseDynamicConnection();
            }

            var file = new ChimeraBreakdownFile(_chimeraBreakDownPath) { Results = chimeraBreakDownRecords };
            file.WriteResults(_chimeraBreakDownPath);
            return file;
        }

        private void AppendChargesAndMassesToBreakdownFile(ChimeraBreakdownFile file)
        {
            var psms = AllPsms.Where(p => p.PEP_QValue <= 0.01)
                .GroupBy(p => p.FileNameWithoutExtension.Replace("-calib", "").Replace("-averaged", ""))
                .ToDictionary(p => p.Key, p => p.ToArray());
            var peptides = AllPeptides.Where(p => p.PEP_QValue <= 0.01)
                .GroupBy(p => p.FileNameWithoutExtension.Replace("-calib", "").Replace("-averaged", ""))
                .ToDictionary(p => p.Key, p => p.ToArray());
            foreach (var fileSpecificRecords in file.GroupBy(p => p.FileName))
            {
                if (!psms.TryGetValue(fileSpecificRecords.Key, out var fileSpecificPsms))
                    continue;
                if (!peptides.TryGetValue(fileSpecificRecords.Key, out var fileSpecificPeptides))
                    continue;

                foreach (var record in fileSpecificRecords)
                {
                    switch (record.Type)
                    {
                        case Util.ResultType.Psm:
                        {
                            var psm = fileSpecificPsms.Where(p =>p.Ms2ScanNumber == record.Ms2ScanNumber).ToArray();
                            record.PsmCharges = psm.Select(p => p.PrecursorCharge).ToArray();
                            record.PsmMasses = psm.Select(p => p.PrecursorMass).ToArray();
                            break;
                        }
                        case Util.ResultType.Peptide:
                        {
                            var peptide = fileSpecificPeptides.Where(p => p.Ms2ScanNumber == record.Ms2ScanNumber).ToArray();
                            record.PeptideCharges = peptide.Select(p => p.PrecursorCharge).ToArray();
                            record.PeptideMasses = peptide.Select(p => p.PrecursorMass).ToArray();
                            break;
                        }
                    }

                }
            }
        }

        #region Retention Time Predictions

        private string _retentionTimePredictionPath => Path.Combine(DirectoryPath, $"{DatasetName}_MM_{FileIdentifiers.RetentionTimePredictionReady}");
        private string _chronologerRunningFilePath => Path.Combine(DirectoryPath, $"{DatasetName}_{FileIdentifiers.ChronologerReadyFile}");
        private RetentionTimePredictionFile _retentionTimePredictionFile;

        public RetentionTimePredictionFile RetentionTimePredictionFile
        {
            get
            {
                if (File.Exists(_retentionTimePredictionPath))
                {
                    _retentionTimePredictionFile ??= new RetentionTimePredictionFile() { FilePath = _retentionTimePredictionPath };
                    return _retentionTimePredictionFile;
                }
                else
                {
                    CreateRetentionTimePredictionReadyFile();
                    _retentionTimePredictionFile ??= new RetentionTimePredictionFile() { FilePath = _retentionTimePredictionPath };
                    return _retentionTimePredictionFile;
                }
            }
        }

        public void CreateRetentionTimePredictionReadyFile()
        {
            string outpath = _retentionTimePredictionPath;
            if (File.Exists(outpath) || !DirectoryPath.Contains("MetaMorpheusWithLibrary"))
                return;
            var modDict = GlobalVariables.AllModsKnown.ToDictionary(p => p.IdWithMotif, p => p.MonoisotopicMass.Value);
            var peptides = AllPeptides
                .Where(p => p.DecoyContamTarget == "T" && p.PEP_QValue <= 0.01)
                .ToList();
            var calc = new SSRCalc3("SSRCalc 3.0 (300A)", SSRCalc3.Column.A300);
            List<RetentionTimePredictionEntry> retentionTimePredictions = new List<RetentionTimePredictionEntry>();
            foreach (var group in peptides.GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer))
            {
                bool isChimeric = group.Count() > 1;
                retentionTimePredictions.AddRange(group.Select(p =>
                    new RetentionTimePredictionEntry(p.FileNameWithoutExtension, p.Ms2ScanNumber, p.PrecursorScanNum,
                        p.RetentionTime.Value, p.BaseSeq, p.FullSequence, p.PeptideModSeq(modDict), p.QValue,
                        p.PEP_QValue, p.PEP, p.SpectralAngle ?? -1, isChimeric)
                    { SSRCalcPrediction = calc.ScoreSequence(new PeptideWithSetModifications(p.FullSequence.Split('|')[0], GlobalVariables.AllModsKnownDictionary)) }));
            }
            var retentionTimePredictionFile = new RetentionTimePredictionFile() { FilePath = outpath, Results = retentionTimePredictions };
            retentionTimePredictionFile.WriteResults(outpath);

            var chronologerReady = new RetentionTimePredictionFile() { FilePath = _chronologerRunningFilePath, Results = retentionTimePredictions.Where(p => p.PeptideModSeq != "").ToList() };
            chronologerReady.WriteResults(_chronologerRunningFilePath);
        }

        public void AppendChronologerPrediction()
        {
            var chronologerResultFile = Directory
                .GetFiles(DirectoryPath, FileIdentifiers.ChoronologerResults, SearchOption.AllDirectories)
                .First();


            foreach (var line in File.ReadAllLines(chronologerResultFile).Skip(1))
            {
                var split = line.Split('\t');
                var fileName = split[0];
                var scanNum = int.Parse(split[1]);
                var precursorScanNumber = int.Parse(split[2]);
                var fullSequence = split[6];
                var prediction = double.Parse(split.Last());
                var result = RetentionTimePredictionFile.Results.First(p => p.ScanNumber == scanNum && p.PrecursorScanNumber == precursorScanNumber && p.FileNameWithoutExtension == fileName && p.FullSequence == fullSequence);
                if (result.PeptideModSeq == "")
                    continue;
                if (result.ChronologerPrediction != 0)
                    continue;
                result.ChronologerPrediction = prediction;
            }

            RetentionTimePredictionFile.WriteResults(_retentionTimePredictionPath);
        }

        #endregion


        private string _bultResultCountingDifferentFilteringFilePath => Path.Combine(DirectoryPath, $"{DatasetName}_{Condition}_{FileIdentifiers.BulkResultComparisonMultipleFilters}");
        private BulkResultCountComparisonMultipleFilteringTypesFile? _bulkResultCountComparisonMultipleFilteringTypesFile;

        public BulkResultCountComparisonMultipleFilteringTypesFile BulkResultCountComparisonMultipleFilteringTypesFile =>
            _bulkResultCountComparisonMultipleFilteringTypesFile ??= GetBulkResultCountComparisonMultipleFilteringTypesFile();

        public BulkResultCountComparisonMultipleFilteringTypesFile GetBulkResultCountComparisonMultipleFilteringTypesFile()
        {
            if (!Override && File.Exists(_bultResultCountingDifferentFilteringFilePath))
                return new BulkResultCountComparisonMultipleFilteringTypesFile(_bultResultCountingDifferentFilteringFilePath);

            var psmCount = AllPsms.Count(p => p.DecoyContamTarget == "T");
            var psmCountDecoys = AllPsms.Count(p => p.DecoyContamTarget == "D");
            var psmCountQValue = AllPsms.Count(p => p is { DecoyContamTarget: "T", QValue: <= 0.01 });
            var psmCountDecoysQValue = AllPsms.Count(p => p is { DecoyContamTarget: "D", QValue: <= 0.01 });
            var psmCountPepQValue = AllPsms.Count(p => p is { DecoyContamTarget: "T", PEP_QValue: <= 0.01 });
            var psmCountDecoysPepQValue = AllPsms.Count(p => p is { DecoyContamTarget: "D", PEP_QValue: <= 0.01 });

            var peptideCount = AllPeptides.Count(p => p.DecoyContamTarget == "T");
            var peptideCountDecoys = AllPeptides.Count(p => p.DecoyContamTarget == "D");
            var peptideCountQValue = AllPeptides.Count(p => p is { DecoyContamTarget: "T", QValue: <= 0.01 });
            var peptideCountDecoysQValue = AllPeptides.Count(p => p is { DecoyContamTarget: "D", QValue: <= 0.01 });
            var peptideCountPepQValue = AllPeptides.Count(p => p is { DecoyContamTarget: "T", PEP_QValue: <= 0.01 });
            var peptideCountDecoysPepQValue = AllPeptides.Count(p => p is { DecoyContamTarget: "D", PEP_QValue: <= 0.01 });


            int proteinCount = 0;
            int proteinCountDecoy = 0;
            int proteinCountQValue = 0;
            int proteinCountQValueDecoy = 0;
            if (File.Exists(ProteinPath))
            {
                using (var sw = new StreamReader(File.OpenRead(ProteinPath)))
                {
                    var header = sw.ReadLine();
                    var headerSplit = header.Split('\t');
                    var qValueIndex = Array.IndexOf(headerSplit, "Protein QValue");
                    var targdecoyIndex = Array.IndexOf(headerSplit, "Protein Decoy/Contaminant/Target");

                    while (!sw.EndOfStream)
                    {
                        var line = sw.ReadLine();
                        var values = line.Split('\t');
                        var targetDecoy = values[targdecoyIndex];
                        if (targetDecoy == "T")
                            proteinCount++;
                        else if (targetDecoy == "D")
                            proteinCountDecoy++;

                        switch (double.Parse(values[qValueIndex]))
                        {
                            case <= 0.01 when targetDecoy == "T":
                                proteinCountQValue++;
                                break;
                            case <= 0.01:
                            {
                                if (targetDecoy == "D")
                                    proteinCountQValueDecoy++;
                                break;
                            }
                        }
                    }
                }
            }

            var resultText = File.ReadAllLines(_searchResultsTextPath);
            var psmsLine = resultText.First(p =>
                p.Contains("All target PSMs with", StringComparison.InvariantCultureIgnoreCase));
            int resultTextPsms = int.Parse(psmsLine.Split(':')[1].Trim());
            var proteoformLine = resultText.First(p =>
                p.Contains("All target proteoforms with", StringComparison.InvariantCultureIgnoreCase) 
                || p.Contains("All target peptides w", StringComparison.InvariantCultureIgnoreCase));
            int resultTextProteoforms = int.Parse(proteoformLine.Split(':')[1].Trim());
            var proteinLine = resultText.First(p =>
                p.Contains("All target protein groups with", StringComparison.InvariantCultureIgnoreCase));
            int resultTextProteins = int.Parse(proteinLine.Split(':')[1].Trim());

            var bulkResultCountComparisonMultipleFilteringTypesFile = new BulkResultCountComparisonMultipleFilteringTypesFile(_bultResultCountingDifferentFilteringFilePath)
            {
                Results = new List<BulkResultCountComparisonMultipleFilteringTypes>
                {
                    new BulkResultCountComparisonMultipleFilteringTypes()
                    {
                        DatasetName = DatasetName,
                        Condition = Condition,
                        PsmCount = psmCount,
                        PsmCountDecoys = psmCountDecoys,
                        PsmCount_QValue = psmCountQValue,
                        PsmCountDecoys_QValue = psmCountDecoysQValue,
                        PsmCount_PepQValue = psmCountPepQValue,
                        PsmCountDecoys_PepQValue = psmCountDecoysPepQValue,
                        ProteoformCount = peptideCount,
                        ProteoformCountDecoys = peptideCountDecoys,
                        ProteoformCount_QValue = peptideCountQValue,
                        ProteoformCountDecoys_QValue = peptideCountDecoysQValue,
                        ProteoformCount_PepQValue = peptideCountPepQValue,
                        ProteoformCountDecoys_PepQValue = peptideCountDecoysPepQValue,
                        ProteinGroupCount = proteinCount,
                        ProteinGroupCountDecoys = proteinCountDecoy,
                        ProteinGroupCount_QValue = proteinCountQValue,
                        ProteinGroupCountDecoys_QValue = proteinCountQValueDecoy,
                        PsmCount_ResultFile = resultTextPsms,
                        ProteoformCount_ResultFile = resultTextProteoforms,
                        ProteinGroupCount_ResultFile = resultTextProteins
                    }
                }
            };

            bulkResultCountComparisonMultipleFilteringTypesFile.WriteResults(_bultResultCountingDifferentFilteringFilePath);
            return bulkResultCountComparisonMultipleFilteringTypesFile;
        }

        private string _individualFileResultCountingDifferentFilteringFilePath => Path.Combine(DirectoryPath, $"{DatasetName}_{Condition}_{FileIdentifiers.IndividualFileComparisonMultipleFilters}");
        private BulkResultCountComparisonMultipleFilteringTypesFile? _individualFileResultCountingMultipleFilteringTypesFile;
        public BulkResultCountComparisonMultipleFilteringTypesFile IndividualFileResultCountingMultipleFilteringTypesFile =>
            _individualFileResultCountingMultipleFilteringTypesFile ??= GetIndividualFileResultCountingMultipleFilteringTypesFile();

        public BulkResultCountComparisonMultipleFilteringTypesFile GetIndividualFileResultCountingMultipleFilteringTypesFile()
        {
            if (!Override && File.Exists(_individualFileResultCountingDifferentFilteringFilePath))
                return new BulkResultCountComparisonMultipleFilteringTypesFile(_individualFileResultCountingDifferentFilteringFilePath);
            switch (IndividualFileResults.Count)
            {
                case 0 when File.Exists(_individualFileResultCountingDifferentFilteringFilePath):
                    return new BulkResultCountComparisonMultipleFilteringTypesFile(_individualFileResultCountingDifferentFilteringFilePath);
                case 0:
                    return null;
            }

            

            var results = new List<BulkResultCountComparisonMultipleFilteringTypes>();
            foreach (var individualFileResults in IndividualFileResults)
            {
                var psmCount = individualFileResults.AllPsms.Count(p => p.DecoyContamTarget == "T");
                var psmCountDecoys = individualFileResults.AllPsms.Count(p => p.DecoyContamTarget == "D");
                var psmCountQValue = individualFileResults.AllPsms.Count(p => p is { DecoyContamTarget: "T", QValue: <= 0.01 });
                var psmCountDecoysQValue = individualFileResults.AllPsms.Count(p => p is { DecoyContamTarget: "D", QValue: <= 0.01 });
                var psmCountPepQValue = individualFileResults.AllPsms.Count(p => p is { DecoyContamTarget: "T", PEP_QValue: <= 0.01 });
                var psmCountDecoysPepQValue = individualFileResults.AllPsms.Count(p => p is { DecoyContamTarget: "D", PEP_QValue: <= 0.01 });

                var peptideCount = individualFileResults.AllPeptides.Count(p => p.DecoyContamTarget == "T");
                var peptideCountDecoys = individualFileResults.AllPeptides.Count(p => p.DecoyContamTarget == "D");
                var peptideCountQValue = individualFileResults.AllPeptides.Count(p => p is { DecoyContamTarget: "T", QValue: <= 0.01 });
                var peptideCountDecoysQValue = individualFileResults.AllPeptides.Count(p => p is { DecoyContamTarget: "D", QValue: <= 0.01 });
                var peptideCountPepQValue = individualFileResults.AllPeptides.Count(p => p is { DecoyContamTarget: "T", PEP_QValue: <= 0.01 });
                var peptideCountDecoysPepQValue = individualFileResults.AllPeptides.Count(p => p is { DecoyContamTarget: "D", PEP_QValue: <= 0.01 });

                int proteinCount = 0;
                int proteinCountDecoy = 0;
                int proteinCountQValue = 0;
                int proteinCountQValueDecoy = 0;
                if (individualFileResults.ProteinPath is not null)
                {
                    using (var sw = new StreamReader(File.OpenRead(individualFileResults.ProteinPath)))
                    {
                        var header = sw.ReadLine();
                        var headerSplit = header.Split('\t');
                        var qValueIndex = Array.IndexOf(headerSplit, "Protein QValue");
                        var targdecoyIndex = Array.IndexOf(headerSplit, "Protein Decoy/Contaminant/Target");

                        while (!sw.EndOfStream)
                        {
                            var line = sw.ReadLine();
                            var values = line.Split('\t');
                            var targetDecoy = values[targdecoyIndex];
                            if (targetDecoy == "T")
                                proteinCount++;
                            else if (targetDecoy == "D")
                                proteinCountDecoy++;

                            switch (double.Parse(values[qValueIndex]))
                            {
                                case <= 0.01 when targetDecoy == "T":
                                    proteinCountQValue++;
                                    break;
                                case <= 0.01:
                                {
                                    if (targetDecoy == "D")
                                        proteinCountQValueDecoy++;
                                    break;
                                }
                            }
                        }
                    }
                }

                // parse result text
                bool finished = false;
                int resultTextProteins = 0;
                int resultTextPsms = 0;
                int resultTextProteoforms = 0;

                using (var sr = new StreamReader(File.OpenRead(individualFileResults.ResultsTextPath)))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (line.IsNullOrEmpty())
                            continue;

                        if (line!.Contains("target PSMs with", StringComparison.InvariantCultureIgnoreCase) 
                            && line.Contains(individualFileResults.FileName))
                        {
                            resultTextPsms = int.Parse(line.Split(':')[1].Trim());
                        }

                        if ((line.Contains("Target proteoforms with", StringComparison.InvariantCultureIgnoreCase) 
                             || line.Contains("Target peptides with", StringComparison.InvariantCultureIgnoreCase))
                            && line.Contains(individualFileResults.FileName))
                        {
                            resultTextProteoforms = int.Parse(line.Split(':')[1].Trim());
                        }

                        if (line.Contains("Target protein groups", StringComparison.InvariantCultureIgnoreCase) 
                            && line.Contains(individualFileResults.FileName))
                        {
                            resultTextProteins = int.Parse(line.Split(':')[1].Trim());
                        }

                        if (resultTextProteins != 0 && resultTextProteoforms != 0 && resultTextPsms != 0)
                            finished = true;

                        if (finished)
                            break;
                    }
                }

                var result = new BulkResultCountComparisonMultipleFilteringTypes()
                {
                    DatasetName = DatasetName,
                    Condition = Condition,
                    FileName = individualFileResults.FileName,
                    PsmCount = psmCount,
                    PsmCountDecoys = psmCountDecoys,
                    PsmCount_QValue = psmCountQValue,
                    PsmCountDecoys_QValue = psmCountDecoysQValue,
                    PsmCount_PepQValue = psmCountPepQValue,
                    PsmCountDecoys_PepQValue = psmCountDecoysPepQValue,
                    ProteoformCount = peptideCount,
                    ProteoformCountDecoys = peptideCountDecoys,
                    ProteoformCount_QValue = peptideCountQValue,
                    ProteoformCountDecoys_QValue = peptideCountDecoysQValue,
                    ProteoformCount_PepQValue = peptideCountPepQValue,
                    ProteoformCountDecoys_PepQValue = peptideCountDecoysPepQValue,
                    ProteinGroupCount = proteinCount,
                    ProteinGroupCountDecoys = proteinCountDecoy,
                    ProteinGroupCount_QValue = proteinCountQValue,
                    ProteinGroupCountDecoys_QValue = proteinCountQValueDecoy,
                    PsmCount_ResultFile = resultTextPsms,
                    ProteoformCount_ResultFile = resultTextProteoforms,
                    ProteinGroupCount_ResultFile = resultTextProteins
                };
                results.Add(result);
            }

            var bulkResultCountComparisonMultipleFilteringTypesFile = new BulkResultCountComparisonMultipleFilteringTypesFile(_individualFileResultCountingDifferentFilteringFilePath)
            {
                Results = results
            };

            bulkResultCountComparisonMultipleFilteringTypesFile.WriteResults(_individualFileResultCountingDifferentFilteringFilePath);
            return bulkResultCountComparisonMultipleFilteringTypesFile;
        }


        public string _chimericSpectrumSummaryFilePath => Path.Combine(DirectoryPath, $"{DatasetName}_{Condition}_{FileIdentifiers.ChimericSpectrumSummary}");
        private ChimericSpectrumSummaryFile? _chimericSpectrumSummaryFile;
        public ChimericSpectrumSummaryFile ChimericSpectrumSummaryFile => _chimericSpectrumSummaryFile ??= GetChimericSpectrumSummaryFile();

        public ChimericSpectrumSummaryFile GetChimericSpectrumSummaryFile()
        {
            if (!Override && File.Exists(_chimericSpectrumSummaryFilePath))
                return new ChimericSpectrumSummaryFile(_chimericSpectrumSummaryFilePath);

            List<string> massSpecFiles = Directory.GetFiles(
                Path.Combine(@"B:\RawSpectraFiles\Mann_11cell_lines", DatasetName, "CalibratedAveraged"), "*.mzML",
                SearchOption.AllDirectories).ToList();
            if (massSpecFiles.Count != 18)
                throw new ArgumentException("Not all raw files found");

            var chimericSpectra = new List<ChimericSpectrumSummary>();
            foreach (var individualFile in massSpecFiles)
            {
                MsDataFile dataFile = FileReader.ReadFile<MsDataFileToResultFileAdapter>(individualFile).LoadAllStaticData();
                string fileName = Path.GetFileNameWithoutExtension(dataFile.FilePath).ConvertFileName();
                MetaMorpheusIndividualFileResult? mmResult = IndividualFileResults.FirstOrDefault(p =>
                    p.FileName.Contains(Path.GetFileNameWithoutExtension(dataFile.FilePath).Replace("-calib", "")
                        .Replace("-averaged", ""))) ?? null;

                if (mmResult is null)
                    continue;

                var psmDictionaryByScanNumber = mmResult.AllPsms
                    .GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer)
                    .ToDictionary(p => p.Key.Ms2ScanNumber, p => p.OrderByDescending(p => p.Score)
                        .ThenBy(p => Math.Abs(double.Parse(p.MassDiffDa.Split('|')[0].Trim()))).ToArray());
                var peptideDictionaryByScanNumber = mmResult.AllPeptides
                    .GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer)
                    .ToDictionary(p => p.Key.Ms2ScanNumber, p => p.OrderByDescending(p => p.Score)
                        .ThenBy(p => Math.Abs(double.Parse(p.MassDiffDa.Split('|')[0].Trim()))).ToArray());
                var ms2ScanToDeconResultDictionary = psmDictionaryByScanNumber.SelectMany(p =>
                        p.Value.Select(m => (m.PrecursorScanNum, m.Ms2ScanNumber)))
                    .Distinct()
                    .ToDictionary(p => p.Ms2ScanNumber,
                        p =>
                        {
                            var ms1Scan = dataFile.GetOneBasedScan(p.PrecursorScanNum);
                            var ms2Scan = dataFile.GetOneBasedScan(p.Ms2ScanNumber);
                            var envelopes = Deconvoluter.Deconvolute(ms1Scan,
                                                               new ClassicDeconvolutionParameters(1, 40, 20, 3), ms2Scan.IsolationRange).ToArray();
                            var minXIndex = ms1Scan.MassSpectrum.GetClosestPeakIndex(ms2Scan.IsolationRange.Minimum);
                            var maxXIndex = ms1Scan.MassSpectrum.GetClosestPeakIndex(ms2Scan.IsolationRange.Maximum);
                            var sumOfIntensity = ms1Scan.MassSpectrum.YArray[minXIndex..maxXIndex].Sum();
                            return (envelopes, sumOfIntensity);
                        });


                foreach (var scan in dataFile.Scans)
                {
                    if (scan.MsnOrder is 1 or > 2)
                        continue;

                    var isolationRange = scan.IsolationRange;
                    if (isolationRange is null)
                        continue;

                    // Determine potential features for this scan
                    int possibleFeatureCount = 0;

                    // if we have chimericPsm identifications for this ms2 scan
                    if (psmDictionaryByScanNumber.TryGetValue(scan.OneBasedScanNumber, out PsmFromTsv[] psms))
                    {
                        PsmFromTsv? parent = null;
                        var ms1ScanInfo = ms2ScanToDeconResultDictionary[scan.OneBasedScanNumber];
                        foreach (var chimericPsm in psms.OrderBy(p => Math.Abs(p.PrecursorMz - scan.IsolationMz.Value)))
                        {
                            var envelope = ms1ScanInfo.envelopes.MinBy(p => Math.Abs(p.MonoisotopicMass - chimericPsm.PrecursorMass));
                            double fractionalIntensity = 0.0;
                            if (envelope is not null)
                                fractionalIntensity = envelope.TotalIntensity / ms1ScanInfo.sumOfIntensity;

                            var resultToWrite = new ChimericSpectrumSummary()
                            {
                                Dataset = DatasetName,
                                FileName = fileName,
                                Condition = Condition,
                                Type = Util.ResultType.Psm,
                                Ms2ScanNumber = scan.OneBasedScanNumber,
                                Ms1ScanNumber = scan.OneBasedPrecursorScanNumber.Value,
                                IsolationMz = scan.IsolationMz.Value,
                                PEP_QValue = chimericPsm.PEP_QValue,
                                PrecursorCharge = chimericPsm.PrecursorCharge,
                                PrecursorMass = chimericPsm.PrecursorMass,
                                PrecursorMz = chimericPsm.PrecursorMz,
                                IsDecoy = chimericPsm.DecoyContamTarget == "D",
                                IsChimeric = psms.Count(p => p.PEP_QValue <= 0.01) > 1,
                                PossibleFeatureCount = possibleFeatureCount,
                                IdPerSpectrum = psms.Length,
                                FractionalIntensity = fractionalIntensity
                            };

                            if (parent is null)
                            {
                                parent = chimericPsm;
                                resultToWrite.IsParent = true;
                            }
                            else if (parent.FullSequence == chimericPsm.FullSequence)
                            {
                                resultToWrite.IsDuplicate = true;
                            }
                            else if (parent.Accession == chimericPsm.Accession)
                            {
                                resultToWrite.IsUniqueForm = true;
                            }
                            else
                            {
                                resultToWrite.IsUniqueProtein = true;
                            }
                            chimericSpectra.Add(resultToWrite);
                        }
                    }
                    else
                    {
                        var psmResult = new ChimericSpectrumSummary()
                        {
                            Dataset = DatasetName,
                            FileName = fileName,
                            Condition = Condition,
                            Type = Util.ResultType.Psm,
                            Ms2ScanNumber = scan.OneBasedScanNumber,
                            Ms1ScanNumber = scan.OneBasedPrecursorScanNumber.Value,
                            IsolationMz = scan.IsolationMz.Value,
                            PossibleFeatureCount = possibleFeatureCount,
                        };
                        chimericSpectra.Add(psmResult);
                    }

                    // repeat for peptides/proteoforms
                    if (peptideDictionaryByScanNumber.TryGetValue(scan.OneBasedScanNumber, out PsmFromTsv[] peptides))
                    {
                        PsmFromTsv? parent = null;
                        var ms1ScanInfo = ms2ScanToDeconResultDictionary[scan.OneBasedScanNumber];
                        foreach (var chimericPeptide in peptides.OrderBy(p => Math.Abs(p.PrecursorMz - scan.IsolationMz.Value)))
                        {
                            var envelope = ms1ScanInfo.envelopes.MinBy(p => Math.Abs(p.MonoisotopicMass - chimericPeptide.PrecursorMass));
                            double fractionalIntensity = 0.0;
                            if (envelope is not null)
                                fractionalIntensity = envelope.TotalIntensity / ms1ScanInfo.sumOfIntensity;

                            var resultToWrite = new ChimericSpectrumSummary()
                            {
                                Dataset = DatasetName,
                                FileName = fileName,
                                Condition = Condition,
                                Type = Util.ResultType.Peptide,
                                Ms2ScanNumber = scan.OneBasedScanNumber,
                                Ms1ScanNumber = scan.OneBasedPrecursorScanNumber.Value,
                                IsolationMz = scan.IsolationMz.Value,
                                PEP_QValue = chimericPeptide.PEP_QValue,
                                PrecursorCharge = chimericPeptide.PrecursorCharge,
                                PrecursorMass = chimericPeptide.PrecursorMass,
                                PrecursorMz = chimericPeptide.PrecursorMz,
                                IsDecoy = chimericPeptide.DecoyContamTarget == "D",
                                IsChimeric = peptides.Count(p => p.PEP_QValue <= 0.01) > 1,
                                PossibleFeatureCount = possibleFeatureCount,
                                IdPerSpectrum = psms.Length,
                                FractionalIntensity = fractionalIntensity
                            };

                            if (parent is null)
                            {
                                parent = chimericPeptide;
                                resultToWrite.IsParent = true;
                            }
                            else if (parent.FullSequence == chimericPeptide.FullSequence)
                            {
                                resultToWrite.IsDuplicate = true;
                            }
                            else if (parent.Accession == chimericPeptide.Accession)
                            {
                                resultToWrite.IsUniqueForm = true;
                            }
                            else
                            {
                                resultToWrite.IsUniqueProtein = true;
                            }
                            chimericSpectra.Add(resultToWrite);
                        }
                    }
                    else
                    {
                        var psmResult = new ChimericSpectrumSummary()
                        {
                            Dataset = DatasetName,
                            FileName = fileName,
                            Condition = Condition,
                            Type = Util.ResultType.Peptide,
                            Ms2ScanNumber = scan.OneBasedScanNumber,
                            Ms1ScanNumber = scan.OneBasedPrecursorScanNumber.Value,
                            IsolationMz = scan.IsolationMz.Value,
                            PossibleFeatureCount = possibleFeatureCount,
                        };
                        chimericSpectra.Add(psmResult);
                    }
                }
            }


            var file = new ChimericSpectrumSummaryFile(_chimericSpectrumSummaryFilePath) { Results = chimericSpectra };
            file.WriteResults(_chimericSpectrumSummaryFilePath);
            return file;
        }





        public new void Dispose()
        {
            base.Dispose();
            _chimeraBreakdownFile = null;
            _chimeraPeptideFile = null;
        }
    }

    

    public static class Extensions
    {
        public static MsDataScan CloneWithNewPrecursor(this MsDataScan scan, double precursorMz, int precursorCharge,
            double precursorIntensity)
        {
            return new MsDataScan(scan.MassSpectrum, scan.OneBasedScanNumber, scan.MsnOrder, scan.IsCentroid,
                scan.Polarity, scan.RetentionTime, scan.ScanWindowRange, scan.ScanFilter, scan.MzAnalyzer,
                scan.TotalIonCurrent, scan.InjectionTime, scan.NoiseData, scan.NativeId, precursorMz,
                precursorCharge, precursorIntensity, scan.IsolationMz, scan.IsolationWidth,
                scan.DissociationType, scan.OneBasedPrecursorScanNumber, scan.SelectedIonMonoisotopicGuessMz,
                scan.HcdEnergy);
        }

        public static MsDataScan Clone(this MsDataScan scan)
        {
            return new MsDataScan(scan.MassSpectrum, scan.OneBasedScanNumber, scan.MsnOrder, scan.IsCentroid,
                scan.Polarity, scan.RetentionTime, scan.ScanWindowRange, scan.ScanFilter, scan.MzAnalyzer,
                scan.TotalIonCurrent, scan.InjectionTime, scan.NoiseData, scan.NativeId, scan.SelectedIonMZ,
                scan.SelectedIonChargeStateGuess, scan.SelectedIonIntensity, scan.IsolationMz, scan.IsolationWidth,
                scan.DissociationType, scan.OneBasedPrecursorScanNumber, scan.SelectedIonMonoisotopicGuessMz,
                scan.HcdEnergy);
        }



        public static string[] AcceptableMods = new[]
        {
            "Oxidation on M",
            "Acetylation on X",
            "Acetylation on K",
            "Phosphorylation on S",
            "Phosphorylation on T",
            "Phosphorylation on Y",
            "Succinylation on K",
            "Methylation on K",
            "Methylation on R",
            "Dimethylation on K",
            "Dimethylation on R",
            "Trimethylation on K",
            "Ammonia loss on N",
            "Deamidation on Q",
            "Carbamidomethyl on C",
            "Hydroxylation on M",
        };
        public static string PeptideModSeq(this PsmFromTsv psm, Dictionary<string, double> modDictionary)
        {
            // Regex pattern to match words in brackets
            string pattern = @"\[(.*?)\]";

            // Replace words in brackets with numerical values
            string output = Regex.Replace(psm.FullSequence.Split('|')[0], pattern, match =>
            {
                string[] parts = match.Groups[1].Value.Split(':');
                var mod = modDictionary.TryGetValue(parts[1], out double value);
                if (parts.Length == 2 && modDictionary.ContainsKey(parts[1]))
                {
                    if (AcceptableMods.Contains(parts[1]))
                    {
                        var symbol = modDictionary[parts[1]] > 0 ? "+" : "";
                        return $"[{symbol}{modDictionary[parts[1]]:N6}]";
                    }

                    return "-1";
                }
                return "-1";
            });

            return output.Contains("-1") ? "" : output;
        }
    }


    public class MetaMorpheusIndividualFileResult
    {
        public string FileName { get; }
        private string _psmPath;
        private string _peptidePath;
        public string? ProteinPath;
        public string ResultsTextPath;

        private List<PsmFromTsv>? _allPsms;
        public List<PsmFromTsv> AllPsms => _allPsms ??= SpectrumMatchTsvReader.ReadPsmTsv(_psmPath, out _);

        private List<PsmFromTsv>? _filteredPsms;
        public List<PsmFromTsv> FilteredPsms => _filteredPsms ??= AllPsms.Where(p => p is { DecoyContamTarget: "T", PEP_QValue: <= 0.01 }).ToList();

        private List<PsmFromTsv>? _allPeptides;
        public List<PsmFromTsv> AllPeptides => _allPeptides ??= SpectrumMatchTsvReader.ReadPsmTsv(_peptidePath, out _);

        public MetaMorpheusIndividualFileResult(string fileName, string psmPath, string peptidePath, string? proteinPath)
        {
            FileName = fileName;
            _psmPath = psmPath;
            _peptidePath = peptidePath;
            ProteinPath = proteinPath;

            var individualFileDirectory = Path.GetDirectoryName(psmPath);
            var searchResultsDirectory = Directory.GetParent(individualFileDirectory).FullName;
            ResultsTextPath = Directory.GetFiles(searchResultsDirectory).First(p => p.EndsWith("results.txt"));
        }
    }
}
