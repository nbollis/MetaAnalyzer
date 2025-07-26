﻿using Analyzer.Plotting;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Plotly.NET;
using Plotting;
using Plotting.Util;
using Readers;
using ResultAnalyzerUtil;

namespace TaskLayer.ChimeraAnalysis
{
    public class SingleRunSpectralAngleComparisonTask : BaseResultAnalyzerTask
    {
        public override MyTask MyTask => MyTask.SpectralAngleComparisonTask;
        public override SingleRunAnalysisParameters Parameters { get; }

        public SingleRunSpectralAngleComparisonTask(SingleRunAnalysisParameters parameters)
        {
            Parameters = parameters;
        }

        /// <summary>
        /// Calculate the spectral similarity distribution between chimeric and non-chimeric identifications and output the plots
        /// </summary>
        protected override void RunSpecific()
        {
            MetaMorpheusResult mm;
            switch (Parameters.RunResult)
            {
                case null:
                    try
                    {
                        mm = new MetaMorpheusResult(Parameters.SingleRunResultsDirectoryPath);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }

                    break;
                case MetaMorpheusResult m:
                    mm = m;
                    break;
                default:
                    return;
            }

            var peptideChimericAngles = new List<double>();
            var peptideNonChimericAngles = new List<double>();
            Log("Parsing Peptide Spectral Angles");
            foreach (var individualFileResult in mm.IndividualFileResults)
            {
                foreach (var chimeraGroup in individualFileResult.AllPeptides.Where(p => p is { PEP_QValue: <= 0.01, DecoyContamTarget: "T" })
                             .GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer))
                {
                    var filtered = chimeraGroup.Where(p => !p.SpectralAngle.Equals(-1.0) && !p.SpectralAngle.Equals(double.NaN) && !p.SpectralAngle.Equals(null)).ToArray();

                    if (!filtered.Any()) continue;
                    if (chimeraGroup.Count() == 1)
                    {
                        var first = filtered.First();
                        if (first.SpectralAngle.HasValue)
                            peptideNonChimericAngles.Add(first.SpectralAngle!.Value);
                    }
                    else
                    {
                        peptideChimericAngles.AddRange(filtered.Select(p => p.SpectralAngle!.Value));
                    }
                }
            }

            string outName;
            GenericChart.GenericChart peptidePlot = null;
            Log("Plotting Peptide Angles");
            switch (Parameters.PlotType)
            {
                case DistributionPlotTypes.ViolinPlot:
                    peptidePlot = AnalyzerGenericPlots.SpectralAngleChimeraComparisonViolinPlot(peptideChimericAngles.ToArray(), peptideNonChimericAngles.ToArray(),
                                               "", mm.IsTopDown, ResultType.Peptide);
                    outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)}_{FileIdentifiers.SpectralAngleFigure}_ViolinPlot";
                    break;

