using MathNet.Numerics;
using Microsoft.FSharp.Core;
using Omics.Modifications;
using Plotly.NET;
using Plotly.NET.ImageExport;
using Plotly.NET.LayoutObjects;
using Plotting.Util;
using Proteomics.ProteolyticDigestion;
using ResultAnalyzerUtil;
using UsefulProteomicsDatabases;
using Chart = Plotly.NET.CSharp.Chart;

namespace Plotting.RadicalFragmentation;

public class DatabaseMassPlots
{
    public DatabaseMassPlots
        (string dbPath, int mods, string outputDir, int missedCleavages = 0, double ppmBins = 10)
    {
        DatabasePath = dbPath;
        NumberOfMods = mods;
        MissedCleavages = missedCleavages;
        PpmBins = ppmBins;
        OutputDirectory = outputDir;

        fixedMods = new();
        variableMods = new();
    }

    private string DatabasePath { get; init; }
    private string OutputDirectory { get; init; }
    private int NumberOfMods { get; init; } 
    private int MissedCleavages { get; init; } 
    double PpmBins { get; init; }

    protected List<Modification> fixedMods;
    protected List<Modification> variableMods;

    internal GenericChart.GenericChart GeneratePlots()
    {
        int roundTo = 1;
        double maxMass = 60000;
        var modifications = NumberOfMods == 0 ? new List<Modification>() : GlobalVariables.AllModsKnown;
        var topDownDigestionParams = new DigestionParams("top-down", MissedCleavages, 2,
            Int32.MaxValue, 100000, InitiatorMethionineBehavior.Retain, NumberOfMods);
        var bottomUpDigestionParams = new DigestionParams("trypsin", MissedCleavages, 2,
            Int32.MaxValue, 100000, InitiatorMethionineBehavior.Retain, NumberOfMods);

        var proteins = ProteinDbLoader.LoadProteinXML(DatabasePath, true, DecoyType.None, modifications,
            false, new List<string>(), out var um);


        List<double> proteoformMasses = proteins.SelectMany(prot => prot.Digest(topDownDigestionParams, fixedMods, variableMods)
                .Where(p => p.MonoisotopicMass <= maxMass)
                .DistinctBy(p => p.FullSequence))
            .Select(proteoform => proteoform.MonoisotopicMass.Round(roundTo))
            .ToList();
        List<double> peptidoformMasses = proteins.SelectMany(prot => prot.Digest(bottomUpDigestionParams, fixedMods, variableMods)
                .Where(p => p.MonoisotopicMass <= maxMass)
                .DistinctBy(p => p.FullSequence))
            .Select(peptidoform => peptidoform.MonoisotopicMass.Round(roundTo))
            .ToList();


        proteins.ForEach(prot => prot.OneBasedPossibleLocalizedModifications.Clear());
        List<double> proteinMasses = proteins.SelectMany(prot => prot.Digest(topDownDigestionParams, fixedMods, variableMods)
                .Where(p => p.MonoisotopicMass <= maxMass)
                .DistinctBy(p => p.BaseSequence))
            .Select(protein => protein.MonoisotopicMass.Round(roundTo))
            .ToList();

        List<double> peptideMasses = proteins.SelectMany(prot => prot.Digest(bottomUpDigestionParams, fixedMods, variableMods)
                .Where(p => p.MonoisotopicMass <= maxMass)
                .DistinctBy(p => p.BaseSequence))
            .Select(peptidoform => peptidoform.MonoisotopicMass.Round(roundTo))
            .ToList();

        var peptidePlot = GenericPlots.Histogram(peptideMasses, "Peptide", "Neutral Mass (Da)", "Count");
        var peptidoformPlot = GenericPlots.Histogram(peptidoformMasses, "Peptidoform", "Neutral Mass (Da)", "Count");
        var proteinPlot = GenericPlots.Histogram(proteinMasses, "Protein", "Neutral Mass (Da)", "Count");
        var proteoformPlot = GenericPlots.Histogram(proteoformMasses, "Proteoform", "Neutral Mass (Da)", "Count");

        var histCombinedPeptides = Chart.Combine(new[]
                { peptidoformPlot, peptidePlot })
            .WithTitle("Mass Distribution of Peptides and Peptidoforms")
            .WithYAxisStyle(Title.init("Count"))
            .WithXAxisStyle(Title.init("Neutral Mass (Da)"), 
                new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(0, 10000)))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
        var peptidePlotPath = Path.Combine(OutputDirectory, "MassHistogram_Peptides");
        histCombinedPeptides.SavePNG(peptidePlotPath, null, 1000, 600);

