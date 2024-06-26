using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Analyzer.FileTypes.Internal;
using Analyzer.Interfaces;
using Analyzer.Plotting.AggregatePlots;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using MathNet.Numerics;
using Microsoft.FSharp.Core;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.ImageExport;
using Plotly.NET.LayoutObjects;
using Proteomics.PSM;
using Chart = Plotly.NET.CSharp.Chart;
using GenericChartExtensions = Plotly.NET.CSharp.GenericChartExtensions;



namespace Analyzer.Plotting.ComparativePlots
{
    public static class AllResultsComparativePlots
    {
        public static void PlotStackedIndividualFileComparison(this AllResults allResults, ResultType? resultTypeNullable = null, bool filterByCondition = true)
        {
            int width = 0;
            int height = 0;

            double heightScaler = allResults.First().First().IsTopDown ? 1.0 : 2.5;
            var resultType = resultTypeNullable ?? (allResults.First().First().IsTopDown ? ResultType.Psm : ResultType.Peptide);
            var title = Labels.GetLabel(allResults.First().First().IsTopDown, resultType);
            var chart = Chart.Grid(
                    allResults.Select(p => p.GetIndividualFileResultsBarChart(out width, out height, resultType, filterByCondition)
                        .WithYAxisStyle(Title.init(p.CellLine))),
                    allResults.Count(), 1, Pattern: StyleParam.LayoutGridPattern.Independent, YGap: 0.4)
                .WithTitle($"Individual File Comparison 1% {title}")
                .WithSize(width, (int)(height * allResults.Count() / heightScaler))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            string outpath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(), $"AllResults_{FileIdentifiers.IndividualFileComparisonFigure}_{title}_Stacked");

            if (!filterByCondition)
                height *= 2;
            chart.SavePNG(outpath, null, width, (int)(height * allResults.Count() / heightScaler));
        }

