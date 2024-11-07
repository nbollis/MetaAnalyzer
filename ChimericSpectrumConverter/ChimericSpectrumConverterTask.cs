using Analyzer.SearchType;
using Chemistry;
using MassSpectrometry;
using MathNet.Numerics;
using Readers;
using TaskLayer;

namespace ChimericSpectrumConverter
{
    internal class ChimericSpectrumConverterTask : BaseResultAnalyzerTask
    {
        internal List<(string InputPath, string OutputPath)> InputOutputPaths { get; init; }
        public override MyTask MyTask => MyTask.ChimericSpectrumConversion;
        public override ChimericSpectrumConverterParameters Parameters { get; }

        public ChimericSpectrumConverterTask(ChimericSpectrumConverterParameters parameters)
        {
            Parameters = parameters;
            InputOutputPaths = new List<(string, string)>();
            foreach (var inputFile in parameters.SpectraFiles)
            {
                if (!File.Exists(inputFile))
                    throw new ArgumentException($"Spectra File Not Found: {inputFile}");
                var inputFileName = Path.GetFileName(inputFile);
                var outFileName = Path.Combine(parameters.OutputDirectory,
                    inputFileName.Replace(Path.GetExtension(inputFile), "_SetPrecursor.mzML"));

                if (File.Exists(outFileName) && !parameters.Override)
                {
                    Warn($"File already exists: {outFileName}");
                    continue;
                }
                InputOutputPaths.Add((inputFile, outFileName));
            }
        }

