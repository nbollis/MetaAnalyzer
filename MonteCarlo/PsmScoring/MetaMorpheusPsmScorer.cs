using MassSpectrometry;
using MzLibUtil;

namespace MonteCarlo;

public class MetaMorpheusPsmScorer : IPsmScorer
{
    public readonly Tolerance FragmentMatchingTolerance;
    public int MinFragmentCharge { get; }
    public int MaxFragmentCharge { get; }

    public MetaMorpheusPsmScorer(int minFragmentCharge, int maxFragmentCharge, Tolerance fragmentMatchingTolerance)
    {
        MinFragmentCharge = minFragmentCharge;
        MaxFragmentCharge = maxFragmentCharge;
        FragmentMatchingTolerance = fragmentMatchingTolerance;
    }

    public double ScorePeptideSpectralMatch(MsDataScan scan, HashSet<double> fragmentMzs)
    {
        double score = 0;
        MzSpectrum spectra = scan.MassSpectrum;

        for (int peakIndex = 0; peakIndex < spectra.XArray.Length; peakIndex++)
        {
            if (spectra.YArray[peakIndex] == 0)
                continue;

            double mz = spectra.XArray[peakIndex];
            foreach (var fragmentMz in fragmentMzs)
            {
                if (FragmentMatchingTolerance.Within(mz, fragmentMz))
                {
                    score += 1 + spectra.YArray[peakIndex] / spectra.SumOfAllY;
                    break;
                }
            }
        }
        return score;
    }
}

