using System.Text;
using Omics.Modifications;

namespace MonteCarlo;

public class MonteCarloRunner(MonteCarloParameters parameters)
{
    public MonteCarloParameters Parameters { get; } = parameters;
    public SimulationResultHandler Run()
    {
        var resultHandler = new SimulationResultHandler(Parameters.OutputDirectory, Parameters.ConditionIdentifier);
        if (resultHandler.SimulationComplete())
        {
            Console.WriteLine($"Simulation for {Parameters.ConditionIdentifier} is already complete. Skipping.");
            return resultHandler;
        }

        var spectraProvider = SpectraProviderFactory.CreateSpectraProvider(Parameters.SpectraProviderType, Parameters.MaximumSpectraPerIteration, Parameters.InputSpectraPaths);
        var peptideSetProvider = PeptideSetFactory.GetPeptideSetProvider(Parameters.PeptideProviderType, Parameters.InputPeptidePath, Parameters.MaximumPeptidesPerIteration, Parameters.DecoyType, Parameters.CustomDigestionParams, Parameters.VariableMods, Parameters.FixedMods);
        var psmScorer = PsmScoringMethodFactory.GetPsmScorer(PsmScoringMethods.MetaMorpheus, Parameters.MinFragmentCharge, Parameters.MaxFragmentCharge, Parameters.Tolerance);
        

        // Start some summary text for logging TODO: Make this a toml or something more reusable. 
        StringBuilder summary = new();
        summary.AppendLine("Monte Carlo Simulation Summary");
        summary.AppendLine($"========== Parameters ==========");
        summary.AppendLine($"Iterations: {Parameters.Iterations}");
        summary.AppendLine($"Condition Identifier: {Parameters.ConditionIdentifier}");
        summary.AppendLine($"Output Directory: {Parameters.OutputDirectory}");
        summary.AppendLine();

        summary.AppendLine($"========== Spectra Parameters ==========");
        summary.AppendLine($"Spectra Provider Type: {Parameters.SpectraProviderType}");
        summary.AppendLine($"Spectra Available: {spectraProvider.Count}");
        summary.AppendLine($"Spectra Per Iteration: {spectraProvider.SpectraPerIteration}");
        summary.AppendLine($"Spectra Files Used: {string.Join(", ", Parameters.InputSpectraPaths)}");
        summary.AppendLine();

        summary.AppendLine($"========== Peptide Parameters ==========");
        summary.AppendLine($"Peptide Provider Type: {Parameters.PeptideProviderType}");
        summary.AppendLine($"Decoy Type: {Parameters.DecoyType}");
        summary.AppendLine($"Peptides Available: {peptideSetProvider.Count}");
        summary.AppendLine($"Peptides Per Iteration: {peptideSetProvider.PeptidesPerIteration}");
        summary.AppendLine($"Peptide Files Used: {Parameters.InputPeptidePath}"); 
        summary.AppendLine($"Variable Mods: {string.Join(", ", Parameters.VariableMods?.Select(mod => mod.IdWithMotif) ?? new List<string>())}");
        summary.AppendLine($"Fixed Mods: {string.Join(", ", Parameters.FixedMods?.Select(mod => mod.IdWithMotif) ?? new List<string>())}");
        summary.AppendLine();


        summary.AppendLine($"========== Spectral Matching Parameters ==========");
        summary.AppendLine($"Scoring Method: {psmScorer.GetType().Name}");
        summary.AppendLine($"Tolerance: {Parameters.Tolerance}");
        summary.AppendLine($"Min Fragment Charge: {Parameters.MinFragmentCharge}");
        summary.AppendLine($"Max Fragment Charge: {Parameters.MaxFragmentCharge}");
        summary.AppendLine();

        var simulator = new MonteCarloSimulator(spectraProvider, peptideSetProvider, resultHandler, psmScorer, summary, Parameters.Threads);
        try
        {
            simulator.RunSimulation(Parameters.Iterations);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during simulation: {ex.Message}");
        }

        resultHandler.DoPostProcessing();
        resultHandler.WriteAllResults();
        return resultHandler;
    }
}


