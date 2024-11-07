using Analyzer.FileTypes.Internal;
using Analyzer.Interfaces;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Plotting.IndividualRunPlots;
using Plotly.NET;
using Plotly.NET.ImageExport;
using Plotting.Util;
using Proteomics.PSM;
using ResultAnalyzerUtil;

namespace Analyzer.Plotting.AggregatePlots
{
    public static class CellLineAggregatePlots
    {

        public static void PlotCellLineSpectralSimilarity(this CellLineResults cellLine)
        {

            string outpath = Path.Combine(cellLine.GetChimeraPaperFigureDirectory(), $"{FileIdentifiers.SpectralAngleFigure}_{cellLine.CellLine}");
            var chart = cellLine.GetCellLineSpectralSimilarity();
            chart.SavePNG(outpath);
            outpath = Path.Combine(cellLine.FigureDirectory, $"{FileIdentifiers.SpectralAngleFigure}_{cellLine.CellLine}");
            cellLine.GetCellLineSpectralSimilarity().SavePNG(outpath);
        }

        internal static GenericChart.GenericChart GetCellLineSpectralSimilarity(this CellLineResults cellLine)
        {
            bool isTopDown = cellLine.First().IsTopDown;
            double[] chimeraAngles;
            double[] nonChimeraAngles;
            if (isTopDown)
            {
                var angles = cellLine.Results
                    .Where(p => isTopDown.GetSingleResultSelector(cellLine.CellLine).Contains(p.Condition))
                    .SelectMany(p => ((MetaMorpheusResult)p).AllPeptides.Where(m => m.SpectralAngle is not -1 or double.NaN))
                    .GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer)
                    .SelectMany(chimeraGroup =>
                        chimeraGroup.Select(prsm => (prsm.SpectralAngle ?? -1, chimeraGroup.Count() > 1)))
                    .ToList();
                chimeraAngles = angles.Where(p => p.Item2).Select(p => p.Item1).ToArray();
                nonChimeraAngles = angles.Where(p => !p.Item2).Select(p => p.Item1).ToArray();
            }
            else
            {
                var angles = cellLine.Results
                    .Where(p => isTopDown.GetSingleResultSelector(cellLine.CellLine).Contains(p.Condition))
                    .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
                    .Select(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile)
                    .SelectMany(p => p.Where(m => m.SpectralAngle is not -1 or double.NaN))
                    .ToList();
                chimeraAngles = angles.Where(p => p.IsChimeric).Select(p => p.SpectralAngle).ToArray();
                nonChimeraAngles = angles.Where(p => !p.IsChimeric).Select(p => p.SpectralAngle).ToArray();
            }

            return AnalyzerGenericPlots.SpectralAngleChimeraComparisonViolinPlot(chimeraAngles, nonChimeraAngles, cellLine.CellLine, isTopDown, ResultType.Peptide);
        }


        #region Target Decoy

        /// <summary>
        /// Stacked Column: Plots the target decoy distribution as a function of the degree of chimericity
        /// </summary>
        /// <param name="cellLine"></param>
        /// <param name="absolute"></param>
        public static void PlotCellLineChimeraBreakdown_TargetDecoy(this CellLineResults cellLine, bool absolute = false)
        {
            bool isTopDown = cellLine.First().IsTopDown;
            List<ChimeraBreakdownRecord> results;
            try // plot aggregated cell line result for specific targeted file from the selector
            {
                var selector = isTopDown.GetSingleResultSelector(cellLine.CellLine);
                results = cellLine.Where(p => p is IChimeraBreakdownCompatible && selector.Contains(p.Condition))
                    .SelectMany(p => ((IChimeraBreakdownCompatible)p).ChimeraBreakdownFile.Results)
                    .ToList();
            }
            catch
            {
                results = cellLine.Where(p => p is IChimeraBreakdownCompatible)
                    .SelectMany(p => ((IChimeraBreakdownCompatible)p).ChimeraBreakdownFile.Results)
                    .ToList();
            }
            var smLabel = Labels.GetSpectrumMatchLabel(isTopDown);
            var pepLabel = Labels.GetPeptideLabel(isTopDown);
            string smOutName = $"{FileIdentifiers.ChimeraBreakdownTargetDecoy}_{smLabel}_{cellLine.CellLine}";
            string pepOutName = $"{FileIdentifiers.ChimeraBreakdownTargetDecoy}_{pepLabel}_{cellLine.CellLine}";


            var psmChart =
                results.GetChimeraBreakDownStackedColumn_TargetDecoy(ResultType.Psm, cellLine.First().IsTopDown, absolute, out int width);
            string psmOutPath = Path.Combine(cellLine.GetChimeraPaperFigureDirectory(), smOutName);
            psmChart.SavePNG(psmOutPath, null, width, PlotlyBase.DefaultHeight);
            psmOutPath = Path.Combine(cellLine.FigureDirectory, smOutName);
            psmChart.SavePNG(psmOutPath, null, width, PlotlyBase.DefaultHeight);

            var peptideChart =
                results.GetChimeraBreakDownStackedColumn_TargetDecoy(ResultType.Peptide, cellLine.First().IsTopDown, absolute, out width);
            string peptideOutPath = Path.Combine(cellLine.GetChimeraPaperFigureDirectory(), pepOutName);
            peptideChart.SavePNG(peptideOutPath, null, width, PlotlyBase.DefaultHeight);
            peptideOutPath = Path.Combine(cellLine.FigureDirectory, pepOutName);
            peptideChart.SavePNG(peptideOutPath, null, width, PlotlyBase.DefaultHeight);
        }

