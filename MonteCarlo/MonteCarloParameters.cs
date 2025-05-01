using Omics.Digestion;
using Omics.Modifications;
using UsefulProteomicsDatabases;

namespace MonteCarlo;

public class MonteCarloParameters
{
    // Simulation
    public string OutputDirectory { get; init; }
    public string ConditionIdentifier { get; init; }
    public int Iterations { get; set; } = 1000;
    public int Threads = 10;

    // Peptide
    public PeptideSetProviderType PeptideProviderType { get; set; }
    public string InputPeptidePath { get; init; }
    public DecoyType DecoyType { get; set; } = DecoyType.None;
    public int MaximumPeptidesPerIteration { get; set; } = 400;
    public double Tolerance { get; set; } = 10;
    public IDigestionParams? CustomDigestionParams { get; set; } = null;
    public List<Modification>? VariableMods { get; set; } = null;
    public List<Modification>? FixedMods { get; set; } = null;

    // Scoring
    public PsmScoringMethods ScoringMethod { get; set; } = PsmScoringMethods.MetaMorpheus;
    public int MinFragmentCharge { get; set; } = 1;
    public int MaxFragmentCharge { get; set; } = 1;

    // Spectra
    public SpectraProviderType SpectraProviderType { get; set; } = SpectraProviderType.AllMs2;
    public string[] InputSpectraPaths { get; init; }
    public int MaximumSpectraPerIteration { get; set; } = 50;

    public MonteCarloParameters(string outputDirectory, string inputPeptidePath, string[] inputSpectraPath, string? conditionIdentifier = null )
    {
        OutputDirectory = outputDirectory;
        InputPeptidePath = inputPeptidePath;
        InputSpectraPaths = inputSpectraPath;
        ConditionIdentifier = conditionIdentifier ?? string.Empty;
    }
}


