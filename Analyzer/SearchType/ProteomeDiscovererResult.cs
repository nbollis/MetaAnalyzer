using Analyzer.FileTypes.External;
using Analyzer.FileTypes.Internal;
using Analyzer.Util;
using System.Linq;

namespace Analyzer.SearchType
{
    public class ProteomeDiscovererResult : BulkResult
    {
        private ProteomeDiscovererPsmFile _psmFile;
        private ProteomeDiscovererProteoformFile _peptideFile;
        private ProteomeDiscovererProteinFile _proteinFile;
        private string _inputFilePath;
        private ProteomeDiscovererInputFileFile _inputFile;
        private Dictionary<string, string> _idToFileNameDictionary;

        public ProteomeDiscovererPsmFile PrsmFile => _psmFile ??= new ProteomeDiscovererPsmFile(_psmPath);
        public ProteomeDiscovererProteoformFile ProteoformFile => _peptideFile ??= new ProteomeDiscovererProteoformFile(_peptidePath);
        public ProteomeDiscovererProteinFile ProteinFile => _proteinFile ??= new ProteomeDiscovererProteinFile(_proteinPath);
        public ProteomeDiscovererInputFileFile InputFile => _inputFile ??= new ProteomeDiscovererInputFileFile(_inputFilePath);
        public Dictionary<string,string> IdToFileNameDictionary => _idToFileNameDictionary ??= InputFile.ToDictionary(p => p.FileID, p => Path.GetFileNameWithoutExtension(p.FileName));

        public ProteomeDiscovererResult(string directoryPath) : base(directoryPath)
        {
            var files = Directory.GetFiles(directoryPath);
            _proteinPath = files.First(p => p.Contains("Proteins"));
            _inputFilePath = files.First(p => p.Contains("Input"));
            if (files.Any(file => file.Contains("PrSMs")))
            {
                IsTopDown = true;
                _psmPath = files.First(p => p.Contains("PrSMs"));
                _peptidePath = files.First(p => p.Contains("Proteoforms"));
            }
            else if (files.Any(file => file.Contains("PSMs")))
            {
                _psmPath  = files.First(p => p.Contains("PSMs"));
                _peptidePath = files.First(p => p.Contains("PeptideGroups"));
            }
        }

        public override BulkResultCountComparisonFile IndividualFileComparison(string path = null)
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

            var onePercentPsmCount = PrsmFile.Count(p => IsTopDown ? p.NegativeLogEValue >= 5 : p.QValue <= 0.01);
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
    }
}
