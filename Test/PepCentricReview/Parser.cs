using Analyzer;
using Google.Protobuf.WellKnownTypes;
using Omics;
using Omics.Modifications;
using Proteomics.ProteolyticDigestion;
using ResultAnalyzerUtil;
using System.Diagnostics;
using System.Threading.Tasks;
using UsefulProteomicsDatabases;

namespace Test.PepCentricReview
{
    public class Parser
    {
        public static string ProformaFilePath = @"B:\Users\Nic\Chimeras\InternalMMAnalysis\Mann_11cell_lines\Figures\ProformaResults\MetaMorpheus⠀_PSM_ProformaFile.tsv";
        public static string ParsedFilepath = @"D:\Projects\Paper Reviews\PepCentric\Mann11_PSMs.tsv";
        [Test]
        public static void ParseBest()
        {
            // Parse out Mann11 Confident Sequences
            var proformaFile = new ProformaFile(ProformaFilePath);
            var records = PepCentricConversions.GetDbOfOriginRecordsFromProforma(proformaFile).ToList();
            
            
            var isFastaCount = records.Count(p => p.IdClassification == IdClassification.Fasta);
            var isXmlCount = records.Count(p => p.IdClassification == IdClassification.Xml);
            var isGptmdCount = records.Count(p => p.IdClassification == IdClassification.Gptmd);
            var isUnknownCount = records.Count(p => p.IdClassification == IdClassification.Unknown);

            var file = new PepCentricReviewFile(ParsedFilepath) { Results = records };
            file.WriteResults(ParsedFilepath);
        }



        [Test]
        public static async Task TryUsePepCentricAPI()
        {
            var file = new PepCentricReviewFile(ParsedFilepath);
            var results = file.OrderByDescending(p => p.FullSequenceCount)
                .ThenByDescending(p => p.BaseSequenceCount)
                .ToList();

            var client = new PepCentricClient();
            var toTry = results.Skip(10).Take(2).Select(p => p.FullSequenceWithMassShifts).ToList();

            var jobId = await client.SubmitJobAsync(toTry);
            
            var isComplete = await client.WaitUntilJobComplete(jobId.JobIdentifier);

            //var result2 = await client.GetResultAsync(jobId.JobIdentifier);
            //var pep = await client.GetSequenceAsync(jobId.JobIdentifier, "0");
            //var pep2 = await client.GetPeptideAsync(jobId.JobIdentifier, "0", 0);


            var pepi = await client.GetSequenceAsync(jobId.JobIdentifier, "0");
            var pep2i = await client.GetPeptideAsync(jobId.JobIdentifier, "0", 0);
        }


        [Test]
        public static void PopulateFileWithApiResponse()
        {
            var client = new PepCentricClient();
            var file = new PepCentricReviewFile(ParsedFilepath);
            var results = file.ToHashSet();

            Dictionary<string, List<PepCentricReviewRecord>> resultsLookup = results
                .GroupBy(r => r.FullSequenceWithMassShifts)
                .ToDictionary(g => g.Key, g => g.ToList()); // Handle duplicates by taking the first record

            List<string> toProcess = new();
            int batchCounter = 5000;

            foreach (var item in results.Skip(50))
            {
                toProcess.Add(item.FullSequenceWithMassShifts);

                if (toProcess.Count >= 10)
                {
                    var peptideResponses = client.GetPeptidesFromFullSequences(toProcess).Result;

                    foreach (var peptide in peptideResponses)
                    {
                        var toLookup = peptide.FullSequenceWithMassShifts.Replace("n[", "[");
                        if (resultsLookup.TryGetValue(toLookup, out var records))
                        {
                            records.ForEach(p => p.SetValuesFromPepCentric(peptide));
                        }
                    }

                    toProcess.Clear();
                }

                // Write intermediate file every 1000 results
                if (++batchCounter % 1000 == 0)
                {
                    var tempFilePath = $"{Path.GetFileNameWithoutExtension(ParsedFilepath)}_temp_{batchCounter / 1000}.tsv";
                    var tempFile = new PepCentricReviewFile(tempFilePath) { Results = results.ToList() };
                    tempFile.WriteResults(tempFilePath);
                }
            }

            // Process any remaining items
            if (toProcess.Count > 0)
            {
                var peptideResponses = client.GetPeptidesFromFullSequences(toProcess).Result;

                foreach (var peptide in peptideResponses)
                {
                    var toLookup = peptide.FullSequenceWithMassShifts.Replace("n[", "[");
                    if (resultsLookup.TryGetValue(toLookup, out var records))
                    {
                        records.ForEach(p => p.SetValuesFromPepCentric(peptide));
                    }
                }
            }

            // Write final results to the main file
            file.WriteResults(ParsedFilepath);
        }
    }
}
