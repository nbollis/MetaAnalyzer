using Analyzer.FileTypes.External;
using Analyzer.FileTypes.Internal;
using Analyzer.Util;
using MassSpectrometry;
using Readers;
using System.Linq;
using Analyzer.Interfaces;
using Chemistry;
using Easy.Common.Extensions;

namespace Analyzer.SearchType
{
    public class ProteomeDiscovererResult : BulkResult, IChimeraBreakdownCompatible, IDisposable
    {
        private ProteomeDiscovererPsmFile _psmFile;
        private ProteomeDiscovererProteoformFile _peptideFile;
        private ProteomeDiscovererProteinFile _proteinFile;
        private string _inputFilePath;
        private ProteomeDiscovererInputFileFile _inputFile;
        private Dictionary<string, string> _idToFileNameDictionary;

        public ProteomeDiscovererPsmFile PrsmFile => _psmFile ??= new ProteomeDiscovererPsmFile(PsmPath);
        public ProteomeDiscovererProteoformFile ProteoformFile => _peptideFile ??= new ProteomeDiscovererProteoformFile(PeptidePath);
        public ProteomeDiscovererProteinFile ProteinFile => _proteinFile ??= new ProteomeDiscovererProteinFile(ProteinPath);
        public ProteomeDiscovererInputFileFile InputFile => _inputFile ??= new ProteomeDiscovererInputFileFile(_inputFilePath);
        public Dictionary<string,string> IdToFileNameDictionary => _idToFileNameDictionary ??= InputFile.ToDictionary(p => p.FileID, p => Path.GetFileNameWithoutExtension(p.FileName));

        public ProteomeDiscovererResult(string directoryPath) : base(directoryPath)
        {
            var files = Directory.GetFiles(directoryPath);
            ProteinPath = files.First(p => p.Contains("Proteins"));
            _inputFilePath = files.First(p => p.Contains("Input"));
            if (files.Any(file => file.Contains("PrSMs")))
            {
                IsTopDown = true;
                PsmPath = files.First(p => p.Contains("PrSMs"));
                PeptidePath = files.First(p => p.Contains("Proteoforms"));
            }
            else if (files.Any(file => file.Contains("PSMs")))
            {
                PsmPath  = files.First(p => p.Contains("PSMs"));
                PeptidePath = files.First(p => p.Contains("PeptideGroups"));
            }
        }

