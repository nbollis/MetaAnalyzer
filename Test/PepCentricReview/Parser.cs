using Analyzer;
using Google.Protobuf.WellKnownTypes;
using Omics;
using Omics.Modifications;
using Proteomics.ProteolyticDigestion;
using ResultAnalyzerUtil;
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
            var records = PepCentricConversions.GetRecordsFromProforma(proformaFile).ToList();
            
            
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
            var pepiR = await client.ShowSequenceAsync(jobId.JobIdentifier, "0", "0.01", "0.01", "0.01");
            var pep2i = await client.GetPeptideAsync(jobId.JobIdentifier, "0", 0);
            var pep2iR = await client.ShowPeptideAsync(jobId.JobIdentifier, "0", "0", "0.01", "0.01");
       
        }






        


        

       
    }
}
