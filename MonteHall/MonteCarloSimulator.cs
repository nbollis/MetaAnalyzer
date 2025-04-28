using Chemistry;
using MassSpectrometry;
using Omics;
using Omics.Fragmentation;
using System.Text;

namespace MonteCarlo;

public class MonteCarloSimulator
{
    public readonly StringBuilder SummaryText;
    private readonly ISpectraProvider _spectraProvider;
    private readonly IPeptideSetProvider _peptideSetProvider;
    private readonly IPsmScorer _psmScorer;
    private readonly ISimulationResultHandler _resultHandler;

    public MonteCarloSimulator(
        ISpectraProvider spectraProvider,
        IPeptideSetProvider peptideSetProvider,
        ISimulationResultHandler resultHandler,
        IPsmScorer psmScorer, StringBuilder summaryText)
    {
        _spectraProvider = spectraProvider;
        _peptideSetProvider = peptideSetProvider;
        _resultHandler = resultHandler;
        _psmScorer = psmScorer;
        SummaryText = summaryText;
    }

    public void RunSimulation(int iterations)
    {
        int i = 0;
        int searchEvents = 0;
        for (; i < iterations; i++)
        {
            var spectra = _spectraProvider.GetSpectra();
            var peptides = _peptideSetProvider.GetPeptides();

            // Perform matching logic
            var result = PerformMatching(spectra.ToList(), peptides);
            searchEvents += spectra.Count() * peptides.Count();

            // Handle the result
            _resultHandler.HandleResult(result, i);
        }

        SummaryText.AppendLine($"========== Results ==========");
        SummaryText.AppendLine($"Simulation completed with {i}/{iterations} iterations.");
        SummaryText.AppendLine($"Spectra Remaining: {_spectraProvider.Count}");
        SummaryText.AppendLine($"Peptides Remaining: {_peptideSetProvider.Count}");
        SummaryText.AppendLine($"Total Search Events: {searchEvents}");

        _resultHandler.SummaryText = SummaryText.ToString();
    }

    private SimulationResult PerformMatching(List<MzSpectrum> spectra, IEnumerable<IBioPolymerWithSetMods> peptides)
    {
        List<double> allScores = new();
        Parallel.ForEach(peptides, peptide =>
        {
            HashSet<double> fragmentMzs = new();
            List<Product> neutralFragments = new();

            // Generate fragments for the peptide
            peptide.Fragment(DissociationType.HCD, FragmentationTerminus.Both, neutralFragments);
            foreach (var fragment in neutralFragments)
            {
                for (int z = _psmScorer.MinFragmentCharge; z <= _psmScorer.MaxFragmentCharge; z++)
                {
                    fragmentMzs.Add(fragment.ToMz(z));
                }
            }

            foreach (var spectrum in spectra.Where(s => s != null))
            {
                double psmScore = _psmScorer.ScorePeptideSpectralMatch(spectrum, fragmentMzs);
                lock (allScores)
                {
                    allScores.Add(psmScore);
                }
            }
        });

        return new SimulationResult()
        {
            AllScores = allScores
        };
    }
}


