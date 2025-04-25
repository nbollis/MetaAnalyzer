using MonteCarlo;
using Readers;

namespace Test.MonteCarlo
{
    [TestFixture]
    public class MontePlayground
    {

        [Test]
        public static void Test()
        {
            string dbPath = @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\UP000005640_reviewed.fasta";
            string spectraPath = @"B:\RawSpectraFiles\Mann_11cell_lines\A549\A549_2\20100721_Velos1_TaGe_SA_A549_04.raw";
            string outputDir = @"D:\Projects\MonteCarlo";
            var parameters = new MonteCarloParameters(outputDir, dbPath, spectraPath);

            var runner = new MonteCarloRunner(parameters);
            var results = runner.Run();
        }
    }
}
