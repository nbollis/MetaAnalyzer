using MonteCarlo;
using Readers;

namespace Test.MonteCarlo
{
    [TestFixture]
    public class MontePlayground
    {

        [Test]
        public static void SingleRun()
        {
            string dbPath = @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\UP000005640_reviewed.fasta";
            string[] spectraPath = [@"B:\RawSpectraFiles\Mann_11cell_lines\A549\A549_2\20100721_Velos1_TaGe_SA_A549_04.raw"];
            string outputDir = @"D:\Projects\MonteCarlo";
            var parameters = new MonteCarloParameters(outputDir, dbPath, spectraPath)
            {
                MaximumPeptidesPerIteration = 20,
                MaximumSpectraPerIteration = 20,
            };

            var runner = new MonteCarloRunner(parameters);
            var results = runner.Run();
        }

        [Test] 
        public static void GoHomeRunner() 
        {
            MultiRun();
            MultiRun_WithOtherOrganisms();
            MultiRun_WithOtherOrganisms_TD();
        }

        [Test]
        public static void MultiRun()
        {
            string dbPath = @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\UP000005640_reviewed.fasta";
            string[] spectraPath = [@"B:\RawSpectraFiles\Mann_11cell_lines\A549\A549_2\20100721_Velos1_TaGe_SA_A549_04.raw"];
            string outputDir = @"D:\Projects\MonteCarlo\BuFirstPass";

            List<MonteCarloParameters> allparams = new()
            {
                new MonteCarloParameters(outputDir, dbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "NoDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                    ConditionIdentifier = "Target"
                },
                new MonteCarloParameters(outputDir, dbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "ReverseDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                    ConditionIdentifier = "Reverse"
                },
                new MonteCarloParameters(outputDir, dbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "SlideDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Slide,
                    ConditionIdentifier = "Slide"
                },
            };


            var runner = new MultiMonteCarloRunner(allparams, outputDir);
            runner.RunAll();
        }

        [Test]
        public static void MultiRun_WithOtherOrganisms()
        {
            string dbPath = @"D:\Proteomes\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";
            string ecoliDbPath = @"D:\Proteomes\uniprotkb_ecoli_proteome_AND_reviewed_t_2024_03_25.xml";
            string horseDbPath = @"D:\Proteomes\horse_reference_organism_id_9796_AND_proteome_2025_02_04.xml";
            string yeastDbPath = @"D:\Proteomes\uniprotkb_yeast_proteome_AND_model_orga_2024_03_27.xml";

            string[] spectraPath = 
                [
                    @"B:\RawSpectraFiles\Mann_11cell_lines\A549\A549_2\20100721_Velos1_TaGe_SA_A549_04.raw",
                    @"B:\RawSpectraFiles\Mann_11cell_lines\Hela\Hela_2\20100726_Velos1_TaGe_SA_HeLa_4.raw",
                    @"B:\RawSpectraFiles\Mann_11cell_lines\RKO\rko_1\20100616_Velos1_TaGe_SA_RKO_5.raw",
                ];
            string outputDir = @"D:\Projects\MonteCarlo\BU_ManyOrganism";

            List<MonteCarloParameters> allparams = new()
            {
                new MonteCarloParameters(outputDir, dbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "NoDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                    ConditionIdentifier = "Human_Target"
                },
                new MonteCarloParameters(outputDir, dbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "ReverseDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                    ConditionIdentifier = "Human_Reverse"
                },
                new MonteCarloParameters(outputDir, dbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "SlideDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Slide,
                    ConditionIdentifier = "Human_Slide"
                },
                new MonteCarloParameters(outputDir, ecoliDbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Ecoli_NoDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                    ConditionIdentifier = "Ecoli_Target"
                },
                new MonteCarloParameters(outputDir, ecoliDbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Ecoli_ReverseDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                    ConditionIdentifier = "Ecoli_Reverse"
                },
                new MonteCarloParameters(outputDir, horseDbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Horse_NoDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                    ConditionIdentifier = "Horse_Target"
                },
                new MonteCarloParameters(outputDir, horseDbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Horse_ReverseDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                    ConditionIdentifier = "Horse_Reverse"
                },
                new MonteCarloParameters(outputDir, yeastDbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Yeast_NoDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                    ConditionIdentifier = "Yeast_Target"
                },
                new MonteCarloParameters(outputDir, yeastDbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Yeast_ReverseDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                    ConditionIdentifier = "Yeast_Reverse"
                },
            };


            var runner = new MultiMonteCarloRunner(allparams, outputDir);
            runner.RunAll();

        }

        [Test]
        public static void MultiRun_WithOtherOrganisms_TD()
        {
            string dbPath = @"D:\Proteomes\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";
            string ecoliDbPath = @"D:\Proteomes\uniprotkb_ecoli_proteome_AND_reviewed_t_2024_03_25.xml";
            string horseDbPath = @"D:\Proteomes\horse_reference_organism_id_9796_AND_proteome_2025_02_04.xml";
            string yeastDbPath = @"D:\Proteomes\uniprotkb_yeast_proteome_AND_model_orga_2024_03_27.xml";

            string[] spectraPath = 
                [
                @"B:\RawSpectraFiles\JurkatTopDown\02-18-20_jurkat_td_rep2_fract5.raw",
                @"B:\RawSpectraFiles\JurkatTopDown\02-18-20_jurkat_td_rep2_fract6.raw",
                @"B:\RawSpectraFiles\JurkatTopDown\02-18-20_jurkat_td_rep2_fract7.raw",
                ];
            string outputDir = @"D:\Projects\MonteCarlo\TD_ManyOrganism";


            List<MonteCarloParameters> allparams = new()
            {
                new MonteCarloParameters(outputDir, dbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Target"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                    ConditionIdentifier = "Human_Target",
                    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                    MaximumPeptidesPerIteration = 300
                },
                new MonteCarloParameters(outputDir, dbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Reverse"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                    ConditionIdentifier = "Human_Reverse",
                    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                    MaximumPeptidesPerIteration = 300
                },
                new MonteCarloParameters(outputDir, dbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Slide"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Slide,
                    ConditionIdentifier = "Human_Slide",
                    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                    MaximumPeptidesPerIteration = 300
                },
                new MonteCarloParameters(outputDir, ecoliDbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Ecoli_Target"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                    ConditionIdentifier = "Ecoli_Target",
                    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                    MaximumPeptidesPerIteration = 300
                },
                new MonteCarloParameters(outputDir, ecoliDbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Ecoli_Reverse"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                    ConditionIdentifier = "Ecoli_Reverse",
                    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                    MaximumPeptidesPerIteration = 300
                },
                new MonteCarloParameters(outputDir, horseDbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Horse_Target"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                    ConditionIdentifier = "Horse_Target",
                    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                    MaximumPeptidesPerIteration = 300
                },
                new MonteCarloParameters(outputDir, horseDbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Horse_Reverse"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                    ConditionIdentifier = "Horse_Reverse",
                    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                    MaximumPeptidesPerIteration = 300
                },
                new MonteCarloParameters(outputDir, yeastDbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Yeast_Target"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                    ConditionIdentifier = "Yeast_Target",
                    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                    MaximumPeptidesPerIteration = 300
                },
                new MonteCarloParameters(outputDir, yeastDbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Yeast_Reverse"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                    ConditionIdentifier = "Yeast_Reverse",
                    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                    MaximumPeptidesPerIteration = 300
                },
            };


            var runner = new MultiMonteCarloRunner(allparams, outputDir);
            runner.RunAll();

        }
    }
}