                case DistributionPlotTypes.BoxPlot:
                    peptidePlot = Chart.Combine(new[]
                    {
                        GenericPlots.BoxPlot(peptideChimericAngles, "Chimeric",  "", "Spectral Angle" ),
                        GenericPlots.BoxPlot(peptideNonChimericAngles, "Non-Chimeric", "", "Spectral Angle")
                    });
                    outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)}_{FileIdentifiers.SpectralAngleFigure}_BoxPlot";
                    break;
                case DistributionPlotTypes.KernelDensity:
                    peptidePlot = Chart.Combine(new[]
                    {
                        GenericPlots.KernelDensityPlot(peptideChimericAngles, "Chimeric",  "Spectral Angle", "Density", 0.02),
                        GenericPlots.KernelDensityPlot(peptideNonChimericAngles, "Non-Chimeric", "Spectral Angle", "Density", 0.02)
                    });
                    outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)}_{FileIdentifiers.SpectralAngleFigure}_KernelDensity";
                    break;
                case DistributionPlotTypes.Histogram:
                    peptidePlot = Chart.Combine(new[]
                    {
                        GenericPlots.Histogram(peptideChimericAngles, "Chimeric",  "Spectral Angle", "Count" ),
                        GenericPlots.Histogram(peptideNonChimericAngles, "Non-Chimeric", "Spectral Angle", "Count")
                    });
                    outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)}_{FileIdentifiers.SpectralAngleFigure}_Histogram";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            peptidePlot = peptidePlot.WithTitle(
                    $"MetaMorpheus 1% {Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)} Spectral Angle Distribution");
            peptidePlot.SaveInRunResultOnly(mm, outName, 600, 600);


            var psmChimericAngles = new List<double>();
            var psmNonChimericAngles = new List<double>();
            Log("Parsing Psm Angles");
            foreach (var individualFileResult in mm.IndividualFileResults)
            {
                foreach (var chimeraGroup in individualFileResult.AllPsms.Where(p => p is { PEP_QValue: <= 0.01, DecoyContamTarget: "T" })
                             .GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer))
                {
                    var filtered = chimeraGroup.Where(p =>!p.SpectralAngle.Equals(-1.0) && !p.SpectralAngle.Equals(double.NaN) &&
                        !p.SpectralAngle.Equals(null)).ToArray();

                    if (!filtered.Any()) continue;
                    if (chimeraGroup.Count() == 1)
                    {
                        psmNonChimericAngles.AddRange(filtered.Select(p => p.SpectralAngle!.Value));
                    }
                    else
                    {
                        psmChimericAngles.AddRange(filtered.Select(p => p.SpectralAngle!.Value));
                    }
                }
            }

            GenericChart.GenericChart psmPlot = null;
            Log("Plotting Psm Angles");
            switch (Parameters.PlotType)
            {
                case DistributionPlotTypes.ViolinPlot:
                    psmPlot = AnalyzerGenericPlots.SpectralAngleChimeraComparisonViolinPlot(psmChimericAngles.ToArray(), psmNonChimericAngles.ToArray(),
                        "", mm.IsTopDown, ResultType.Peptide);
                    outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Psm)}_{FileIdentifiers.SpectralAngleFigure}_ViolinPlot";
                    break;

                case DistributionPlotTypes.BoxPlot:
                    psmPlot = Chart.Combine(new[]
                    {
                        GenericPlots.BoxPlot(psmChimericAngles, "Chimeric",  "Chimeric", "Spectral Angle" ),
                        GenericPlots.BoxPlot(psmNonChimericAngles, "Non-Chimeric", "Non-Chimeric", "Spectral Angle")
                    });
                    outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Psm)}_{FileIdentifiers.SpectralAngleFigure}_BoxPlot";
                    break;
                case DistributionPlotTypes.KernelDensity:
                    psmPlot = Chart.Combine(new[]
                    {
                        GenericPlots.KernelDensityPlot(psmChimericAngles, "Chimeric",  "Spectral Angle", "Density", 0.02),
                        GenericPlots.KernelDensityPlot(psmNonChimericAngles, "Non-Chimeric", "Spectral Angle", "Density", 0.02)
                    });
                    outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Psm)}_{FileIdentifiers.SpectralAngleFigure}_KernelDensity";
                    break;
                case DistributionPlotTypes.Histogram:
                    psmPlot = Chart.Combine(new[]
                    {
                        GenericPlots.Histogram(psmChimericAngles, "Chimeric",  "Spectral Angle", "Count" ),
                        GenericPlots.Histogram(psmNonChimericAngles, "Non-Chimeric", "Spectral Angle", "Count")
                    });
                    outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Psm)}_{FileIdentifiers.SpectralAngleFigure}_Histogram";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            psmPlot = psmPlot.WithTitle(
                                   $"MetaMorpheus 1% {Labels.GetLabel(mm.IsTopDown, ResultType.Psm)} Spectral Angle Distribution");
            psmPlot.SaveInRunResultOnly(mm, outName, 600, 600);
            mm.Dispose();
        }
    }
}
