namespace MsPathFinderTRunner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int degreeOfParallelism = 6;
            int threadsTouse = 30 / degreeOfParallelism;
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
}
