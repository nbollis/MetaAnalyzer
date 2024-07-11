using Analyzer.Plotting.Util;
using Analyzer.Plotting;
using Analyzer.SearchType;
using Analyzer.Util;
using Proteomics.PSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plotly.NET;

namespace TaskLayer.ChimeraAnalysis
{
    public class SingleRunChimeraRetentionTimeDistribution : BaseResultAnalyzerTask
    {
        public override MyTask MyTask => MyTask.RetentionTimeComparison;
        public override SingleRunAnalysisParameters Parameters { get; }

        public SingleRunChimeraRetentionTimeDistribution(SingleRunAnalysisParameters parameters)
        {
            Parameters = parameters;
        }

        protected override void RunSpecific()
        {
            if (Parameters.RunResult is not MetaMorpheusResult mm)
                return;

            var peptideChimericRT = new List<double>();
            var peptideNonChimericRT = new List<double>();
            Log("Parsing Peptide Retention Times");
            foreach (var individualFileResult in mm.IndividualFileResults)
            {
                foreach (var chimeraGroup in individualFileResult.AllPeptides.Where(p => p is { PEP_QValue: <= 0.01, DecoyContamTarget: "T" })
                             .GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer))
                {
                    if (!chimeraGroup.Any()) continue;
                    if (chimeraGroup.Count() == 1)
                    {
                        var first = chimeraGroup.First();
                        peptideNonChimericRT.Add(first.RetentionTime!.Value);
                    }
                    else
                    {
                        peptideChimericRT.AddRange(chimeraGroup.Select(p => p.RetentionTime!.Value));
                    }
                }
            }

            string outName;
            GenericChart.GenericChart peptidePlot = null;
            Log("Plotting Peptide Retention Times");
            switch (Parameters.PlotType)
            {
                case DistributionPlotTypes.ViolinPlot:
                    peptidePlot = GenericPlots.SpectralAngleChimeraComparisonViolinPlot(peptideChimericRT.ToArray(), peptideNonChimericRT.ToArray(),
                                               "", mm.IsTopDown, ResultType.Peptide);
                    outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)}_{FileIdentifiers.RetentionTimeFigure}_ViolinPlot";
                    break;

                case DistributionPlotTypes.BoxPlot:
                    peptidePlot = Chart.Combine(new[]
                    {
                        GenericPlots.BoxPlot(peptideChimericRT, "Chimeric",  "", "Retention Time" ),
                        GenericPlots.BoxPlot(peptideNonChimericRT, "Non-Chimeric", "", "Retention Time")
                    });
                    outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)}_{FileIdentifiers.RetentionTimeFigure}_BoxPlot";
                    break;
                case DistributionPlotTypes.KernelDensity:
                    peptidePlot = Chart.Combine(new[]
                    {
                        GenericPlots.KernelDensityPlot(peptideChimericRT, "Chimeric",  "Retention Time", "Density", 0.2),
                        GenericPlots.KernelDensityPlot(peptideNonChimericRT, "Non-Chimeric", "Retention Time", "Density", 0.2)
                    });
                    outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)}_{FileIdentifiers.RetentionTimeFigure}_KernelDensity";
                    break;
                case DistributionPlotTypes.Histogram:
                    peptidePlot = Chart.Combine(new[]
                    {
                        GenericPlots.Histogram(peptideChimericRT, "Chimeric",  "Retention Time", "Count" ),
                        GenericPlots.Histogram(peptideNonChimericRT, "Non-Chimeric", "Retention Time", "Count")
                    });
                    outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)}_{FileIdentifiers.RetentionTimeFigure}_Histogram";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            peptidePlot = peptidePlot.WithTitle(
                    $"MetaMorpheus 1% {Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)} Retention Time Distribution");
            peptidePlot.SaveInRunResultOnly(mm, outName, 600, 600);


            var psmChimericRT = new List<double>();
            var psmNonChimericRT = new List<double>();
            Log("Parsing Psm Retention Times");
            foreach (var individualFileResult in mm.IndividualFileResults)
            {
                foreach (var chimeraGroup in individualFileResult.AllPsms.Where(p => p is { PEP_QValue: <= 0.01, DecoyContamTarget: "T" })
                             .GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer))
                {
                    if (!chimeraGroup.Any()) continue;
                    if (chimeraGroup.Count() == 1)
                    {
                        psmNonChimericRT.AddRange(chimeraGroup.Select(p => p.RetentionTime!.Value));
                    }
                    else
                    {
                        psmChimericRT.AddRange(chimeraGroup.Select(p => p.RetentionTime!.Value));
                    }
                }
            }

            GenericChart.GenericChart psmPlot = null;
            Log("Plotting Psm Retention Times");
            switch (Parameters.PlotType)
            {
                case DistributionPlotTypes.ViolinPlot:
                    psmPlot = GenericPlots.SpectralAngleChimeraComparisonViolinPlot(psmChimericRT.ToArray(), psmNonChimericRT.ToArray(),
                        "", mm.IsTopDown, ResultType.Peptide);
                    outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Psm)}_{FileIdentifiers.RetentionTimeFigure}_ViolinPlot";
                    break;

                case DistributionPlotTypes.BoxPlot:
                    psmPlot = Chart.Combine(new[]
                    {
                        GenericPlots.BoxPlot(psmChimericRT, "Chimeric",  "Chimeric", "Retention Time" ),
                        GenericPlots.BoxPlot(psmNonChimericRT, "Non-Chimeric", "Non-Chimeric", "Retention Time")
                    });
                    outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Psm)}_{FileIdentifiers.RetentionTimeFigure}_BoxPlot";
                    break;
                case DistributionPlotTypes.KernelDensity:
                    psmPlot = Chart.Combine(new[]
                    {
                        GenericPlots.KernelDensityPlot(psmChimericRT, "Chimeric",  "Retention Time", "Density", 0.2),
                        GenericPlots.KernelDensityPlot(psmNonChimericRT, "Non-Chimeric", "Retention Time", "Density", 0.2)
                    });
                    outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Psm)}_{FileIdentifiers.RetentionTimeFigure}_KernelDensity";
                    break;
                case DistributionPlotTypes.Histogram:
                    psmPlot = Chart.Combine(new[]
                    {
                        GenericPlots.Histogram(psmChimericRT, "Chimeric",  "Retention Time", "Count" ),
                        GenericPlots.Histogram(psmNonChimericRT, "Non-Chimeric", "Retention Time", "Count")
                    });
                    outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Psm)}_{FileIdentifiers.RetentionTimeFigure}_Histogram";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            psmPlot = psmPlot.WithTitle(
                                   $"MetaMorpheus 1% {Labels.GetLabel(mm.IsTopDown, ResultType.Psm)} Retention Time Distribution");
            psmPlot.SaveInRunResultOnly(mm, outName, 600, 600);
        }
    }
}
