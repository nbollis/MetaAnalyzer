using Analyzer.Plotting;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using Proteomics.PSM;

namespace TaskLayer.ChimeraAnalysis
{
    public class ChimeraPaperSpectralAngleComparisonTask : BaseResultAnalyzerTask
    {
        public override MyTask MyTask => MyTask.SpectralAngleComparisonTask;
        protected override SingleRunAnalysisParameters Parameters { get; }

        public ChimeraPaperSpectralAngleComparisonTask(SingleRunAnalysisParameters parameters)
        {
            Parameters = parameters;
        }

        /// <summary>
        /// Calculate the spectral similarity distribution between chimeric and non-chimeric identifications and output the plots
        /// </summary>
        protected override void RunSpecific()
        {
            if (Parameters.RunResult is not MetaMorpheusResult mm)
                return;

            Log("Reading Peptides File");
            var peptides = mm.AllPeptides.Where(p => p is { PEP_QValue: <= 0.01, DecoyContamTarget: "T" }).ToArray();

            var peptideChimericAngles = new List<double>();
            var peptideNonChimericAngles = new List<double>();
            Log("Parsing Peptide Spectral Angles");
            foreach (var chimeraGroup in peptides.GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer))
            {
                var filtered = chimeraGroup.Where(p => !p.SpectralAngle.Equals(-1.0) && !p.SpectralAngle.Equals(double.NaN) && !p.SpectralAngle.Equals(null)).ToArray();

                if (!filtered.Any()) continue;
                if (chimeraGroup.Count() == 1)
                {
                    peptideNonChimericAngles.AddRange(filtered.Select(p => p.SpectralAngle!.Value));
                }
                else
                {
                    peptideChimericAngles.AddRange(filtered.Select(p => p.SpectralAngle!.Value));
                }
            }

            Log("Plotting Peptide Angles");
            var peptidePlot = GenericPlots.SpectralAngleChimeraComparisonViolinPlot(peptideChimericAngles.ToArray(), peptideNonChimericAngles.ToArray(),
                "", mm.IsTopDown, ResultType.Peptide);
            var outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Peptide)}_{FileIdentifiers.SpectralAngleFigure}";
            peptidePlot.SaveInRunResultOnly(mm, outName, 600, 600);




            Log("Reading Psm File");
            var psms = mm.AllPsms.Where(p => p is { PEP_QValue: <= 0.01, DecoyContamTarget: "T" }).ToArray();

            var psmChimericAngles = new List<double>();
            var psmNonChimericAngles = new List<double>();
            Log("Parsing Psm Angles");
            foreach (var chimeraGroup in psms.GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer))
            {
                var filtered = chimeraGroup.Where(p => !p.SpectralAngle.Equals(-1.0) && !p.SpectralAngle.Equals(double.NaN) && !p.SpectralAngle.Equals(null)).ToArray();

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

            Log("Plotting Psm Angles");
            var psmPlot = GenericPlots.SpectralAngleChimeraComparisonViolinPlot(psmChimericAngles.ToArray(), psmNonChimericAngles.ToArray(),
                "", mm.IsTopDown, ResultType.Psm);
            outName = $"FdrAnalysis_{Labels.GetLabel(mm.IsTopDown, ResultType.Psm)}_{FileIdentifiers.SpectralAngleFigure}";
            psmPlot.SaveInRunResultOnly(mm, outName, 600, 600);
        }
    }
}
