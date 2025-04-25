using Chemistry;
using MassSpectrometry;
using Omics;
using Omics.Fragmentation;

namespace MonteCarlo;

public class MonteCarloSimulator
{
    private readonly ISpectraProvider _spectraProvider;
    private readonly IPeptideSetProvider _peptideSetProvider;
    private readonly IPsmScorer _psmScorer;
    private readonly ISimulationResultHandler _resultHandler;

    public MonteCarloSimulator(
        ISpectraProvider spectraProvider,
        IPeptideSetProvider peptideSetProvider,
        ISimulationResultHandler resultHandler,
        IPsmScorer psmScorer)
    {
        _spectraProvider = spectraProvider;
        _peptideSetProvider = peptideSetProvider;
        _resultHandler = resultHandler;
        _psmScorer = psmScorer;
    }

    public void RunSimulation(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var spectra = _spectraProvider.GetSpectra();
            var peptides = _peptideSetProvider.GetPeptides();

            // Perform matching logic
            var result = PerformMatching(spectra, peptides);

            // Handle the result
            _resultHandler.HandleResult(result);
        }
    }

    private SimulationResult PerformMatching(IEnumerable<MzSpectrum> spectra, IEnumerable<IBioPolymerWithSetMods> peptides)
    {
        List<double> allScores = new();
        HashSet<double> fragmentMzs = new();
        List<Product> neutralFragments = new();
        foreach (var peptide in peptides)
        {
            fragmentMzs.Clear();
            neutralFragments.Clear();

            // Generate fragments for the peptide
            peptide.Fragment(DissociationType.HCD, FragmentationTerminus.Both, neutralFragments);
            foreach (var fragment in neutralFragments)
            {
                for (int z = _psmScorer.MinFragmentCharge; z < _psmScorer.MaxFragmentCharge; z++)
                {
                    fragmentMzs.Add(fragment.ToMz(z));
                }
            }

            foreach (var spectrum in spectra)
            {
                double psmScore = _psmScorer.ScorePeptideSpectralMatch(spectrum, fragmentMzs);
                allScores.Add(psmScore);
            }
        }

        // Implement matching logic here
        return new SimulationResult()
        {
            AllScores = allScores
        };
    }
}


