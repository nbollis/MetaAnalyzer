using Analyzer.SearchType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.FileTypes.Internal;
using Analyzer.Plotting;
using Analyzer.Plotting.Util;
using Analyzer.Util;
using MathNet.Numerics;
using Plotly.NET;

namespace TaskLayer.ChimeraAnalysis
{
    public class SingleRunChimericSpectrumSummaryTask : BaseResultAnalyzerTask
    {
        public override MyTask MyTask => MyTask.ChimericSpectrumSummary;
        public override SingleRunAnalysisParameters Parameters { get; }

        public SingleRunChimericSpectrumSummaryTask(SingleRunAnalysisParameters parameters)
        {
            Parameters = parameters;
        }

        protected override void RunSpecific()
        {
            if (Parameters.RunResult is not MetaMorpheusResult mm)
                return;

            // Run the parsing
            mm.Override = Parameters.Override;
            var summary = mm.GetChimericSpectrumSummaryFile();
            mm.Override = false;

            // Plot the results
            var dataDictionary = summary.GroupBy(p => p.Type)
                .ToDictionary(p => p.Key,
                    p => p.GroupBy(m => m.IsChimeric)
                        .ToDictionary(n => n.Key,
                            n => n.Select(b => (b.PossibleFeatureCount, b.IdPerSpectrum, b.PrecursorFractionalIntensity, b.FragmentFractionalIntensity))
                                .ToArray()));

            // Features per MS2 Isolation Window Histograms
            Log("Creating Feature Count Plots");
            GeneratePossibleFeatureHistogram(ResultType.Psm,
                dataDictionary[ResultType.Psm.ToString()][true].Select(p => (double)p.PossibleFeatureCount).ToList(),
                dataDictionary[ResultType.Psm.ToString()][false].Select(p => (double)p.PossibleFeatureCount).ToList());
            GeneratePossibleFeatureHistogram(ResultType.Peptide,
                dataDictionary[ResultType.Peptide.ToString()][true].Select(p => (double)p.PossibleFeatureCount)
                    .ToList(),
                dataDictionary[ResultType.Peptide.ToString()][false].Select(p => (double)p.PossibleFeatureCount)
                    .ToList());

            GeneratePossibleFeatureKDE(ResultType.Psm,
                dataDictionary[ResultType.Psm.ToString()][true].Select(p => (double)p.PossibleFeatureCount).ToList(),
                dataDictionary[ResultType.Psm.ToString()][false].Select(p => (double)p.PossibleFeatureCount).ToList());
            GeneratePossibleFeatureKDE(ResultType.Peptide,
                dataDictionary[ResultType.Peptide.ToString()][true].Select(p => (double)p.PossibleFeatureCount)
                    .ToList(),
                dataDictionary[ResultType.Peptide.ToString()][false].Select(p => (double)p.PossibleFeatureCount)
                    .ToList());

            // Fractional Intensity Histograms
            Log("Creating Fragment Fractional Intensity Plots");
            GenerateFractionalIntensityHistogram(ResultType.Psm,
                dataDictionary[ResultType.Psm.ToString()][true].Select(p => p.FragmentFractionalIntensity).ToList(),
                dataDictionary[ResultType.Psm.ToString()][false].Select(p => p.FragmentFractionalIntensity).ToList(), false);
            GenerateFractionalIntensityHistogram(ResultType.Peptide,
                dataDictionary[ResultType.Peptide.ToString()][true].Select(p => p.FragmentFractionalIntensity).ToList(),
                dataDictionary[ResultType.Peptide.ToString()][false].Select(p => p.FragmentFractionalIntensity).ToList(), false);

            GenerateFractionalIntensityKDE(ResultType.Psm,
                dataDictionary[ResultType.Psm.ToString()][true].Select(p => p.FragmentFractionalIntensity).ToList(),
                dataDictionary[ResultType.Psm.ToString()][false].Select(p => p.FragmentFractionalIntensity).ToList(), false);
            GenerateFractionalIntensityKDE(ResultType.Peptide,
                dataDictionary[ResultType.Peptide.ToString()][true].Select(p => p.FragmentFractionalIntensity).ToList(),
                dataDictionary[ResultType.Peptide.ToString()][false].Select(p => p.FragmentFractionalIntensity).ToList(), false);


            Log("Creating Precursor Fractional Intensity Plots");
            var summedPrecursorIntDict = summary.Results
                .GroupBy(p => p.Type)
                .ToDictionary(p => p.Key, 
                    p => p.GroupBy(m => m, 
                            new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber, result => result.FileName))
                        .ToDictionary(n => n.Key, n => n.Select(b => b.PrecursorFractionalIntensity).Sum()));
            
