﻿using Analyzer.FileTypes.Internal;
using Analyzer.Plotting.Util;
using Analyzer.Util;
using Plotly.NET.LayoutObjects;
using Plotly.NET;
using Chart = Plotly.NET.CSharp.Chart;
using Proteomics.PSM;
using Analyzer.SearchType;
using Plotly.NET.ImageExport;
using Analyzer.Interfaces;
using Plotting.Util;
using ResultAnalyzerUtil;
using MathNet.Numerics;

namespace Analyzer.Plotting.IndividualRunPlots
{
    public static class ChimeraBreakdownPlots
    {
        /// <summary>
        /// Plots targets and decoys as stacked bar plots as a function of the degree of chimericity for q value and pep filtered at both asPercent and relative scales
        /// </summary>
        /// <param name="results"></param>
        /// <param name="outputDir"></param>
        /// <param name="selectedCondition"></param>
        public static void ExportCombinedChimeraTargetDecoyExploration(this MetaMorpheusResult results, string? outDir = null, string? selectedcondition = null)
        {
            string outputDir = outDir ?? results.DirectoryPath;
            string selectedCondition = selectedcondition ?? results.Condition;

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
            var temp = $"Unique {form}";

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
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
                .WithTitle($"{title2} {title} Identifications per Spectra")
                .WithXAxisStyle(Title.init("IDs per Spectrum"))
                .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(AxisType: StyleParam.AxisType.Log))
                .WithYAxisStyle(Title.init("Count"))
                .WithSize(width, PlotlyBase.DefaultHeight);
            return chart;
        }


        public static void PlotChimeraBreakdownStackedColumn_Scaled(this AllResults allResults, ResultType resultType)
        => allResults.SelectMany(p => p
                .Where(q => q is MetaMorpheusResult && allResults.GetSingleResultSelector().Contains(q.Condition))
                .SelectMany(q => (q as MetaMorpheusResult)?.ChimeraBreakdownFile.Results))
            .ToList()
            .GetChimeraBreakdownStackedColumn_Scaled(resultType, allResults.First().First().IsTopDown)
            .WithTitle($"{allResults.Name} 1% {Labels.GetLabel(allResults.First().First().IsTopDown, resultType)} Identifications")
            .SaveInAllResultsOnly(allResults, $"ChimeraBreakdown_Hybrid_{Labels.GetLabel(allResults.First().First().IsTopDown, resultType)}", 1000, 1000);

        public static void PlotChimeraBreakdownStackedColumn_Scaled(this CellLineResults cellLine, ResultType resultType)
            => cellLine.Where(p => cellLine.GetSingleResultSelector().Contains(p.Condition))
                        .SelectMany(p => (p as MetaMorpheusResult)?.ChimeraBreakdownFile.Results)
                        .ToList()
                        .GetChimeraBreakdownStackedColumn_Scaled(resultType, cellLine.First().IsTopDown)
                        .WithTitle($"{cellLine.CellLine} 1% {Labels.GetLabel(cellLine.First().IsTopDown, resultType)} Identifications")
                        .SaveInCellLineOnly(cellLine, $"ChimeraBreakdown_Hybrid_{Labels.GetLabel(cellLine.First().IsTopDown, resultType)}_{cellLine.CellLine}", 1000, 1000);
        public static void PlotChimeraBreakDownStackedColumn_Scaled(this MetaMorpheusResult result, ResultType resultType)
            => result.ChimeraBreakdownFile.Results
                .GetChimeraBreakdownStackedColumn_Scaled(resultType, result.IsTopDown)
                .WithTitle($"{result.DatasetName} {result.Condition} 1% {Labels.GetLabel(result.IsTopDown, resultType)} Identifications")
                .SaveInRunResultOnly(result, $"ChimeraBreakdown_Hybrid_{Labels.GetLabel(result.IsTopDown, resultType)}", 1000, 1000);

