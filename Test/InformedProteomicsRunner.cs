using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{

    public static class InformedProteomicsRunner
    {
        [Test]
        public static void RunMsPathFinderT()
        {
            int degreeOfParallelism = 6;
            int threadsTouse = 30 / degreeOfParallelism;
            int[] threads = Enumerable.Range(0, degreeOfParallelism).ToArray();
            var prompts = MsPathFinderTParams.AllParamsLeftToRun
                .SelectMany(p => GenerateCommandPromptsOfThoseLeftToRun(p, threadsTouse)).ToArray();

            Parallel.ForEach(
                MsPathFinderTParams.AllParamsLeftToRun.SelectMany(p =>
                    GenerateCommandPromptsOfThoseLeftToRun(p, threadsTouse)),
                new ParallelOptions() { MaxDegreeOfParallelism = degreeOfParallelism },
                prompt =>
                {
                    try
                    {
                        Console.WriteLine(
                            $"{DateTime.Now}....{Path.GetFileNameWithoutExtension(prompt.Split(' ')[1])}....Begin");
                        var proc = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "MSPathFinderT.exe",
                                Arguments = $"{prompt}",
                                UseShellExecute = true,
                                CreateNoWindow = true,
                                WorkingDirectory = @"C:\Informed-Proteomics"
                            }
                        };
                        proc.Start();
                        proc.WaitForExit();
                        Console.WriteLine(
                            $"{DateTime.Now}....{Path.GetFileNameWithoutExtension(prompt.Split(' ')[1])}....End");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                });

        }

        public static IEnumerable<string> GenerateCommandPromptsOfThoseLeftToRun(MsPathFinderTParams param,
            int threadsPerFile)
        {

            var inputFiles = Directory.GetFiles(param.InputDirectory, "*.pbf").ToList();
            var inputParamFiles = Directory.GetFiles(param.InputDirectory, "*.param").ToList();
            var inputFeatures = Directory.GetFiles(param.InputDirectory, "*.ms1ft").ToList();

            for (int i = 0; i < inputFiles.Count; i++)
            {
                var spectraFile = inputFiles[i];
                var paramFile = inputParamFiles[i];
                var featureFile = inputFeatures[i];
                //if (spectraFile.Contains("jurkat") &&
                //    spectraFile.Contains("rep1")) // TODO: Remove this when no longer interested in just rep2 of jurkat 
                //    continue;


                var resultPaths = new string[]
                {
                    Path.Combine(param.OutputDirectory, Path.GetFileNameWithoutExtension(spectraFile) + "_IcTarget.tsv"),
                    Path.Combine(param.OutputDirectory, Path.GetFileNameWithoutExtension(spectraFile) + "_IcDecoy.tsv"),
                    Path.Combine(param.OutputDirectory, Path.GetFileNameWithoutExtension(spectraFile) + "_IcTda.tsv")
                };
                if (resultPaths.All(File.Exists))
                    continue;

                yield return
                    $"-s {spectraFile} -feature {featureFile} -o {param.OutputDirectory} -d {param.DatabasePath} -mod {param.ModFilePath} " +
                    $"-maxCharge 60 -tda 1 -IncludeDecoys True -n {param.MaxIdsPerSpectra} -ic 1 -threads {threadsPerFile} -act 2 -f 20 -t 20 ";
            }
        }

    }

    public class MsPathFinderTParams
    {
        internal string InputDirectory { get; set; }
        internal string OutputDirectory { get; set; }
        internal string DatabasePath { get; set; }
        internal string ModFilePath { get; set; }
        internal int MaxIdsPerSpectra { get; set; }

        internal static List<MsPathFinderTParams> AllParamsLeftToRun => new List<MsPathFinderTParams>()
        {
            new MsPathFinderTParams()
            {
                InputDirectory = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\SearchResults\MsPathFinderT",
                OutputDirectory =
                    @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\SearchResults\MsPathFinderTWithMods_15Rep2_Final",
                DatabasePath =
                    @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\uniprotkb_human_proteome_AND_reviewed_t_2024_03_25.fasta",
                ModFilePath = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\InformedProteomicsMods.txt",
                MaxIdsPerSpectra = 15,
            },
            //new MsPathFinderTParams()
            //{
            //    InputDirectory = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\SearchResults\MsPathFinderT",
            //    OutputDirectory = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\SearchResults\MsPathFinderTWithModsNoChimeras",
            //    DatabasePath =  @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\uniprotkb_human_proteome_AND_reviewed_t_2024_03_25.fasta",
            //    ModFilePath = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\InformedProteomicsMods.txt",
            //    MaxIdsPerSpectra = 1,
            //},
            new MsPathFinderTParams()
            {
                InputDirectory = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Ecoli\SearchResults\MsPathFinderT",
                OutputDirectory =
                    @"B:\Users\Nic\Chimeras\TopDown_Analysis\Ecoli\SearchResults\MsPathFinderTWithMods_15_Final",
                DatabasePath =
                    @"B:\Users\Nic\Chimeras\TopDown_Analysis\Ecoli\Ecoli_uniprotkb_proteome_UP000000625_AND_revi_2024_04_04.fasta",
                ModFilePath = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Ecoli\InformedProteomicsMods.txt",
                MaxIdsPerSpectra = 15,
            },
            //new MsPathFinderTParams()
            //{
            //    InputDirectory = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Ecoli\SearchResults\MsPathFinderT",
            //    OutputDirectory = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Ecoli\SearchResults\MsPathFinderTWithModsNoChimeras",
            //    DatabasePath =  @"B:\Users\Nic\Chimeras\TopDown_Analysis\Ecoli\Ecoli_uniprotkb_proteome_UP000000625_AND_revi_2024_04_04.fasta",
            //    ModFilePath = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Ecoli\InformedProteomicsMods.txt",
            //    MaxIdsPerSpectra = 1,
            //},
        };
    }
}