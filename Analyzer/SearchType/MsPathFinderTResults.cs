global using MsPathFinderTResultFile = Analyzer.FileTypes.External.MsPathFinderTResultFile;
global using MsPathFinderTResult = Analyzer.FileTypes.External.MsPathFinderTResult;
using System.Collections;
using Analyzer.FileTypes.External;
using Analyzer.FileTypes.Internal;
using Analyzer.Interfaces;
using Analyzer.Util;
using Chemistry;
using Easy.Common.Extensions;
using MassSpectrometry;
using Plotting.Util;
using Proteomics;
using Proteomics.ProteolyticDigestion;
using Readers;
using ResultAnalyzerUtil;
using UsefulProteomicsDatabases;
using MzLibUtil;

namespace Analyzer.SearchType
{
    public class MsPathFinderTResults : SingleRunResults, IEnumerable<MsPathFinderTIndividualFileResult>, 
        IChimeraBreakdownCompatible, IDisposable
    {
        private string _datasetInfoFilePath => Path.Combine(DirectoryPath, "DatasetInfoFile.tsv");
        private string _crossTabResultFilePath;

        private bool _runBulk =>
            Override ||
            !File.Exists(_bulkResultCountComparisonPath) ||
            (new BulkResultCountComparisonFile(_bulkResultCountComparisonPath).First().ProteinGroupCount == 0 && !_crossTabResultFilePath.IsNullOrEmpty());

        private MsPathFinderTCrossTabResultFile _crossTabResultFile;
        public MsPathFinderTCrossTabResultFile CrossTabResultFile => _crossTabResultFile ??= new MsPathFinderTCrossTabResultFile(_crossTabResultFilePath);
        private string _combinedTargetResultFilePath => Path.Combine(DirectoryPath, "CombinedTargetResults_IcTarget.tsv");
        private MsPathFinderTResultFile? _combinedTargetResults;
        public MsPathFinderTResultFile CombinedTargetResults => _combinedTargetResults ??= CombinePrSMFiles();
        public List<MsPathFinderTIndividualFileResult> IndividualFileResults { get; set; }
        public MsPathFinderTResults(string directoryPath) : base(directoryPath)
        {
            IsTopDown = true;
            IndividualFileResults = new List<MsPathFinderTIndividualFileResult>();

            // combined file if ProMexAlign was ran
            _crossTabResultFilePath = Directory.GetFiles(DirectoryPath).FirstOrDefault(p => p.Contains("crosstab.tsv")); 

            // sorting out the individual result files
            var files = Directory.GetFiles(DirectoryPath)
                .Where(p => !p.Contains(".txt") && !p.Contains(".png") && !p.Contains(".db") && !p.Contains("Dataset"))
                .GroupBy(p => string.Join("_", Path.GetFileNameWithoutExtension(
                    p.Replace("_IcDecoy", "").Replace("_IcTarget", "").Replace("_IcTda", ""))))
                .ToDictionary(p => p.Key, p => p.ToList());
            foreach (var resultFile in files.Where(p => p.Value.Count == 6))
            {
                var key = resultFile.Key;
                var decoyPath = resultFile.Value.First(p => p.Contains("Decoy"));
                var targetPath = resultFile.Value.First(p => p.Contains("Target"));
                var combinedPath = resultFile.Value.First(p => p.Contains("IcTda"));
                var rawFilePath = resultFile.Value.First(p => p.Contains(".pbf"));
                var paramsPath = resultFile.Value.First(p => p.Contains(".param"));
                var ftFilepath = resultFile.Value.First(p => p.Contains(".ms1ft"));

                IndividualFileResults.Add(new MsPathFinderTIndividualFileResult(decoyPath, targetPath, combinedPath, key, ftFilepath, paramsPath, rawFilePath));
            }
            // TODO: Add case for the with mods search where not all items will be in the same directory
            foreach (var resultFile in files.Where(p => p.Value.Count is (4 or 5)))
            {
                var key = resultFile.Key;
                var decoyPath = resultFile.Value.First(p => p.Contains("Decoy"));
                var targetPath = resultFile.Value.First(p => p.Contains("Target"));
                var combinedPath = resultFile.Value.First(p => p.Contains("IcTda"));
                var paramsPath = resultFile.Value.First(p => p.Contains(".param"));
                var rawFilePath = Directory.GetParent(directoryPath).GetDirectories("MsPathFinderT").First()
                    .GetFiles($"{key}.pbf").First().FullName;
                var ftPath = Directory.GetParent(directoryPath).GetDirectories("MsPathFinderT").First()
                    .GetFiles($"{key}.ms1ft").First().FullName;
                IndividualFileResults.Add(new MsPathFinderTIndividualFileResult(decoyPath, targetPath, combinedPath, key, ftPath, paramsPath, rawFilePath));
            }
        }

        /// <summary>
        /// Uses each individual target results file to count PrSMs, Proteins, and Proteoforms
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public override BulkResultCountComparisonFile GetIndividualFileComparison(string path = null)
        {
            if (!Override && File.Exists(_IndividualFilePath))
                return new BulkResultCountComparisonFile(_IndividualFilePath);

            var results = new List<BulkResultCountComparison>();
            foreach (var file in IndividualFileResults)
            {
                var psmCount = file.TargetResults.Results.Count;
                var onePercentPsmCount = file.TargetResults.FilteredResults.Count;
                


                var proteoformCount = file.TargetResults.GroupBy(p => p,
                    CustomComparerExtensions.MsPathFinderTDistinctProteoformComparer).Count();
                var onePercentProteoformCount = file.TargetResults.FilteredResults.GroupBy(p => p,
                    CustomComparerExtensions.MsPathFinderTDistinctProteoformComparer).Count();


                var proteinCount = file.TargetResults.GroupBy(p => p,
                    CustomComparerExtensions.MsPathFinderTDistinctProteinComparer).Count();
                var onePercentProteinCount = file.TargetResults.FilteredResults.GroupBy(p => p,
                    CustomComparerExtensions.MsPathFinderTDistinctProteinComparer).Count();

                results.Add(new BulkResultCountComparison()
                {
                    Condition = Condition,
                    DatasetName = DatasetName,
                    FileName = file.Name,
                    OnePercentPsmCount = onePercentPsmCount,
                    PsmCount = psmCount,
                    PeptideCount = proteoformCount,
                    OnePercentPeptideCount = onePercentProteoformCount,
                    ProteinGroupCount = proteinCount,
                    OnePercentProteinGroupCount = onePercentProteinCount
                });
            }

            var bulkComparisonFile = new BulkResultCountComparisonFile(_IndividualFilePath)
            {
                Results = results
            };
            bulkComparisonFile.WriteResults(_IndividualFilePath);
            return bulkComparisonFile;
        }

        public override ChimeraCountingFile CountChimericPsms()
        {
            if (!Override && File.Exists(_chimeraPsmPath))
                return new ChimeraCountingFile(_chimeraPsmPath);

            var prsms = CombinedTargetResults.GroupBy(p => p, CustomComparerExtensions.MsPathFinderTChimeraComparer)
                .GroupBy(m => m.Count()).ToDictionary(p => p.Key, p => p.Count());


            var filtered = CombinedTargetResults

                .Where(p => p.FileNameWithoutExtension.Contains("rep2"))

                .GroupBy(p => p, CustomComparerExtensions.MsPathFinderTChimeraComparer)
                .GroupBy(m => m.Count()).ToDictionary(p => p.Key, p => p.Count());

            var results = prsms.Keys.Select(count => new ChimeraCountingResult(count, prsms[count],
                filtered.TryGetValue(count, out var psmCount) ? psmCount : 0, DatasetName, Condition)).ToList();
            _chimeraPsmFile = new ChimeraCountingFile() { FilePath = _chimeraPsmPath, Results = results };
            _chimeraPsmFile.WriteResults(_chimeraPsmPath);
            return _chimeraPsmFile;
        }

        public override BulkResultCountComparisonFile GetBulkResultCountComparisonFile(string path = null)
        {
            if (!_runBulk)
                return new BulkResultCountComparisonFile(_bulkResultCountComparisonPath);
            
            int proteoformCount = 0;
            int onePercentProteoformCount = 0;
            int proteinCount = 0;
            int onePercentProteinCount = 0;
            if (!_crossTabResultFilePath.IsNullOrEmpty()) // if ProMexAlign was ran
            {
                proteoformCount = CrossTabResultFile.TargetResults.DistinctBy(p => p,
                        CustomComparerExtensions
                            .MsPathFinderTCrossTabDistinctProteoformComparer)
                    .Count();
                onePercentProteoformCount = CrossTabResultFile.FilteredTargetResults.DistinctBy(p => p,
                        CustomComparerExtensions
                            .MsPathFinderTCrossTabDistinctProteoformComparer)
                    .Count();

                proteinCount = CrossTabResultFile.TargetResults.SelectMany(p => p.ProteinAccession).Distinct().Count();
                onePercentProteinCount = CrossTabResultFile.FilteredTargetResults.SelectMany(p => p.ProteinAccession).Distinct().Count();
            }

            var result = new BulkResultCountComparison()
            {
                Condition = Condition,
                DatasetName = DatasetName,
                FileName = "Combined",
                OnePercentPsmCount = CombinedTargetResults.FilteredResults.Count,
                PsmCount = CombinedTargetResults.Results.Count,
                PeptideCount = proteoformCount,
                OnePercentPeptideCount = onePercentProteoformCount,
                ProteinGroupCount = proteinCount,
                OnePercentProteinGroupCount = onePercentProteinCount
            };

            var bulkComparisonFile = new BulkResultCountComparisonFile(_bulkResultCountComparisonPath)
            {
                Results = new List<BulkResultCountComparison> { result }
            };
            bulkComparisonFile.WriteResults(_bulkResultCountComparisonPath);
            return bulkComparisonFile;
        }

        public MsPathFinderTResultFile CombinePrSMFiles()
        {
            if (!Override && File.Exists(_combinedTargetResultFilePath))
                return new MsPathFinderTResultFile(_combinedTargetResultFilePath);

            var results = IndividualFileResults.SelectMany(p => p.TargetResults.Results).ToList();
            var file = new MsPathFinderTResultFile(_combinedTargetResultFilePath) { Results = results };
            file.WriteResults(_combinedTargetResultFilePath);
            return file;
        }

        public void CreateDatasetInfoFile()
        {
            if (File.Exists(_datasetInfoFilePath))
                return;
            using var sw = new StreamWriter(_datasetInfoFilePath);
            sw.WriteLine("Label\tRawFilePath\tMs1FtFilePath\tMsPathfinderIdFilePath");
            foreach (var individualFile in IndividualFileResults)
            {
                sw.WriteLine($"{individualFile.Name}\t{individualFile.PbfFilePath}\t{individualFile.Ms1FtFilePath}\t{individualFile.CombinedPath}");
            }
            sw.Dispose();
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
            var tolerance = new PpmTolerance(10);
            HashSet<MsPathFinderTResult> evaluated = new();

            // PrSMs
            foreach (var individualFileResult in IndividualFileResults)
            {
                useIsolation = true;
                MsDataFile dataFile = null;
                var dataFilePath = individualFileResult.PbfFilePath;
                if (dataFilePath is null)
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

                // PrSMs
                foreach (var chimeraGroup in individualFileResult.CombinedResults.FilteredResults.GroupBy(p => p,
                             CustomComparerExtensions.MsPathFinderTChimeraComparer)
                             .Select(p => p.ToArray()))
                {
                    var record = new ChimeraBreakdownRecord()
                    {
                        Dataset = DatasetName,
                        FileName = chimeraGroup.First().FileNameWithoutExtension,
                        Condition = Condition,
                        Ms2ScanNumber = chimeraGroup.First().OneBasedScanNumber,
                        Type = ResultAnalyzerUtil.ResultType.Psm,
                        IdsPerSpectra = chimeraGroup.Length,
                        TargetCount = chimeraGroup.Count(p => !p.IsDecoy),
                        DecoyCount = chimeraGroup.Count(p => p.IsDecoy),
                        PsmCharges = chimeraGroup.Select(p => p.Charge).ToArray(),
                        PsmMasses = chimeraGroup.Select(p => p.MonoisotopicMass).ToArray()
                    };

                    MsPathFinderTResult? parent = null;
                    if (chimeraGroup.Length != 1)
                    {
                        MsPathFinderTResult[] orderedChimeras;
                        if (useIsolation)
                        {
                            var ms2Scan =
                                dataFile.GetOneBasedScanFromDynamicConnection(chimeraGroup.First().OneBasedScanNumber);
                            var isolationMz = ms2Scan.IsolationMz;
                            if (isolationMz is null)
                                orderedChimeras = chimeraGroup.Where(p => !p.IsDecoy)
                                    .OrderBy(p => p.EValue)
                                    .ThenBy(p => p.Probability)
                                    .ToArray();
                            else 
                                orderedChimeras = chimeraGroup.Where(p => !p.IsDecoy)
                                    .OrderBy(p => Math.Abs(p.MostAbundantIsotopeMz - (double)isolationMz))
                                    .ThenBy(p => p.EValue)
                                    .ThenBy(p => p.Probability)
                                    .ToArray();
                            record.IsolationMz = isolationMz ?? -1;
                        }
                        else
                        {
                            orderedChimeras = chimeraGroup.Where(p => !p.IsDecoy)
                                .OrderBy(p => p.EValue)
                                .ThenBy(p => p.Probability)
                                .ToArray();
                        }


                        // determine composition of chimera group
                        evaluated.Clear();
                        for (int index = 0; index < orderedChimeras.Length; index++)
                        {
                            MsPathFinderTResult? chimera = orderedChimeras[index];

                            if (index == 0)
                                parent = chimera;
                            // Duplicated MS1 Signal
                            else if (evaluated.Any(assignedChimera => tolerance.Within(assignedChimera.MonoisotopicMass, chimera.MonoisotopicMass)
                                                                   && tolerance.Within(assignedChimera.MostAbundantIsotopeMz, chimera.MostAbundantIsotopeMz)))
                            {
                                record.ZeroSumShiftCount++;
                            }
                            // Missed Monoisotopic Error
                            else if (evaluated.Any(assigned =>
                            MonoisotopicSimilarityChecker.AreSameSpeciesByMonoisotopicError(assigned.MonoisotopicMass, chimera.MonoisotopicMass,
                                assigned.MostAbundantIsotopeMz, chimera.MostAbundantIsotopeMz, assigned.Charge, chimera.Charge, tolerance)))
                            {
                                record.MissedMonoCount++;
                            }
                            else if (evaluated.Any(p => p.FullSequence == chimera.FullSequence))
                                record.DuplicateCount++;
                            else if (parent.Accession == chimera.Accession)
                            {
                                record.UniqueForms++;
                            }
                            else
                            {
                                record.UniqueProteins++;
                            }
                            evaluated.Add(chimera);
                        }
                    }
                    chimeraBreakDownRecords.Add(record);
                }

                // unique proteoforms
                foreach (var chimeraGroup in individualFileResult.CombinedResults.FilteredResults.GroupBy(p => p,
                                 CustomComparerExtensions.MsPathFinderTDistinctProteoformComparer)
                             .Select(p => p.OrderBy(m => m.EValue).ThenByDescending(m => m.Probability).First())
                             .GroupBy(p => p, CustomComparerExtensions.MsPathFinderTChimeraComparer)
                             .Select(p => p.ToArray()))
                {
                    var record = new ChimeraBreakdownRecord()
                    {
                        Dataset = DatasetName,
                        FileName = chimeraGroup.First().FileNameWithoutExtension,
                        Condition = Condition,
                        Ms2ScanNumber = chimeraGroup.First().OneBasedScanNumber,
                        Type = ResultAnalyzerUtil.ResultType.Peptide,
                        IdsPerSpectra = chimeraGroup.Length,
                        TargetCount = chimeraGroup.Count(p => !p.IsDecoy),
                        DecoyCount = chimeraGroup.Count(p => p.IsDecoy),
                        PeptideCharges = chimeraGroup.Select(p => p.Charge).ToArray(),
                        PeptideMasses = chimeraGroup.Select(p => p.MonoisotopicMass).ToArray()
                    };

                    MsPathFinderTResult? parent = null;
                    if (chimeraGroup.Length != 1)
                    {
                        MsPathFinderTResult[] orderedChimeras;
                        if (useIsolation)
                        {
                            var ms2Scan =
                                dataFile.GetOneBasedScanFromDynamicConnection(chimeraGroup.First().OneBasedScanNumber);
                            var isolationMz = ms2Scan.IsolationMz;
                            if (isolationMz is null)
                                orderedChimeras = chimeraGroup.Where(p => !p.IsDecoy)
                                    .OrderBy(p => p.EValue)
                                    .ThenBy(p => p.Probability)
                                    .ToArray();
                            else
                                orderedChimeras = chimeraGroup.Where(p => !p.IsDecoy)
                                    .OrderBy(p => Math.Abs(p.MostAbundantIsotopeMz - (double)isolationMz))
                                    .ThenBy(p => p.EValue)
                                    .ThenBy(p => p.Probability)
                                    .ToArray();
                            record.IsolationMz = isolationMz ?? -1;
                        }
                        else
                        {
                            orderedChimeras = chimeraGroup.Where(p => !p.IsDecoy)
                                .OrderBy(p => p.EValue)
                                .ThenBy(p => p.Probability)
                                .ToArray();
                        }

                        evaluated.Clear();
                        for (int index = 0; index < orderedChimeras.Length; index++)
                        {
                            MsPathFinderTResult? chimera = orderedChimeras[index];

                            if (index == 0)
                                parent = chimera;
                            // Duplicated MS1 Signal
                            else if (evaluated.Any(assignedChimera => tolerance.Within(assignedChimera.MonoisotopicMass, chimera.MonoisotopicMass) 
                                                                   && tolerance.Within(assignedChimera.MostAbundantIsotopeMz, chimera.MostAbundantIsotopeMz)))
                            {
                                record.ZeroSumShiftCount++;
                            }
                            // Missed Monoisotopic Error
                            else if (evaluated.Any(assigned =>
                            MonoisotopicSimilarityChecker.AreSameSpeciesByMonoisotopicError(assigned.MonoisotopicMass, chimera.MonoisotopicMass,
                                assigned.MostAbundantIsotopeMz, chimera.MostAbundantIsotopeMz, assigned.Charge, chimera.Charge, tolerance)))
                            {
                                record.MissedMonoCount++;
                            }
                            else if (evaluated.Any(p => p.FullSequence == chimera.FullSequence))
                                record.DuplicateCount++;
                            else if (parent.Accession == chimera.Accession)
                            {
                                record.UniqueForms++;
                            }
                            else
                            {
                                record.UniqueProteins++;
                            }
                            evaluated.Add(chimera);
                        }
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
            var psms = IndividualFileResults.ToDictionary(p => p.Name, p => p.TargetResults.Results);
            var peptides = IndividualFileResults.ToDictionary(p => p.Name, p => p.TargetResults.Results);
            foreach (var fileSpecificRecords in file.GroupBy(p => p.FileName))
            {
                var fileSpecificPsms = psms[fileSpecificRecords.Key];
                var fileSpecificPeptides = peptides[fileSpecificRecords.Key];
                foreach (var record in fileSpecificRecords)
                {
                    switch (record.Type)
                    {
                        case ResultAnalyzerUtil.ResultType.Psm:
                        {
                            var psm = fileSpecificPsms.Where(p => p.OneBasedScanNumber == record.Ms2ScanNumber).ToArray();
                            record.PsmCharges = psm.Select(p => p.Charge).ToArray();
                            record.PsmMasses = psm.Select(p => p.MonoisotopicMass).ToArray();
                            break;
                        }
                        case ResultAnalyzerUtil.ResultType.Peptide:
                        {
                            var peptide = fileSpecificPeptides.Where(p => p.OneBasedScanNumber == record.Ms2ScanNumber).ToArray();
                            record.PeptideCharges = peptide.Select(p => p.Charge).ToArray();
                            record.PeptideMasses = peptide.Select(p => p.MonoisotopicMass).ToArray();
                            break;
                        }
                    }

                }
            }
        }

        public override ProformaFile ToPsmProformaFile()
        {
            if (File.Exists(_proformaPsmFilePath) && !Override)
                return _proformaPsmFile ??= new ProformaFile(_proformaPsmFilePath);
            string condition = Condition.ConvertConditionName();
            List<ProformaRecord> records = new();
            foreach (var file in IndividualFileResults)
            {
                string fileName = file.Name.ConvertFileName();
                foreach (var psm in file.TargetResults.Where(p => p.PassesConfidenceFilter))
                {
                    int modMass = 0;
                    if (psm.FullSequence.Contains('['))
                        modMass += new PeptideWithSetModifications(psm.FullSequence.Split('|')[0].Trim(),
                                GlobalVariables.AllModsKnownDictionary).AllModsOneIsNterminus
                            .Sum(p => (int)p.Value.MonoisotopicMass!.RoundedDouble(0)!);

                    var record = new ProformaRecord()
                    {
                        Condition = condition,
                        FileName = fileName,
                        BaseSequence = psm.BaseSequence,
                        FullSequence = psm.FullSequence,
                        ModificationMass = modMass,
                        PrecursorCharge = psm.Charge,
                        ProteinAccession = psm.ProteinAccession.Replace(';', '|'),
                        ScanNumber = psm.OneBasedScanNumber,
                    };

                    records.Add(record);
                }

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

            var psms = IndividualFileResults.SelectMany(p => p.TargetResults.Results.Where(m => m.PassesConfidenceFilter).Cast<ISpectralMatch>()).ToList();
            var records = ProteinCountingRecord.GetRecords(psms, proteins, Condition);
            var proteinCountingFile = new ProteinCountingFile(_proteinCountingFilePath) { Results = records };
            proteinCountingFile.WriteResults(_proteinCountingFilePath);
            return _proteinCountingFile = proteinCountingFile;

        }

        public IEnumerator<MsPathFinderTIndividualFileResult> GetEnumerator()
        {
            return IndividualFileResults.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public new void Dispose()
        {
            base.Dispose();
            _chimeraBreakdownFile = null;
        }
    }

    public class MsPathFinderTIndividualFileResult
    {
        public string Name { get; set; }
        private string _targetPath;
        private MsPathFinderTResultFile _targetResults;
        public MsPathFinderTResultFile TargetResults => _targetResults ??= new MsPathFinderTResultFile(_targetPath);

        private string _decoyPath;
        private MsPathFinderTResultFile _decoyResults;
        public MsPathFinderTResultFile DecoyResults => _decoyResults ??= new MsPathFinderTResultFile(_decoyPath);

        internal string CombinedPath;
        private MsPathFinderTResultFile _combinedResults;
        public MsPathFinderTResultFile CombinedResults => _combinedResults ??= new MsPathFinderTResultFile(CombinedPath);


        public string Ms1FtFilePath { get; set; }
        public string ParamPath { get; set; }
        public string PbfFilePath { get; set; }
        public string RawFilePath { get; set; }

        public MsPathFinderTIndividualFileResult(string decoyPath, string targetPath, string combinedPath, string name, string ms1FtFilePath, string paramPath, string pbfFilePath)
        {
            _decoyPath = decoyPath;
            _targetPath = targetPath;
            CombinedPath = combinedPath;
            Name = name;
            Ms1FtFilePath = ms1FtFilePath;
            ParamPath = paramPath;
            PbfFilePath = pbfFilePath;

            string rawFileDirPath = pbfFilePath.Contains("Ecoli") ?
                @"B:\RawSpectraFiles\Ecoli_SEC_CZE" :
                @"B:\RawSpectraFiles\JurkatTopDown";
            string rawPath = Path.Combine(rawFileDirPath, Path.GetFileNameWithoutExtension(pbfFilePath)+".raw");
            if (File.Exists(rawPath))
                RawFilePath = rawPath;
            else
                RawFilePath = pbfFilePath;
        }
    }
}
