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
    private readonly int _threads;
    private readonly int _minScoreToRecord;

    public MonteCarloSimulator(
        ISpectraProvider spectraProvider,
        IPeptideSetProvider peptideSetProvider,
        ISimulationResultHandler resultHandler,
        IPsmScorer psmScorer, StringBuilder summaryText, int threads, int minScoreToRecord = 8)
    {
        _spectraProvider = spectraProvider;
        _peptideSetProvider = peptideSetProvider;
        _resultHandler = resultHandler;
        _psmScorer = psmScorer;
        SummaryText = summaryText;
        _threads = threads;
        _minScoreToRecord = minScoreToRecord;
    }

    public void RunSimulation(int iterations)
    {
        int i = 0;
        int searchEvents = 0;
        for (; i < iterations; i++)
        {
            var spectra = _spectraProvider.GetSpectra().ToList();
            var peptides = _peptideSetProvider.GetPeptides().ToList();

            // Perform matching logic
            var result = PerformMatching(spectra, peptides);
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

    private SimulationResult PerformMatching(List<MsDataScan> spectra, List<IBioPolymerWithSetMods> peptides)
    {
        List<double> allScores = new();
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _threads // Limit the number of threads
        }; 
        int rangeSize = (int)Math.Ceiling((double)spectra.Count / _threads); // Divide spectra into ranges


        Parallel.For(0, _threads, parallelOptions, threadIndex =>
        {
            int start = threadIndex * rangeSize;
            int end = Math.Min(start + rangeSize, spectra.Count);

            List<double> localScores = new(); // Thread-local storage for scores
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
                    for (int z = _psmScorer.MinFragmentCharge; z <= _psmScorer.MaxFragmentCharge; z++)
                    {
                        fragmentMzs.Add(fragment.ToMz(z));
                    }
                }

                // Sort fragmentMzs once here
                var sortedFragmentMzs = fragmentMzs.OrderBy(mz => mz).ToArray();

                for (int i = start; i < end; i++)
                {
                    var spectrum = spectra[i];
                    if (spectrum == null) continue;

                    // Score the peptide-spectral match
                    double psmScore = _psmScorer.ScorePeptideSpectralMatch(spectrum, sortedFragmentMzs);
                    localScores.Add(psmScore);

                    if (psmScore >= _minScoreToRecord)
                        _resultHandler.HandleBestScoreRecord(new()
                        {
                            BaseSequence = peptide.BaseSequence,
                            Score = psmScore,
                            FullSequence = peptide.FullSequence,
                            FileNameWithoutExtension = spectrum.NativeId,
                            OneBasedScanNumber = spectrum.OneBasedScanNumber,
                            Condition = _resultHandler.ConditionIdentifier,
                            Accession = peptide.Parent?.Accession ?? "",
                            Name = peptide.Parent?.Name ?? ""
                        });
                }
            }

            // Add local scores to the shared list
            lock (allScores)
            {
                allScores.AddRange(localScores);
            }
        });

        return new SimulationResult()
        {
            AllScores = allScores
        };
    }
}


