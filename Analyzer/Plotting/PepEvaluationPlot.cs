using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.FileTypes.Internal;
using MathNet.Numerics;
using pepXML.Generated;
using Chart = Plotly.NET.CSharp.Chart;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.ImageExport;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;
using GenericChartExtensions = Plotly.NET.GenericChartExtensions;

namespace Analyzer.Plotting
{
    public class PepEvaluationPlot
    {
        private int exportWidth;
        private int exportHeight;
        public List<PepAnalysis> AllResults { get; set; }

        private GenericChart.GenericChart? _pepChart;
        public GenericChart.GenericChart PepChart => _pepChart ??= GenerateChart();

        public PepEvaluationPlot(List<PepAnalysis> allResults)
        {
            AllResults = allResults;
        }

        public PepEvaluationPlot(string filePath)
        {
            var pepAnalysis = new PepAnalysisForPercolatorFile(filePath);
            pepAnalysis.LoadResults();
            AllResults = pepAnalysis.Results;
        }

        /// <summary>
        /// Generate a grid chart that is a scatter plot matrix of the properties of the PSMs
        /// </summary>
        public GenericChart.GenericChart GenerateChart()
        {
            List<GenericChart.GenericChart> scatters = new();
            foreach (var datum in GenerateScatterData())
            {
                if (datum.Bins.Count == 0)
                    continue;
                var chart = Chart.Bubble<double, double, string>(datum.Bins.Select(p => p.x),
                        datum.Bins.Select(p => p.y), datum.Bins.Select(p => (int)Math.Max(p.size, 1)),
                        datum.Label, false)
                    .WithXAxisStyle(Title.init(datum.Label));
                scatters.Add(chart);
            }

            var totalPlots = scatters.Count;
            var columns = 3;
            var rows = (int)Math.Ceiling(totalPlots / (double)columns);

            exportHeight = 300 * rows;
            exportWidth = 300 * columns;
            var grid = Chart.Grid(scatters, rows, columns)
                .WithSize(exportWidth, exportHeight);
            return grid;
        }

        public void ShowChart()
        {
           GenericChartExtensions.Show(PepChart);
        }

        public void Export(string path)
        {
            try
            {
                PepChart.SavePNG(path, null, exportWidth, exportHeight);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public List<PepSplomData> GenerateScatterData()
        {
            var data = new List<PepSplomData>
            {
                new PepSplomData("MatchedIonCount", true),
                new PepSplomData("Intensity", false),
                new PepSplomData("PrecursorChargeDifference", false),
                new PepSplomData("DeltaScore", false),
                new PepSplomData("Notch", true),
                new PepSplomData("ModCount", true),
                new PepSplomData("FragmentMassError", false),
                new PepSplomData("AmbiguityLevel", true),
                new PepSplomData("LongestIonSeries", true),
                new PepSplomData("ComplementaryIonCount", false),
                new PepSplomData("PeaksInPrecursorEnvelope", true),
                new PepSplomData("PrecursorEnvelopeScore", false),
                new PepSplomData("ChimeraCount", true),
                new PepSplomData("ChimeraDecoyRatio", false),

                new PepSplomData("HasSpectralAngle", true),
                new PepSplomData("SpectralAngle", false),
                new PepSplomData("PsmCount", false),

                // for bottom up only
                new PepSplomData("HydrophobicityZScore", false),
                new PepSplomData("MissedCleavages", true),
                new PepSplomData("IsVariantPeptide", false),
                new PepSplomData("IsDeadEnd", false),
                new PepSplomData("IsLoop", false),
            };
            data.ForEach(p => p.CreateBins(AllResults));
            return data;
        }
    }


    public class PepSplomData
    {
        public string Label { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public int NumberOfBins { get; set; }
        public bool IsDiscrete { get; set; } = false;
        /// <summary>
        /// List where x is the value of the property and y is the portion of targets/total that fall into that bin
        /// </summary>
        public List<(double x, double y, int size)> Bins { get; set; }

        public PepSplomData(string Label, bool isDiscrete, double? min= null, double? max = null, int numberOfBins = 30)
        {
            this.Label = Label;
            IsDiscrete = isDiscrete;
            Min = min ?? 0;
            Max = max ?? 0;
            NumberOfBins = numberOfBins;
        }


        public void CreateBins(List<PepAnalysis> test)
        {
            var propertyData = test.Select(p => ((double)p.GetType().GetProperty(Label).GetValue(p), p.IsDecoy)).ToList();
            int numberOfSizes = 10;
            int sizeScaler = 5;


            List<(double x, double y, int size)> allBins = new List<(double x, double y, int size)>();
            if (IsDiscrete)
            {
                var groups = propertyData.GroupBy(p => p.Item1).ToList();
                // normalize sizes with the smallest being 1 and the largest being numberOfSizes and linearly scaling to interger values inbetween
                var minSize = groups.Min(p => p.Count());
                var maxSize = groups.Max(p => p.Count());
                var sizeScale = (maxSize - minSize) / (double)numberOfSizes;
                var groupSizes = groups.Select(p => (int)Math.Round((p.Count() - minSize) / sizeScale + sizeScaler)).ToList();
                for (int i = 0; i < groups.Count; i++)
                {
                    var group = groups[i];
                    var portion = group.Count(p => !p.IsDecoy) / (double)group.Count();
                    if (double.IsNaN(portion)) continue;
                    allBins.Add((group.Key, portion, groupSizes[i]));
                }
            }
            else
            {
                var min = propertyData.Min(p => p.Item1);
                var max = propertyData.Max(p => p.Item1);
                var binSize = (max - min) / NumberOfBins;
                for (int i = 0; i < NumberOfBins; i++)
                {
                    var binMin = min + i * binSize;
                    var binMax = min + (i + 1) * binSize;
                    var bin = propertyData.Where(p => p.Item1 >= binMin && p.Item1 < binMax).ToList();
                    var portion = bin.Count(p => !p.IsDecoy) / (double)bin.Count();
                    if (double.IsNaN(portion)) continue;
                    allBins.Add((binMin, portion, bin.Count()));
                }

                if (!allBins.Any())
                {
                    Bins = allBins.OrderBy(p => p.x).ToList();
                    return;
                }


                // normalize sizes with the smallest being 1 and the largest being numberOfSizes and linearly scaling to interger values inbetween
                var minSize = allBins.Min(p => p.size);
                var maxSize = allBins.Max(p => p.size);
                var sizeScale = (maxSize - minSize) / (double)numberOfSizes;
                allBins = allBins.Select(p => (p.x, p.y, (int)Math.Round((p.size - minSize) / sizeScale + sizeScaler))).ToList();
            }

            Bins = allBins.OrderBy(p => p.x).ToList();
        }
    }
}
