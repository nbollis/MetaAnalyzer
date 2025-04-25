using MassSpectrometry;

namespace MonteCarlo;

public interface IPsmScorer
{
    int MinFragmentCharge { get; }
    int MaxFragmentCharge { get; }

    double ScorePeptideSpectralMatch(MzSpectrum spectra, HashSet<double> fragmentMzs);
}

