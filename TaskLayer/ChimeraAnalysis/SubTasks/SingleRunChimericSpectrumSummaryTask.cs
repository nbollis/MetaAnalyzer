using Analyzer.SearchType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                            n => n.Select(b => (b.PossibleFeatureCount, b.IdPerSpectrum, b.FractionalIntensity))
                                .ToArray()));

            
            // Features per MS2 Isolation Window Histograms
            Log("Creating Feature Count Plots");
            var psmHist = Chart.Combine(new[]
            {
                GenericPlots.Histogram(
                    dataDictionary[ResultType.Psm.ToString()][true].Select(p => (double)p.PossibleFeatureCount).ToList(),
                    "Chimeric", "Features per Isolation Window", "Number of Spectra"),
                GenericPlots.Histogram(
                    dataDictionary[ResultType.Psm.ToString()][false].Select(p => (double)p.PossibleFeatureCount).ToList(),
                    "Non-Chimeric", "Features per Isolation Window", "Number of Spectra")
            })
            .WithTitle($"1% {Labels.GetLabel(mm.IsTopDown, ResultType.Psm)} Detected Features Per MS2 Isolation Window");
            var outname = $"SpectrumSummary_FeatureCount_{Labels.GetLabel(mm.IsTopDown, ResultType.Psm)}_Histogram";
            psmHist.SaveInRunResultOnly(mm, outname);

            var peptideHist = Chart.Combine(new[]
                {
                    GenericPlots.Histogram(
                        dataDictionary[ResultType.Peptide.ToString()][true].Select(p => (double)p.PossibleFeatureCount)
                            .ToList(),
                        "Chimeric", "Features per Isolation Window", "Number of Spectra"),
                    GenericPlots.Histogram(
                        dataDictionary[ResultType.Peptide.ToString()][false].Select(p => (double)p.PossibleFeatureCount)
                            .ToList(),
                        "Non-Chimeric", "Features per Isolation Window", "Number of Spectra")
                })
                .WithTitle(
                    $"1% {Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)} Detected Features Per MS2 Isolation Window");
            outname = $"SpectrumSummary_FeatureCount_{Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)}_Histogram";
            peptideHist.SaveInRunResultOnly(mm, outname);

            var psmKDE = Chart.Combine(new[]
                {
                    GenericPlots.KernelDensityPlot(
                        dataDictionary[ResultType.Psm.ToString()][true].Select(p => (double)p.PossibleFeatureCount)
                            .ToList(),
                        "Chimeric", "Features per Isolation Window", "Density"),
                    GenericPlots.KernelDensityPlot(
                        dataDictionary[ResultType.Psm.ToString()][false].Select(p => (double)p.PossibleFeatureCount)
                            .ToList(),
                        "Non-Chimeric", "Features per Isolation Window", "Density")
                })
                .WithTitle(
                    $"1% {Labels.GetLabel(mm.IsTopDown, ResultType.Psm)} Detected Features Per MS2 Isolation Window");
            outname = $"SpectrumSummary_FeatureCount_{Labels.GetLabel(mm.IsTopDown, ResultType.Psm)}_KernelDensity";
            psmKDE.SaveInRunResultOnly(mm, outname);

            var peptideKDE = Chart.Combine(new[]
                {
                    GenericPlots.KernelDensityPlot(
                        dataDictionary[ResultType.Peptide.ToString()][true].Select(p => (double)p.PossibleFeatureCount)
                            .ToList(),
                        "Chimeric", "Features per Isolation Window", "Density"),
                    GenericPlots.KernelDensityPlot(
                        dataDictionary[ResultType.Peptide.ToString()][false].Select(p => (double)p.PossibleFeatureCount)
                            .ToList(),
                        "Non-Chimeric", "Features per Isolation Window", "Density")
                })
                .WithTitle(
                    $"1% {Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)} Detected Features Per MS2 Isolation Window");
            outname = $"SpectrumSummary_FeatureCount_{Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)}_KernelDensity";
            peptideKDE.SaveInRunResultOnly(mm, outname);


            // Fractional Intensity Histograms
            Log("Creating Fractional Intensity Plots");
            psmHist = Chart.Combine(new[]
                {
                    GenericPlots.Histogram(
                        dataDictionary[ResultType.Psm.ToString()][true].Select(p => p.FractionalIntensity.Round(2)).ToList(),
                        "Chimeric", "Fractional Intensity", "Number of Spectra"),
                    GenericPlots.Histogram(
                        dataDictionary[ResultType.Psm.ToString()][false].Select(p => p.FractionalIntensity.Round(2)).ToList(),
                        "Non-Chimeric", "Fractional Intensity", "Number of Spectra")
                })
                .WithTitle($"1% {Labels.GetLabel(mm.IsTopDown, ResultType.Psm)} Detected Features Per MS2 Isolation Window");
            outname = $"SpectrumSummary_PrecursorFractionalIntensity_{Labels.GetLabel(mm.IsTopDown, ResultType.Psm)}_Histogram";
            psmHist.SaveInRunResultOnly(mm, outname);

            peptideHist = Chart.Combine(new[]
                {
                    GenericPlots.Histogram(
                        dataDictionary[ResultType.Peptide.ToString()][true].Select(p => p.FractionalIntensity.Round(2)).ToList(),
                        "Chimeric", "Fractional Intensity", "Number of Spectra"),
                    GenericPlots.Histogram(
                        dataDictionary[ResultType.Peptide.ToString()][false].Select(p => p.FractionalIntensity.Round(2)).ToList(),
                        "Non-Chimeric", "Fractional Intensity", "Number of Spectra")
                })
                .WithTitle($"1% {Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)} Precursor Fractional Intensity");
            outname = $"SpectrumSummary_PrecursorFractionalIntensity_{Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)}_Histogram";
            peptideHist.SaveInRunResultOnly(mm, outname);

            psmKDE = Chart.Combine(new[]
                {
                    GenericPlots.KernelDensityPlot(
                        dataDictionary[ResultType.Psm.ToString()][true].Select(p => p.FractionalIntensity)
                            .ToList(),
                        "Chimeric", "Fractional Intensity", "Density"),
                    GenericPlots.KernelDensityPlot(
                        dataDictionary[ResultType.Psm.ToString()][false].Select(p => p.FractionalIntensity)
                            .ToList(),
                        "Non-Chimeric", "Fractional Intensity", "Density")
                })
                .WithTitle($"1% {Labels.GetLabel(mm.IsTopDown, ResultType.Psm)} Precursor Fractional Intensity");
            outname =
                $"SpectrumSummary_PrecursorFractionalIntensity_{Labels.GetLabel(mm.IsTopDown, ResultType.Psm)}_KernelDensity";
            psmKDE.SaveInRunResultOnly(mm, outname);

            peptideKDE = Chart.Combine(new[]
                {
                    GenericPlots.KernelDensityPlot(
                        dataDictionary[ResultType.Peptide.ToString()][true].Select(p => p.FractionalIntensity)
                            .ToList(),
                        "Chimeric", "Fractional Intensity", "Density"),
                    GenericPlots.KernelDensityPlot(
                        dataDictionary[ResultType.Peptide.ToString()][false].Select(p => p.FractionalIntensity)
                            .ToList(),
                        "Non-Chimeric", "Fractional Intensity", "Density")
                })
                .WithTitle($"1% {Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)} Precursor Fractional Intensity");
            outname =
                $"SpectrumSummary_PrecursorFractionalIntensity_{Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)}_KernelDensity";
            peptideKDE.SaveInRunResultOnly(mm, outname);
        }
    }
}
