﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        protected override ChimericSpectrumConverterParameters Parameters { get; }

        public ChimericSpectrumConverterTask(ChimericSpectrumConverterParameters parameters)
        {
            Parameters = parameters;
            InputOutputPaths = new List<(string, string)>();
            foreach (var inputFile in parameters.SpectraFiles)
            {
                if (!File.Exists(inputFile))
                    throw new ArgumentException($"Spectra File Not Found: {inputFile}");
                var outputFilename = inputFile.Replace(Path.GetExtension(inputFile), "_SetPrecursor.mzML");

                if (File.Exists(outputFilename) && !parameters.Override)
                {
                    Warn($"File already exists: {outputFilename}");
                    continue;
                }
                InputOutputPaths.Add((inputFile, outputFilename));
            }
        }


        protected override void RunSpecific()
        {
            Task? outputTask = null;
            Task<MsDataFile> dataFileTask = LoadMsDataFileAsync(InputOutputPaths[0].InputPath);
            dataFileTask.Start();
            for (int i = 0; i < InputOutputPaths.Count; i++)
            {
                var inputPath = InputOutputPaths[i].InputPath;
                var outputPath = InputOutputPaths[i].OutputPath;

                dataFileTask.Wait();
                var dataFile = dataFileTask.Result;
                if (inputPath != InputOutputPaths.Last().InputPath)
                    dataFileTask = LoadMsDataFileAsync(InputOutputPaths[i+1].InputPath);

                List<MsDataScan> newScanList = new();
                var ms1ToDeconvolutedEnvelopeDictionary = dataFile.Scans.ToDictionary(p => p.OneBasedScanNumber,
                    p => Deconvoluter.Deconvolute(p, Parameters.PrecursorDeconvolutionParameters, p.ScanWindowRange)
                        .OrderBy(m => m.Score)
                        .ToArray());

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
                        .Where(p => p.Peaks.Any(peak => isolationWindow.Contains(peak.mz))).ToArray();

                    if (envelopes.Any())
                    {
                        var ordered = envelopes.OrderBy(p =>
                                Math.Abs(p.Peaks.MaxBy(peak => peak.intensity).mz - isolationMz).Round(2))
                            .ThenBy(p => p.Score);
                        var newPrecursor = ordered.First();
                        var newMz = newPrecursor.MonoisotopicMass.ToMz(newPrecursor.Charge);
                        var newInt = newPrecursor.TotalIntensity;

                        var newMs2Scan = scan.CloneWithNewPrecursor(newMz, newPrecursor.Charge, newInt);
                        newScanList.Add(newMs2Scan);
                    }
                    else
                        Debugger.Break();

                }

                var convertedDataFile = new GenericMsDataFile(newScanList.ToArray(), dataFile.GetSourceFile());
                if (outputTask is null)
                    outputTask = WriteSpectraFileAsync(convertedDataFile, outputPath);
                else
                {
                    outputTask.Wait();
                    outputTask = WriteSpectraFileAsync(convertedDataFile, outputPath);
                }
                outputTask.Start();

            }

            outputTask?.Wait();
        }

        private Task<MsDataFile> LoadMsDataFileAsync(string inputPath)
        {
            return new Task<MsDataFile>(() =>
            {
                Log($"Reading File: {Path.GetFileNameWithoutExtension(inputPath)}, 2");
                return MsDataFileReader.GetDataFile(inputPath).LoadAllStaticData();
            });
        }

        private Task WriteSpectraFileAsync(MsDataFile toExport, string outputPath)
        {
            return new Task(() =>
            {
                Log($"Writing File: {Path.GetFileNameWithoutExtension(outputPath)}, 2");
                toExport.ExportAsMzML(outputPath, true);
            });
        }
    }
}