        protected override void RunSpecific()
        {

            Parallel.ForEach(InputOutputPaths,
                new ParallelOptions() { MaxDegreeOfParallelism = Parameters.MaxDegreesOfParallelism }, (ioPath) =>
                {
                    var inputPath = ioPath.InputPath;
                    var outputPath = ioPath.OutputPath;

                    var dataFile = MsDataFileReader.GetDataFile(inputPath).LoadAllStaticData();

                    Log($"Deconvoluting MS1 Scans File: {Path.GetFileNameWithoutExtension(inputPath)}");
                    var ms1ToDeconvolutedEnvelopeDictionary = dataFile.Scans.Where(p => p.MsnOrder == 1)
                        .ToDictionary(p => p.OneBasedScanNumber,
                            p => Deconvoluter
                                .Deconvolute(p, Parameters.PrecursorDeconvolutionParameters, p.ScanWindowRange)
                                .OrderBy(m => m.Score)
                                .ToArray());

                    Log($"Processing File: {Path.GetFileNameWithoutExtension(inputPath)}");
                    int replacedPrecursor = 0;
                    int didNotReplacePrecursor = 0;
                    List<MsDataScan> newScanList = new();
                    foreach (var scan in dataFile.Scans)
                    {
                        if (scan.MsnOrder != 2)
                        {
                            newScanList.Add(scan);
                            continue;
                        }

                        var isolationMz = scan.IsolationMz ?? throw new ArgumentException();
                        var isolationWindow = scan.IsolationRange ?? throw new ArgumentException();
                        var envelopes = ms1ToDeconvolutedEnvelopeDictionary[scan.OneBasedPrecursorScanNumber!.Value]
                            .Where(p => p.Peaks.Any(peak =>
                                isolationWindow.Minimum <= peak.mz && isolationWindow.Maximum >= peak.mz))
                            .ToArray();

                        if (envelopes.Any())
                        {
                            var ordered = envelopes.OrderBy(p =>
                                    Math.Abs(p.Peaks.MaxBy(peak => peak.intensity).mz - isolationMz).Round(2))
                                .ThenByDescending(p => p.Score);
                            var newPrecursor = ordered.First();
                            var newMz = newPrecursor.MonoisotopicMass.ToMz(newPrecursor.Charge);
                            var newInt = newPrecursor.TotalIntensity;

                            var newMs2Scan = scan.CloneWithNewPrecursor(newMz, newPrecursor.Charge, newInt);
                            newScanList.Add(newMs2Scan);
                            replacedPrecursor++;
                        }
                        else
                        {
                            didNotReplacePrecursor++;
                            newScanList.Add(scan);
                            //Debugger.Break();
                        }
                    }

                    Log($"Finsihed Processing File: {Path.GetFileNameWithoutExtension(inputPath)}");
                    Log(
                        $"Replaced {replacedPrecursor}/{replacedPrecursor + didNotReplacePrecursor} Precursors in MS2 Scans",
                        2);
                    var convertedDataFile = new GenericMsDataFile(newScanList.ToArray(), dataFile.GetSourceFile());
                    convertedDataFile.ExportAsMzML(outputPath, true);
                });

            //for (int i = 0; i < InputOutputPaths.Count; i++)
            //{
            //    var inputPath = InputOutputPaths[i].InputPath;
            //    var outputPath = InputOutputPaths[i].OutputPath;

            //    var dataFile = MsDataFileReader.GetDataFile(inputPath).LoadAllStaticData();

            //    Log($"Deconvoluting MS1 Scans File: {Path.GetFileNameWithoutExtension(inputPath)}");
            //    var ms1ToDeconvolutedEnvelopeDictionary = dataFile.Scans.Where(p => p.MsnOrder == 1)
            //        .ToDictionary(p => p.OneBasedScanNumber,
            //        p => Deconvoluter.Deconvolute(p, Parameters.PrecursorDeconvolutionParameters, p.ScanWindowRange)
            //            .OrderBy(m => m.Score)
            //            .ToArray());

            //    Log($"Processing File: {Path.GetFileNameWithoutExtension(inputPath)}");
            //    int replacedPrecursor = 0;
            //    int didNotReplacePrecursor = 0;
            //    List<MsDataScan> newScanList = new();
            //    foreach (var scan in dataFile.Scans)
            //    {
            //        if (scan.MsnOrder != 2)
            //        {
            //            newScanList.Add(scan);
            //            continue;
            //        }

            //        var isolationMz = scan.IsolationMz ?? throw new ArgumentException();
            //        var isolationWindow = scan.IsolationRange ?? throw new ArgumentException();
            //        var envelopes = ms1ToDeconvolutedEnvelopeDictionary[scan.OneBasedPrecursorScanNumber!.Value]
            //            .Where(p => p.Peaks.Any(peak => isolationWindow.Minimum <= peak.mz && isolationWindow.Maximum >= peak.mz))
            //            .ToArray();

            //        if (envelopes.Any())
            //        {
            //            var ordered = envelopes.OrderBy(p =>
            //                    Math.Abs(p.Peaks.MaxBy(peak => peak.intensity).mz - isolationMz).Round(2))
            //                .ThenByDescending(p => p.Score);
            //            var newPrecursor = ordered.First();
            //            var newMz = newPrecursor.MonoisotopicMass.ToMz(newPrecursor.Charge);
            //            var newInt = newPrecursor.TotalIntensity;

            //            var newMs2Scan = scan.CloneWithNewPrecursor(newMz, newPrecursor.Charge, newInt);
            //            newScanList.Add(newMs2Scan);
            //            replacedPrecursor++;
            //        }
            //        else
            //        {
            //            didNotReplacePrecursor++;
            //            newScanList.Add(scan);
            //            //Debugger.Break();
            //        }
            //    }

            //    Log($"Finsihed Processing File: {Path.GetFileNameWithoutExtension(inputPath)}");
            //    Log($"Replaced {replacedPrecursor}/{replacedPrecursor + didNotReplacePrecursor} Precursors in MS2 Scans", 2);
            //    var convertedDataFile = new GenericMsDataFile(newScanList.ToArray(), dataFile.GetSourceFile());
            //    convertedDataFile.ExportAsMzML(outputPath, true);
            //}
        }

        //protected override void RunSpecific()
        //{
        //    Task? outputTask = null;
        //    Task<MsDataFile> dataFileTask = LoadMsDataFileAsync(InputOutputPaths[0].InputPath);
        //    dataFileTask.Start();
        //    for (int i = 0; i < InputOutputPaths.Count; i++)
        //    {
        //        var inputPath = InputOutputPaths[i].InputPath;
        //        var outputPath = InputOutputPaths[i].OutputPath;

