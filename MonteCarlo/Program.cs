using MonteCarlo.IO;
using Omics.Modifications;
using ResultAnalyzerUtil;

namespace MonteCarlo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Logger.Log("Starting Bottom-Up...");
            //MultiRun_WithOtherOrganisms();

            Logger.Log("Starting Top-Down...");
            MultiRun_WithOtherOrganisms_TD();

            Logger.Log("All runs completed.");
            Console.ReadLine();
        }

        public static void MultiRun_WithOtherOrganisms()
        {
            string dbPath = @"D:\Proteomes\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";
            string ecoliDbPath = @"D:\Proteomes\uniprotkb_ecoli_proteome_AND_reviewed_t_2024_03_25.xml";
            string horseDbPath = @"D:\Proteomes\horse_reference_organism_id_9796_AND_proteome_2025_02_04.xml";
            string yeastDbPath = @"D:\Proteomes\uniprotkb_yeast_proteome_AND_model_orga_2024_03_27.xml";
            string droosophilaProteomePath = @"D:\Proteomes\uniprotkb_DrosophilaMelangaster_proteome_UP000000803_2025_04_28.xml";
            string musMusculusPath = @"D:\Proteomes\uniprotkb_MusMusculus_proteome_UP000000589_2025_04_28.xml";
            string panTroglodyteProteomePath = @"D:\Proteomes\uniprotkb_PanTroglodyte_proteome_UP000002277_2025_04_28.xml";

            int peptidesPerIteration = 400;
            string[] spectraPath =
                [
                    @"B:\RawSpectraFiles\Mann_11cell_lines\A549\A549_2\20100721_Velos1_TaGe_SA_A549_04.raw",
                    @"B:\RawSpectraFiles\Mann_11cell_lines\Hela\Hela_2\20100726_Velos1_TaGe_SA_HeLa_4.raw",
                    @"B:\RawSpectraFiles\Mann_11cell_lines\RKO\rko_1\20100616_Velos1_TaGe_SA_RKO_5.raw",
                ];
            string outputDir = @"D:\Projects\MonteCarlo\BU_ManyOrganism_ReBalanced_WithFixedMods";


            var fixedMod = GlobalVariables.AllModsKnown.First(p => p.IdWithMotif == "Carbamidomethyl on C");
            var fixedMods = new List<Modification>() { fixedMod };
            List<MonteCarloParameters> allparams = new()
            {
                new MonteCarloParameters(outputDir, dbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "NoDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                    ConditionIdentifier = "Human_Target",
                    FixedMods = fixedMods,
                },
                new MonteCarloParameters(outputDir, dbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "ReverseDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                    ConditionIdentifier = "Human_Reverse",
                    FixedMods = fixedMods,
                },
                new MonteCarloParameters(outputDir, dbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "SlideDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Slide,
                    ConditionIdentifier = "Human_Slide",
                    FixedMods = fixedMods,
                },
                new MonteCarloParameters(outputDir, ecoliDbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Ecoli_NoDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                    ConditionIdentifier = "Ecoli_Target",
                    FixedMods = fixedMods
                },
                new MonteCarloParameters(outputDir, ecoliDbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Ecoli_ReverseDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                    ConditionIdentifier = "Ecoli_Reverse",
                    FixedMods = fixedMods
                },
                new MonteCarloParameters(outputDir, yeastDbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Yeast_NoDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                    ConditionIdentifier = "Yeast_Target",
                    FixedMods = fixedMods
                },
                new MonteCarloParameters(outputDir, yeastDbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Yeast_ReverseDecoy"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                    ConditionIdentifier = "Yeast_Reverse",
                    FixedMods = fixedMods
                },
                new MonteCarloParameters(outputDir, panTroglodyteProteomePath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Chimp_Target"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                    ConditionIdentifier = "Chimp_Target",
                    FixedMods = fixedMods
                },
                new MonteCarloParameters(outputDir, panTroglodyteProteomePath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Chimp_Reverse"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                    ConditionIdentifier = "Chimp_Reverse",
                    FixedMods = fixedMods
                },
                new MonteCarloParameters(outputDir, musMusculusPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Mouse_Target"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                    ConditionIdentifier = "Mouse_Target",
                    FixedMods = fixedMods
                },
                new MonteCarloParameters(outputDir, musMusculusPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Mouse_Reverse"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                    ConditionIdentifier = "Mouse_Reverse",
                    FixedMods = fixedMods
                },
                new MonteCarloParameters(outputDir, droosophilaProteomePath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "FruitFly_Target"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                    ConditionIdentifier = "FruitFly_Target",
                    FixedMods = fixedMods
                },
                new MonteCarloParameters(outputDir, droosophilaProteomePath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "FruitFly_Reverse"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                    ConditionIdentifier = "FruitFly_Reverse",
                    FixedMods = fixedMods
                },
            };

            var runner = new MultiMonteCarloRunner(allparams, outputDir);
            runner.RunAll();
        }

        public static void MultiRun_WithOtherOrganisms_TD()
        {
            string dbPath = @"D:\Proteomes\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";
            string ecoliDbPath = @"D:\Proteomes\uniprotkb_ecoli_proteome_AND_reviewed_t_2024_03_25.xml";
            string horseDbPath = @"D:\Proteomes\horse_reference_organism_id_9796_AND_proteome_2025_02_04.xml";
            string yeastDbPath = @"D:\Proteomes\uniprotkb_yeast_proteome_AND_model_orga_2024_03_27.xml";
            string droosophilaProteomePath = @"D:\Proteomes\uniprotkb_DrosophilaMelangaster_proteome_UP000000803_2025_04_28.xml";
            string musMusculusPath = @"D:\Proteomes\uniprotkb_MusMusculus_proteome_UP000000589_2025_04_28.xml";
            string panTroglodyteProteomePath = @"D:\Proteomes\uniprotkb_PanTroglodyte_proteome_UP000002277_2025_04_28.xml";

            string[] spectraPath =
                [
                @"B:\RawSpectraFiles\JurkatTopDown\02-18-20_jurkat_td_rep2_fract5.raw",
                @"B:\RawSpectraFiles\JurkatTopDown\02-18-20_jurkat_td_rep2_fract6.raw",
                @"B:\RawSpectraFiles\JurkatTopDown\02-18-20_jurkat_td_rep2_fract7.raw",
                @"B:\RawSpectraFiles\JurkatTopDown\02-18-20_jurkat_td_rep1_fract5.raw",
                @"B:\RawSpectraFiles\JurkatTopDown\02-18-20_jurkat_td_rep1_fract6.raw",
                @"B:\RawSpectraFiles\JurkatTopDown\02-18-20_jurkat_td_rep1_fract7.raw"
                ];
            string outputDir = @"D:\Projects\MonteCarlo\TD_SmallToTestOutput";

            int peptidesPerIteration = 20;
            int spectraPerIteration = 20;
            List<MonteCarloParameters> allparams = new()
            {
                new MonteCarloParameters(outputDir, dbPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Target"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                    ConditionIdentifier = "Human_Target",
                    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                    MaximumPeptidesPerIteration = peptidesPerIteration,
                    MaximumSpectraPerIteration = spectraPerIteration
                },
                //new MonteCarloParameters(outputDir, dbPath, spectraPath)
                //{
                //    OutputDirectory = Path.Combine(outputDir, "Reverse"),
                //    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                //    ConditionIdentifier = "Human_Reverse",
                //    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                //    MaximumPeptidesPerIteration = peptidesPerIteration,
                //    MaximumSpectraPerIteration = spectraPerIteration
                //},
                //new MonteCarloParameters(outputDir, dbPath, spectraPath)
                //{
                //    OutputDirectory = Path.Combine(outputDir, "Slide"),
                //    DecoyType = UsefulProteomicsDatabases.DecoyType.Slide,
                //    ConditionIdentifier = "Human_Slide",
                //    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                //    MaximumPeptidesPerIteration = peptidesPerIteration,
                //    MaximumSpectraPerIteration = spectraPerIteration
                //},
                //new MonteCarloParameters(outputDir, ecoliDbPath, spectraPath)
                //{
                //    OutputDirectory = Path.Combine(outputDir, "Ecoli_Target"),
                //    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                //    ConditionIdentifier = "Ecoli_Target",
                //    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                //    MaximumPeptidesPerIteration = peptidesPerIteration,
                //    MaximumSpectraPerIteration = spectraPerIteration
                //},
                //new MonteCarloParameters(outputDir, ecoliDbPath, spectraPath)
                //{
                //    OutputDirectory = Path.Combine(outputDir, "Ecoli_Reverse"),
                //    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                //    ConditionIdentifier = "Ecoli_Reverse",
                //    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                //    MaximumPeptidesPerIteration = peptidesPerIteration,
                //    MaximumSpectraPerIteration = spectraPerIteration
                //},
                //new MonteCarloParameters(outputDir, yeastDbPath, spectraPath)
                //{
                //    OutputDirectory = Path.Combine(outputDir, "Yeast_Target"),
                //    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                //    ConditionIdentifier = "Yeast_Target",
                //    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                //    MaximumPeptidesPerIteration = peptidesPerIteration,
                //    MaximumSpectraPerIteration = spectraPerIteration
                //},
                //new MonteCarloParameters(outputDir, yeastDbPath, spectraPath)
                //{
                //    OutputDirectory = Path.Combine(outputDir, "Yeast_Reverse"),
                //    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                //    ConditionIdentifier = "Yeast_Reverse",
                //    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                //    MaximumPeptidesPerIteration = peptidesPerIteration,
                //    MaximumSpectraPerIteration = spectraPerIteration
                //},
                //new MonteCarloParameters(outputDir, panTroglodyteProteomePath, spectraPath)
                //{
                //    OutputDirectory = Path.Combine(outputDir, "Chimp_Target"),
                //    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                //    ConditionIdentifier = "Chimp_Target",
                //    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                //    MaximumPeptidesPerIteration = peptidesPerIteration,
                //    MaximumSpectraPerIteration = spectraPerIteration
                //},
                //new MonteCarloParameters(outputDir, panTroglodyteProteomePath, spectraPath)
                //{
                //    OutputDirectory = Path.Combine(outputDir, "Chimp_Reverse"),
                //    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                //    ConditionIdentifier = "Chimp_Reverse",
                //    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                //    MaximumPeptidesPerIteration = peptidesPerIteration,
                //    MaximumSpectraPerIteration = spectraPerIteration
                //},
                new MonteCarloParameters(outputDir, musMusculusPath, spectraPath)
                {
                    OutputDirectory = Path.Combine(outputDir, "Mouse_Target"),
                    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                    ConditionIdentifier = "Mouse_Target",
                    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                    MaximumPeptidesPerIteration = peptidesPerIteration,
                    MaximumSpectraPerIteration = spectraPerIteration
                },
                //new MonteCarloParameters(outputDir, musMusculusPath, spectraPath)
                //{
                //    OutputDirectory = Path.Combine(outputDir, "Mouse_Reverse"),
                //    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                //    ConditionIdentifier = "Mouse_Reverse",
                //    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                //    MaximumPeptidesPerIteration = peptidesPerIteration,
                //    MaximumSpectraPerIteration = spectraPerIteration
                //},
                //new MonteCarloParameters(outputDir, droosophilaProteomePath, spectraPath)
                //{
                //    OutputDirectory = Path.Combine(outputDir, "FruitFly_Target"),
                //    DecoyType = UsefulProteomicsDatabases.DecoyType.None,
                //    ConditionIdentifier = "FruitFly_Target",
                //    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                //    MaximumPeptidesPerIteration = peptidesPerIteration,
                //    MaximumSpectraPerIteration = spectraPerIteration
                //},
                //new MonteCarloParameters(outputDir, droosophilaProteomePath, spectraPath)
                //{
                //    OutputDirectory = Path.Combine(outputDir, "FruitFly_Reverse"),
                //    DecoyType = UsefulProteomicsDatabases.DecoyType.Reverse,
                //    ConditionIdentifier = "FruitFly_Reverse",
                //    PeptideProviderType = PeptideSetProviderType.TopDownFromDatabase,
                //    MaximumPeptidesPerIteration = peptidesPerIteration,
                //    MaximumSpectraPerIteration = spectraPerIteration
                //},
            };

            var runner = new MultiMonteCarloRunner(allparams, outputDir);
            runner.RunAll();
        }
    }

    public class CommandLineParameters
    {

    }
}