        public static void PlotInternalMMComparison(this AllResults allResults)
        {
            bool isTopDown = allResults.First().First().IsTopDown;
            var noChimeras = new List<BulkResultCountComparison>();
            var withChimeras = new List<BulkResultCountComparison>();
            var labels = new List<string>();

            foreach (var cellLine in allResults)
            {
                labels.Add(cellLine.CellLine);
                var selector = cellLine.GetInternalMetaMorpheusFileComparisonSelector();
                var results = cellLine.Where(p => selector.Contains(p.Condition)).ToArray();

                var chimeric = results.First(p => !p.Condition.Contains("NoChimeras"));
                withChimeras.Add(chimeric.BulkResultCountComparisonFile.First());


                // Recalculate No Chimeras from the chimeric results accepting only one Psm per spectrum
                if (isTopDown)
                {


                    var psmCount = ((MetaMorpheusResult)chimeric).AllPsms.Where(p => p is { DecoyContamTarget: "T", PEP_QValue: <= 0.01 })
                        .GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer).Count();
                    var peptides = ((MetaMorpheusResult)chimeric).AllPeptides.Where(p => p is { DecoyContamTarget: "T", PEP_QValue: <= 0.01 })
                        .GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer).ToArray();
                    var peptideCount = peptides.Length;
                    var accessions = peptides
                        .Select(p => p.MinBy(m => m.PEP_QValue)?.Accession).ToArray();

                    List<string> proteins = new();
                    using (var sw = new StreamReader(File.OpenRead(((MetaMorpheusResult)chimeric).ProteinPath)))
                    {
                        var header = sw.ReadLine();
                        var headerSplit = header.Split('\t');
                        var qValueIndex = Array.IndexOf(headerSplit, "Protein QValue");
                        var targdecoyIndex = Array.IndexOf(headerSplit, "Protein Decoy/Contaminant/Target");
                        var accessionIndex = Array.IndexOf(headerSplit, "Protein Accession");


                        while (!sw.EndOfStream)
                        {
                            var line = sw.ReadLine();
                            var values = line.Split('\t');
                            if (values[targdecoyIndex] == "T" && double.Parse(values[qValueIndex]) <= 0.01)
                                proteins.Add(values[accessionIndex]);
                        }
                    }

                    var proteinCount = proteins.Count(p => accessions.Contains(p));

                    var bulkResultCountComparison = new BulkResultCountComparison()
                    {
                        DatasetName = chimeric.DatasetName,
                        Condition = "Non-Chimeric",
                        FileName = "Combined",
                        OnePercentPeptideCount = peptideCount,
                        OnePercentProteinGroupCount = proteinCount,
                        OnePercentPsmCount = psmCount,
                    };

                    noChimeras.Add(bulkResultCountComparison);
                }
                else
                {
                    var nonChimeric = results.First(p => p.Condition.Contains("NoChimeras"));
                    noChimeras.Add(nonChimeric.BulkResultCountComparisonFile.First());
                }
            }



            var psmChart = Chart.Combine(new[]
            {
                Chart2D.Chart.Column<int, string, string, int, int>(noChimeras.Select(p => p.OnePercentPsmCount),
                    labels, null, "No Chimeras", MarkerColor: noChimeras.First().Condition.ConvertConditionToColor(),
                    MultiText: noChimeras.Select(p => p.OnePercentPsmCount.ToString()).ToArray()),
                Chart2D.Chart.Column<int, string, string, int, int>(withChimeras.Select(p => p.OnePercentPsmCount),
                    labels, null, "Chimeras", MarkerColor: withChimeras.First().Condition.ConvertConditionToColor(),
                    MultiText: withChimeras.Select(p => p.OnePercentPsmCount.ToString()).ToArray()),
                //Chart2D.Chart.Column<int, string, string, int, int>(others.Select(chimeraGroup => chimeraGroup.OnePercentPsmCount),
                //    labels, null, "Others", MarkerColor: ConditionToColorDictionary[others.First().Condition])
            });
            var smLabel = allResults.First().First().IsTopDown ? "PrSMs" : "PSMs";
            psmChart.WithTitle($"MetaMorpheus 1% FDR {smLabel}")
                .WithXAxisStyle(Title.init("Cell Line"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            string psmOutpath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(),
                $"InternalMetaMorpheusComparison_{smLabel}");
            psmChart.SavePNG(psmOutpath);

            var peptideChart = Chart.Combine(new[]
            {
                Chart2D.Chart.Column<int, string, string, int, int>(noChimeras.Select(p => p.OnePercentPeptideCount),
                    labels, null, "No Chimeras", MarkerColor: noChimeras.First().Condition.ConvertConditionToColor(),
                    MultiText: noChimeras.Select(p => p.OnePercentPeptideCount.ToString()).ToArray()),
                Chart2D.Chart.Column<int, string, string, int, int>(withChimeras.Select(p => p.OnePercentPeptideCount),
                    labels, null, "Chimeras", MarkerColor: withChimeras.First().Condition.ConvertConditionToColor(),
                    MultiText: withChimeras.Select(p => p.OnePercentPeptideCount.ToString()).ToArray()),
                //Chart2D.Chart.Column<int, string, string, int, int>(others.Select(chimeraGroup => chimeraGroup.OnePercentPeptideCount),
                //    labels, null, "Others", MarkerColor: ConditionToColorDictionary[others.First().Condition])
            });
            peptideChart.WithTitle($"MetaMorpheus 1% FDR {allResults.First().First().ResultType}s")
                .WithXAxisStyle(Title.init("Cell Line"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            string peptideOutpath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(),
                $"InternalMetaMorpheusComparison_{allResults.First().First().ResultType}s");
            peptideChart.SavePNG(peptideOutpath);

            var proteinChart = Chart.Combine(new[]
            {
                Chart2D.Chart.Column<int, string, string, int, int>(
                    noChimeras.Select(p => p.OnePercentProteinGroupCount),
                    labels, null, "No Chimeras", MarkerColor: noChimeras.First().Condition.ConvertConditionToColor(),
                    MultiText: noChimeras.Select(p => p.OnePercentProteinGroupCount.ToString()).ToArray()),
                Chart2D.Chart.Column<int, string, string, int, int>(
                    withChimeras.Select(p => p.OnePercentProteinGroupCount),
                    labels, null, "Chimeras", MarkerColor: withChimeras.First().Condition.ConvertConditionToColor(),
                    MultiText: withChimeras.Select(p => p.OnePercentProteinGroupCount.ToString()).ToArray()),
                //Chart2D.Chart.Column<int, string, string, int, int>(others.Select(chimeraGroup => chimeraGroup.OnePercentProteinGroupCount),
                //    labels, null, "Chimeras", MarkerColor: ConditionToColorDictionary[others.First().Condition]),
            });
            proteinChart.WithTitle("MetaMorpheus 1% FDR Proteins")
                .WithXAxisStyle(Title.init("Cell Line"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            string proteinOutpath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(),
                "InternalMetaMorpheusComparison_Proteins");
            proteinChart.SavePNG(proteinOutpath);
        }

        public static void PlotBulkResultComparisons(this AllResults allResults, string? outputDirectory = null, bool filterByCondition = true)
        {
            bool isTopDown = allResults.First().First().IsTopDown;
            outputDirectory ??= allResults.GetChimeraPaperFigureDirectory();

            var results = allResults.CellLineResults.SelectMany(p => p.BulkResultCountComparisonFile.Results)
                .Where(p => filterByCondition ?
                    isTopDown.GetBulkResultComparisonSelector(p.DatasetName).Contains(p.Condition) : p != null)
                .OrderBy(p => p.Condition.ConvertConditionName())
                .ToList();

            var psmChart = GenericPlots.BulkResultBarChart(results, isTopDown, ResultType.Psm);
            var peptideChart = GenericPlots.BulkResultBarChart(results, isTopDown, ResultType.Peptide);
            var proteinChart = GenericPlots.BulkResultBarChart(results, isTopDown, ResultType.Protein);

            var psmPath = Path.Combine(outputDirectory, $"BulkResultComparison_{Labels.GetLabel(isTopDown, ResultType.Psm)}");
            var peptidePath = Path.Combine(outputDirectory, $"BulkResultComparison_{Labels.GetLabel(isTopDown, ResultType.Peptide)}");
            var proteinPath = Path.Combine(outputDirectory, $"BulkResultComparison_{Labels.GetLabel(isTopDown, ResultType.Protein)}");

            psmChart.SavePNG(psmPath);
            peptideChart.SavePNG(peptidePath);
            proteinChart.SavePNG(proteinPath);
        }


        /// <summary>
        /// Stacked column: Plots the type of chimeric identifications as a function of the degree of chimericity
        /// </summary>
        /// <param name="allResults"></param>
        public static void PlotBulkResultChimeraBreakDown(this AllResults allResults)
        {
            var selector = allResults.GetSingleResultSelector();
            bool isTopDown = allResults.First().First().IsTopDown;
            var smLabel = isTopDown ? "PrSM" : "PSM";
            var pepLabel = isTopDown ? "Proteoform" : "Peptide";
            var results = allResults.SelectMany(z => z.Results
                    .Where(p => p is IChimeraBreakdownCompatible && selector.Contains(p.Condition))
                    .SelectMany(p => ((IChimeraBreakdownCompatible)p).ChimeraBreakdownFile.Results))
                .ToList();

            var psmChart = results.GetChimeraBreakDownStackedColumn(ResultType.Psm, isTopDown, out int width);
            var psmOutPath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(),
                $"AllResults_{FileIdentifiers.ChimeraBreakdownComparisonFigure}{smLabel}s");
            psmChart.SavePNG(psmOutPath, null, width, PlotlyBase.DefaultHeight);

            var stackedAreaPsmChart = results.GetChimeraBreakDownStackedArea(ResultType.Psm, isTopDown, out width);
            var stackedAreaPsmOutPath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(),
                           $"AllResults_{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaFigure}{smLabel}s_StackedArea");
            stackedAreaPsmChart.SavePNG(stackedAreaPsmOutPath, null, width, PlotlyBase.DefaultHeight);

            var stackedAreaPercentPsmChart = results.GetChimeraBreakDownStackedArea(ResultType.Psm, isTopDown, out width, true);
            var stackedAreaPercentPsmOutPath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(),
                           $"AllResults_{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaPercentFigure}{smLabel}s_StackedArea_Percent");
            stackedAreaPercentPsmChart.SavePNG(stackedAreaPercentPsmOutPath, null, width, PlotlyBase.DefaultHeight);

            if (results.All(p => p.Type == ResultType.Psm))
                return;

            var peptideChart = results.GetChimeraBreakDownStackedColumn(ResultType.Peptide, isTopDown, out width);
            var peptideOutPath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(),
                $"AllResults_{FileIdentifiers.ChimeraBreakdownComparisonFigure}{pepLabel}s");
            peptideChart.SavePNG(peptideOutPath, null, width, PlotlyBase.DefaultHeight);

            var stackedAreaPeptideChart = results.GetChimeraBreakDownStackedArea(ResultType.Peptide, isTopDown, out width);
            var stackedAreaPeptideOutPath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(),
                $"AllResults_{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaFigure}{pepLabel}s_StackedArea");
            stackedAreaPeptideChart.SavePNG(stackedAreaPeptideOutPath, null, width, PlotlyBase.DefaultHeight);

            var stackedAreaPercentPeptideChart = results.GetChimeraBreakDownStackedArea(ResultType.Peptide, isTopDown, out width, true);
            var stackedAreaPercentPeptideOutPath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(),
                                                 $"AllResults_{FileIdentifiers.ChimeraBreakdownComparisonStackedAreaPercentFigure}{pepLabel}s_StackedArea_Percent");
            stackedAreaPercentPeptideChart.SavePNG(stackedAreaPercentPeptideOutPath, null, width, PlotlyBase.DefaultHeight);
        }

        /// <summary>
        /// Stacked Column: Plots the target decoy distribution as a function of the degree of chimericity
        /// </summary>
        /// <param name="allResults"></param>
        public static void PlotBulkResultChimeraBreakDown_TargetDecoy(this AllResults allResults)
        {
            var selector = allResults.GetSingleResultSelector();
            bool isTopDown = allResults.First().First().IsTopDown;
            var smLabel = isTopDown ? "PrSM" : "PSM";
            var pepLabel = isTopDown ? "Proteoform" : "Peptide";
            var results = allResults.SelectMany(z => z.Results
                    .Where(p => p is MetaMorpheusResult && selector.Contains(p.Condition))
                    .SelectMany(p => ((MetaMorpheusResult)p).ChimeraBreakdownFile.Results))
                .ToList();
            var psmChart =
                results.GetChimeraBreakDownStackedColumn_TargetDecoy(ResultType.Psm, isTopDown, false, out int width);
            var psmOutPath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(),
                $"AllResults_{FileIdentifiers.ChimeraBreakdownTargetDecoy}_{smLabel}");
            psmChart.SavePNG(psmOutPath, null, width, PlotlyBase.DefaultHeight);

            var peptideChart =
                results.GetChimeraBreakDownStackedColumn_TargetDecoy(ResultType.Peptide, isTopDown, false, out width);
            var peptideOutPath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(),
                $"AllResults_{FileIdentifiers.ChimeraBreakdownTargetDecoy}_{pepLabel}");
            peptideChart.SavePNG(peptideOutPath, null, width, PlotlyBase.DefaultHeight);
        }

        #region Spectral Similarity

        public static void PlotStackedSpectralSimilarity(this AllResults allResults)
        {
            bool isTopDown = allResults.First().First().IsTopDown;
            var chart = Chart.Grid(
                    allResults.Select(p => p.GetCellLineSpectralSimilarity().WithYAxisStyle(Title.init(p.CellLine))),
                    4, 3, Pattern: StyleParam.LayoutGridPattern.Independent, YGap: 0.2)
                .WithTitle($"Spectral Angle Distribution (1% {Labels.GetSpectrumMatchLabel(isTopDown)})")
                .WithSize(1000, 800)
                .WithLayout(PlotlyBase.DefaultLayout);
            string outpath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(), $"AllResults_{FileIdentifiers.SpectralAngleFigure}_Stacked");
            chart.SavePNG(outpath, null, 1000, 800);
        }

        public static void PlotAggregatedSpectralSimilarity(this AllResults allResults)
        {
            bool isTopDown = allResults.First().First().IsTopDown;
            var results = allResults.CellLineResults.SelectMany(n => n
                .Where(p => isTopDown.GetSingleResultSelector(n.CellLine).Contains(p.Condition))
                .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
                .SelectMany(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.Results.Where(m => m.SpectralAngle is not -1 or double.NaN)))
                .ToList();

            double[] chimeraAngles = results.Where(p => p.IsChimeric).Select(p => p.SpectralAngle).ToArray();
            double[] nonChimeraAngles = results.Where(p => !p.IsChimeric).Select(p => p.SpectralAngle).ToArray();
            var violin = GenericPlots.SpectralAngleChimeraComparisonViolinPlot(chimeraAngles, nonChimeraAngles, "AllResults", isTopDown)
                .WithTitle($"All Results Spectral Angle Distribution (1% {Labels.GetPeptideLabel(isTopDown)})")
                .WithYAxisStyle(Title.init("Spectral Angle"))
                .WithLayout(PlotlyBase.DefaultLayout)
                .WithSize(1000, 600);
            string outpath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(),
                $"AllResults_{FileIdentifiers.SpectralAngleFigure}_Aggregated");
            violin.SavePNG(outpath);
        }

        #endregion

        #region Retention Time

        // Too big to export
        public static void PlotBulkResultRetentionTimePredictions(this AllResults allResults)
        {
            var retentionTimePredictions = allResults.CellLineResults
                .SelectMany(p => p.Where(b => false.GetSingleResultSelector(p.CellLine).Contains(b.Condition))
                    .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
                    .Select(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile))
                .ToList();

            var chronologer = retentionTimePredictions
                .SelectMany(p => p.Where(m => m.ChronologerPrediction != 0 && m.PeptideModSeq != ""))
                .ToList();
            var ssrCalc = retentionTimePredictions
                .SelectMany(p => p.Where(m => m.SSRCalcPrediction is not 0 or double.NaN or -1))
                .ToList();

            var chronologerPlot = Chart.Combine(new[]
                {
                Chart2D.Chart.Scatter<double, double, string>(
                    chronologer.Where(p => !p.IsChimeric).Select(p => p.RetentionTime),
                    chronologer.Where(p => !p.IsChimeric).Select(p => p.ChronologerPrediction), StyleParam.Mode.Markers,
                    "No Chimeras", MarkerColor: "No Chimeras".ConvertConditionToColor()),
                Chart2D.Chart.Scatter<double, double, string>(
                    chronologer.Where(p => p.IsChimeric).Select(p => p.RetentionTime),
                    chronologer.Where(p => p.IsChimeric).Select(p => p.ChronologerPrediction), StyleParam.Mode.Markers,
                    "Chimeras", MarkerColor: "Chimeras".ConvertConditionToColor())
            })
                .WithTitle($"All Results Chronologer Predicted HI vs Retention Time (1% Peptides)")
                .WithXAxisStyle(Title.init("Retention Time"))
                .WithYAxisStyle(Title.init("Chronologer Prediction"))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
                .WithSize(1000, 600);


            string outpath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(), $"AllResults_{FileIdentifiers.ChronologerFigure}_Aggregated");
            chronologerPlot.SavePNG(outpath, ExportEngine.PuppeteerSharp, 1000, 600);

            var ssrCalcPlot = Chart.Combine(new[]
                {
                Chart2D.Chart.Scatter<double, double, string>(
                    ssrCalc.Where(p => !p.IsChimeric).Select(p => p.RetentionTime),
                    ssrCalc.Where(p => !p.IsChimeric).Select(p => p.SSRCalcPrediction), StyleParam.Mode.Markers,
                    "No Chimeras", MarkerColor: "No Chimeras".ConvertConditionToColor()),
                Chart2D.Chart.Scatter<double, double, string>(
                    ssrCalc.Where(p => p.IsChimeric).Select(p => p.RetentionTime),
                    ssrCalc.Where(p => p.IsChimeric).Select(p => p.SSRCalcPrediction), StyleParam.Mode.Markers,
                    "Chimeras", MarkerColor: "Chimeras".ConvertConditionToColor())
            })
                .WithTitle($"All Results SSRCalc3 Predicted HI vs Retention Time (1% Peptides)")
                .WithXAxisStyle(Title.init("Retention Time"))
                .WithYAxisStyle(Title.init("SSRCalc3 Prediction"))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
                .WithSize(1000, 600);
            outpath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(), $"AllResults_{FileIdentifiers.SSRCalcFigure}_Aggregated");
            ssrCalcPlot.SavePNG(outpath, null, 1000, 600);
        }

        // too big to export
        public static void PlotStackedRetentionTimePredictions(this AllResults allResults)
        {
            var results = allResults.Select(p => p.GetCellLineRetentionTimePredictions()).ToList();

            var chronologer = Chart.Grid(results.Select(p => p.Chronologer),
                    results.Count(), 1, Pattern: StyleParam.LayoutGridPattern.Independent, YGap: 0.2,
                    XSide: StyleParam.LayoutGridXSide.Bottom)
                .WithTitle("Chronologer Predicted HI vs Retention Time (1% Peptides)")
                .WithSize(1000, 400 * results.Count())
                .WithXAxisStyle(Title.init("Retention Time"))
                .WithYAxisStyle(Title.init("Chronologer Prediction"))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            string outpath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(), $"AllResults_{FileIdentifiers.ChronologerFigure}_Stacked");
            chronologer.SavePNG(outpath, ExportEngine.PuppeteerSharp, 1000, 400 * results.Count());

            var ssrCalc = Chart.Grid(results.Select(p => p.SSRCalc3),
                    results.Count(), 1, Pattern: StyleParam.LayoutGridPattern.Independent, YGap: 0.2)
                .WithTitle("SSRCalc3 Predicted HI vs Retention Time (1% Peptides)")
                .WithSize(1000, 400 * results.Count())
                .WithXAxisStyle(Title.init("Retention Time"))
                .WithYAxisStyle(Title.init("SSRCalc3 Prediction"))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            outpath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(), $"AllResults_{FileIdentifiers.SSRCalcFigure}_Stacked");
            ssrCalc.SavePNG(outpath, null, 1000, 400 * results.Count());
        }


        public static void PlotChronologerVsPercentHi(this AllResults allResults)
        {
            var results = allResults.SelectMany(p => p.Results
                    .Where(b => b is MetaMorpheusResult && false.GetSingleResultSelector(p.CellLine).Contains(b.Condition))
                    .SelectMany(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.Results.Where(m => m.ChronologerPrediction != 0 && m.PeptideModSeq != "")))
                .ToList();
            var chronologer = results
                    .Where(m => m.ChronologerPrediction != 0 && m.PeptideModSeq != "")
                    .Select(p => (p.ChronologerPrediction, p.RetentionTime, p.IsChimeric))
                    .ToList();

            var noChimeras = chronologer.Where(p => !p.IsChimeric)
                .ToList();
            var chimeras = chronologer.Where(p => p.IsChimeric)
                .ToList();

            var chronologerInterceptSlope = Fit.Line(chronologer.Select(p => p.RetentionTime).ToArray(),
                chronologer.Select(p => p.ChronologerPrediction).ToArray());
            var chimeraR2 = GoodnessOfFit.CoefficientOfDetermination(chimeras
                    .Select(p => p.RetentionTime * chronologerInterceptSlope.B + chronologerInterceptSlope.A),
                chronologer.Where(p => p.IsChimeric)
                    .Select(p => p.ChronologerPrediction)).Round(4);
            var nonChimericR2 = GoodnessOfFit.CoefficientOfDetermination(noChimeras
                    .Select(p => p.RetentionTime * chronologerInterceptSlope.B + chronologerInterceptSlope.A),
                chronologer.Where(p => !p.IsChimeric)
                    .Select(p => p.ChronologerPrediction)).Round(4);

            (double RT, double Prediction)[] line = new[]
            {
            (chronologer.Min(p => p.RetentionTime), chronologerInterceptSlope.A + chronologerInterceptSlope.B * chronologer.Min(p => p.RetentionTime)),
            (chronologer.Max(p => p.RetentionTime), chronologerInterceptSlope.A + chronologerInterceptSlope.B * chronologer.Max(p => p.RetentionTime))
        };

            var distinctNoChimeras = noChimeras.DistinctBy(p => (p.RetentionTime.Round(2), p.ChronologerPrediction.Round(2)))
                .ToList();
            var distinctChimeras = chimeras.DistinctBy(p => (p.RetentionTime.Round(2), p.ChronologerPrediction.Round(2)))
                .ToList();

            var chronologerPlot = Chart.Combine(new[]
                {
                Chart2D.Chart.Scatter<double, double, string>(
                    distinctNoChimeras.Select(p => p.RetentionTime),
                    distinctNoChimeras.Select(p => p.ChronologerPrediction), StyleParam.Mode.Markers,
                    $"No Chimeras - R^2={nonChimericR2}", MarkerColor: "No Chimeras".ConvertConditionToColor()),
                Chart2D.Chart.Scatter<double, double, string>(
                    distinctChimeras.Select(p => p.RetentionTime),
                    distinctChimeras.Select(p => p.ChronologerPrediction), StyleParam.Mode.Markers,
                    $"Chimeras - R^2={chimeraR2}", MarkerColor: "Chimeras".ConvertConditionToColor()),
                Chart.Line<double, double, string>(line.Select(p => p.RT), line.Select(p => p.Prediction))
                    .WithLegend(false)
            })
                .WithTitle($"Chronologer Predicted HI vs Percent ACN (1% Peptides)")
                .WithXAxisStyle(Title.init("Percent ACN"))
                .WithYAxisStyle(Title.init("Chronologer Prediction"))
                .WithLayout(Layout.init<string>(PaperBGColor: Color.fromKeyword(ColorKeyword.White),
                    PlotBGColor: Color.fromKeyword(ColorKeyword.White),
                    ShowLegend: true,
                    Legend: Legend.init(X: 0.5, Y: -0.2, Orientation: StyleParam.Orientation.Horizontal, EntryWidth: 0,
                        VerticalAlign: StyleParam.VerticalAlign.Bottom,
                        XAnchor: StyleParam.XAnchorPosition.Center,
                        YAnchor: StyleParam.YAnchorPosition.Top
                    )))
                .WithSize(1000, PlotlyBase.DefaultHeight);
            GenericChartExtensions.Show(chronologerPlot);
            var outpath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(), $"AllResults_{FileIdentifiers.ChronologerFigureACN}");
            try
            {
                chronologerPlot.SavePNG(outpath, null, 1000, 400);
            }
            catch (TimeoutException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void PlotBulkChronologerDeltaPlotKernalPDF(this AllResults allResults)
        {
            var results = allResults.SelectMany(p => p.Results
                       .Where(b => b is MetaMorpheusResult && false.GetSingleResultSelector(p.CellLine).Contains(b.Condition))
                       .SelectMany(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.Results.Where(m => m.ChronologerPrediction != 0 && m.PeptideModSeq != "")))
                       .ToList();

            var noChimeras = results.Where(p => !p.IsChimeric).ToList();
            var chimeras = results.Where(p => p.IsChimeric).ToList();

            var noChimerasKernel = GenericPlots.KernelDensityPlot(noChimeras.Select(p => p.DeltaChronologerRT).OrderBy(p => p).ToList(), "No Chimeras");
            var chimerasKernel = GenericPlots.KernelDensityPlot(chimeras.Select(p => p.DeltaChronologerRT).OrderBy(p => p).ToList(), "Chimeras");

            var chart = Chart.Combine(new[] { noChimerasKernel, chimerasKernel })
                .WithTitle("Chronologer Delta Kernel Density")
                .WithXAxisStyle(Title.init("Delta Chronologer RT"))
                .WithYAxisStyle(Title.init("Density"))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
                .WithSize(1000, 600);
            string outpath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(), $"AllResults_{FileIdentifiers.ChronologerDeltaKdeFigure}_KernelDensity_RT");
            chart.SavePNG(outpath, null, 1000, 600);
        }

        public static void PlotGridChronologerDeltaPlotKernalPDF(this AllResults allResults)
        {
            bool isTopDown = allResults.First().First().IsTopDown;
            var charts = allResults.Select(p => p.GetChronologerDeltaPlotKernelPDF()).ToList();
            var chart = Chart.Grid(
                    allResults.Select(p => p.GetChronologerDeltaPlotKernelPDF().WithYAxisStyle(Title.init(p.CellLine))),
                    4, 3,
                    Pattern: StyleParam.LayoutGridPattern.Independent, YGap: 0.2)
                .WithTitle($"Chronologer Delta Kernel Density (1% {Labels.GetPeptideLabel(isTopDown)})")
                .WithSize(1000, 800)
                .WithLayout(PlotlyBase.DefaultLayout)
                .WithLegend(false);
            string outpath = Path.Combine(allResults.GetChimeraPaperFigureDirectory(), $"AllResults_{FileIdentifiers.ChronologerDeltaKdeFigure}_Grid_RT");
            chart.SavePNG(outpath, null, 1000, 1000);
        }



        #endregion

    }
}