        //        dataFileTask.Wait();
        //        var dataFile = dataFileTask.Result;
        //        if (inputPath != InputOutputPaths.Last().InputPath)
        //        {
        //            dataFileTask = LoadMsDataFileAsync(InputOutputPaths[i + 1].InputPath);
        //            dataFileTask.Start();
        //        }

        //        Log($"Deconvoluting MS1 Scans File: {Path.GetFileNameWithoutExtension(inputPath)}");
        //        var ms1ToDeconvolutedEnvelopeDictionary = dataFile.Scans.Where(p => p.MsnOrder == 1)
        //            .ToDictionary(p => p.OneBasedScanNumber,
        //            p => Deconvoluter.Deconvolute(p, Parameters.PrecursorDeconvolutionParameters, p.ScanWindowRange)
        //                .OrderBy(m => m.Score)
        //                .ToArray());

        //        Log($"Processing File: {Path.GetFileNameWithoutExtension(inputPath)}");
        //        int replacedPrecursor = 0;
        //        int didNotReplacePrecursor = 0;
        //        List<MsDataScan> newScanList = new();
        //        foreach (var scan in dataFile.Scans)
        //        {
        //            if (scan.MsnOrder != 2)
        //            {
        //                newScanList.Add(scan);
        //                continue;
        //            }

        //            var isolationMz = scan.IsolationMz ?? throw new ArgumentException();
        //            var isolationWindow = scan.IsolationRange ?? throw new ArgumentException();
        //            var envelopes = ms1ToDeconvolutedEnvelopeDictionary[scan.OneBasedPrecursorScanNumber!.Value]
        //                .Where(p => p.Peaks.Any(peak => isolationWindow.Minimum <= peak.mz && isolationWindow.Maximum >= peak.mz))
        //                .ToArray();

        //            if (envelopes.Any())
        //            {
        //                var ordered = envelopes.OrderBy(p =>
        //                        Math.Abs(p.Peaks.MaxBy(peak => peak.intensity).mz - isolationMz).Round(2))
        //                    .ThenByDescending(p => p.Score);
        //                var newPrecursor = ordered.First();
        //                var newMz = newPrecursor.MonoisotopicMass.ToMz(newPrecursor.Charge);
        //                var newInt = newPrecursor.TotalIntensity;

        //                var newMs2Scan = scan.CloneWithNewPrecursor(newMz, newPrecursor.Charge, newInt);
        //                newScanList.Add(newMs2Scan);
        //                replacedPrecursor++;
        //            }
        //            else
        //            {
        //                didNotReplacePrecursor++;
        //                newScanList.Add(scan);
        //                //Debugger.Break();
        //            }
        //        }

        //        Log($"Finsihed Processing File: {Path.GetFileNameWithoutExtension(inputPath)}");
        //        Log($"Replaced {replacedPrecursor}/{replacedPrecursor + didNotReplacePrecursor} Precursors in MS2 Scans", 2);
        //        if (outputTask is null)
        //        {
        //            var convertedDataFile = new GenericMsDataFile(newScanList.ToArray(), dataFile.GetSourceFile());
        //            outputTask = WriteSpectraFileAsync(convertedDataFile, outputPath);
        //        }
        //        else
        //        {
        //            outputTask.Wait();
        //            var convertedDataFile = new GenericMsDataFile(newScanList.ToArray(), dataFile.GetSourceFile());
        //            outputTask = WriteSpectraFileAsync(convertedDataFile, outputPath);
        //        }
        //        outputTask.Start();
        //    }
        //    outputTask?.Wait();
        //}

        private Task<MsDataFile> LoadMsDataFileAsync(string inputPath)
        {
            return new Task<MsDataFile>(() =>
            {
                Log($"Reading File: {Path.GetFileNameWithoutExtension(inputPath)}");
                return MsDataFileReader.GetDataFile(inputPath).LoadAllStaticData();
            });
        }

        private Task WriteSpectraFileAsync(MsDataFile toExport, string outputPath)
        {
            return new Task(() =>
            {
                Log($"Writing File: {Path.GetFileNameWithoutExtension(outputPath)}");
                toExport.ExportAsMzML(outputPath, true);
            });
        }
    }
}
