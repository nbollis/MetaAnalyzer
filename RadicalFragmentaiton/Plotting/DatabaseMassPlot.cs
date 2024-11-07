using MathNet.Numerics;
using Omics.Modifications;
using Plotly.NET;
using Proteomics;
using Proteomics.ProteolyticDigestion;
using UsefulProteomicsDatabases;

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

        List<double> peptideMasses = proteins.SelectMany(prot => prot.Digest(bottomUpDigestionParams, fixedMods, variableMods)
                .Where(p => p.MonoisotopicMass <= maxMass)
                .DistinctBy(p => p.BaseSequence))
            .Select(peptidoform => peptidoform.MonoisotopicMass.Round(roundTo))
            .ToList();

        var allValues = peptideMasses
            .Concat(peptidoformMasses)
            .Concat(proteinMasses)
            .Concat(proteoformMasses)
            .ToList();

        var peptidePlot = GetSingleCharts(peptideMasses, "Peptide");
        var peptidoformPlot = GetSingleCharts(peptidoformMasses, "Peptidoform");
        var proteinPlot = GetSingleCharts(proteinMasses, "Protein");
        var proteoformPlot = GetSingleCharts(proteoformMasses, "Proteoform");

        var kernelCombined = Chart.Combine(new[]
            { peptidePlot.KDE, peptidoformPlot.KDE, proteinPlot.KDE, proteoformPlot.KDE })
            .WithAxisAnchor(Y: 4)
            .WithYAxisStyle(Title.init("Density"), Side: StyleParam.Side.Right, Id: StyleParam.SubPlotId.NewYAxis(4));

        var histCombined = Chart.Combine(new[]
            { peptidePlot.Hist, peptidoformPlot.Hist, proteinPlot.Hist, proteoformPlot.Hist })
            .WithAxisAnchor(Y: 5)
            .WithYAxisStyle(Title.init("Count"), Side: StyleParam.Side.Left, Id: StyleParam.SubPlotId.NewYAxis(5));

        var combined = Chart.Combine(new[] { kernelCombined, histCombined })
            .WithTitle("Mass Distribution of Peptides, Peptidoforms, Proteins, and Proteoforms")
            .WithYAxisStyle(Title.init("Count"), Side: StyleParam.Side.Left,
                Id: StyleParam.SubPlotId.NewYAxis(6))
            .WithYAxisStyle(Title.init("Density"), Side: StyleParam.Side.Right,
                Id: StyleParam.SubPlotId.NewYAxis(7),
                Overlaying: StyleParam.LinearAxisId.NewY(6))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend)
            .WithSize(1000, 600);

        kernelCombined.Show();
        histCombined.Show();
        combined.Show();

        return combined;
    }

    private (GenericChart.GenericChart KDE, GenericChart.GenericChart Hist) GetSingleCharts(List<double> masses, string label)
    {
        var kde = GenericPlots.KernelDensityPlot(masses, label, "Neutral Mass (Da)", "Count")
            .WithAxisAnchor(Y: 1)
            .WithYAxisStyle(Title.init("Density"), Side: StyleParam.Side.Right,
                Id: StyleParam.SubPlotId.NewYAxis(1));

        var hist = GenericPlots.Histogram(masses, label, "Neutral Mass (Da)", "Density")
            .WithAxisAnchor(Y: 2)
            .WithYAxisStyle(Title.init("Count"), Side: StyleParam.Side.Left,
            Id: StyleParam.SubPlotId.NewYAxis(2));

        return (null, hist);
    }
}