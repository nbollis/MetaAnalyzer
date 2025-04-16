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
            var result = await client.GetCompletedResultAsync(jobId);
            
            var isComplete = await client.WaitUntilJobComplete(jobId.JobIdentifier);

            var result2 = await client.GetResultAsync(jobId.JobIdentifier);

        }






        


        

       
    }
}
