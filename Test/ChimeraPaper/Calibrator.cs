using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ResultAnalyzerUtil.CommandLine;
using TaskLayer.CMD;

namespace Test.ChimeraPaper
{
    internal class Calibrator
    {
        static string MetaMorpheusPath = @"C:\Program Files\MetaMorpheus";
        static string Version = "107";

        [Test]
        public static void RunCalibrations()
        {
            var manager = new TaskManager();
            string dataDir = @"B:\RawSpectraFiles\Mann_11cell_lines";
            
            string dbPath = @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";
            string caliToml = @"B:\RawSpectraFiles\Mann_11cell_lines\107_CalibratedAveraged\Task Settings\Task1-CalibrateTaskconfig.toml";
            string averagingToml = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\Task2-AveragingTaskconfig.toml";

            // Aggregate raw files in their replicates
            var cellLines = Directory.GetDirectories(dataDir).Where(p => !p.Contains("107")).ToList();
            Dictionary<(string,string), List<string>> cellLineRepRawFiles = new();
            foreach (var cellLine in cellLines) // each cell line dir
            {
                var directories = Directory.GetDirectories(cellLine);
                foreach (var directory in directories) // each replicate dir
                {
                    var files = Directory.GetFiles(directory, "*.raw");
                    if (files.Length == 0)
                        continue;

                    var name = Path.GetFileName(directory);
                    cellLineRepRawFiles.Add((cellLine, name), files.ToList());
                }
            }


            // Create calibration tasks
            List<CmdProcess> calibrationProcesses = new();
            foreach (var repRawFiles in cellLineRepRawFiles)
            {
                string calibOutDir = Path.Combine(dataDir, repRawFiles.Key.Item1, $"{repRawFiles.Key.Item2}_{Version}_Calibrated");
                string[] specPaths = repRawFiles.Value.ToArray();
                var caliProcess = new MetaMorpheusCalibrationCmdProcess(specPaths, dbPath, caliToml, calibOutDir, $"Calibrating {repRawFiles.Key.Item2}" , 0.5, MetaMorpheusPath);

                calibrationProcesses.Add(caliProcess);
            }

            // run calibrations
            manager.RunProcesses(calibrationProcesses).Wait();

            // Create averaging tasks
            List<CmdProcess> averagingProcesses = new();
            foreach (var repRawFiles in cellLineRepRawFiles)
            {
                string calibOutDir = Path.Combine(dataDir, repRawFiles.Key.Item1, $"{repRawFiles.Key.Item2}_{Version}_Calibrated");
                var calibratedFiles = Directory.GetFiles(calibOutDir, $"*.mzML");

                string averagedOutDir = Path.Combine(dataDir, repRawFiles.Key.Item1, $"{repRawFiles.Key.Item2}_{Version}_CalibratedAveraged");

                var avgProcess = new MetaMorpheusAveragingCmdProcess(calibratedFiles, dbPath, averagingToml, averagedOutDir, $"Averaging {repRawFiles.Key.Item2}", 0.5, MetaMorpheusPath);
                averagingProcesses.Add(avgProcess);
            }

            // run averaging
            manager.RunProcesses(averagingProcesses).Wait();
        }
    }
}
