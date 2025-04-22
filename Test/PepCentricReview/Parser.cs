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
            var results = file.OrderBy(p => p.FullSequenceWithMassShifts).ToHashSet();

            List<string> toProcess = new();
            foreach (var item in file)
            {
                toProcess.Add(item.FullSequenceWithMassShifts);

                if (toProcess.Count >= 10)
                {
                    var peptideResponses = client.GetPeptidesFromFullSequences(toProcess).Result;

                    foreach (var peptide in peptideResponses)
                    {
                        var record = results.FirstOrDefault(r => 
                        r.FullSequenceWithMassShifts == peptide.FullSequenceWithMassShifts.Replace("n[", "["));
                        if (record != null)
                        {
                            record.SetValuesFromPepCentric(peptide);
                        }
                        else
                            Debugger.Break();
                    }

                    toProcess.Clear();
                }
            }

            // Process any remaining items
            if (toProcess.Count > 0)
            {
                var peptideResponses = client.GetPeptidesFromFullSequences(toProcess).Result;

                foreach (var peptide in peptideResponses.SelectMany(p => p.Results))
                {
                    var record = results.FirstOrDefault(r => r.FullSequenceWithMassShifts == peptide.FullSequenceWithMassShifts);
                    if (record != null)
                    {
                        record.SetValuesFromPepCentric(peptide);
                    }
                }
            }

            file.WriteResults(ParsedFilepath);
        }





        


        

       
    }
}
