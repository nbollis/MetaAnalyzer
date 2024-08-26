using Analyzer.FileTypes.External;
using Analyzer.FileTypes.Internal;
using Analyzer.Interfaces;
using Analyzer.Util;
using RetentionTimePrediction;

namespace Analyzer.SearchType
{
    public class MsFraggerResult : SingleRunResults, IChimeraPaperResults, IRetentionTimePredictionAnalysis
    {
        public List<MsFraggerIndividualFileResult> IndividualFileResults { get; set; }

        
        private MsFraggerPsmFile _psmFile;
        public MsFraggerPsmFile CombinedPsms => _psmFile ??= CombinePsmFiles();

        private MsFraggerPeptideFile _peptideFile;
        public MsFraggerPeptideFile CombinedPeptides => _peptideFile ??= new MsFraggerPeptideFile(PeptidePath);

        private MsFraggerProteinFile _proteinFile;
        public MsFraggerProteinFile CombinedProteins => _proteinFile ??= new MsFraggerProteinFile(ProteinPath);

        public MsFraggerResult(string directoryPath) : base(directoryPath)
        {
            PsmPath = Path.Combine(DirectoryPath, "Combined_psm.tsv");
            PeptidePath = Path.Combine(DirectoryPath, "combined_peptide.tsv");
            //_peptideBaseSeqPath = Path.Combine(DirectoryPath, "Combined_BaseSequence_peptide.tsv");
            ProteinPath = Path.Combine(DirectoryPath, "combined_protein.tsv");

            IndividualFileResults = new List<MsFraggerIndividualFileResult>();
            foreach (var directory in System.IO.Directory.GetDirectories(DirectoryPath)
                         .Where(p => !p.Contains("shepherd") && !p.Contains("meta") && !p.Contains("Figures")))
            {
                IndividualFileResults.Add(new MsFraggerIndividualFileResult(directory));
            }

            _individualFileComparison = null;
            _chimeraPsmFile = null;
        }

        /// <summary>
        /// Combine psm files by aggregating all individual psm files
        /// </summary>
        /// <returns></returns>
        public MsFraggerPsmFile CombinePsmFiles()
        {
            if (!Override && File.Exists(PsmPath))
                return new MsFraggerPsmFile(PsmPath);

            var results = new List<MsFraggerPsm>();
            foreach (var file in IndividualFileResults.Select(p => p.PsmFile))
            {
                file.LoadResults();
                results.AddRange(file.Results);
            }

            var combinedMsFraggerPsmFile = new MsFraggerPsmFile(PsmPath) { Results = results };
            combinedMsFraggerPsmFile.WriteResults(PsmPath);
            return combinedMsFraggerPsmFile;
        }

        /// <summary>
        /// Compare individual file results. If the file already exists, return it. Otherwise, create it.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public override BulkResultCountComparisonFile GetIndividualFileComparison(string path = null)
        {
            path ??= _IndividualFilePath;
            if (!Override && File.Exists(path))
                return new BulkResultCountComparisonFile(path);

            List<BulkResultCountComparison> bulkResultCountComparisonFiles = new List<BulkResultCountComparison>();
            foreach (var file in IndividualFileResults)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.PsmFile.First().FileNameWithoutExtension);
                string fileName = fileNameWithoutExtension!.Replace("interact-", "");

                var uniquePeptides = path.Contains("BaseS")
                    ? file.PeptideFile.Results.GroupBy(p => p.BaseSequence).Count()
                    : file.PeptideFile.Results.Count;
                var uniquePsms = file.PsmFile.Results.Count;

                var uniquePeptidesProb = path.Contains("BaseS")
                    ? file.PeptideFile.Results.GroupBy(p => p.BaseSequence)
                        .Select(p => p.MaxBy(m => m.Probability))
                        .Count()
                    : file.PeptideFile.Results.Count();
                var uniquePsmsProb = file.PsmFile.Results.Count(p => p.PeptideProphetProbability >= 0.99);

                var uniqueProteins = file.ProteinFile.Results.GroupBy(p => p.Accession).Count();
                var uniqueProteinsProb = file.ProteinFile.Results.Count(p => p.ProteinProbability >= 0.99);

                bulkResultCountComparisonFiles.Add(new BulkResultCountComparison
                {
                    DatasetName = DatasetName,
                    Condition = Condition,
                    FileName = fileName,
                    PsmCount = uniquePsms,
                    PeptideCount = uniquePeptides,
                    ProteinGroupCount = uniqueProteins,
                    OnePercentPsmCount = uniquePsmsProb,
                    OnePercentPeptideCount = uniquePeptidesProb,
                    OnePercentProteinGroupCount = uniqueProteinsProb
                });
            }
            
