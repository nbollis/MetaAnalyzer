using MassSpectrometry;
using MzLibUtil;

namespace MonteCarlo;

public interface IPsmScorer
{
    int MinFragmentCharge { get; }
    int MaxFragmentCharge { get; }

    double ScorePeptideSpectralMatch(MsDataScan spectra, HashSet<double> fragmentMzs);
}

public enum PsmScoringMethods
{
    MetaMorpheus,
}

public static class PsmScoringMethodFactory
{
    public static IPsmScorer GetPsmScorer(PsmScoringMethods method, int minCharge, int maxCharge, double tolerance)
    {
        var tol = new PpmTolerance(tolerance);
        return method switch
        {
            PsmScoringMethods.MetaMorpheus => new MetaMorpheusPsmScorer(minCharge, maxCharge, tol),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };
    }
}
