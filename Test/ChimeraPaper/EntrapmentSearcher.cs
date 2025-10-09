using ResultAnalyzerUtil.CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.ChimeraPaper;
internal class EntrapmentSearcher
{

    public static string MetaMorpheusLocation= @"C:\Program Files\MetaMorpheus";

    public void RunSearches(string[] entrapmentPaths, string targetDbPath, string[] specPaths, string outDir, string searchToml, string gptmdToml)
    {
        List<CmdProcess> processes = new List<CmdProcess>();
        foreach (var dbPath in entrapmentPaths)
        {
            var dbName = Path.GetFileNameWithoutExtension(dbPath);
            var dbSubName = string.Join('_', dbName.Split('_').TakeLast(2));
            var outputPath = Path.Combine(outDir, dbSubName);

            var gptmd = new MetaMorpheusGptmdSearchCmdProcess(specPaths, new string[] { targetDbPath, dbPath }, gptmdToml, searchToml, outputPath,
                $"Entrapment Search of {dbSubName}", 1, MetaMorpheusLocation);

            processes.Add(gptmd);
        }

        TaskManager manager = new(3);
        manager.RunProcesses(processes).Wait();
    }

    [Test]
    public void RunOnSubsetOfSpectra()
    {
        string[] specPaths =
        {
            //@"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-17-20_jurkat_td_rep2_fract1-averaged.mzML",
            //@"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-17-20_jurkat_td_rep2_fract2-calib-averaged.mzML",
            //@"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-17-20_jurkat_td_rep2_fract3-calib-averaged.mzML",
            //@"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-17-20_jurkat_td_rep2_fract4-calib-averaged.mzML",
            @"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-18-20_jurkat_td_rep2_fract5-calib-averaged.mzML",
            @"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-18-20_jurkat_td_rep2_fract6-calib-averaged.mzML",
            @"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-18-20_jurkat_td_rep2_fract7-calib-averaged.mzML",
            //@"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-18-20_jurkat_td_rep2_fract8-calib-averaged.mzML",
            //@"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-18-20_jurkat_td_rep2_fract9-calib-averaged.mzML",
            //@"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\02-18-20_jurkat_td_rep2_fract10-calib-averaged.mzML"
        };

        // All
        string[] all =
        {
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_2Fold_Retain2.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_4Fold_Retain2.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_6Fold_Retain2.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_8Fold_Retain2.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_10Fold_Retain2.xml",
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_2Fold_Retain3.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_4Fold_Retain3.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_6Fold_Retain3.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_8Fold_Retain3.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_10Fold_Retain3.xml",
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_2Fold_Retain4.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_4Fold_Retain4.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_6Fold_Retain4.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_8Fold_Retain4.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_10Fold_Retain4.xml",
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_2Fold_Retain1.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_4Fold_Retain1.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_6Fold_Retain1.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_8Fold_Retain1.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_10Fold_Retain1.xml",
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_2Fold_Retain0.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_4Fold_Retain0.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_6Fold_Retain0.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_8Fold_Retain0.xml" ,
            @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22_10Fold_Retain0.xml",
        };

        string[] except8 =
        {
            @"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_2Fold_Retain2.xml" ,
            @"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_4Fold_Retain2.xml" ,
            @"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_6Fold_Retain2.xml" ,
            //@"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_8Fold_Retain2.xml" ,
            @"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_10Fold_Retain2.xml",
            @"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_2Fold_Retain3.xml" ,
            @"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_4Fold_Retain3.xml" ,
            @"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_6Fold_Retain3.xml" ,
            //@"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_8Fold_Retain3.xml" ,
            @"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_10Fold_Retain3.xml",
            @"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_2Fold_Retain4.xml" ,
            @"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_4Fold_Retain4.xml" ,
            @"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_6Fold_Retain4.xml" ,
            //@"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_8Fold_Retain4.xml" ,
            @"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_10Fold_Retain4.xml"
        };

        string[] eightFoldOnly =
        {
            @"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_8Fold_Retain2.xml" ,
            @"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_8Fold_Retain3.xml" ,
            @"D:\Proteomes\HumanEntrapment\Human_AND_model_organism_9606_2025_09_22_8Fold_Retain4.xml" ,
        };

        string targetDbPath = @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22.xml";
        string gptmdToml = @"B:\Users\Nic\TopDownEntrapment\GPTMD.toml";
        string searchToml = @"B:\Users\Nic\TopDownEntrapment\Search.toml";

        string outPath = @"B:\Users\Nic\TopDownEntrapment\113_RangeFoldExploration_Mini";

        RunSearches(all, targetDbPath, specPaths, outPath, searchToml, gptmdToml);
    }
    
}
