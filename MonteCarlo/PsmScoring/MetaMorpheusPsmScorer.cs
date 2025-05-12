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

    public double ScorePeptideSpectralMatch(MsDataScan scan, double[] sortedFragmentsMzs)
    {
        double score = 0;
        MzSpectrum spectra = scan.MassSpectrum;

        // Precompute the normalized intensities to avoid repeated division
        double[] normalizedIntensities = new double[spectra.YArray.Length];
        double sumOfAllY = spectra.SumOfAllY;
        for (int i = 0; i < spectra.YArray.Length; i++)
        {
            normalizedIntensities[i] = spectra.YArray[i] / sumOfAllY;
        }

        // Iterate over sortedFragmentsMzs first since it is smaller
        int fragmentIndex = 0;
        // Iterate through the spectrum peaks and match to fragment m/z values
        for (int peakIndex = 0; peakIndex < spectra.XArray.Length && fragmentIndex < sortedFragmentsMzs.Length; peakIndex++)
        {
            if (spectra.YArray[peakIndex] == 0)
                continue;

            double mz = spectra.XArray[peakIndex];
            double fragmentMz = sortedFragmentsMzs[fragmentIndex];
            double minimumFragmentMzToMatchWithSpectralPeak = FragmentMatchingTolerance.GetMinimumValue(mz);

            // Advance fragmentIndex if the current peak is below the fragment m/z
            while (fragmentIndex < sortedFragmentsMzs.Length && minimumFragmentMzToMatchWithSpectralPeak > fragmentMz)
            {
                fragmentIndex++;
                if (fragmentIndex < sortedFragmentsMzs.Length)
                    fragmentMz = sortedFragmentsMzs[fragmentIndex];
            }

            // Check for a match
            if (fragmentIndex < sortedFragmentsMzs.Length && FragmentMatchingTolerance.Within(mz, fragmentMz))
            {
                score += 1 + normalizedIntensities[peakIndex];
                fragmentIndex++; // Move to the next fragment m/z after a match
            }
        }

        return score;
    }
}