        #endregion


        /// <summary>
        /// Stacked column: Plots the resultType of chimeric identifications as a function of the degree of chimericity
        /// </summary>
        /// <param name="cellLine"></param>
        public static void PlotCellLineChimeraBreakdown(this CellLineResults cellLine)
        {
            bool isTopDown = cellLine.First().IsTopDown;
            List<ChimeraBreakdownRecord> results;
            try // plot aggregated cell line result for specific targeted file from the selector
            {
                var selector = isTopDown.GetSingleResultSelector(cellLine.CellLine);
                results = cellLine.Where(p => p is IChimeraBreakdownCompatible && selector.Contains(p.Condition))
                    .SelectMany(p => ((IChimeraBreakdownCompatible)p).ChimeraBreakdownFile.Results).ToList();
            }
            catch
            {
                results = cellLine.Where(p => p is IChimeraBreakdownCompatible)
                    .SelectMany(p => ((IChimeraBreakdownCompatible)p).ChimeraBreakdownFile.Results).ToList();
            }

            var smLabel = Labels.GetSpectrumMatchLabel(isTopDown);
            var pepLabel = Labels.GetPeptideLabel(isTopDown);
            string smOutName = $"{FileIdentifiers.ChimeraBreakdownComparisonFigure}_{smLabel}_{cellLine.CellLine}";
            string smAreaOutName = $"{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaFigure}_{smLabel}_{cellLine.CellLine}";
            string smAreaRelativeName = $"{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaPercentFigure}_{smLabel}_{cellLine.CellLine}";
            string pepOutName = $"{FileIdentifiers.ChimeraBreakdownComparisonFigure}_{pepLabel}_{cellLine.CellLine}";
            string pepAreaOutName = $"{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaFigure}_{pepLabel}_{cellLine.CellLine}";
            string pepAreaRelativeName = $"{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaPercentFigure}_{pepLabel}_{cellLine.CellLine}";

            var psmChart = results.GetChimeraBreakDownStackedColumn(ResultType.Psm, cellLine.First().IsTopDown, out int width);
            string psmOutPath = Path.Combine(cellLine.FigureDirectory, smOutName);
            psmChart.SavePNG(psmOutPath, null, width, PlotlyBase.DefaultHeight);

            var stackedAreaPsmChart = results.GetChimeraBreakDownStackedArea(ResultType.Psm, cellLine.First().IsTopDown, out width);
            string stackedAreaPsmOutPath = Path.Combine(cellLine.FigureDirectory, smAreaOutName);
            stackedAreaPsmChart.SavePNG(stackedAreaPsmOutPath, null, width, PlotlyBase.DefaultHeight);

            var statckedAreaPsmChartRelative = results.GetChimeraBreakDownStackedArea(ResultType.Psm, cellLine.First().IsTopDown, out width, true);
            string stackedAreaPsmRelativeOutPath = Path.Combine(cellLine.FigureDirectory, smAreaRelativeName);
            statckedAreaPsmChartRelative.SavePNG(stackedAreaPsmRelativeOutPath, null, width, PlotlyBase.DefaultHeight);

            if (results.All(p => p.Type == ResultType.Psm))
                goto IndividualResults;

            var peptideChart = results.GetChimeraBreakDownStackedColumn(ResultType.Peptide, cellLine.First().IsTopDown, out width);
            string peptideOutPath = Path.Combine(cellLine.FigureDirectory, pepOutName);
            peptideChart.SavePNG(peptideOutPath, null, width, PlotlyBase.DefaultHeight);

            var stackedAreaPeptideChart = results.GetChimeraBreakDownStackedArea(ResultType.Peptide, cellLine.First().IsTopDown, out width);
            string stackedAreaPeptideOutPath = Path.Combine(cellLine.FigureDirectory, pepAreaOutName);
            stackedAreaPeptideChart.SavePNG(stackedAreaPeptideOutPath, null, width, PlotlyBase.DefaultHeight);

            var stackedAreaPeptideChartRelative = results.GetChimeraBreakDownStackedArea(ResultType.Peptide, cellLine.First().IsTopDown, out width, true);
            string stackedAreaPeptideRelativeOutPath = Path.Combine(cellLine.FigureDirectory, pepAreaRelativeName);
            stackedAreaPeptideChartRelative.SavePNG(stackedAreaPeptideRelativeOutPath, null, width, PlotlyBase.DefaultHeight);


        IndividualResults:
            // plot individual result for each IChimeraBreakdownCompatible file resultType
            var compatibleResults = cellLine.Where(m => m is IChimeraBreakdownCompatible)
                .Cast<IChimeraBreakdownCompatible>().ToList();
            foreach (var file in compatibleResults)
            {
                results = file.ChimeraBreakdownFile.Results;

                psmChart = results.GetChimeraBreakDownStackedColumn(ResultType.Psm, cellLine.First().IsTopDown, out width, file.Condition);
                psmOutPath = Path.Combine(file.FigureDirectory, smOutName);
                psmChart.SavePNG(psmOutPath, null, width, PlotlyBase.DefaultHeight);


                stackedAreaPsmChart = results.GetChimeraBreakDownStackedArea(ResultType.Psm, cellLine.First().IsTopDown, out width, false, file.Condition);
                stackedAreaPsmOutPath = Path.Combine(file.FigureDirectory, smAreaOutName);
                stackedAreaPsmChart.SavePNG(stackedAreaPsmOutPath, null, width, PlotlyBase.DefaultHeight);


                statckedAreaPsmChartRelative = results.GetChimeraBreakDownStackedArea(ResultType.Psm, cellLine.First().IsTopDown, out width, true, file.Condition);
                stackedAreaPsmRelativeOutPath = Path.Combine(file.FigureDirectory, smAreaRelativeName);
                statckedAreaPsmChartRelative.SavePNG(stackedAreaPsmRelativeOutPath, null, width, PlotlyBase.DefaultHeight);


                if (results.All(p => p.Type == ResultType.Psm))
                    continue;

                peptideChart = results.GetChimeraBreakDownStackedColumn(ResultType.Peptide, cellLine.First().IsTopDown, out width, file.Condition);
                peptideOutPath = Path.Combine(file.FigureDirectory, pepOutName);
                peptideChart.SavePNG(peptideOutPath, null, width, PlotlyBase.DefaultHeight);

                stackedAreaPeptideChart = results.GetChimeraBreakDownStackedArea(ResultType.Peptide, cellLine.First().IsTopDown, out width, false, file.Condition);
                stackedAreaPeptideOutPath = Path.Combine(file.FigureDirectory, pepAreaOutName);
                stackedAreaPeptideChart.SavePNG(stackedAreaPeptideOutPath, null, width, PlotlyBase.DefaultHeight);

                stackedAreaPeptideChartRelative = results.GetChimeraBreakDownStackedArea(ResultType.Peptide, cellLine.First().IsTopDown, out width, true, file.Condition);
                stackedAreaPeptideRelativeOutPath = Path.Combine(file.FigureDirectory, pepAreaRelativeName);
                stackedAreaPeptideChartRelative.SavePNG(stackedAreaPeptideRelativeOutPath, null, width, PlotlyBase.DefaultHeight);
            }
        }
    }
}