        public override BulkResultCountComparisonFile GetIndividualFileComparison(string path = null)
        {
            if (!Override && File.Exists(_IndividualFilePath))
                return new BulkResultCountComparisonFile(_IndividualFilePath);

            // set up result dictionary
            var results = PrsmFile
                .Select(p => p.FileID).Distinct()
                .ToDictionary(fileID => fileID,
                    fileID => new BulkResultCountComparison()
                    {
                        DatasetName = DatasetName, 
                        Condition = Condition, 
                        FileName = IdToFileNameDictionary[fileID],
                    });

            
            // foreach psm, if the proteoform shares accession and mods, count it. If the protein shares accession, count it.
            foreach (var fileGroupedPrsms in PrsmFile.GroupBy(p => p.FileID))
            {
                results[fileGroupedPrsms.Key].PsmCount = fileGroupedPrsms.Count();
                results[fileGroupedPrsms.Key].OnePercentPsmCount = fileGroupedPrsms.Count(p => IsTopDown ? p.NegativeLogEValue >= 5 : p.QValue <= 0.01);

                List<ProteomeDiscovererProteoformRecord> fileSpecificProteoforms = new();
                List<ProteomeDiscovererProteinRecord> fileSpecificProteins = new();
                foreach (var prsm in fileGroupedPrsms)
                {
                    var proteoforms = ProteoformFile.Where(p => p.Equals(prsm))
                        .DistinctBy(p => (p.ProteinAccessions, p.Modifications.Length)).ToArray();
                    fileSpecificProteoforms.AddRange(proteoforms);

                    var proteins = ProteinFile.Where(p => p.Equals(prsm))
                        .DistinctBy(p => p.Accession).ToArray();
                    fileSpecificProteins.AddRange(proteins);
                }

                var uniqueProteoforms = fileSpecificProteoforms.Distinct().ToArray();
                var onePercentProteoforms = uniqueProteoforms.Count(p => p.QValue <= 0.01);
                var uniqueProteins = fileSpecificProteins.Distinct().ToArray();
                var onePercentProteins = uniqueProteins.Count(p => p.QValue <= 0.01);

                results[fileGroupedPrsms.Key].PeptideCount = uniqueProteoforms.Length;
                results[fileGroupedPrsms.Key].ProteinGroupCount = uniqueProteins.Length;
                results[fileGroupedPrsms.Key].OnePercentPeptideCount = onePercentProteoforms;
                results[fileGroupedPrsms.Key].OnePercentProteinGroupCount = onePercentProteins;
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
            if (!Override && File.Exists(_chimeraPsmPath))
                return new ChimeraCountingFile(_chimeraPsmPath);

            var allPsms = PrsmFile
                .GroupBy(p => p, CustomComparer<ProteomeDiscovererPsmRecord>.PSPDPrSMChimeraComparer)
                .GroupBy(p => p.Count())
                .ToDictionary(p => p.Key, p => p.Count());
            var filtered = PrsmFile
                .Where(p => IsTopDown ? p.NegativeLogEValue >= 5 : p.QValue <= 0.01)
                .GroupBy(p => p, CustomComparer<ProteomeDiscovererPsmRecord>.PSPDPrSMChimeraComparer)
                .GroupBy(p => p.Count())
                .ToDictionary(p => p.Key, p => p.Count());

            var results = allPsms.Keys.Select(count => new ChimeraCountingResult(count, allPsms[count],
                filtered.TryGetValue(count, out var psmCount) ? psmCount : 0, DatasetName, Condition)).ToList();
            _chimeraPsmFile = new ChimeraCountingFile() { FilePath = _chimeraPsmPath, Results = results };
            _chimeraPsmFile.WriteResults(_chimeraPsmPath);
            return _chimeraPsmFile;
        }

        public override BulkResultCountComparisonFile GetBulkResultCountComparisonFile(string path = null)
        {
            if (!Override && File.Exists(_bulkResultCountComparisonPath))
                return new BulkResultCountComparisonFile(_bulkResultCountComparisonPath);

            var psmCount = PrsmFile.Count();
            var proteoformCount = ProteoformFile.Count();
            var proteinCount = ProteinFile.Count();

            // TODO: Consider if this distinct comparer is necessary for the final results to be comparable
            var onePercentPsmCount = PrsmFile.FilteredResults.DistinctBy(p => p, CustomComparer<ProteomeDiscovererPsmRecord>.PSPDPrSMDistinctProteoformComparer)
                .Count(p => IsTopDown ? p.NegativeLogEValue >= 5 : p.QValue <= 0.01);
            var onePercentProteoformCount = ProteoformFile.Count(p => p.QValue <= 0.01);
            var onePercentProteinCount = ProteinFile.Count(p => p.QValue <= 0.01);

            var bulkResultCountComparison = new BulkResultCountComparison()
            {
                DatasetName = DatasetName,
                Condition = Condition,
                FileName = "Combined",
                PsmCount = psmCount,
                PeptideCount = proteoformCount,
                ProteinGroupCount = proteinCount,
                OnePercentPsmCount = onePercentPsmCount,
                OnePercentPeptideCount = onePercentProteoformCount,
                OnePercentProteinGroupCount = onePercentProteinCount
            };

            var bulkComparisonFile = new BulkResultCountComparisonFile(_bulkResultCountComparisonPath)
            {
                Results = new List<BulkResultCountComparison> { bulkResultCountComparison }
            };
            bulkComparisonFile.WriteResults(_bulkResultCountComparisonPath);
            return bulkComparisonFile;
        }

        private string _chimeraBreakDownPath => Path.Combine(DirectoryPath, $"{DatasetName}_{Condition}_{FileIdentifiers.ChimeraBreakdownComparison}");
        private ChimeraBreakdownFile? _chimeraBreakdownFile;
        public ChimeraBreakdownFile ChimeraBreakdownFile => _chimeraBreakdownFile ??= GetChimeraBreakdownFile();

        /// <summary>
        /// Breaks down the distribution of chimeras between targets, unique proteoforms, and unique proteins
        /// DOES NOT DO DECOYS - PSPD does not report them with enough information to determine where they came from
        /// DOES NOT DO PROTEOFORMS - PSPD does not tell which result file the proteoforms came from
        /// </summary>
        /// <returns></returns>
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

            bool useIsolation = false;
            List<ChimeraBreakdownRecord> chimeraBreakDownRecords = new();
            foreach (var fileGroup in PrsmFile.FilteredResults.GroupBy(p => p.FileID))
            {
                useIsolation = true;
                MsDataFile dataFile = null;
                var dataFilePath = InputFile.Select(p => p.FileName)
                    .FirstOrDefault(p => p.Contains(IdToFileNameDictionary[fileGroup.Key],
                        StringComparison.InvariantCultureIgnoreCase));
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

                //TODO: Consider if this distinct comparer is necessary
                foreach (var chimeraGroup in fileGroup
                             .DistinctBy(p => p, CustomComparer<ProteomeDiscovererPsmRecord>.PSPDPrSMDistinctProteoformComparer)
                             .GroupBy(p => p, CustomComparer<ProteomeDiscovererPsmRecord>.PSPDPrSMChimeraComparer)
                             .Select(p => p.ToArray()))
                {
                    var record = new ChimeraBreakdownRecord()
                    {
                        Dataset = DatasetName,
                        FileName = IdToFileNameDictionary[chimeraGroup.First().FileID],
                        Condition = Condition,
                        Ms2ScanNumber = int.Parse(chimeraGroup.First().Ms2ScanNumber.Split(';')[0].Trim()),
                        Type = Util.ResultType.Psm,
                        IdsPerSpectra = chimeraGroup.Length,
                        TargetCount = chimeraGroup.Count(),
                        DecoyCount = 0,
                        PsmCharges = chimeraGroup.Select(p => p.Charge).ToArray(),
                        PsmMasses = chimeraGroup.Select(p => p.PrecursorMass).ToArray()
                    };

                    ProteomeDiscovererPsmRecord parent = null;
                    if (chimeraGroup.Length != 1)
                    {
                        ProteomeDiscovererPsmRecord[] orderedChimeras;
                        if (useIsolation)
                        {
                            var ms2Scan = dataFile.GetOneBasedScanFromDynamicConnection(record.Ms2ScanNumber);
                            var isolationMz = ms2Scan?.IsolationMz ?? null;
                            if (isolationMz is null)
                                orderedChimeras = chimeraGroup.OrderByDescending(p => p.NegativeLogEValue)
                                    .ThenBy(p => p.DeltaMassDa)
                                    .ToArray();
                            else
                                orderedChimeras = chimeraGroup
                                    .OrderBy(p => p.Mz.ToMz(p.Charge) - (double)isolationMz)
                                    .ThenByDescending(p => p.NegativeLogEValue)
                                    .ToArray();
                            record.IsolationMz = isolationMz ?? -1;
                        }
                        else
                        {
                            orderedChimeras = chimeraGroup.OrderByDescending(p => p.NegativeLogEValue)
                                .ThenBy(p => p.DeltaMassDa)
                                .ToArray();
                        }

                        foreach (var chimericPsm in orderedChimeras)
                            if (parent is null)
                                parent = chimericPsm;
                            else if (parent.AnnotatedSequence == chimericPsm.AnnotatedSequence)
                                record.DuplicateCount++;
                            else if (parent.ProteinAccessions == chimericPsm.ProteinAccessions)
                                record.UniqueForms++;
                            else
                                record.UniqueProteins++;
                    }
                    chimeraBreakDownRecords.Add(record);
                }

                if (useIsolation && dataFile is not null)
                    dataFile.CloseDynamicConnection();
            }

            var file = new ChimeraBreakdownFile(_chimeraBreakDownPath) { Results = chimeraBreakDownRecords };
            file.WriteResults(_chimeraBreakDownPath);
            return file;
        }

        private void AppendChargesAndMassesToBreakdownFile(ChimeraBreakdownFile file)
        {
            var psms = PrsmFile.FilteredResults.GroupBy(p => IdToFileNameDictionary[p.FileID])
                .ToDictionary(p => p.Key, p => p.ToArray());
            foreach (var fileSpecificRecords in file.GroupBy(p => p.FileName))
            {
                var fileSpecificPsms = psms[fileSpecificRecords.Key];
                foreach (var record in fileSpecificRecords)
                {
                    if (record.Type == Util.ResultType.Psm)
                    {
                        var psm = fileSpecificPsms.Where(p => p.FragmentationScans == record.Ms2ScanNumber).ToArray();
                        record.PsmCharges = psm.Select(p => p.Charge).ToArray();
                        record.PsmMasses = psm.Select(p => p.PrecursorMass).ToArray();
                    }
                }
            }
        }

        public new void Dispose()
        {
            base.Dispose();
            _chimeraBreakdownFile = null;
        }
    }
}