            var bulkComparisonFile = new BulkResultCountComparisonFile(path)
            {
                Results = bulkResultCountComparisonFiles
            };
            bulkComparisonFile.WriteResults(path);
            return bulkComparisonFile;
        }

        public override ChimeraCountingFile CountChimericPsms()
        {
            if (!Override && File.Exists(_chimeraPsmPath))
                return new ChimeraCountingFile(_chimeraPsmPath);

            var allPSms = CombinedPsms.Results
                .GroupBy(p => p, CustomComparer<MsFraggerPsm>.MsFraggerChimeraComparer)
                .GroupBy(m => m.Count())
                .ToDictionary(p => p.Key, p => p.Count());
            var filtered = CombinedPsms.Results
                .Where(p => p.PeptideProphetProbability >= 0.99)
                .GroupBy(p => p, CustomComparer<MsFraggerPsm>.MsFraggerChimeraComparer)
                .GroupBy(m => m.Count())
                .ToDictionary(p => p.Key, p => p.Count());

            var results = allPSms.Keys.Select(count => new ChimeraCountingResult(count, allPSms[count],
                filtered.TryGetValue(count, out var psmCount) ? psmCount : 0, DatasetName, Condition )).ToList();
            _chimeraPsmFile = new ChimeraCountingFile() { FilePath = _chimeraPsmPath, Results = results };
            _chimeraPsmFile.WriteResults(_chimeraPsmPath);
            return _chimeraPsmFile;
        }

        public override BulkResultCountComparisonFile GetBulkResultCountComparisonFile(string path = null)
        {
            path ??= _bulkResultCountComparisonPath;
            if (!Override && File.Exists(path))
                return new BulkResultCountComparisonFile(path);

            var peptideFile = /*path.Contains("BaseS") ? CombinedPeptideBaseSeq :*/ CombinedPeptides;
            var psmsCount = CombinedPsms.Results.Count;
            var peptidesCount = peptideFile.Results.Count;

            var psmsProbCount = CombinedPsms.Results.Count(p => p.PeptideProphetProbability > 0.99);
            var peptidesProbCount = peptideFile.Results.Count(p => p.Probability > 0.99);

            int proteinCount;
            int onePercentProteinCount;
            using (var sr = new StreamReader(ProteinPath))
            {
                var header = sr.ReadLine();
                var headerSplit = header.Split('\t');
                var qValueIndex = Array.IndexOf(headerSplit, "Protein Probability");
                int count = 0;
                int onePercentCount = 0;

                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    var values = line.Split('\t');
                    count++;
                    if (double.Parse(values[qValueIndex]) >= 0.99)
                        onePercentCount++;
                }

                proteinCount = count;
                onePercentProteinCount = onePercentCount;
            }

            var bulkResultCountComparison = new BulkResultCountComparison
            {
                DatasetName = DatasetName,
                Condition = Condition,
                FileName = "c",
                PsmCount = psmsCount,
                PeptideCount = peptidesCount,
                ProteinGroupCount = proteinCount,
                OnePercentPsmCount = psmsProbCount,
                OnePercentPeptideCount = peptidesProbCount,
                OnePercentProteinGroupCount = onePercentProteinCount
            };

            var bulkComparisonFile = new BulkResultCountComparisonFile(path)
            {
                Results = new List<BulkResultCountComparison> { bulkResultCountComparison }
            };
            bulkComparisonFile.WriteResults(path);
            return bulkComparisonFile;
        }

        public string[] IndividualFilePeptidePaths => IndividualFileResults.Select(p => p.PeptidePath).ToArray();
        public string CalibratedRetentionTimeFilePath => Path.Combine(DirectoryPath, $"{DatasetName}_{Condition}_{FileIdentifiers.CalibratedRetentionTimeFile}");

        private string _retentionTimePredictionPath => Path.Combine(DirectoryPath, $"{DatasetName}_MM_{FileIdentifiers.RetentionTimePredictionReady}");
        private RetentionTimePredictionFile _retentionTimePredictionFile;
        public RetentionTimePredictionFile RetentionTimePredictionFile => _retentionTimePredictionFile ??= CreateRetentionTimePredictionFile();

        public RetentionTimePredictionFile CreateRetentionTimePredictionFile()
        {
            if (_retentionTimePredictionPath.TryGetFile<RetentionTimePredictionFile>(out RetentionTimePredictionFile? file))
                if (file is not null)
                    return file;

            var psms = IndividualFileResults
                .SelectMany(p => p.PsmFile.Results.Where(psm => psm.PeptideProphetProbability > 0.99))
                .ToList();

            Log($"{Condition}: Making Retention time predctions with chronologer", 2);
            var sequenceToPredictionDictionary = psms.Select(p => (p.BaseSequence, p.FullSequence))
                .Distinct()
                .ToDictionary(p => p, p => ChronologerEstimator.PredictRetentionTime(p.BaseSequence, p.FullSequence));

            List<RetentionTimePredictionEntry> results = new();
            foreach (var chimeraGroup in psms.GroupBy(p => p, CustomComparer<MsFraggerPsm>.MsFraggerChimeraComparer))
            {
                bool isChimeric = chimeraGroup.Count() > 1;
                results.AddRange(chimeraGroup.Select(psm => 
                    new RetentionTimePredictionEntry(psm.FileNameWithoutExtension, psm.OneBasedScanNumber, 0, 
                        psm.RetentionTime, psm.BaseSequence, psm.FullSequence, psm.FullSequence, psm.PeptideProphetProbability,
                        psm.PeptideProphetProbability, psm.PeptideProphetProbability, 0, isChimeric)
                    {
                        ChronologerPrediction = sequenceToPredictionDictionary.TryGetValue((psm.BaseSequence, psm.FullSequence), out var value) ? value ?? 0 : 0
                    }));
            }
            var retentionTimePredictionFile = new RetentionTimePredictionFile(_retentionTimePredictionPath) { Results = results };
            retentionTimePredictionFile.WriteResults(_retentionTimePredictionPath);

            Log($"{Condition}: Finished Retention time predctions with chronologer", 2);
            return retentionTimePredictionFile;
        }

        string _proformaPsmFilePath => Path.Combine(DirectoryPath, $"{DatasetName}_{Condition}_PSMs_{FileIdentifiers.ProformaFile}");
        private ProformaFile? _proformaPsmFile;
        public ProformaFile ToPsmProformaFile()
        {
            if (File.Exists(_proformaPsmFilePath) && !Override)
                return _proformaPsmFile ??= new ProformaFile(_proformaPsmFilePath);

            List<ProformaRecord> records = new();
            foreach (var file in IndividualFileResults)
            {
                file.PsmFile.LoadResults();
                foreach (var psm in file.PsmFile.Results)
                {
                    if (!psm.PassesConfidenceFilter)
                        continue;

                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.PsmFile.First().FileNameWithoutExtension);
                    string fileName = fileNameWithoutExtension!.Replace("interact-", "");

                    int modMass = 0;
                    if (psm.AssignedModifications != "")
                        modMass = psm.AssignedModifications.Split(',')
                        .Sum(p => (int)Math.Round(MsFraggerPsm.ParseString(p.Trim()).Item3));

                    var record = new ProformaRecord()
                    {
                        Condition = Condition,
                        FileName = fileName,
                        BaseSequence = psm.BaseSequence,
                        ModificationMass = modMass,
                        PrecursorCharge = psm.Charge,
                        ProteinAccession = psm.ProteinAccession,
                        ScanNumber = psm.OneBasedScanNumber,
                        FullSequence = psm.FullSequence
                    };
                    records.Add(record);
                }
            }
            var proformaFile = new ProformaFile(_proformaPsmFilePath) { Results = records };
            proformaFile.WriteResults(_proformaPsmFilePath);
            return _proformaPsmFile = proformaFile;
        }


        string _proformaPeptideFilePath => Path.Combine(DirectoryPath, $"{DatasetName}_{Condition}_Peptides_{FileIdentifiers.ProformaFile}");
        private ProformaFile? _proformaPeptideFile;
    }

    public class MsFraggerIndividualFileResult
    {
        public string DirectoryPath { get; set; }

        internal string PsmPath;
        private MsFraggerPsmFile _psmFile;
        public MsFraggerPsmFile PsmFile => _psmFile ??= new MsFraggerPsmFile(PsmPath);

        internal string PeptidePath;
        private MsFraggerPeptideFile _peptideFile;
        public MsFraggerPeptideFile PeptideFile => _peptideFile ??= new MsFraggerPeptideFile(PeptidePath);

        internal string ProteinPath;
        private MsFraggerProteinFile _proteinFile;
        public MsFraggerProteinFile ProteinFile => _proteinFile ??= new MsFraggerProteinFile(ProteinPath);


        public MsFraggerIndividualFileResult(string directoryPath)
        {
            DirectoryPath = directoryPath;
            PsmPath = System.IO.Path.Combine(DirectoryPath, "psm.tsv");
            PeptidePath = System.IO.Path.Combine(DirectoryPath, "peptide.tsv");
            ProteinPath = System.IO.Path.Combine(DirectoryPath, "protein.tsv");
        }
    }
}