            GenerateFractionalIntensityHistogram(ResultType.Psm,
                summedPrecursorIntDict[ResultType.Psm.ToString()].Where(p => p.Key.IsChimeric).Select(m => m.Value).ToList(),
                summedPrecursorIntDict[ResultType.Psm.ToString()].Where(p => !p.Key.IsChimeric).Select(m => m.Value).ToList());
            GenerateFractionalIntensityHistogram(ResultType.Peptide,
                summedPrecursorIntDict[ResultType.Peptide.ToString()].Where(p => p.Key.IsChimeric).Select(m => m.Value).ToList(),
                summedPrecursorIntDict[ResultType.Peptide.ToString()].Where(p => !p.Key.IsChimeric).Select(m => m.Value).ToList());

            GenerateFractionalIntensityKDE(ResultType.Psm,
                summedPrecursorIntDict[ResultType.Psm.ToString()].Where(p => p.Key.IsChimeric).Select(m => m.Value).ToList(),
                summedPrecursorIntDict[ResultType.Psm.ToString()].Where(p => !p.Key.IsChimeric).Select(m => m.Value).ToList());
            GenerateFractionalIntensityKDE(ResultType.Peptide,
                summedPrecursorIntDict[ResultType.Peptide.ToString()].Where(p => p.Key.IsChimeric).Select(m => m.Value).ToList(),
                summedPrecursorIntDict[ResultType.Peptide.ToString()].Where(p => !p.Key.IsChimeric).Select(m => m.Value).ToList());





            mm.Dispose();
        }

        private void GeneratePossibleFeatureHistogram(ResultType resultType, List<double> chimeric,
            List<double> nonChimeric)
        {
            var psmHist = Chart.Combine(new[]
                {
                    GenericPlots.Histogram(chimeric,
                        "Chimeric", "Features per Isolation Window", "Number of Spectra"),
                    GenericPlots.Histogram(nonChimeric,
                        "Non-Chimeric", "Features per Isolation Window", "Number of Spectra")
                })
                .WithTitle(
                    $"1% {Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)} Detected Features Per MS2 Isolation Window");
            var outname =
                $"SpectrumSummary_FeatureCount_{Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)}_Histogram";
            psmHist.SaveInRunResultOnly(Parameters.RunResult, outname);
        }

        private void GeneratePossibleFeatureKDE(ResultType resultType, List<double> chimeric, List<double> nonChimeric)
        {
            var psmKDE = Chart.Combine(new[]
                {
                    GenericPlots.KernelDensityPlot(chimeric,
                        "Chimeric", "Features per Isolation Window", "Density", 0.5),
                    GenericPlots.KernelDensityPlot(nonChimeric,
                        "Non-Chimeric", "Features per Isolation Window", "Density", 0.5)
                })
                .WithTitle(
                    $"1% {Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)} Detected Features Per MS2 Isolation Window");
            var outname =
                $"SpectrumSummary_FeatureCount_{Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)}_KernelDensity";
            psmKDE.SaveInRunResultOnly(Parameters.RunResult, outname);
        }

        private void GenerateFractionalIntensityHistogram(ResultType resultType, List<double> chimeric,
            List<double> nonChimeric, bool isPrecursor = true)
        {
            var label = isPrecursor ? "Precursor Fractional Intensity" : "Fragment Fractional Intensity";
            var titleEnd = isPrecursor ? "Per Isolation Window" : "Per MS2";
            var outType = isPrecursor ? "Precursor" : "Fragment";

            var psmHist = Chart.Combine(new[]
                {
                    GenericPlots.Histogram(chimeric,
                        "Chimeric", label, "Number of Spectra"),
                    GenericPlots.Histogram(nonChimeric,
                        "Non-Chimeric", label, "Number of Spectra")
                })
                .WithTitle(
                    $"1% {Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)} Detected Features {titleEnd}");
            var outname =
                $"SpectrumSummary_{outType}FractionalIntensity_{Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)}_Histogram";
            psmHist.SaveInRunResultOnly(Parameters.RunResult, outname);
        }

        private void GenerateFractionalIntensityKDE(ResultType resultType, List<double> chimeric,
            List<double> nonChimeric, bool isPrecursor = true)
        {
            var label = isPrecursor ? "Precursor Fractional Intensity" : "Fragment Fractional Intensity";
            var titleEnd = isPrecursor ? "Per Isolation Window" : "Per MS2";
            var outType = isPrecursor ? "Precursor" : "Fragment";

            var psmKDE = Chart.Combine(new[]
                {
                    GenericPlots.KernelDensityPlot(chimeric,
                        "Chimeric", label, "Density", 0.02),
                    GenericPlots.KernelDensityPlot(nonChimeric,
                        "Non-Chimeric", label, "Density", 0.02)
                })
                .WithTitle(
                    $"1% {Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)} Detected Features {titleEnd}");
            var outname =
                $"SpectrumSummary_{outType}FractionalIntensity_{Labels.GetLabel(Parameters.RunResult.IsTopDown, resultType)}_KernelDensity";
            psmKDE.SaveInRunResultOnly(Parameters.RunResult, outname);
        }


    }

}
