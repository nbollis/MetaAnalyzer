using Analyzer.FileTypes.Internal;
using Analyzer.ResultType;
using Analyzer.Util;
using Proteomics.PSM;
using Readers;
using Chart = Plotly.NET.CSharp.Chart;
using Plotly.NET;
using Plotly.NET.ImageExport;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;


namespace Analyzer.Plotting
{
    public static class Plotting
    {
        // Bottom Up
        public static string[] AcceptableConditionsToPlotIndividualFileComparisonBottomUp =
        {
            "MetaMorpheusWithLibrary", "MetaMorpheusNoChimerasWithLibrary", "MetaMorpheus_NoNormalization",
            "ReviewdDatabaseNoPhospho_MsFraggerDDA", "ReviewdDatabaseNoPhospho_MsFraggerDDA+", "ReviewdDatabaseNoPhospho_MsFragger"
        };

        public static string[] AcceptableConditionsToPlotInternalMMComparisonBottomUp =
        {
            "MetaMorpheusWithLibrary", "MetaMorpheusNoChimerasWithLibrary"
        };

        public static string[] AcceptableConditionsToPlotBulkResultComparisonBottomUp =
        {
            "MetaMorpheusWithLibrary", "MetaMorpheusNoChimerasWithLibrary",
            "ReviewdDatabaseNoPhospho_MsFraggerDDA+", "ReviewdDatabaseNoPhospho_MsFragger",
        };

        public static string[] AcceptableConditionsToPlotFDRComparisonResults =
        {
            "MetaMorpheusWithLibrary"
        };



        // Top Down
        public static string[] AcceptableConditionsToPlotIndividualFileComparisonTopDown =
        {
            "MetaMorpheus", "MetaMorpheusNoChimeras",
            "MsPathFinderTWithModsNoChimerasRep2", "MsPathFinderTWithMods_7Rep2",
            /*"MsPathFinderTWithModsNoChimeras", "MsPathFinderTWithMods_7",*/ 
            "ProsightPDChimeras", "ProsightPDNoChimeras", 


            //"MetaMorpheus_Rep1_BuildLibrary",
            //"MetaMorpheus_Rep2_NoLibrary",
            "MetaMorpheus_Rep2_WithLibrary",
            //"Full_ChimeraIncorporation",
            //"MetaMorpheus_FullPEPChimeraIncorporation",
            //"Full_ChimeraIncorporation_NoNormalization",
            //"Small_ChimeraIncorporation"
        };

        public static string[] AcceptableConditonsToPlotInternalMMComparisonTopDown =
        {
            "MetaMorpheus", "MetaMorpheusNoChimeras", "MetaMorpheus_FullPEPChimeraIncorporation"
        };

        public static string[] AcceptableConditionsToPlotBulkResultsComparisonTopDown =
        {
            "MetaMorpheus", "MetaMorpheusNoChimeras",
            "MsPathFinderTWithModsNoChimerasRep2", "MsPathFinderTWithMods_7Rep2",
            /*"MsPathFinderTWithModsNoChimeras", "MsPathFinderTWithMods_7",*/ 
            "ProsightPDChimeras", "ProsightPDNoChimeras", 
        };

        public static string[] AcceptableConditionsToPlotChimeraBreakdownTopDown =
        {
            "MetaMorpheus", "MetaMorpheus_FullPEPChimeraIncorporation"
        };

        public static string[] AcceptableConditionsToPlotFDRComparisonResultsTopDown =
        {
            "MetaMorpheus_Rep2_WithLibrary"
        };


        

        #region Plotly Things

        public static int DefaultHeight = 600;
        public static Layout DefaultLayout => Layout.init<string>(PaperBGColor: Color.fromKeyword(ColorKeyword.White), PlotBGColor: Color.fromKeyword(ColorKeyword.White));

        private static Layout DefaultLayoutWithLegend => Layout.init<string>(
            //PaperBGColor: Color.fromARGB(0, 0,0,0),
            //PlotBGColor: Color.fromARGB(0, 0, 0, 0),
            PaperBGColor: Color.fromKeyword(ColorKeyword.White),
            PlotBGColor: Color.fromKeyword(ColorKeyword.White),
            ShowLegend: true,
            Legend: Legend.init(X: 0.5, Y: -0.2, Orientation: StyleParam.Orientation.Horizontal, EntryWidth: 0,
                VerticalAlign: StyleParam.VerticalAlign.Bottom,
                XAnchor: StyleParam.XAnchorPosition.Center,
                YAnchor: StyleParam.YAnchorPosition.Top
            ));

        #endregion

        #region Cell Line

