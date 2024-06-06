using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.FileTypes.Internal;
using Analyzer.Util;
using Chart = Plotly.NET.CSharp.Chart;
using Plotly.NET;
using Plotly.NET.ImageExport;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;
using Proteomics.PSM;
using Analyzer.SearchType;
using Easy.Common;
using static Analyzer.Plotting.CellLinePlots;
using MathNet.Numerics.Statistics;

namespace Analyzer.Plotting
{
    public enum TargetDecoyCurveMode
    {
        Score,
        QValue,
        PepQValue,
        Pep,
    }

    public static class GenericPlots
    {

        public static Func<BulkResultCountComparison, int> ResultSelector(ResultType resultType)
        {
            return resultType switch
            {
                ResultType.Psm => m => m.OnePercentPsmCount,
                ResultType.Peptide => m => m.OnePercentPeptideCount,
                ResultType.Protein => m => m.OnePercentProteinGroupCount,
                _ => throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null)
            };
        }

        #region Labels

        public static string SpectralMatchLabel(bool isTopDown) => isTopDown ? "PrSMs" : "PSMs";
        public static string ResultLabel(bool isTopDown) => isTopDown ? "Proteoforms" : "Peptides";

        public static string Label(bool isTopDown, ResultType resultType) => resultType switch
        {
            ResultType.Psm => SpectralMatchLabel(isTopDown),
            ResultType.Peptide => ResultLabel(isTopDown),
            ResultType.Protein => "Proteins",
            _ => throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null)
        };

        #endregion

        #region Plotly Things

        public static int DefaultHeight = 600;
        public static Layout DefaultLayout => Layout.init<string>(PaperBGColor: Color.fromKeyword(ColorKeyword.White), PlotBGColor: Color.fromKeyword(ColorKeyword.White));

        public static Layout DefaultLayoutWithLegend => Layout.init<string>(
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

        public static Layout DefaultLayoutWithLegendTransparentBackground => Layout.init<string>(
            PaperBGColor: Color.fromARGB(0, 0, 0, 0),
            PlotBGColor: Color.fromARGB(0, 0, 0, 0),
            ShowLegend: true,
            Legend: Legend.init(X: 0.5, Y: -0.2, Orientation: StyleParam.Orientation.Horizontal, EntryWidth: 0,
                VerticalAlign: StyleParam.VerticalAlign.Bottom,
                XAnchor: StyleParam.XAnchorPosition.Center,
                YAnchor: StyleParam.YAnchorPosition.Top
            ));

        #endregion

        public static GenericChart.GenericChart IndividualFileResultBarChart(List<BulkResultCountComparisonFile> results, 
            out int width, out int height, string title = "", bool isTopDown = false, ResultType resultType = ResultType.Psm)
        {
            results.ForEach(p => p.Results = p.Results.OrderBy(m => m.FileName.ConvertFileName()).ToList());
            var labels = results.SelectMany(p => p.Results.Select(m => m.FileName))
                .ConvertFileNames().Distinct().ToList();
            

            // if results exist for one dataset but not the other, ensure they are plotted in the correct order
            foreach (var individualFile in results)
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

            List<GenericChart.GenericChart> charts = results.Select(result =>
                Chart2D.Chart.Column<int, string, string, int, int>(result.Select(ResultSelector(resultType)), labels, null,
                    result.Results.First().Condition.ConvertConditionName(),
                    MarkerColor: result.First().Condition.ConvertConditionToColor())).ToList();

            width = 50 * labels.Count + 10 * results.Count;
            height = DefaultHeight;
            var chart = Chart.Combine(charts)
                .WithTitle($"{title} 1% FDR {Label(isTopDown, resultType)}")
                .WithXAxisStyle(Title.init("File"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(DefaultLayoutWithLegend)
                .WithSize(width, height);
            return chart;
        }

        public static GenericChart.GenericChart SpectralAngleChimeraComparisonViolinPlot(double[] chimeraAngles,
            double[] nonChimeraAngles, string identifier = "", bool isTopDown = false)
        {
            var chimeraLabels = Enumerable.Repeat("Chimeras", chimeraAngles.Length).ToArray();
            var nonChimeraLabels = Enumerable.Repeat("No Chimeras", nonChimeraAngles.Length).ToArray();
            string resultType = ResultLabel(isTopDown);
            var violin = Chart.Combine(new[]
                {
                    // chimeras
                    Chart.Violin<string, double, string> (chimeraLabels,chimeraAngles, null, MarkerColor: "Chimeras".ConvertConditionToColor(),
                        MeanLine: MeanLine.init(true,  "Chimeras".ConvertConditionToColor()), ShowLegend: false), 
                    // not chimeras
                    Chart.Violin<string, double, string> (nonChimeraLabels,nonChimeraAngles, null, MarkerColor:  "No Chimeras".ConvertConditionToColor(),
                        MeanLine: MeanLine.init(true,  "No Chimeras".ConvertConditionToColor()), ShowLegend: false)

                })
                .WithTitle($"{identifier} Spectral Angle Distribution (1% {resultType})")
                .WithYAxisStyle(Title.init("Spectral Angle"))
                .WithLayout(DefaultLayout)
                .WithSize(1000, 600);
            return violin;
        }

        public static GenericChart.GenericChart BulkResultBarChart(List<BulkResultCountComparison> results,
            bool isTopDown = false, ResultType resultType = ResultType.Psm)
        {
            var labels = results.Select(p => p.DatasetName).Distinct().ConvertConditionNames().ToList();

            List<GenericChart.GenericChart> charts = new();
            foreach (var condition in results.Select(p => p.Condition).ConvertConditionNames().Distinct())
            {
                var conditionSpecificResults = results.Where(p => p.Condition.ConvertConditionName() == condition).ToList();

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
                charts.Add(Chart2D.Chart.Column<int, string, string, int, int>(
                    conditionSpecificResults.Select(ResultSelector(resultType)), labels, null, conditionToWrite,
                    MarkerColor: condition.ConvertConditionToColor()));
            }

            return Chart.Combine(charts).WithTitle($"1% FDR {Label(isTopDown, resultType)}")
                .WithXAxisStyle(Title.init("Cell Line"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(DefaultLayoutWithLegend);
        }


        /// <summary>
        /// Plots the type of chimera as a function of the number of results they are chimeric with
        /// types include sharing the same base sequence or being an entirely different protein/peptide
        /// </summary>
        /// <param name="results"></param>
        /// <param name="type"></param>
        /// <param name="isTopDown"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        internal static GenericChart.GenericChart GetChimeraBreakDownStackedColumn(this List<ChimeraBreakdownRecord> results,
            ResultType type, bool isTopDown, out int width, string? extraTitle = null)
        {
            (int IdPerSpec, int Parent, int UniqueProtein, int UniqueForms, int Decoys, int Duplicates)[] data = results.Where(p => p.Type == type)
                .GroupBy(p => p.IdsPerSpectra)
                .OrderBy(p => p.Key)
                .Select(p =>
                    (
                        p.Key,
                        p.Sum(m => m.Parent),
                        p.Sum(m => m.UniqueProteins),
                        p.Sum(m => m.UniqueForms),
                        p.Sum(m => m.DecoyCount),
                        p.Sum(m => m.DuplicateCount))
                    )
                .ToArray();
            var keys = data.Select(p => p.IdPerSpec).ToArray();
            width = Math.Max(600, 50 * data.Length);
            var form = isTopDown ? "Proteoform" : "Peptidoform";
            string title = isTopDown ? type == ResultType.Psm ? "PrSM" : "Proteoform" :
                type == ResultType.Psm ? "PSM" : "Peptide";
            var title2 = results.Select(p => p.Dataset).Distinct().Count() == 1 ? results.First().Dataset : "All Results";

            var charts = new[]
            {
                Chart.StackedColumn<int, int, string>(data.Select(p => p.Parent), keys, "Isolated Species",
                    MarkerColor: "Isolated Species".ConvertConditionToColor(),
                    MultiText: data.Select(p => p.Parent.ToString()).ToArray()),
                Chart.StackedColumn<int, int, string>(data.Select(p => p.Decoys), keys, "Decoys",
                    MarkerColor: "Decoys".ConvertConditionToColor(),
                    MultiText: data.Select(p => p.Decoys.ToString()).ToArray()),
                Chart.StackedColumn<int, int, string>(data.Select(p => p.UniqueProtein), keys, $"Unique Protein",
                    MarkerColor: "Unique Protein".ConvertConditionToColor(),
                    MultiText: data.Select(p => p.UniqueProtein.ToString()).ToArray()),
                Chart.StackedColumn<int, int, string>(data.Select(p => p.UniqueForms), keys, $"Unique {form}",
                    MarkerColor: $"Unique {form}".ConvertConditionToColor(),
                    MultiText: data.Select(p => p.UniqueForms.ToString()).ToArray()),
            };

            if (data.Any(p => p.Duplicates > 0))
                charts = charts.Append(Chart.StackedColumn<int, int, string>(data.Select(p => p.Duplicates), keys,
                    "Duplicates",
                    MarkerColor: "Duplicates".ConvertConditionToColor(),
                    MultiText: data.Select(p => p.Duplicates.ToString()).ToArray())).ToArray();

            var chart = Chart.Combine(charts)
                .WithLayout(DefaultLayoutWithLegend)
                .WithTitle($"{title2} {title} Identifications per Spectra")
                .WithXAxisStyle(Title.init("IDs per Spectrum"))
                .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(AxisType: StyleParam.AxisType.Log))
                .WithYAxisStyle(Title.init("Count"))
                .WithSize(width, DefaultHeight);
            return chart;
        }

        internal static GenericChart.GenericChart GetChimeraBreakDownStackedArea(this List<ChimeraBreakdownRecord> results,
            ResultType type, bool isTopDown, out int width, bool asPercent = false, string? extraTitle = null)
        {
            (int IdPerSpec, double Parent, double UniqueProtein, double UniqueForms, double Decoys, double Duplicates)[] data = results.Where(p => p.Type == type)
                .GroupBy(p => p.IdsPerSpectra)
                .OrderBy(p => p.Key)
                .Select(p =>
                    (
                        p.Key,
                        (double)p.Sum(m => m.Parent),
                        (double)p.Sum(m => m.UniqueProteins),
                        (double)p.Sum(m => m.UniqueForms),
                        (double)p.Sum(m => m.DecoyCount),
                        (double)p.Sum(m => m.DuplicateCount))
                )
                .ToArray();
            var keys = data.Select(p => p.IdPerSpec).ToArray();
            width = Math.Max(600, 50 * data.Length);
            var form = isTopDown ? "Proteoform" : "Peptidoform";
            string title = isTopDown ? type == ResultType.Psm ? "PrSM" : "Proteoform" :
                type == ResultType.Psm ? "PSM" : "Peptide";
            var title2 = results.Select(p => p.Dataset).Distinct().Count() == 1 ? results.First().Dataset : "All Results";

            if (asPercent) // convert each column to a percent
            {
                for (int i = 0; i < data.Length; i++)
                {
                    var total = data[i].Parent + data[i].UniqueProtein + data[i].UniqueForms + data[i].Decoys + data[i].Duplicates;
                    data[i].Parent = data[i].Parent / total * 100;
                    data[i].UniqueProtein = data[i].UniqueProtein / total * 100;
                    data[i].UniqueForms = data[i].UniqueForms / total * 100;
                    data[i].Decoys = data[i].Decoys / total * 100;
                }
            }

            var charts = new []
            {
                Chart.StackedArea<int, double, string>(keys, data.Select(p => p.Parent), Name: "Isolated Species",
                    MarkerColor: "Isolated Species".ConvertConditionToColor(), MultiText: data.Select(p => p.Parent.ToString()).ToArray()),

                Chart.StackedArea<int, double, string>(keys, data.Select(p => p.Decoys), Name: "Decoys",
                    MarkerColor: "Decoys".ConvertConditionToColor(), MultiText: data.Select(p => p.Decoys.ToString()).ToArray()),

                Chart.StackedArea<int, double, string>(keys, data.Select(p => p.UniqueProtein), Name: $"Unique Protein",
                    MarkerColor: "Unique Protein".ConvertConditionToColor(), MultiText: data.Select(p => p.UniqueProtein.ToString()).ToArray()),

                Chart.StackedArea<int, double, string>(keys, data.Select(p => p.UniqueForms), Name: $"Unique {form}",
                    MarkerColor: $"Unique {form}".ConvertConditionToColor(), MultiText: data.Select(p => p.UniqueForms.ToString()).ToArray()),
            };

            if (data.Any(p => p.Duplicates > 0))
                charts = charts.Append(Chart.StackedArea<int, double, string>(keys, data.Select(p => p.Duplicates), Name: "Duplicates",
                    MarkerColor: "Duplicates".ConvertConditionToColor(), MultiText: data.Select(p => p.Duplicates.ToString()).ToArray())).ToArray();

            var chart = Chart.Combine(charts)
                .WithLayout(DefaultLayoutWithLegend)
                .WithTitle($"{title2} {title} Identifications per Spectra \n{extraTitle ?? ""}")
                .WithXAxisStyle(Title.init("IDs per Spectrum"))
                .WithYAxisStyle(Title.init( asPercent ? "Percent" :"Count"))
                .WithSize(width, DefaultHeight);
            return chart;
        }

        #region Target Decoy Exploration

        /// <summary>
        /// Plots targets and decoys as stacked bar plots as a function of the degree of chimericity
        /// </summary>
        /// <param name="results"></param>
        /// <param name="type"></param>
        /// <param name="isTopDown"></param>
        /// <param name="absolute"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        internal static GenericChart.GenericChart GetChimeraBreakDownStackedColumn_TargetDecoy(
            this List<ChimeraBreakdownRecord> results, ResultType type, bool isTopDown, bool absolute, out int width)
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
            string title = isTopDown ? type == ResultType.Psm ? "PrSM" : "Proteoform" :
                type == ResultType.Psm ? "PSM" : "Peptide";
            var title2 = results.Select(p => p.Dataset).Distinct().Count() == 1 ? results.First().Dataset : "All Results";
            var chart = Chart.Combine(new[]
                {
                    Chart.StackedColumn<double, int, string>(data.Select(p => p.Targets), keys, "Targets",
                        MarkerColor: "Targets".ConvertConditionToColor(), MultiText: data.Select(p => Math.Round(p.Targets, 2).ToString()).ToArray()),
                    Chart.StackedColumn<double, int, string>(data.Select(p => p.Decoys), keys, $"Decoys",
                        MarkerColor: "Decoys".ConvertConditionToColor(), MultiText: data.Select(p => Math.Round(p.Decoys, 2).ToString()).ToArray()),
                })
                .WithLayout(DefaultLayoutWithLegend)
                .WithTitle($"{title2} {title} Identifications per Spectra")
                .WithXAxisStyle(Title.init("IDs per Spectrum"))
                .WithYAxisStyle(Title.init("Percent"))
                .WithSize(width, DefaultHeight);
            return chart;
        }

        /// <summary>
        /// Plots targets and decoys as stacked bar plots as a function of the degree of chimericity
        /// </summary>
        /// <param name="psms"></param>
        /// <param name="isTopDown"></param>
        /// <param name="type"></param>
        /// <param name="filterType"></param>
        /// <param name="absolute"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static GenericChart.GenericChart GetChimeraBreakDownStackedColumn_TargetDecoy(
            this List<PsmFromTsv> psms, bool isTopDown, ResultType type, string filterType, bool absolute, out int width)
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
            string title = isTopDown ? type == ResultType.Psm ? "PrSM" : "Proteoform" :
                type == ResultType.Psm ? "PSM" : "Peptide";

            width = Math.Max(600, 50 * data.Length);
            var chart = Chart.Combine(new[]
                {

                    Chart.StackedColumn<double, int, string>(data.Select(p => p.Item3), keys, "Targets",
                        MarkerColor: "Targets".ConvertConditionToColor(),
                        MultiText: data.Select(p => p.Item3.ToString()).ToArray()),
                    Chart.StackedColumn<double, int, string>(data.Select(p => p.Item2), keys, "Decoys",
                        MarkerColor: "Decoys".ConvertConditionToColor(),
                        MultiText: data.Select(p => p.Item2.ToString()).ToArray())
                })
                .WithLayout(Layout.init<string>(
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

        /// <summary>
        /// Plots targets and decoys as stacked bar plots as a function of the degree of chimericity for q value and pep filtered at both asPercent and relative scales
        /// </summary>
        /// <param name="results"></param>
        /// <param name="outputDir"></param>
        /// <param name="selectedCondition"></param>
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
                    qValueFiltered.GetChimeraBreakDownStackedColumn_TargetDecoy(true, ResultType.Psm, "QValue", false, out width)
                        .WithXAxisStyle(Title.init(""))
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewXAxis(1))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 0.90)), StyleParam.SubPlotId.NewYAxis(1)),
                    pepQValueFiltered
                        .GetChimeraBreakDownStackedColumn_TargetDecoy(true, ResultType.Psm, "PEP QValue", false, out width)
                        .WithXAxisStyle(Title.init(""))
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 1.00)), StyleParam.SubPlotId.NewXAxis(2))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 0.90)), StyleParam.SubPlotId.NewYAxis(2)),
                    qValueFiltered.GetChimeraBreakDownStackedColumn_TargetDecoy(true, ResultType.Psm, "QValue", true, out width)
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewXAxis(3))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewYAxis(3)),
                    pepQValueFiltered
                        .GetChimeraBreakDownStackedColumn_TargetDecoy(true, ResultType.Psm, "PEP QValue", true, out width)
                        .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 1.00)), StyleParam.SubPlotId.NewXAxis(4))
                        .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewYAxis(4)),
                }, 2, 2, YGap: 50)
                .WithSize(1000, 1000)
                .WithTitle($"{selectedCondition} PSMs Target Decoy: QValue Filtered {qValueFiltered.Count} | PEP QValue Filtered {pepQValueFiltered.Count}");
            string psmChartOutPath = Path.Combine(outputDir, $"PSMs_Target_Decoy_{selectedCondition}");
            psmChart.SavePNG(psmChartOutPath, null, 1000, 1000);

            var proteoformChart = Chart.Grid(new List<GenericChart.GenericChart>()
            {
                qValueFilteredProteoforms.GetChimeraBreakDownStackedColumn_TargetDecoy(true, ResultType.Peptide, "QValue", false, out width)
                    .WithXAxisStyle(Title.init(""))
                    .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewXAxis(1))
                    .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 0.90)), StyleParam.SubPlotId.NewYAxis(1)),
                pepQValueFilteredProteoforms.GetChimeraBreakDownStackedColumn_TargetDecoy(true, ResultType.Peptide, "PEP QValue", false, out width)
                    .WithXAxisStyle(Title.init(""))
                    .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 1.00)), StyleParam.SubPlotId.NewXAxis(2))
                    .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 0.90)), StyleParam.SubPlotId.NewYAxis(2)),
                qValueFilteredProteoforms.GetChimeraBreakDownStackedColumn_TargetDecoy(true, ResultType.Peptide, "QValue", true, out width)
                    .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewXAxis(3))
                    .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewYAxis(3)),
                pepQValueFilteredProteoforms.GetChimeraBreakDownStackedColumn_TargetDecoy(true, ResultType.Peptide, "PEP QValue", true, out width)
                    .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.55, 1.00)), StyleParam.SubPlotId.NewXAxis(4))
                    .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.45)), StyleParam.SubPlotId.NewYAxis(4))
            }, 2, 2, YGap: 50)
            .WithSize(1000, 1000)
            .WithTitle($"{selectedCondition} Proteoforms Target Decoy: QValue Filtered {qValueFilteredProteoforms.Count} | PEP QValue Filtered {pepQValueFilteredProteoforms.Count}");
            string proteoformChartOutPath = Path.Combine(outputDir, $"Proteoforms_Target_Decoy_{selectedCondition}");
            proteoformChart.SavePNG(proteoformChartOutPath, null, 1000, 1000);
        }


        /// <summary>
        /// Exports a grid of scatter plots of the ratio of targets/total results for each PEP training feature
        /// </summary>
        /// <param name="results"></param>
        /// <param name="condition"></param>
        public static void ExportPepFeaturesPlots(this MetaMorpheusResult results, string? condition = null)
        {
            string pepForPercolatorPath = Directory.GetFiles(results.DirectoryPath, "*.tab", SearchOption.AllDirectories).First();
            string exportPath = Path.Combine(results.GetFigureDirectory(),
                $"{FileIdentifiers.PepGridChartFigure}_{results.DatasetName}_{condition ?? results.Condition}");
            var plot = new PepEvaluationPlot(pepForPercolatorPath);
            plot.Export(exportPath);

            exportPath = Path.Combine(results.FigureDirectory,
                $"{FileIdentifiers.PepGridChartFigure}_{results.DatasetName}_{condition ?? results.Condition}");
            plot.Export(exportPath);
        }


        /// <summary>
        /// Plots the target decoy curve(s) for the dataset, if result type or mode is left null, it will perform all
        /// </summary>
        /// <param name="results"></param>
        /// <param name="resultType"></param>
        /// <param name="mode"></param>
        public static void PlotTargetDecoyCurves(this MetaMorpheusResult results,
            ResultType? resultType = null, TargetDecoyCurveMode? mode = null)
        {
            List<(ResultType, TargetDecoyCurveMode)> plotsToRun = new List<(ResultType, TargetDecoyCurveMode)>();

            
            if (mode != null) // mode is selected
            {
                if (resultType != null)
                    plotsToRun.Add((resultType.Value, mode.Value));
                else
                {
                    plotsToRun.Add((ResultType.Psm, mode.Value));
                    plotsToRun.Add((ResultType.Peptide, mode.Value));
                }
            }
            else // mode is not selected
            {
                if (resultType != null)
                    plotsToRun.AddRange(Enum.GetValues<TargetDecoyCurveMode>().Select(m => (resultType.Value, m)));
                else
                {
                    plotsToRun.AddRange(Enum.GetValues<TargetDecoyCurveMode>().Select(m => (ResultType.Psm, m)));
                    plotsToRun.AddRange(Enum.GetValues<TargetDecoyCurveMode>().Select(m => (ResultType.Peptide, m)));
                }
            }

            foreach (var plotParams in plotsToRun)
            {
                var plot = results.CreateTargetDecoyCurve(plotParams.Item1, plotParams.Item2);
                string outPath = Path.Combine(results.FigureDirectory,
                                       $"{FileIdentifiers.TargetDecoyCurve}_{results.DatasetName}_{results.Condition}_{Label(results.IsTopDown, plotParams.Item1)}_{plotParams.Item2}");
                plot.SavePNG(outPath, null, 600, 400);
            }
        }

        internal static GenericChart.GenericChart CreateTargetDecoyCurve(this MetaMorpheusResult results, ResultType resultType, TargetDecoyCurveMode mode)
        {
            var allResults = resultType switch
            {
                ResultType.Psm => results.AllPsms,
                ResultType.Peptide => results.AllPeptides,
                _ => throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null)
            };

            IEnumerable<IGrouping<double, PsmFromTsv>>? binnedResults;
            switch (mode)
            {
                case TargetDecoyCurveMode.Score:
                    binnedResults = allResults.GroupBy(p => Math.Floor(p.Score));
                    break;
                case TargetDecoyCurveMode.QValue: // from 0 to 1 in 100 bins
                    binnedResults = allResults.GroupBy(p => Math.Floor(p.QValue * 100));
                    break;
                case TargetDecoyCurveMode.PepQValue: // from 0 to 1 in 100 bins
                    binnedResults = allResults.GroupBy(p => Math.Floor(p.PEP_QValue * 100));
                    break;
                case TargetDecoyCurveMode.Pep: // from 0 to 1 in 100 bins
                    binnedResults = allResults.GroupBy(p => Math.Floor(p.PEP * 100));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
            var resultDict = binnedResults.OrderBy(p => p.Key).ToDictionary(p => p.Key,
                p => (p.Count(sm => sm.IsDecoy()), p.Count(sm => !sm.IsDecoy())));
            var xValues = mode == TargetDecoyCurveMode.Score ? resultDict.Keys.ToArray() : resultDict.Keys.Select(p => p / 1000.0).ToArray();
            var targetValues = mode == TargetDecoyCurveMode.Score 
                ? resultDict.Values.Select(p => p.Item2).ToArray() 
                : resultDict.Values.Select(p => p.Item2 / 100).ToArray();
            var decoyValues = mode == TargetDecoyCurveMode.Score 
                ? resultDict.Values.Select(p => p.Item1).ToArray() 
                : resultDict.Values.Select(p => p.Item1 / 100).ToArray();

            var targetDecoyChart = Chart.Combine(new[]
                {
                    Chart.Spline<double, int, string>(xValues, targetValues, Name: "Targets"),
                    Chart.Spline<double, int, string>(xValues, decoyValues, Name: "Decoys"),
                })
                .WithTitle($"{results.DatasetName} {results.Condition} {Label(results.IsTopDown, resultType)} by {mode}")
                .WithSize(600, 400)
                .WithLayout(DefaultLayoutWithLegend);


            return targetDecoyChart;
        }


        #endregion

        internal static GenericChart.GenericChart KernelDensityPlot(List<double> values, string title,
            string xTitle = "", string yTitle = "", double bandwidth = 0.2, Kernels kernel = Kernels.Gaussian)
        {
            List<(double, double)> data = new List<(double, double)>();

            foreach (var sample in values)
            {
                var pdf = kernel switch
                {
                    Kernels.Gaussian => KernelDensity.EstimateGaussian(sample, bandwidth, values),
                    Kernels.Epanechnikov => KernelDensity.EstimateEpanechnikov(sample, bandwidth, values),
                    Kernels.Triangular => KernelDensity.EstimateTriangular(sample, bandwidth, values),
                    Kernels.Uniform => KernelDensity.EstimateUniform(sample, bandwidth, values),
                    _ => throw new ArgumentOutOfRangeException(nameof(kernel), kernel, null)
                };
                data.Add((sample, pdf));
            }

            var chart =
                Chart.Line<double, double, string>(data.Select(p => p.Item1), data.Select(p => p.Item2), Name: title,
                        LineColor: title.ConvertConditionToColor())
                    .WithSize(400, 400)
                    .WithXAxisStyle(Title.init(xTitle))
                    .WithYAxisStyle(Title.init(yTitle))
                    .WithLayout(DefaultLayoutWithLegend);
            return chart;
        }
    }

}