        var histCombinedProteins = Chart.Combine(new[]
                { proteoformPlot, proteinPlot })
            .WithTitle("Mass Distribution of Proteins and Proteoforms")
            .WithYAxisStyle(Title.init("Count"))
            .WithXAxisStyle(Title.init("Neutral Mass (Da)"),
                new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(0, maxMass)))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
        var proteinPlotPath = Path.Combine(OutputDirectory, "MassHistogram_Proteins");
        histCombinedProteins.SavePNG(proteinPlotPath, null, 1000, 600);


        peptidePlot = GenericPlots.Histogram(peptideMasses, "Peptide", "Neutral Mass (Da)", "Count");
        peptidoformPlot = GenericPlots.Histogram(peptidoformMasses, "Peptidoform", "Neutral Mass (Da)", "Count");
        proteinPlot = GenericPlots.Histogram(proteinMasses, "Protein", "Neutral Mass (Da)", "Count");
        proteoformPlot = GenericPlots.Histogram(proteoformMasses, "Proteoform", "Neutral Mass (Da)", "Count");

        histCombinedPeptides = Chart.Combine(new[]
                { peptidoformPlot, peptidePlot })
            .WithTitle("Mass Distribution of Peptides and Peptidoforms")
            .WithYAxisStyle(Title.init("Log Count"))
            .WithYAxis(LinearAxis.init<double, double, double, double, double, double>(AxisType: StyleParam.AxisType.Log))
            .WithXAxisStyle(Title.init("Neutral Mass (Da)"),
                new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(0, maxMass)))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
        var logPeptidePlotPath = Path.Combine(OutputDirectory, "MassLogHistogram_Peptides");
        histCombinedPeptides.SavePNG(logPeptidePlotPath, null, 1000, 600);

        histCombinedProteins = Chart.Combine(new[]
                { proteoformPlot, proteinPlot })
            .WithTitle("Mass Distribution of Proteins and Proteoforms")
            .WithYAxisStyle(Title.init("Log Count"))
            .WithYAxis(LinearAxis.init<double, double, double, double, double, double>(AxisType: StyleParam.AxisType.Log))
            .WithXAxisStyle(Title.init("Neutral Mass (Da)"),
                new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(0, maxMass)))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
        var logProteinPlotPath = Path.Combine(OutputDirectory, "MassLogHistogram_Proteins");
        histCombinedProteins.SavePNG(logProteinPlotPath, null, 1000, 600);


        peptidePlot = GenericPlots.Histogram(peptideMasses.Select(p => Math.Log(p, 10)).ToList(), "Peptide", "Neutral Mass (Da)", "Count");
        peptidoformPlot = GenericPlots.Histogram(peptidoformMasses.Select(p => Math.Log(p, 10)).ToList(), "Peptidoform", "Neutral Mass (Da)", "Count");
        proteinPlot = GenericPlots.Histogram(proteinMasses.Select(p => Math.Log(p, 10)).ToList(), "Protein", "Neutral Mass (Da)", "Count");
        proteoformPlot = GenericPlots.Histogram(proteoformMasses.Select(p => Math.Log(p, 10)).ToList(), "Proteoform", "Neutral Mass (Da)", "Count");

        histCombinedPeptides = Chart.Combine(new[]
                { peptidoformPlot, peptidePlot })
            .WithTitle("Mass Distribution of Peptides and Peptidoforms")
            .WithYAxisStyle(Title.init("Count"))
            .WithXAxisStyle(Title.init("Log Neutral Mass (Da)"))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
        var logNeutralPeptidePlotPath = Path.Combine(OutputDirectory, "LogMassHistogram_Peptides");
        histCombinedPeptides.SavePNG(logNeutralPeptidePlotPath, null, 1000, 600);

        histCombinedProteins = Chart.Combine(new[]
                { proteoformPlot, proteinPlot })
            .WithTitle("Mass Distribution of Proteins and Proteoforms")
            .WithYAxisStyle(Title.init("Count"))
            .WithXAxisStyle(Title.init("Log Neutral Mass (Da)"),
                new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(3, Math.Log(maxMass, 10))))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
        var logNeutralProteinPlotPath = Path.Combine(OutputDirectory, "LogMassHistogram_Proteins");
        histCombinedProteins.SavePNG(logNeutralProteinPlotPath, null, 1000, 600);


        histCombinedPeptides = Chart.Combine(new[]
                { peptidoformPlot, peptidePlot })
            .WithTitle("Mass Distribution of Peptides and Peptidoforms")
            .WithYAxisStyle(Title.init("Log Count"))
            .WithYAxis(LinearAxis.init<double, double, double, double, double, double>(AxisType: StyleParam.AxisType.Log))
            .WithXAxisStyle(Title.init("Log Neutral Mass (Da)"))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
        logNeutralPeptidePlotPath = Path.Combine(OutputDirectory, "LogMassLogHistogram_Peptides");
        histCombinedPeptides.SavePNG(logNeutralPeptidePlotPath, null, 1000, 600);

        histCombinedProteins = Chart.Combine(new[]
                { proteoformPlot, proteinPlot })
            .WithTitle("Mass Distribution of Proteins and Proteoforms")
            .WithYAxisStyle(Title.init("Log Count"))
            .WithYAxis(LinearAxis.init<double, double, double, double, double, double>(AxisType: StyleParam.AxisType.Log))
            .WithXAxisStyle(Title.init("Log Neutral Mass (Da)"),
                new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(3, Math.Log(maxMass, 10))))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
        logNeutralProteinPlotPath = Path.Combine(OutputDirectory, "LogMassLogHistogram_Proteins");
        histCombinedProteins.SavePNG(logNeutralProteinPlotPath, null, 1000, 600);

        return histCombinedProteins;
    }
}