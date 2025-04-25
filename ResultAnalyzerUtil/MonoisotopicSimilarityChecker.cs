using Chemistry;
using MzLibUtil;

public class MonoisotopicSimilarityChecker
{
    private static int MaxIsotopeOffset = 2;
    private const double IsotopeMassDiff = Constants.C13MinusC12; // 1.0033548...;

    public static List<double> IsotopeMassDifferences;

    static MonoisotopicSimilarityChecker()
    {
        var missedMonoInts = new int[] { -2, -1, 0, 1, 2 };
        IsotopeMassDifferences = missedMonoInts.Select(p => p * IsotopeMassDiff).ToList();
    }

    /// <summary>
    /// Checks if two PSMs might be the same species, differing only due to monoisotopic error.
    /// </summary>
    public static bool AreSameSpeciesByMonoisotopicError(
        double monoMass1, double observedMz1,
        double monoMass2, double observedMz2,
        int charge1, int charge2,
        PpmTolerance tolerance)
    {
        if (charge1 != charge2)
            return false;

        foreach (var iso in IsotopeMassDifferences)
        {
            if (tolerance.Within(observedMz1, observedMz2 + iso.ToMz(charge2)) ||
                tolerance.Within(monoMass1, monoMass2 + iso))
            {
                return true;
            }
        }

        return false;
    }
}