        /// <summary>
        /// TODO: Finish this
        /// </summary>
        /// <param name="cellLine"></param>
        public static void PlotCellLineSummary(this CellLineResults cellLine)
        {
            bool isTopDown = cellLine.First().IsTopDown;
            var smLabel = isTopDown ? "PrSM" : "PSM";
            var pepLabel = isTopDown ? "Proteoform" : "Peptide";

            // chimera breakdown
            var selector = isTopDown
                ? AcceptableConditionsToPlotChimeraBreakdownTopDown
                : AcceptableConditionsToPlotFDRComparisonResults;
            var results = cellLine.Results
                .Where(p => p is MetaMorpheusResult && selector.Contains(p.Condition))
                .SelectMany(p => ((MetaMorpheusResult)p).ChimeraBreakdownFile)
                .ToList();

            var psmChart = results.GetChimeraBreakDownStackedColumn(ChimeraBreakdownType.Psm, isTopDown, out int width);
            var peptideChart = results.GetChimeraBreakDownStackedColumn(ChimeraBreakdownType.Peptide, isTopDown, out width);
            var absolutePsmChart = results.GetChimeraBreakDownStackedColumn_TargetDecoy(ChimeraBreakdownType.Psm, isTopDown, true, out width);
            var relativePsmChart = results.GetChimeraBreakDownStackedColumn_TargetDecoy(ChimeraBreakdownType.Psm, isTopDown, false, out width);
            var absolutePeptideChart = results.GetChimeraBreakDownStackedColumn_TargetDecoy(ChimeraBreakdownType.Peptide, isTopDown, true, out width);
            var relativePeptideChart = results.GetChimeraBreakDownStackedColumn_TargetDecoy(ChimeraBreakdownType.Peptide, isTopDown, false, out width);

            GenericChart.GenericChart grid;
            if (isTopDown)
            {
                var allPlots = new List<GenericChart.GenericChart>()
                {
                    cellLine.GetIndividualFileResults(out int widthIndividual, out int heightIndividual)
                        .WithXAxis(LinearAxis.init<int, int, int, int ,int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 1.00)), StyleParam.SubPlotId.NewXAxis(1))
                        .WithYAxis(LinearAxis.init<int, int, int, int ,int, int>(Domain: StyleParam.Range.NewMinMax(0.77, 1.00)), StyleParam.SubPlotId.NewYAxis(1)),
                    psmChart
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.5)), StyleParam.SubPlotId.NewXAxis(2))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 0.70)), StyleParam.SubPlotId.NewYAxis(2)),
                    peptideChart
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.5, 1.0)), StyleParam.SubPlotId.NewXAxis(3))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.4, 0.5)), StyleParam.SubPlotId.NewYAxis(3)),
                    absolutePsmChart
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.5)), StyleParam.SubPlotId.NewXAxis(4))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.2, 0.30)), StyleParam.SubPlotId.NewYAxis(4)),
                    absolutePeptideChart
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.5, 1.0)), StyleParam.SubPlotId.NewXAxis(5))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.2, 0.3)), StyleParam.SubPlotId.NewYAxis(5)),
                    relativePsmChart
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.5)), StyleParam.SubPlotId.NewXAxis(6))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.10)), StyleParam.SubPlotId.NewYAxis(6)),
                    relativePeptideChart
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.5, 1.0)), StyleParam.SubPlotId.NewXAxis(7))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.10)), StyleParam.SubPlotId.NewYAxis(7)),
                };
                grid = Chart.Grid(allPlots, 2, 4);
            }
            else
            {
                var retentionTimePredictionPlot = cellLine.GetCellLineRetentionTimePredictions().Chronologer;
                var similarityPlot = cellLine.GetCellLineSpectralSimilarity();
                var allPlots = new List<GenericChart.GenericChart>()
                {
                    cellLine.GetIndividualFileResults(out int widthIndividual, out int heightIndividual)
                        .WithXAxis(LinearAxis.init<int, int, int, int ,int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 1.00)), StyleParam.SubPlotId.NewXAxis(1))
                        .WithYAxis(LinearAxis.init<int, int, int, int ,int, int>(Domain: StyleParam.Range.NewMinMax(0.75, 1.00)), StyleParam.SubPlotId.NewYAxis(1)),
                    retentionTimePredictionPlot
                        .WithXAxis(LinearAxis.init<int, int, int, int ,int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.7)), StyleParam.SubPlotId.NewXAxis(2))
                        .WithYAxis(LinearAxis.init<int, int, int, int ,int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 0.70)), StyleParam.SubPlotId.NewYAxis(2)),
                    similarityPlot
                        .WithXAxis(LinearAxis.init<int, int, int, int ,int, int>(Domain: StyleParam.Range.NewMinMax(0.8, 1.00)), StyleParam.SubPlotId.NewXAxis(3))
                        .WithYAxis(LinearAxis.init<int, int, int, int ,int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 0.70)), StyleParam.SubPlotId.NewYAxis(3)),
                    psmChart
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewXAxis(4))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.35, 0.5)), StyleParam.SubPlotId.NewYAxis(4))
                        .WithXAxisStyle(Title.init("")),
                    peptideChart
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 1.0)), StyleParam.SubPlotId.NewXAxis(5))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.35, 0.5)), StyleParam.SubPlotId.NewYAxis(5))
                        .WithXAxisStyle(Title.init("")),
                    absolutePsmChart
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewXAxis(6))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.17, 0.30)), StyleParam.SubPlotId.NewYAxis(6))
                        .WithXAxisStyle(Title.init("")),
                    absolutePeptideChart
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 1.0)), StyleParam.SubPlotId.NewXAxis(7))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.17, 0.30)), StyleParam.SubPlotId.NewYAxis(7))
                        .WithXAxisStyle(Title.init("")),
                    relativePsmChart
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewXAxis(8))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.13)), StyleParam.SubPlotId.NewYAxis(8)),
                    relativePeptideChart
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 1.0)), StyleParam.SubPlotId.NewXAxis(9))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.10)), StyleParam.SubPlotId.NewYAxis(9)),
                };
                grid = Chart.Grid(allPlots, 5, 2, YGap: 10);
            }

            grid.WithSize(1000, isTopDown ? 1400 : 2400)
                .WithTitle($"{cellLine.CellLine} Result Summary");
            grid.Show();
        }

        public static void PlotIndividualFileResults(this CellLineResults cellLine)
        {
            string outPath = Path.Combine(cellLine.GetFigureDirectory(), $"{FileIdentifiers.IndividualFileComparisonFigure}_{cellLine.CellLine}");
            cellLine.GetIndividualFileResults(out int width, out int height).SavePNG(outPath, null, width, height);
        }

        private static GenericChart.GenericChart GetIndividualFileResults(this CellLineResults cellLine, out int width, out int height)
        {
            var selector = cellLine.First().IsTopDown
                ? AcceptableConditionsToPlotIndividualFileComparisonTopDown
                : AcceptableConditionsToPlotIndividualFileComparisonBottomUp;
            var individualFileResults = cellLine.Results.Select(p => p.IndividualFileComparisonFile )
                .Where(p => p != null && selector.Contains(p.First().Condition))
                .OrderBy(p => p.First().Condition.ConvertConditionName())
                .ToList();
            var labels = individualFileResults.SelectMany(p => p.Results.Select(m => m.FileName))
                .ConvertFileNames().Distinct().ToList();

            GenericChart.GenericChart chart;
            string resultType;
            if (cellLine.First().IsTopDown)
            {
                individualFileResults.ForEach(p => p.Results = p.Results.OrderBy(m => m.FileName.ConvertFileName()).ToList());
                labels = labels.OrderBy(p => p.ConvertFileName()).ToList();

                // if results exist for one dataset but not the other, ensure they are plotted in the correct order
                foreach (var individualFile in individualFileResults)
                {
                    if (individualFile.Results.Count != labels.Count)
                    {
                        var allResults = new List<BulkResultCountComparison>();
                        foreach (var file in labels)
                        {
                            if (individualFile.Any(p => p.FileName.ConvertFileName() == file))
                                allResults.Add(individualFile.First(p => p.FileName.ConvertFileName() == file));
                            else
                                allResults.Add(new BulkResultCountComparison()
                                {
                                    FileName = file,
                                    Condition = individualFile.First().Condition,
                                    OnePercentPsmCount = 0,
                                    OnePercentPeptideCount = 0,
                                    OnePercentProteinGroupCount = 0
                                });
                        }

                        individualFile.Results = allResults;
                    }
                }


                chart = Chart.Combine(individualFileResults.Select(p =>
                    Chart2D.Chart.Column<int, string, string, int, int>(p.Select(m => m.OnePercentPsmCount), labels, null,
                        p.Results.First().Condition.ConvertConditionName(), MarkerColor: PlottingTranslators.ConditionToColorDictionary[p.First().Condition])));
                resultType = "PrSMs";
            }
            else
            {
                chart = Chart.Combine(individualFileResults.Select(p =>
                    Chart2D.Chart.Column<int, string, string, int, int>(p.Select(m => m.OnePercentPeptideCount), labels, null,
                        p.Results.First().Condition.ConvertConditionName(), MarkerColor: PlottingTranslators.ConditionToColorDictionary[p.First().Condition])));
                resultType = "Peptides";
            }
            
            width = 50 * labels.Count + 10 * individualFileResults.Count;
            height = DefaultHeight;
            chart.WithTitle($"{cellLine.CellLine} 1% FDR {resultType}")
                .WithXAxisStyle(Title.init("File"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(DefaultLayoutWithLegend)
                .WithSize(width, height);
            return chart;
        }

        public static void PlotCellLineChimeraBreakdown(this CellLineResults cellLine)
        {
            var selector = cellLine.First().IsTopDown
                ? AcceptableConditionsToPlotChimeraBreakdownTopDown
                : AcceptableConditionsToPlotFDRComparisonResults;
            var smLabel = cellLine.First().IsTopDown ? "PrSM" : "PSM";
            var pepLabel = cellLine.First().IsTopDown ? "Proteoform" : "Peptide";

            var results = cellLine.Results
                .Where(p => p is MetaMorpheusResult && selector.Contains(p.Condition))
                .SelectMany(p => ((MetaMorpheusResult)p).ChimeraBreakdownFile)
                .ToList();
            var psmChart =
                results.GetChimeraBreakDownStackedColumn(ChimeraBreakdownType.Psm, cellLine.First().IsTopDown, out int width);
            string psmOutPath = Path.Combine(cellLine.GetFigureDirectory(),
                $"{FileIdentifiers.ChimeraBreakdownComparisonFigure}_{smLabel}_{cellLine.CellLine}");
            psmChart.SavePNG(psmOutPath, null, width, DefaultHeight);

            var peptideChart =
                results.GetChimeraBreakDownStackedColumn(ChimeraBreakdownType.Peptide, cellLine.First().IsTopDown, out width);
            string peptideOutPath = Path.Combine(cellLine.GetFigureDirectory(),
                $"{FileIdentifiers.ChimeraBreakdownComparisonFigure}_{pepLabel}_{cellLine.CellLine}");
            peptideChart.SavePNG(peptideOutPath, null, width, DefaultHeight);
        }

        public static void PlotCellLineChimeraBreakdown_TargetDecoy(this CellLineResults cellLine, bool absolute = false)
        {
            var selector = cellLine.First().IsTopDown
                ? AcceptableConditionsToPlotChimeraBreakdownTopDown
                : AcceptableConditionsToPlotFDRComparisonResults;
            var smLabel = cellLine.First().IsTopDown ? "PrSM" : "PSM";
            var pepLabel = cellLine.First().IsTopDown ? "Proteoform" : "Peptide";

            var results = cellLine.Results
                .Where(p => p is MetaMorpheusResult && selector.Contains(p.Condition))
                .SelectMany(p => ((MetaMorpheusResult)p).ChimeraBreakdownFile)
                .ToList();
            var psmChart =
                results.GetChimeraBreakDownStackedColumn_TargetDecoy(ChimeraBreakdownType.Psm, cellLine.First().IsTopDown, absolute, out int width);
            string psmOutPath = Path.Combine(cellLine.GetFigureDirectory(),
                $"{FileIdentifiers.ChimeraBreakdownTargetDecoy}_{smLabel}_{cellLine.CellLine}");
            psmChart.SavePNG(psmOutPath, null, width, DefaultHeight);

            var peptideChart =
                results.GetChimeraBreakDownStackedColumn_TargetDecoy(ChimeraBreakdownType.Peptide, cellLine.First().IsTopDown, absolute, out width);
            string peptideOutPath = Path.Combine(cellLine.GetFigureDirectory(),
                $"{FileIdentifiers.ChimeraBreakdownTargetDecoy}_{pepLabel}_{cellLine.CellLine}");
            peptideChart.SavePNG(peptideOutPath, null, width, DefaultHeight);
        }

        public static void PlotCellLineRetentionTimePredictions(this CellLineResults cellLine)
        {
            var plots = cellLine.GetCellLineRetentionTimePredictions();
            string outPath = Path.Combine(cellLine.GetFigureDirectory(), $"{FileIdentifiers.ChronologerFigure}_{cellLine.CellLine}");
            plots.Chronologer.SavePNG(outPath, null, 1000, DefaultHeight);

            outPath = Path.Combine(cellLine.GetFigureDirectory(), $"{FileIdentifiers.SSRCalcFigure}_{cellLine.CellLine}");
            plots.SSRCalc3.SavePNG(outPath, null, 1000, DefaultHeight);
        }

        private static (GenericChart.GenericChart Chronologer, GenericChart.GenericChart SSRCalc3) GetCellLineRetentionTimePredictions(this CellLineResults cellLine)
        {
            var individualFiles = cellLine.Results
                .Where(p => AcceptableConditionsToPlotFDRComparisonResults.Contains(p.Condition))
                .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
                .Select(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile)
                .ToList();
            var chronologer = individualFiles
                .SelectMany(p => p.Where(m => m.ChronologerPrediction != 0 && m.PeptideModSeq != ""))
                .ToList();
            var ssrCalc = individualFiles
                .SelectMany(p => p.Where(m => m.SSRCalcPrediction is not 0 or double.NaN or -1))
                .ToList();

            var chronologerPlot = Chart.Combine(new[]
                {
                    Chart2D.Chart.Scatter<double, double, string>(
                        chronologer.Where(p => !p.IsChimeric).Select(p => p.RetentionTime),
                        chronologer.Where(p => !p.IsChimeric).Select(p => p.ChronologerPrediction), StyleParam.Mode.Markers,
                        "No Chimeras", MarkerColor: PlottingTranslators.ConditionToColorDictionary["No Chimeras"]),
                    Chart2D.Chart.Scatter<double, double, string>(
                        chronologer.Where(p => p.IsChimeric).Select(p => p.RetentionTime),
                        chronologer.Where(p => p.IsChimeric).Select(p => p.ChronologerPrediction), StyleParam.Mode.Markers,
                        "Chimeras", MarkerColor: PlottingTranslators.ConditionToColorDictionary["Chimeras"])
                })
                .WithTitle($"{cellLine.CellLine} Chronologer Predicted HI vs Retention Time (1% Peptides)")
                .WithXAxisStyle(Title.init("Retention Time"))
                .WithYAxisStyle(Title.init("Chronologer Prediction"))
                .WithLayout(DefaultLayoutWithLegend)
                .WithSize(1000, DefaultHeight);

            var ssrCalcPlot = Chart.Combine(new[]
                {
                    Chart2D.Chart.Scatter<double, double, string>(
                        ssrCalc.Where(p => !p.IsChimeric).Select(p => p.RetentionTime),
                        ssrCalc.Where(p => !p.IsChimeric).Select(p => p.SSRCalcPrediction), StyleParam.Mode.Markers,
                        "No Chimeras", MarkerColor: PlottingTranslators.ConditionToColorDictionary["No Chimeras"]),
                    Chart2D.Chart.Scatter<double, double, string>(
                        ssrCalc.Where(p => p.IsChimeric).Select(p => p.RetentionTime),
                        ssrCalc.Where(p => p.IsChimeric).Select(p => p.SSRCalcPrediction), StyleParam.Mode.Markers,
                        "Chimeras", MarkerColor: PlottingTranslators.ConditionToColorDictionary["Chimeras"])
                })
                .WithTitle($"{cellLine.CellLine} SSRCalc3 Predicted HI vs Retention Time (1% Peptides)")
                .WithXAxisStyle(Title.init("Retention Time"))
                .WithYAxisStyle(Title.init("SSRCalc3 Prediction"))
                .WithLayout(DefaultLayoutWithLegend)
                .WithSize(1000, DefaultHeight);
            return (chronologerPlot, ssrCalcPlot);
        }


        public static void PlotCellLineSpectralSimilarity(this CellLineResults cellLine)
        {
            string outpath = Path.Combine(cellLine.GetFigureDirectory(), $"{FileIdentifiers.SpectralAngleFigure}_{cellLine.CellLine}");
            cellLine.GetCellLineSpectralSimilarity().SavePNG(outpath);
        }
        private static GenericChart.GenericChart GetCellLineSpectralSimilarity(this CellLineResults cellLine)
        {
            bool isTopDown = cellLine.First().IsTopDown;
            double[] chimeraAngles;
            double[] nonChimeraAngles;
            if (isTopDown)
            {
                var angles = cellLine.Results
                    .Where(p => AcceptableConditionsToPlotFDRComparisonResultsTopDown.Contains(p.Condition))
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
                    .Where(p => AcceptableConditionsToPlotFDRComparisonResults.Contains(p.Condition))
                    .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
                    .Select(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile)
                    .SelectMany(p => p.Where(m => m.SpectralAngle is not -1 or double.NaN))
                    .ToList();
                chimeraAngles = angles.Where(p => p.IsChimeric).Select(p => p.SpectralAngle).ToArray();
                nonChimeraAngles = angles.Where(p => !p.IsChimeric).Select(p => p.SpectralAngle).ToArray();
            }

            return SpectralAngleChimeraComparisonViolinPlot(chimeraAngles, nonChimeraAngles, cellLine.CellLine, isTopDown);
        }

        #endregion

        #region Bulk Result

        public static void PlotInternalMMComparison(this AllResults allResults)
        {
            var selector = allResults.First().First().IsTopDown
                ? AcceptableConditonsToPlotInternalMMComparisonTopDown
                : AcceptableConditionsToPlotInternalMMComparisonBottomUp;
            var results = allResults.CellLineResults.SelectMany(p => p.BulkResultCountComparisonFile.Results)
                .Where(p => selector.Contains(p.Condition))
                .ToList();
            var labels = results.Select(p => p.DatasetName).Distinct().ConvertConditionNames().ToList();

            var noChimeras = results.Where(p => p.Condition.Contains("NoChimeras")).ToList();
            var withChimeras = results.Where(p => !p.Condition.Contains("NoChimeras") && !p.Condition.Contains("PEP")).ToList();
            var others = results.Except(noChimeras).Except(withChimeras).ToList();

            var psmChart = Chart.Combine(new[]
            {
                Chart2D.Chart.Column<int, string, string, int, int>(noChimeras.Select(p => p.OnePercentPsmCount),
                    labels, null, "No Chimeras", MarkerColor: PlottingTranslators.ConditionToColorDictionary[noChimeras.First().Condition]),
                Chart2D.Chart.Column<int, string, string, int, int>(withChimeras.Select(p => p.OnePercentPsmCount),
                    labels, null, "Chimeras", MarkerColor: PlottingTranslators.ConditionToColorDictionary[withChimeras.First().Condition]),
                //Chart2D.Chart.Column<int, string, string, int, int>(others.Select(chimeraGroup => chimeraGroup.OnePercentPsmCount),
                //    labels, null, "Others", MarkerColor: ConditionToColorDictionary[others.First().Condition])
            });
            var smLabel = allResults.First().First().IsTopDown ? "PrSMs" : "PSMs";
            psmChart.WithTitle($"MetaMorpheus 1% FDR {smLabel}")
                .WithXAxisStyle(Title.init("Cell Line"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(DefaultLayoutWithLegend);
            string psmOutpath = Path.Combine(allResults.GetFigureDirectory(), $"InternalMetaMorpheusComparison_{smLabel}");
            psmChart.SavePNG(psmOutpath);

            var peptideChart = Chart.Combine(new[]
            {
                Chart2D.Chart.Column<int, string, string, int, int>(noChimeras.Select(p => p.OnePercentPeptideCount),
                    labels, null, "No Chimeras", MarkerColor: PlottingTranslators.ConditionToColorDictionary[noChimeras.First().Condition]),
                Chart2D.Chart.Column<int, string, string, int, int>(withChimeras.Select(p => p.OnePercentPeptideCount),
                    labels, null, "Chimeras", MarkerColor: PlottingTranslators.ConditionToColorDictionary[withChimeras.First().Condition]),
                //Chart2D.Chart.Column<int, string, string, int, int>(others.Select(chimeraGroup => chimeraGroup.OnePercentPeptideCount),
                //    labels, null, "Others", MarkerColor: ConditionToColorDictionary[others.First().Condition])
            });
            peptideChart.WithTitle($"MetaMorpheus 1% FDR {allResults.First().First().ResultType}s")
                .WithXAxisStyle(Title.init("Cell Line"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(DefaultLayoutWithLegend);
            string peptideOutpath = Path.Combine(allResults.GetFigureDirectory(), $"InternalMetaMorpheusComparison_{allResults.First().First().ResultType}s");
            peptideChart.SavePNG(peptideOutpath);

            var proteinChart = Chart.Combine(new[]
            {
                Chart2D.Chart.Column<int, string, string, int, int>(noChimeras.Select(p => p.OnePercentProteinGroupCount),
                    labels, null, "No Chimeras", MarkerColor: PlottingTranslators.ConditionToColorDictionary[noChimeras.First().Condition]),
                Chart2D.Chart.Column<int, string, string, int, int>(withChimeras.Select(p => p.OnePercentProteinGroupCount),
                    labels, null, "Chimeras", MarkerColor: PlottingTranslators.ConditionToColorDictionary[withChimeras.First().Condition]),
                //Chart2D.Chart.Column<int, string, string, int, int>(others.Select(chimeraGroup => chimeraGroup.OnePercentProteinGroupCount),
                //    labels, null, "Chimeras", MarkerColor: ConditionToColorDictionary[others.First().Condition]),
            });
            proteinChart.WithTitle("MetaMorpheus 1% FDR Proteins")
                .WithXAxisStyle(Title.init("Cell Line"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(DefaultLayoutWithLegend);
            string proteinOutpath = Path.Combine(allResults.GetFigureDirectory(), "InternalMetaMorpheusComparison_Proteins");
            proteinChart.SavePNG(proteinOutpath);
        }

        public static void PlotBulkResultComparison(this AllResults allResults)
        {
            var selector = allResults.First().First().IsTopDown
                ? AcceptableConditionsToPlotBulkResultsComparisonTopDown
                : AcceptableConditionsToPlotBulkResultComparisonBottomUp;
            var results = allResults.CellLineResults.SelectMany(p => p.BulkResultCountComparisonFile.Results)
                .Where(p => selector.Contains(p.Condition))
                .OrderBy(p => p.Condition.ConvertConditionName())
                .ToList();
            var labels = results.Select(p => p.DatasetName).Distinct().ConvertConditionNames().ToList();

            var psmCharts = new List<GenericChart.GenericChart>();
            var peptideCharts = new List<GenericChart.GenericChart>();
            var proteinCharts = new List<GenericChart.GenericChart>();
            foreach (var condition in results.Select(p => p.Condition).Distinct())
            {
                var conditionSpecificResults = results.Where(p => p.Condition == condition).ToList();

                // if results exist for one dataset but not the other, ensure they are plotted in the correct order
                if (conditionSpecificResults.Count != labels.Count) 
                {
                    var newResults = new List<BulkResultCountComparison>();
                    foreach (var dataset in labels)
                    {
                        if (conditionSpecificResults.Any(p => p.DatasetName == dataset))
                            newResults.Add(conditionSpecificResults.First(p => p.DatasetName == dataset));
                        else
                            newResults.Add(new BulkResultCountComparison()
                            {
                                DatasetName = dataset,
                                OnePercentPsmCount = 0,
                                OnePercentPeptideCount = 0,
                                OnePercentProteinGroupCount = 0
                            });
                    }
                    conditionSpecificResults = newResults;
                }

                var conditionToWrite = condition.ConvertConditionName();
                psmCharts.Add(Chart2D.Chart.Column<int, string, string, int, int>(
                    conditionSpecificResults.Select(p => p.OnePercentPsmCount), labels, null, conditionToWrite,
                    MarkerColor: PlottingTranslators.ConditionToColorDictionary[condition]));
                peptideCharts.Add(Chart2D.Chart.Column<int, string, string, int, int>(
                    conditionSpecificResults.Select(p => p.OnePercentPeptideCount), labels, null, conditionToWrite,
                    MarkerColor: PlottingTranslators.ConditionToColorDictionary[condition]));
                proteinCharts.Add(Chart2D.Chart.Column<int, string, string, int, int>(
                    conditionSpecificResults.Select(p => p.OnePercentProteinGroupCount), labels, null, conditionToWrite,
                    MarkerColor: PlottingTranslators.ConditionToColorDictionary[condition]));
            }
            var smLabel = allResults.First().First().IsTopDown ? "PrSMs" : "PSMs";
            var psmChart = Chart.Combine(psmCharts).WithTitle($"1% FDR {smLabel}")
                .WithXAxisStyle(Title.init("Cell Line"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(DefaultLayoutWithLegend);
            var peptideChart = Chart.Combine(peptideCharts).WithTitle($"1% FDR {allResults.First().First().ResultType}s")
                .WithXAxisStyle(Title.init("Cell Line"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(DefaultLayoutWithLegend);
            var proteinChart = Chart.Combine(proteinCharts).WithTitle("1% FDR Proteins")
                .WithXAxisStyle(Title.init("Cell Line"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(DefaultLayoutWithLegend);

            var psmPath = Path.Combine(allResults.GetFigureDirectory(), $"BulkResultComparison_{smLabel}");
            var peptidePath = Path.Combine(allResults.GetFigureDirectory(), $"BulkResultComparison_{allResults.First().First().ResultType}s");
            var proteinpath = Path.Combine(allResults.GetFigureDirectory(), "BulkResultComparison_Proteins");
            psmChart.SavePNG(psmPath);
            peptideChart.SavePNG(peptidePath);
            proteinChart.SavePNG(proteinpath);
        }

        public static void PlotBulkResultChimeraBreakDown(this AllResults allResults)
        {
            var selector = allResults.First().First().IsTopDown
                ? AcceptableConditionsToPlotChimeraBreakdownTopDown
                : AcceptableConditionsToPlotFDRComparisonResults;
            bool isTopDown = allResults.First().First().IsTopDown;
            var smLabel = isTopDown ? "PrSM" : "PSM";
            var pepLabel = isTopDown ? "Proteoform" : "Peptide";
            var results = allResults.SelectMany(z => z.Results
                .Where(p => p is MetaMorpheusResult && selector.Contains(p.Condition))
                .SelectMany(p => ((MetaMorpheusResult)p).ChimeraBreakdownFile.Results))
                .ToList();
            var psmChart =
                results.GetChimeraBreakDownStackedColumn(ChimeraBreakdownType.Psm, isTopDown, out int width);
            var psmOutPath = Path.Combine(allResults.GetFigureDirectory(),
                               $"AllResults_{FileIdentifiers.ChimeraBreakdownComparisonFigure}{smLabel}s");
            psmChart.SavePNG(psmOutPath, null, width, DefaultHeight);

            var peptideChart =
                results.GetChimeraBreakDownStackedColumn(ChimeraBreakdownType.Peptide, isTopDown, out width);
            var peptideOutPath = Path.Combine(allResults.GetFigureDirectory(),
                               $"AllResults_{FileIdentifiers.ChimeraBreakdownComparisonFigure}{pepLabel}s");
            peptideChart.SavePNG(peptideOutPath, null, width, DefaultHeight);
        }

        public static void PlotBulkResultChimeraBreakDown_TargetDecoy(this AllResults allResults)
        {
            var selector = allResults.First().First().IsTopDown
                ? AcceptableConditionsToPlotChimeraBreakdownTopDown
                : AcceptableConditionsToPlotFDRComparisonResults;
            bool isTopDown = allResults.First().First().IsTopDown;
            var smLabel = isTopDown ? "PrSM" : "PSM";
            var pepLabel = isTopDown ? "Proteoform" : "Peptide";
            var results = allResults.SelectMany(z => z.Results
                           .Where(p => p is MetaMorpheusResult && selector.Contains(p.Condition))
                           .SelectMany(p => ((MetaMorpheusResult)p).ChimeraBreakdownFile.Results))
                .ToList();
            var psmChart =
                results.GetChimeraBreakDownStackedColumn_TargetDecoy(ChimeraBreakdownType.Psm, isTopDown, false, out int width);
            var psmOutPath = Path.Combine(allResults.GetFigureDirectory(),
                                              $"AllResults_{FileIdentifiers.ChimeraBreakdownTargetDecoy}_{smLabel}");
            psmChart.SavePNG(psmOutPath, null, width, DefaultHeight);

            var peptideChart =
                results.GetChimeraBreakDownStackedColumn_TargetDecoy(ChimeraBreakdownType.Peptide, isTopDown, false, out width);
            var peptideOutPath = Path.Combine(allResults.GetFigureDirectory(),
                                              $"AllResults_{FileIdentifiers.ChimeraBreakdownTargetDecoy}_{pepLabel}");
            peptideChart.SavePNG(peptideOutPath, null, width, DefaultHeight);
        }

        #region Validation

        // Too big to export
        public static void PlotBulkResultRetentionTimePredictions(this AllResults allResults)
        {
            var retentionTimePredictions = allResults.CellLineResults
                .SelectMany(p => p.Where(p => AcceptableConditionsToPlotFDRComparisonResults.Contains(p.Condition))
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
                        "No Chimeras", MarkerColor: PlottingTranslators.ConditionToColorDictionary["No Chimeras"]),
                    Chart2D.Chart.Scatter<double, double, string>(
                        chronologer.Where(p => p.IsChimeric).Select(p => p.RetentionTime),
                        chronologer.Where(p => p.IsChimeric).Select(p => p.ChronologerPrediction), StyleParam.Mode.Markers,
                        "Chimeras", MarkerColor: PlottingTranslators.ConditionToColorDictionary["Chimeras"])
                })
                .WithTitle($"All Results Chronologer Predicted HI vs Retention Time (1% Peptides)")
                .WithXAxisStyle(Title.init("Retention Time"))
                .WithYAxisStyle(Title.init("Chronologer Prediction"))
                .WithLayout(DefaultLayoutWithLegend)
                .WithSize(1000, 600);


            PuppeteerSharpRendererOptions.launchOptions.Timeout = 0;
            string outpath = Path.Combine(allResults.GetFigureDirectory(), $"AllResults_{FileIdentifiers.ChronologerFigure}_Aggregated");
            chronologerPlot.SavePNG(outpath, ExportEngine.PuppeteerSharp, 1000, 600);

            var ssrCalcPlot = Chart.Combine(new[]
                {
                    Chart2D.Chart.Scatter<double, double, string>(
                        ssrCalc.Where(p => !p.IsChimeric).Select(p => p.RetentionTime),
                        ssrCalc.Where(p => !p.IsChimeric).Select(p => p.SSRCalcPrediction), StyleParam.Mode.Markers,
                        "No Chimeras", MarkerColor: PlottingTranslators.ConditionToColorDictionary["No Chimeras"]),
                    Chart2D.Chart.Scatter<double, double, string>(
                        ssrCalc.Where(p => p.IsChimeric).Select(p => p.RetentionTime),
                        ssrCalc.Where(p => p.IsChimeric).Select(p => p.SSRCalcPrediction), StyleParam.Mode.Markers,
                        "Chimeras", MarkerColor: PlottingTranslators.ConditionToColorDictionary["Chimeras"])
                })
                .WithTitle($"All Results SSRCalc3 Predicted HI vs Retention Time (1% Peptides)")
                .WithXAxisStyle(Title.init("Retention Time"))
                .WithYAxisStyle(Title.init("SSRCalc3 Prediction"))
                .WithLayout(DefaultLayoutWithLegend)
                .WithSize(1000, 600);
            outpath = Path.Combine(allResults.GetFigureDirectory(), $"AllResults_{FileIdentifiers.SSRCalcFigure}_Aggregated");
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
                .WithLayout(DefaultLayoutWithLegend);
            string outpath = Path.Combine(allResults.GetFigureDirectory(), $"AllResults_{FileIdentifiers.ChronologerFigure}_Stacked");
            chronologer.SavePNG(outpath, ExportEngine.PuppeteerSharp, 1000, 400 * results.Count());

            var ssrCalc = Chart.Grid(results.Select(p => p.SSRCalc3),
                    results.Count(), 1, Pattern: StyleParam.LayoutGridPattern.Independent, YGap: 0.2)
                .WithTitle("SSRCalc3 Predicted HI vs Retention Time (1% Peptides)")
                .WithSize(1000, 400 * results.Count())
                .WithXAxisStyle(Title.init("Retention Time"))
                .WithYAxisStyle(Title.init("SSRCalc3 Prediction"))
                .WithLayout(DefaultLayoutWithLegend);
            outpath = Path.Combine(allResults.GetFigureDirectory(), $"AllResults_{FileIdentifiers.SSRCalcFigure}_Stacked");
            ssrCalc.SavePNG(outpath, null, 1000, 400 * results.Count());
        }

        public static void PlotStackedIndividualFileComparison(this AllResults allResults)
        {
            int width = 0;
            int height = 0;

            double heightScaler = allResults.First().First().IsTopDown ? 1.5 : 2.5;
            var title = allResults.First().First().IsTopDown ? "PrSMs" : "Peptides";
            var chart = Chart.Grid(
                    allResults.Select(p => p.GetIndividualFileResults(out width, out height).WithYAxisStyle(Title.init(p.CellLine))),
                    allResults.Count(), 1, Pattern: StyleParam.LayoutGridPattern.Independent, YGap: 0.2)
                .WithTitle($"Individual File Comparison 1% {title}")
                .WithSize(width, (int)(height * allResults.Count() / heightScaler))
                .WithLayout(DefaultLayoutWithLegend);
            string outpath = Path.Combine(allResults.GetFigureDirectory(), $"AllResults_{FileIdentifiers.IndividualFileComparisonFigure}_Stacked");


            chart.SavePNG(outpath, null, width, (int)(height * allResults.Count() / heightScaler));
        }

        public static void PlotStackedSpectralSimilarity(this AllResults allResults)
        {

            var chart = Chart.Grid(
                allResults.Select(p => p.GetCellLineSpectralSimilarity().WithYAxisStyle(Title.init(p.CellLine))),
                                4, 3, Pattern: StyleParam.LayoutGridPattern.Independent, YGap: 0.2)
                .WithTitle("Spectral Angle Distribution (1% Peptides)")
                .WithSize(1000, 800)
                .WithLayout(DefaultLayout);
            string outpath = Path.Combine(allResults.GetFigureDirectory(), $"AllResults_{FileIdentifiers.SpectralAngleFigure}_Stacked");
            chart.SavePNG(outpath, null, 1000, 800);
        }

        public static void PlotAggregatedSpectralSimilarity(this AllResults allResults)
        {
            var results = allResults.CellLineResults.SelectMany(n => n
                .Where(p => AcceptableConditionsToPlotFDRComparisonResults.Contains(p.Condition))
                .OrderBy(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.First())
                .SelectMany(p => ((MetaMorpheusResult)p).RetentionTimePredictionFile.Results.Where(m => m.SpectralAngle is not -1 or double.NaN)))
                .ToList();

            var violin = Chart.Combine(new[]
                {
                    Chart.Violin<string, double, string>(
                        new Plotly.NET.CSharp.Optional<IEnumerable<string>>(
                            Enumerable.Repeat("Chimeras", results.Count(p => p.IsChimeric)), true),
                        new Plotly.NET.CSharp.Optional<IEnumerable<double>>(
                            results.Where(p => p.IsChimeric).Select(p => p.SpectralAngle), true),
                        null, MarkerColor: PlottingTranslators.ConditionToColorDictionary["Chimeras"],
                        MeanLine: MeanLine.init(true, PlottingTranslators.ConditionToColorDictionary["Chimeras"]),
                        ShowLegend: false),
                    Chart.Violin<string, double, string>(
                        new Plotly.NET.CSharp.Optional<IEnumerable<string>>(
                            Enumerable.Repeat("No Chimeras", results.Count(p => !p.IsChimeric)), true),
                        new Plotly.NET.CSharp.Optional<IEnumerable<double>>(
                            results.Where(p => !p.IsChimeric).Select(p => p.SpectralAngle), true),
                        null, MarkerColor: PlottingTranslators.ConditionToColorDictionary["No Chimeras"],
                        MeanLine: MeanLine.init(true, PlottingTranslators.ConditionToColorDictionary["No Chimeras"]),
                        ShowLegend: false)
                })
                .WithTitle($"All Results Spectral Angle Distribution (1% Peptides)")
                .WithYAxisStyle(Title.init("Spectral Angle"))
                .WithLayout(DefaultLayout)
                .WithSize(1000, 600);
            string outpath = Path.Combine(allResults.GetFigureDirectory(),
                $"AllResults_{FileIdentifiers.SpectralAngleFigure}_Aggregated");
            violin.SavePNG(outpath);
        }

        #endregion

        #endregion

        #region Generic

        internal static GenericChart.GenericChart GetChimeraBreakDownStackedColumn(this List<ChimeraBreakdownRecord> results, ChimeraBreakdownType type, bool isTopDown, out int width)
        {
            (int IdPerSpec, int Parent, int UniqueProtein, int UniqueForms, int Decoys)[] data = results.Where(p => p.Type == type)
                .GroupBy(p => p.IdsPerSpectra)
                .OrderBy(p => p.Key)
                .Select(p => 
                    (
                        p.Key, 
                        p.Sum(m => m.Parent), 
                        p.Sum(m => m.UniqueProteins), 
                        p.Sum(m => m.UniqueForms),
                        p.Sum(m => m.DecoyCount))
                    )
                .ToArray();
            var keys = data.Select(p => p.IdPerSpec).ToArray();
            width = Math.Max(600, 50 * data.Length);
            var form = isTopDown ? "Proteoform" : "Peptidoform";
            string title = isTopDown ? type == ChimeraBreakdownType.Psm ? "PrSM" : "Proteoform" :
                type == ChimeraBreakdownType.Psm ? "PSM" : "Peptide";
            var title2 = results.Select(p => p.Dataset).Distinct().Count() == 1 ? results.First().Dataset : "All Results";
            var chart = Chart.Combine(new[]
                {
                    Chart.StackedColumn<int, int, string>(data.Select(p => p.Parent), keys, "Isolated Species",
                        MarkerColor: PlottingTranslators.ConditionToColorDictionary["Isolated Species"], MultiText: data.Select(p => p.Parent.ToString()).ToArray()),
                    Chart.StackedColumn<int, int, string>(data.Select(p => p.Decoys), keys, "Decoys",
                        MarkerColor: PlottingTranslators.ConditionToColorDictionary["Decoys"], MultiText: data.Select(p => p.Decoys.ToString()).ToArray()),
                    Chart.StackedColumn<int, int, string>(data.Select(p => p.UniqueProtein), keys, $"Unique Protein",
                        MarkerColor: PlottingTranslators.ConditionToColorDictionary["Unique Protein"], MultiText: data.Select(p => p.UniqueProtein.ToString()).ToArray()),
                    Chart.StackedColumn<int, int, string>(data.Select(p => p.UniqueForms), keys, $"Unique {form}",
                        MarkerColor: PlottingTranslators.ConditionToColorDictionary[$"Unique {form}"], MultiText: data.Select(p => p.UniqueForms.ToString()).ToArray()),
                })
                .WithLayout(DefaultLayoutWithLegend)
                .WithTitle($"{title2} {title} Identifications per Spectra")
                .WithXAxisStyle(Title.init("IDs per Spectrum"))
                .WithYAxis(LinearAxis.init<int, int, int,int, int, int>(AxisType: StyleParam.AxisType.Log))
                .WithYAxisStyle(Title.init("Count"))
                .WithSize(width, DefaultHeight);
            return chart;
        }

        internal static GenericChart.GenericChart GetChimeraBreakDownStackedColumn_TargetDecoy(
            this List<ChimeraBreakdownRecord> results, ChimeraBreakdownType type, bool isTopDown, bool absolute, out int width)
        {
            (int IdPerSpec, int Parent, double Targets, double Decoys)[] data = absolute
                ? results.Where(p => p.Type == type)
                    .GroupBy(p => p.IdsPerSpectra)
                    .OrderBy(p => p.Key)
                    .Select(p => (
                        p.Key,
                        0,
                        (double)p.Sum(m => m.TargetCount),
                        (double)p.Sum(m => m.DecoyCount)
                    ))
                    .ToArray()
                : results.Where(p => p.Type == type)
                    .GroupBy(p => p.IdsPerSpectra)
                    .OrderBy(p => p.Key)
                    .Select(p =>
                    (
                        p.Key,
                        p.Sum(m => m.Parent),
                        p.Sum(m => m.TargetCount) / (double)(p.Sum(m => m.TargetCount) + p.Sum(m => m.DecoyCount)) *
                        100,
                        p.Sum(m => m.DecoyCount) / (double)(p.Sum(m => m.TargetCount) + p.Sum(m => m.DecoyCount)) * 100
                    ))
                    .ToArray();
            var keys = data.Select(p => p.IdPerSpec).ToArray();
            width = Math.Max(600, 50 * data.Length);
            var form = isTopDown ? "Proteoform" : "Peptidoform";
            string title = isTopDown ? type == ChimeraBreakdownType.Psm ? "PrSM" : "Proteoform" :
                type == ChimeraBreakdownType.Psm ? "PSM" : "Peptide";
            var title2 = results.Select(p => p.Dataset).Distinct().Count() == 1 ? results.First().Dataset : "All Results";
            var chart = Chart.Combine(new[]
                {
                    Chart.StackedColumn<double, int, string>(data.Select(p => p.Targets), keys, "Targets",
                        MarkerColor: PlottingTranslators.ConditionToColorDictionary["Targets"], MultiText: data.Select(p => Math.Round(p.Targets, 2).ToString()).ToArray()),
                    Chart.StackedColumn<double, int, string>(data.Select(p => p.Decoys), keys, $"Decoys",
                        MarkerColor: PlottingTranslators.ConditionToColorDictionary["Decoys"], MultiText: data.Select(p => Math.Round(p.Decoys, 2).ToString()).ToArray()),
                })
                .WithLayout(DefaultLayoutWithLegend)
                .WithTitle($"{title2} {title} Identifications per Spectra")
                .WithXAxisStyle(Title.init("IDs per Spectrum"))
                .WithYAxisStyle(Title.init("Percent"))
                .WithSize(width, DefaultHeight);
            return chart;
        }


        private static GenericChart.GenericChart SpectralAngleChimeraComparisonViolinPlot(double[] chimeraAngles,
            double[] nonChimeraAngles, string identifier = "", bool isTopDown = false)
        {
            var chimeraLabels = Enumerable.Repeat("Chimeras", chimeraAngles.Length).ToArray();
            var nonChimeraLabels = Enumerable.Repeat("No Chimeras", nonChimeraAngles.Length).ToArray();
            string resultType = isTopDown ? "Proteoforms" : "Peptides";
            var violin = Chart.Combine(new[]
                {
                    // chimeras
                    Chart.Violin<string, double, string> (chimeraLabels,chimeraAngles, null, MarkerColor:  PlottingTranslators.ConditionToColorDictionary["Chimeras"], 
                        MeanLine: MeanLine.init(true,  PlottingTranslators.ConditionToColorDictionary["Chimeras"]), ShowLegend: false), 
                    // not chimeras
                    Chart.Violin<string, double, string> (nonChimeraLabels,nonChimeraAngles, null, MarkerColor:  PlottingTranslators.ConditionToColorDictionary["No Chimeras"], 
                        MeanLine: MeanLine.init(true,  PlottingTranslators.ConditionToColorDictionary["No Chimeras"]), ShowLegend: false)

                })
                .WithTitle($"{identifier} Spectral Angle Distribution (1% {resultType})")
                .WithYAxisStyle(Title.init("Spectral Angle"))
                .WithLayout(DefaultLayout)
                .WithSize(1000, 600);
            return violin;
        }


        #endregion

        #region TargetDecoy Investigation

        public static void ExportCombinedChimeraTargetDecoyExploration(this MetaMorpheusResult results, string outputDir, string selectedCondition)
        {
            var proteoforms = results.AllPeptides;
            var qValueFilteredProteoforms = proteoforms.Where(p => p.QValue <= 0.01).ToList();
            var pepQValueFilteredProteoforms = proteoforms.Where(p => p.PEP_QValue <= 0.01).ToList();
            var psms = results.AllPsms;
            var qValueFiltered = psms.Where(p => p.QValue <= 0.01).ToList();
            var pepQValueFiltered = psms.Where(p => p.PEP_QValue <= 0.01).ToList();

            int width;
            var psmChart = Chart.Grid(new List<GenericChart.GenericChart>()
                {
                    qValueFiltered.ChimeraTargetDecoyChart(true, ChimeraBreakdownType.Psm, "QValue", false, out width)
                        .WithXAxisStyle(Title.init(""))
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewXAxis(1))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 0.90)), StyleParam.SubPlotId.NewYAxis(1)),
                    pepQValueFiltered
                        .ChimeraTargetDecoyChart(true, ChimeraBreakdownType.Psm, "PEP QValue", false, out width)
                        .WithXAxisStyle(Title.init(""))
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 1.00)), StyleParam.SubPlotId.NewXAxis(2))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 0.90)), StyleParam.SubPlotId.NewYAxis(2)),
                    qValueFiltered.ChimeraTargetDecoyChart(true, ChimeraBreakdownType.Psm, "QValue", true, out width)
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewXAxis(3))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewYAxis(3)),
                    pepQValueFiltered
                        .ChimeraTargetDecoyChart(true, ChimeraBreakdownType.Psm, "PEP QValue", true, out width)
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 1.00)), StyleParam.SubPlotId.NewXAxis(4))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewYAxis(4)),
                }, 2, 2, YGap: 50)
                .WithSize(1000, 1000)
                .WithTitle($"{selectedCondition} PSMs Target Decoy: QValue Filtered {qValueFiltered.Count} | PEP QValue Filtered {pepQValueFiltered.Count}");
            string psmChartOutPath = Path.Combine(outputDir, $"PSMs_Target_Decoy_{selectedCondition}");
            psmChart.SavePNG(psmChartOutPath, null, 1000, 1000);


            var proteoformChart = Chart.Grid(new List<GenericChart.GenericChart>()
            {
                qValueFilteredProteoforms.ChimeraTargetDecoyChart(true, ChimeraBreakdownType.Peptide, "QValue", false, out width)
                    .WithXAxisStyle(Title.init(""))
                    .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewXAxis(1))
                    .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 0.90)), StyleParam.SubPlotId.NewYAxis(1)),
                pepQValueFilteredProteoforms.ChimeraTargetDecoyChart(true, ChimeraBreakdownType.Peptide, "PEP QValue", false, out width)
                    .WithXAxisStyle(Title.init(""))
                    .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 1.00)), StyleParam.SubPlotId.NewXAxis(2))
                    .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 0.90)), StyleParam.SubPlotId.NewYAxis(2)),
                qValueFilteredProteoforms.ChimeraTargetDecoyChart(true, ChimeraBreakdownType.Peptide, "QValue", true, out width)
                    .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewXAxis(3))
                    .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewYAxis(3)),
                pepQValueFilteredProteoforms.ChimeraTargetDecoyChart(true, ChimeraBreakdownType.Peptide, "PEP QValue", true, out width)
                    .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 1.00)), StyleParam.SubPlotId.NewXAxis(4))
                    .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewYAxis(4))
            }, 2, 2, YGap: 50)
            .WithSize(1000, 1000)
            .WithTitle($"{selectedCondition} Proteoforms Target Decoy: QValue Filtered {qValueFilteredProteoforms.Count} | PEP QValue Filtered {pepQValueFilteredProteoforms.Count}");
            string proteoformChartOutPath = Path.Combine(outputDir, $"Proteoforms_Target_Decoy_{selectedCondition}");
            proteoformChart.SavePNG(proteoformChartOutPath, null, 1000, 1000);

        }

        public static GenericChart.GenericChart ChimeraTargetDecoyChart(this List<PsmFromTsv> psms, bool isTopDown, ChimeraBreakdownType type, string filterType,
            bool absolute, out int width)
        {
            var data = absolute
                ? psms.GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer)
                    .GroupBy(p => p.Count(), p => p)
                    .ToDictionary(p => p.Key, p => p.SelectMany(m => m).ToList())
                    .Select(p => (
                        p.Key,
                        (double)p.Value.Count(m => m.IsDecoy()),
                        (double)p.Value.Count(m => !m.IsDecoy()))
                    )
                    .ToArray()
                : psms.GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer)
                    .GroupBy(p => p.Count(), p => p)
                    .ToDictionary(p => p.Key, p => p.SelectMany(m => m).ToList())
                    .Select(p => (
                        p.Key,
                        Math.Round(p.Value.Count(m => m.IsDecoy()) / (double)p.Value.Count * 100, 2),
                        Math.Round(p.Value.Count(m => !m.IsDecoy()) / (double)p.Value.Count * 100, 2))
                    )
                    .ToArray();

            var keys = data.Select(p => p.Key).ToArray();
            width = Math.Max(600, 50 * data.Length);
            var form = isTopDown ? "Proteoform" : "Peptidoform";
            string title = isTopDown ? type == ChimeraBreakdownType.Psm ? "PrSM" : "Proteoform" :
                type == ChimeraBreakdownType.Psm ? "PSM" : "Peptide";

            width = Math.Max(600, 50 * data.Length);
            var chart = Chart.Combine(new[]
                {

                    Chart.StackedColumn<double, int, string>(data.Select(p => p.Item3), keys, "Targets",
                        MarkerColor: PlottingTranslators.ConditionToColorDictionary["Targets"],
                        MultiText: data.Select(p => p.Item3.ToString()).ToArray()),
                    Chart.StackedColumn<double, int, string>(data.Select(p => p.Item2), keys, "Decoys",
                        MarkerColor: PlottingTranslators.ConditionToColorDictionary["Decoys"],
                        MultiText: data.Select(p => p.Item2.ToString()).ToArray())
                })
                .WithLayout(Layout.init<string>(
                    //PaperBGColor: Color.fromARGB(0, 0,0,0),
                    //PlotBGColor: Color.fromARGB(0, 0, 0, 0),
                    PaperBGColor: Color.fromKeyword(ColorKeyword.White),
                    PlotBGColor: Color.fromKeyword(ColorKeyword.White),
                    ShowLegend: true,
                    Font: Font.init(null, 12), 
                    Legend: Legend.init(X: 0.5, Y: -0.2, Orientation: StyleParam.Orientation.Horizontal, EntryWidth: 0,
                        VerticalAlign: StyleParam.VerticalAlign.Bottom,
                        XAnchor: StyleParam.XAnchorPosition.Center,
                        YAnchor: StyleParam.YAnchorPosition.Top
                    )))
                .WithTitle($"{psms.Count} {filterType} Filtered {title} Chimera Target Decoy")
                .WithXAxisStyle(Title.init($"1% {title}s Per Spectrum"))
                .WithYAxisStyle(Title.init(absolute ? "Count" : "% Decoys"))
                .WithSize(width, 1200);
            return chart;
        }

        public static void ExportPepFeaturesPlots(this MetaMorpheusResult results, string? condition = null)
        {
            string pepForPercolatorPath = Directory.GetFiles(results.DirectoryPath, "*.tab", SearchOption.AllDirectories).First();
            string exportPath = Path.Combine(results.GetFigureDirectory(),
                $"{FileIdentifiers.PepGridChartFigure}_{condition ?? results.Condition}");
            new PepEvaluationPlot(pepForPercolatorPath).Export(exportPath);
        }

        #endregion

        private static string GetFigureDirectory(this AllResults allResults)
        {
            var directory = Path.Combine(allResults.DirectoryPath, "Figures");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            return directory;
        }

        private static string GetFigureDirectory(this CellLineResults cellLine)
        {
            string directory = cellLine.DirectoryPath.Contains("PEPTesting") ?
                 Path.Combine(cellLine.DirectoryPath, "Figures")
                 : Path.Combine(Path.GetDirectoryName(cellLine.DirectoryPath)!, "Figures");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            return directory;
        }

        private static string GetFigureDirectory(this MetaMorpheusResult result)
        {
            string directory = result.DirectoryPath.Contains("PEPTesting") ?
                Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(result.DirectoryPath)), "Figures")
                : Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(result.DirectoryPath)))!, "Figures");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            return directory;
        }
    }
}
