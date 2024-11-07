using Easy.Common.Extensions;
using MathNet.Numerics;
using Microsoft.FSharp.Core;
using Omics.Modifications;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using Proteomics;
using Proteomics.ProteolyticDigestion;
using UsefulProteomicsDatabases;
using static Plotly.NET.StyleParam.Range;
using Chart = Plotly.NET.CSharp.Chart;

namespace RadicalFragmentation;

internal class DatabaseMassPlot
{

    public DatabaseMassPlot(string dbPath, int mods, int missedCleavages = 0, double ppmBins = 10)
    {
        DatabasePath = dbPath;
        NumberOfMods = mods;
        MissedCleavages = missedCleavages;
        PpmBins = ppmBins;
    }

    private string DatabasePath { get; init; }
    private int NumberOfMods { get; init; } 
    private int MissedCleavages { get; init; } 
    double PpmBins { get; init; }

    protected List<Modification> fixedMods;
    protected List<Modification> variableMods;
    protected List<ProteolysisProduct> proteolysisProducts;
    protected List<DisulfideBond> disulfideBonds;

    internal GenericChart.GenericChart GeneratePlot()
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

        var temp = proteins.SelectMany(prot => prot.Digest(topDownDigestionParams, fixedMods, variableMods)
                .Where(p => p.MonoisotopicMass <= maxMass && p.Parent.FullName.Contains("Histone"))
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
            .WithXAxisStyle(Title.init("Neutral Mass (Da)"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
        histCombinedPeptides.Show();

        var histCombinedProteins = Chart.Combine(new[]
                { proteoformPlot, proteinPlot })
            .WithTitle("Mass Distribution of Proteins and Proteoforms")
            .WithYAxisStyle(Title.init("Count"))
            .WithXAxisStyle(Title.init("Neutral Mass (Da)"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
        histCombinedProteins.Show();

        peptidePlot = GenericPlots.Histogram(peptideMasses, "Peptide", "Neutral Mass (Da)", "Count");
        peptidoformPlot = GenericPlots.Histogram(peptidoformMasses, "Peptidoform", "Neutral Mass (Da)", "Count");
        proteinPlot = GenericPlots.Histogram(proteinMasses, "Protein", "Neutral Mass (Da)", "Count");
        proteoformPlot = GenericPlots.Histogram(proteoformMasses, "Proteoform", "Neutral Mass (Da)", "Count");

        histCombinedPeptides = Chart.Combine(new[]
                { peptidoformPlot, peptidePlot })
            .WithTitle("Mass Distribution of Peptides and Peptidoforms")
            .WithYAxisStyle(Title.init("Log (Count)"))
            .WithYAxis(LinearAxis.init<double, double, double, double, double, double>(AxisType: StyleParam.AxisType.Log))
            .WithXAxisStyle(Title.init("Neutral Mass (Da)"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
        histCombinedPeptides.Show();

        histCombinedProteins = Chart.Combine(new[]
                { proteoformPlot, proteinPlot })
            .WithTitle("Mass Distribution of Proteins and Proteoforms")
            .WithYAxisStyle(Title.init("Log (Count)"))
            .WithYAxis(LinearAxis.init<double, double, double, double, double, double>(AxisType: StyleParam.AxisType.Log))
            .WithXAxisStyle(Title.init("Neutral Mass (Da)"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
        histCombinedProteins.Show();

        peptidePlot = GenericPlots.Histogram(peptideMasses.Select(p => Math.Log(p, 10)).ToList(), "Peptide", "Neutral Mass (Da)", "Count");
        peptidoformPlot = GenericPlots.Histogram(peptidoformMasses.Select(p => Math.Log(p, 10)).ToList(), "Peptidoform", "Neutral Mass (Da)", "Count");
        proteinPlot = GenericPlots.Histogram(proteinMasses.Select(p => Math.Log(p, 10)).ToList(), "Protein", "Neutral Mass (Da)", "Count");
        proteoformPlot = GenericPlots.Histogram(proteoformMasses.Select(p => Math.Log(p, 10)).ToList(), "Proteoform", "Neutral Mass (Da)", "Count");

        histCombinedPeptides = Chart.Combine(new[]
                { peptidoformPlot, peptidePlot })
            .WithTitle("Mass Distribution of Peptides and Peptidoforms")
            .WithYAxisStyle(Title.init("Count"))
            .WithXAxisStyle(Title.init("Log Neutral Mass (Da)"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
        histCombinedPeptides.Show();

        histCombinedProteins = Chart.Combine(new[]
                { proteoformPlot, proteinPlot })
            .WithTitle("Mass Distribution of Proteins and Proteoforms")
            .WithYAxisStyle(Title.init("Count"))
            .WithXAxisStyle(Title.init("Log Neutral Mass (Da)"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
        histCombinedProteins.Show();

        peptidePlot = GenericPlots.Histogram(peptideMasses, "Peptide", "Neutral Mass (Da)", "Count", true);
        peptidoformPlot = GenericPlots.Histogram(peptidoformMasses, "Peptidoform", "Neutral Mass (Da)", "Count", true);
        proteinPlot = GenericPlots.Histogram(proteinMasses, "Protein", "Neutral Mass (Da)", "Count", true);
        proteoformPlot = GenericPlots.Histogram(proteoformMasses, "Proteoform", "Neutral Mass (Da)", "Count", true);

        histCombinedPeptides = Chart.Combine(new[]
                { peptidoformPlot, peptidePlot })
            .WithTitle("Mass Distribution of Peptides and Peptidoforms")
            .WithYAxisStyle(Title.init("Percent of Category"))
            .WithXAxisStyle(Title.init("Neutral Mass (Da)"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
        histCombinedPeptides.Show();

        histCombinedProteins = Chart.Combine(new[]
                { proteoformPlot, proteinPlot })
            .WithTitle("Mass Distribution of Proteins and Proteoforms")
            .WithYAxisStyle(Title.init("Percent of Category"))
            .WithXAxisStyle(Title.init("Neutral Mass (Da)"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend)
            .WithSize(1000, 600);
        histCombinedProteins.Show();

        peptidePlot = Chart.Histogram<double, double, string>(peptideMasses, Name: "Peptide",
            MarkerColor: "Peptide".ConvertConditionToColor(),
            HistNorm: StyleParam.HistNorm.Density);
        peptidoformPlot = Chart.Histogram<double, double, string>(peptidoformMasses, Name: "Peptidoform",
            MarkerColor: "Peptidoform".ConvertConditionToColor(),
            HistNorm: StyleParam.HistNorm.Density);
        proteinPlot = Chart.Histogram<double, double, string>(proteinMasses, Name: "Protein",
            MarkerColor: "Protein".ConvertConditionToColor(),
            HistNorm: StyleParam.HistNorm.Density);
        proteoformPlot = Chart.Histogram<double, double, string>(proteoformMasses, Name: "Proteoform",
            MarkerColor: "Proteoform".ConvertConditionToColor(),
            HistNorm: StyleParam.HistNorm.Density);

        histCombinedPeptides = Chart.Combine(new[]
                { peptidoformPlot, peptidePlot })
            .WithTitle("Mass Distribution of Peptides and Peptidoforms")
            .WithYAxisStyle(Title.init("Density"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend)
            .WithXAxisStyle(Title.init("Neutral Mass (Da)"))
            .WithSize(1000, 600);
        histCombinedPeptides.Show();

        histCombinedProteins = Chart.Combine(new[]
                { proteoformPlot, proteinPlot })
            .WithTitle("Mass Distribution of Proteins and Proteoforms")
            .WithYAxisStyle(Title.init("Density"))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend)
            .WithXAxisStyle(Title.init("Neutral Mass (Da)"))
            .WithSize(1000, 600);
        histCombinedProteins.Show();





        return histCombinedProteins;
    }
}