        public static GenericChart.GenericChart GetChimeraBreakdownStackedColumn_Scaled(
            this List<ChimeraBreakdownRecord> results, ResultType resultType = ResultType.Psm, 
            bool isTopDown = false)
        {

            (int IdsPerSpectra, double Parent, double UniqueProtein, double UniqueForms, double Decoys, double Duplicates, double Entrapment)[] data = 
                results.Where(p => p.Type == resultType)
                        .GroupBy(p => p.IdsPerSpectra)
                        .OrderBy(p => p.Key)
                        .Select(p =>
                            (   p.Key,
                                (double)p.Sum(m => m.Parent),
                                (double)p.Sum(m => m.UniqueProteins),
                                (double)p.Sum(m => m.UniqueForms),
                                (double)p.Sum(m => m.DecoyCount),
                                (double)p.Sum(m => m.DuplicateCount),
                                (double)p.Sum(m => m.EntrapmentCount)))
                        .ToArray();

            // Step 2: Calculate the percentages and scale them

            (int IdsPerSpectra, double Parent, double UniqueProtein, double UniqueForms, double Decoys, double Duplicates, double Entrapment)[]
                scaledPercentData = data;
            scaledPercentData = data.Select(p =>
                {
                    double total = p.Parent + p.UniqueProtein + p.UniqueForms + p.Decoys + p.Duplicates;
                    double parentPercent = InterperateVals(p.Parent,
                        total);
                    double decoysPercent = InterperateVals(p.Parent + p.Decoys,
                        total, parentPercent);
                    double uniqueProteinPercent = InterperateVals(p.Parent + p.Decoys + p.UniqueProtein,
                        total, parentPercent + decoysPercent);
                    double uniqueFormsPercent = InterperateVals(p.Parent + p.Decoys + p.UniqueProtein + p.UniqueForms,
                        total, parentPercent + decoysPercent + uniqueProteinPercent);
                    double duplicatesPercent = InterperateVals(p.Parent + p.Decoys + p.UniqueProtein + p.UniqueForms + p.Duplicates,
                        total, parentPercent + decoysPercent + uniqueProteinPercent + uniqueFormsPercent);
                    double entrapmentPercent = InterperateVals(p.Parent + p.Decoys + p.UniqueProtein + p.UniqueForms + p.Duplicates + p.Entrapment,
                        total, parentPercent + decoysPercent + uniqueProteinPercent + uniqueFormsPercent + duplicatesPercent);
                    // Scale percentages so that each bar's height is based on its total value
                    return (p.IdsPerSpectra,
                        parentPercent.Round(0),
                        uniqueProteinPercent.Round(0),
                        uniqueFormsPercent.Round(0),
                        decoysPercent.Round(0),
                        duplicatesPercent.Round(0),
                        entrapmentPercent.Round(0));
                }).ToArray();

            var form = Labels.GetDifferentFormLabel(isTopDown); var keys = data.Select(p => p.IdsPerSpectra).ToArray();
            var charts = new[]
            {
                Chart.StackedColumn<double, int, string>(scaledPercentData.Select(p => p.Parent).ToArray(), keys, "Isolated Species",
                    MarkerColor: "Isolated Species".ConvertConditionToColor(), 
                    MultiText: data.Select(p => p.Parent.ToString()).ToArray()),
                Chart.StackedColumn<double, int, string>(scaledPercentData.Select(p => p.Decoys), keys, "Decoys",
                    MarkerColor: "Decoys".ConvertConditionToColor(),
                    MultiText: data.Select(p => p.Decoys.ToString()).ToArray()),
                Chart.StackedColumn<double, int, string>(scaledPercentData.Select(p => p.UniqueProtein), keys, $"Unique Protein",
                    MarkerColor: "Unique Protein".ConvertConditionToColor(),
                    MultiText: data.Select(p => p.UniqueProtein.ToString()).ToArray()),
                Chart.StackedColumn<double, int, string>(scaledPercentData.Select(p => p.UniqueForms), keys, $"Unique {form}",
                    MarkerColor: $"Unique {form}".ConvertConditionToColor(),
                    MultiText: data.Select(p => p.UniqueForms.ToString()).ToArray()),
            };
            if (data.Any(p => p.Duplicates > 0) && results.Any(p => !p.Condition.Contains("MetaM")))
                charts = charts.Append(Chart.StackedColumn<double, int, string>(scaledPercentData.Select(p => p.Duplicates), keys,
                    "Duplicates",
                    MarkerColor: "Duplicates".ConvertConditionToColor(),
                    MultiText: data.Select(p => p.Duplicates.ToString()).ToArray())).ToArray();
            if (data.Any(p => p.Entrapment > 0))
                charts = charts.Append(Chart.StackedColumn<double, int, string>(scaledPercentData.Select(p => p.Entrapment), keys,
                    "Entrapment",
                    MarkerColor: "Entrapment".ConvertConditionToColor(),
                    MultiText: data.Select(p => p.Entrapment.ToString()).ToArray())).ToArray();
            var chart = Chart.Combine(charts)
                .WithXAxisStyle(Title.init($"1% {Labels.GetLabel(isTopDown, resultType)} per Spectrum", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(AxisType: StyleParam.AxisType.Log))
                .WithYAxisStyle(Title.init("Count of Spectra", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText);
          
            return chart;
        }


        /// <summary>
        /// Converts the incoming value to a number which represents
        /// how tall the bar should be in a stacked clustered bar graph
        /// with a log10 y axis so that the bar shows the proper percent while keeping the same total value
        /// </summary>
        /// <returns></returns>
        public static double InterperateVals(double value, double total, double previous = 0)
        {
            if (value == 0) return value;
            if (value == previous) return 0;
            double logTotal = Math.Log10(total);

            double cumulativePercent = (value /*+ previous*/) / total;
            double logPercent = logTotal * cumulativePercent;


            var converted = Math.Pow(10, logPercent);
            var toReturn = converted - previous;


            return toReturn;
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
            var form = Labels.GetDifferentFormLabel(isTopDown);
            string title = Labels.GetLabel(isTopDown, type);
            var title2 = results.Select(p => p.Dataset).Distinct().Count() == 1 ? results.First().Dataset : "All Results";

            if (asPercent) // convert each column to a percent
            {
                for (int i = 0; i < data.Length; i++)
                {
                    var total = data[i].Parent + data[i].UniqueProtein + data[i].UniqueForms + data[i].Decoys /*+ data[i].Duplicates*/;
                    data[i].Parent = data[i].Parent / total * 100;
                    data[i].UniqueProtein = data[i].UniqueProtein / total * 100;
                    data[i].UniqueForms = data[i].UniqueForms / total * 100;
                    data[i].Decoys = data[i].Decoys / total * 100;
                    data[i].Duplicates = data[i].Duplicates / total * 100;
                }
            }

            var charts = new[]
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

            //if (data.Any(p => p.Duplicates > 0))
            //    charts = charts.Append(Chart.StackedArea<int, double, string>(keys, data.Select(p => p.Duplicates), Name: "Duplicates",
            //        MarkerColor: "Duplicates".ConvertConditionToColor(), MultiText: data.Select(p => p.Duplicates.ToString()).ToArray())).ToArray();

            var chart = Chart.Combine(charts)
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
                .WithTitle($"{title2} {title} Identifications per Spectra \n{extraTitle ?? ""}")
                .WithXAxisStyle(Title.init("IDs per Spectrum"))
                .WithYAxisStyle(Title.init(asPercent ? "Percent" : "Count"))
                .WithSize(width, PlotlyBase.DefaultHeight);
            return chart;
        }

        /// <summary>
        /// Plots targets and decoys as stacked bar plots as a function of the degree of chimericity
        /// </summary>
        /// <param name="results"></param>
        /// <param name="type"></param>
        /// <param name="isTopDown"></param>
        /// <param name="absolute"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static GenericChart.GenericChart GetChimeraBreakDownStackedColumn_TargetDecoy(
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
            string title = Labels.GetLabel(isTopDown, type);
            var title2 = results.Select(p => p.Dataset).Distinct().Count() == 1 ? results.First().Dataset : "All Results";
            var chart = Chart.Combine(new[]
                {
                    Chart.StackedColumn<double, int, string>(data.Select(p => p.Targets), keys, "Targets",
                        MarkerColor: "Targets".ConvertConditionToColor(), MultiText: data.Select(p => Math.Round(p.Targets, 2).ToString()).ToArray()),
                    Chart.StackedColumn<double, int, string>(data.Select(p => p.Decoys), keys, $"Decoys",
                        MarkerColor: "Decoys".ConvertConditionToColor(), MultiText: data.Select(p => Math.Round(p.Decoys, 2).ToString()).ToArray()),
                })
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
                .WithTitle($"{title2} {title} Identifications per Spectra")
                .WithXAxisStyle(Title.init("IDs per Spectrum"))
                .WithYAxisStyle(Title.init("Percent"))
                .WithSize(width, PlotlyBase.DefaultHeight);
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
            string title = Labels.GetLabel(isTopDown, type);

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

        public static void PlotChimeraBreakdownByMassAndCharge(this CellLineResults cellLine)
        {
            bool isTopDown = cellLine.First().IsTopDown;
            var selector = cellLine.GetSingleResultSelector();

            var (chargePlot, massPlot) = cellLine.Where(p => p is IChimeraBreakdownCompatible && selector.Contains(p.Condition))
                .SelectMany(p => ((IChimeraBreakdownCompatible)p).ChimeraBreakdownFile.Results)
                .Where(p => p.Type == ResultType.Psm).ToList().GetChimeraBreakdownByMassAndCharge(ResultType.Psm, isTopDown);
            chargePlot.SaveInCellLineOnly(cellLine, $"{FileIdentifiers.ChimeraBreakdownByChargeStateFigure}_{cellLine.CellLine}_{ResultType.Psm}", 600, 600);
            massPlot.SaveInCellLineOnly(cellLine, $"{FileIdentifiers.ChimeraBreakdownByMassFigure}_{cellLine.CellLine}_{ResultType.Psm}", 600, 600);

            (chargePlot, massPlot) = cellLine.Where(p => p is IChimeraBreakdownCompatible && selector.Contains(p.Condition))
                .SelectMany(p => ((IChimeraBreakdownCompatible)p).ChimeraBreakdownFile.Results)
                .Where(p => p.Type == ResultType.Peptide).ToList().GetChimeraBreakdownByMassAndCharge(ResultType.Peptide, isTopDown);
            chargePlot.SaveInCellLineOnly(cellLine, $"{FileIdentifiers.ChimeraBreakdownByChargeStateFigure}_{cellLine.CellLine}_{ResultType.Peptide}", 600, 600);
            massPlot.SaveInCellLineOnly(cellLine, $"{FileIdentifiers.ChimeraBreakdownByMassFigure}_{cellLine.CellLine}_{ResultType.Peptide}", 600, 600);
        }

        public static (GenericChart.GenericChart Charge, GenericChart.GenericChart Mass) GetChimeraBreakdownByMassAndCharge(this List<ChimeraBreakdownRecord> results, 
            ResultType resultType = ResultType.Psm, bool isTopDown = false)
        {
            var smLabel = Labels.GetSpectrumMatchLabel(isTopDown);
            var pepLabel = Labels.GetPeptideLabel(isTopDown);
            var label = resultType == ResultType.Psm ? smLabel : pepLabel;

            List<double> yValuesMass = new();
            List<int> yValuesCharge = new();
            List<int> xValues = new();
            foreach (var result in results)
            {
                if (resultType == ResultType.Psm)
                {
                    yValuesMass.AddRange(result.PsmMasses);
                    yValuesCharge.AddRange(result.PsmCharges);
                    xValues.AddRange(Enumerable.Repeat(result.IdsPerSpectra, result.PsmMasses.Length));
                }
                else
                {
                    yValuesMass.AddRange(result.PeptideMasses);
                    yValuesCharge.AddRange(result.PeptideCharges);
                    xValues.AddRange(Enumerable.Repeat(result.IdsPerSpectra, result.PeptideMasses.Length));
                }
            }

            var chargePlot =
                Chart.BoxPlot<int, int, string>(xValues, yValuesCharge)
                    .WithXAxisStyle(Title.init("Degree of Chimerism"))
                    .WithYAxisStyle(Title.init("Precursor Charge State"))
                    .WithTitle($"1% {label} Charge vs Degree of Chimerism");

            var massPlot =
                Chart.BoxPlot<int, double, string>(xValues, yValuesMass)
                    .WithXAxisStyle(Title.init("Degree of Chimerism"))
                    .WithYAxisStyle(Title.init("Precursor Mass"))
                    .WithTitle($"1% {label} Mass vs Degree of Chimerism");

            return (chargePlot, massPlot);
        }
    }
}
