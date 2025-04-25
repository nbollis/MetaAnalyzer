namespace MonteCarlo;

public class MonteCarloRunner(MonteCarloParameters parameters)
{
    public MonteCarloParameters Parameters { get; } = parameters;
    public SimulationResultHandler Run()
    {
        var spectraProvider = SpectraProviderFactory.CreateSpectraProvider(Parameters.SpectraProviderType, Parameters.SpectraPerIteration, Parameters.InputSpectraPath);
        var peptideSetProvider = PeptideSetFactory.GetPeptideSetProvider(Parameters.PeptideProviderType, Parameters.InputPeptidePath, Parameters.PeptidesPerIteration, Parameters.DecoyType, Parameters.CustomDigestionParams, Parameters.VariableMods, Parameters.FixedMods);
        var psmScorer = PsmScoringMethodFactory.GetPsmScorer(PsmScoringMethods.MetaMorpheus, Parameters.MinFragmentCharge, Parameters.MaxFragmentCharge, Parameters.Tolerance);
        var resultHandler = new SimulationResultHandler(Parameters.OutputDirectory);

        var simulator = new MonteCarloSimulator(spectraProvider, peptideSetProvider, resultHandler, psmScorer);
        try
        {
            simulator.RunSimulation(Parameters.Iterations);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during simulation: {ex.Message}");
        }
        return resultHandler;
    }
}


