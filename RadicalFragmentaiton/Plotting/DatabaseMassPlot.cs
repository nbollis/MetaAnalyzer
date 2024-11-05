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
        var modifications = NumberOfMods == 0 ? new List<Modification>() : GlobalVariables.AllModsKnown;
        var topDownDigestionParams = new DigestionParams("top-down", MissedCleavages, 2,
            Int32.MaxValue, 100000, InitiatorMethionineBehavior.Retain, NumberOfMods);
        var bottomUpDigestionParams = new DigestionParams("trypsin", MissedCleavages, 2,
            Int32.MaxValue, 100000, InitiatorMethionineBehavior.Retain, NumberOfMods);

        var proteins = ProteinDbLoader.LoadProteinXML(DatabasePath, true, DecoyType.None, modifications,
            false, new List<string>(), out var um);


        List<double> proteoformMasses = proteins.SelectMany(prot => prot.Digest(topDownDigestionParams, fixedMods, variableMods)
                .DistinctBy(p => p.FullSequence))
            .Select(proteoform => proteoform.MonoisotopicMass)
            .ToList();
        List<double> peptidoformMasses = proteins.SelectMany(prot => prot.Digest(bottomUpDigestionParams, fixedMods, variableMods)
                .DistinctBy(p => p.FullSequence))
            .Select(peptidoform => peptidoform.MonoisotopicMass)
            .ToList();


        proteins.ForEach(prot => prot.OneBasedPossibleLocalizedModifications.Clear());
        List<double> proteinMasses = proteins.SelectMany(prot => prot.Digest(topDownDigestionParams, fixedMods, variableMods)
                .DistinctBy(p => p.BaseSequence))
            .Select(protein => protein.MonoisotopicMass)
            .ToList();

        List<double> peptideMasses = proteins.SelectMany(prot => prot.Digest(bottomUpDigestionParams, fixedMods, variableMods)
                .DistinctBy(p => p.BaseSequence))
            .Select(peptidoform => peptidoform.MonoisotopicMass)
            .ToList();

        var peptidePlot = GetSingleChart(peptideMasses, "Peptide");
        var peptidoformPlot = GetSingleChart(peptidoformMasses, "Peptidoform");
        var proteinPlot = GetSingleChart(proteinMasses, "Protein");
        var proteoformPlot = GetSingleChart(proteoformMasses, "Proteoform");

        var combined = Chart.Combine(new[] { peptidePlot, peptidoformPlot, proteinPlot, proteoformPlot })
            .WithTitle("Mass Distribution of Peptides, Peptidoforms, Proteins, and Proteoforms")
            .WithYAxisStyle(Title.init("Count"), Side: StyleParam.Side.Left,
                Id: StyleParam.SubPlotId.NewYAxis(1))
            .WithYAxisStyle(Title.init("Density"), Side: StyleParam.Side.Right,
                Id: StyleParam.SubPlotId.NewYAxis(2),
                Overlaying: StyleParam.LinearAxisId.NewY(1))
            .WithLayout(GenericPlots.DefaultLayoutWithLegend);
        return combined;
    }


    private GenericChart.GenericChart GetSingleChart(List<double> masses, string label)
    {
        var kde = GenericPlots.KernelDensityPlot(masses, label, "Neutral Mass (Da)", "Count")
            .WithAxisAnchor(Y: 1);
        var hist = GenericPlots.Histogram(masses, label, "Neutral Mass (Da)", "Density")
            .WithAxisAnchor(Y: 2);
        var combined = Chart.Combine(new[] { kde, hist }).WithYAxisStyle(Title.init("Count"), Side: StyleParam.Side.Left,
                    Id: StyleParam.SubPlotId.NewYAxis(1))
                .WithYAxisStyle(Title.init("Density"), Side: StyleParam.Side.Right,
                    Id: StyleParam.SubPlotId.NewYAxis(2),
                    Overlaying: StyleParam.LinearAxisId.NewY(1))
                .WithLayout(GenericPlots.DefaultLayoutNoLegend)
            .WithTitle($"{label} Mass Distribution");
        combined.Show();
        return combined;
    }
}