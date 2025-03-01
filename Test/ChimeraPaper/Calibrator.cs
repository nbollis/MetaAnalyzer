using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskLayer.CMD;

namespace Test.ChimeraPaper
{
    internal class Calibrator
    {
        [Test]
        public static void RunCalibrations()
        {
            string dataDir = @"B:\RawSpectraFiles\Mann_11cell_lines";
            var cellLines = Directory.GetDirectories(dataDir).Where(p => !p.Contains("107")).ToList();

            string dbPath = @"";
            string caliToml = @"";
            string averagingToml = @"";


            // Aggregate raw files in their replicates
            Dictionary<string, List<string>> cellLineRepRawFiles = new();
            foreach (var cellLine in cellLines) // each cell line dir
            {
                var directories = Directory.GetDirectories(cellLine);
                foreach (var directory in directories) // each replicate dir
                {
                    var files = Directory.GetFiles(directory, "*.raw");
                    if (files.Length == 0)
                        continue;

                    var name = Path.GetFileName(directory);
                    cellLineRepRawFiles.Add(name, files.ToList());
                }
            }


            // Create calibration and averaging tasks
            List<MetaMorpheusGptmdSearchCmdProcess> processesToRun = new();
            foreach (var repRawFiles in cellLineRepRawFiles)
            {
                
            }

        }
    }
}
