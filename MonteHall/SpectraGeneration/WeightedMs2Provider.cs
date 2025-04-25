using MassSpectrometry;

namespace MonteCarlo;

public class WeightedMs2Provider : MsDataFileSpectraProvider
{
    // peak dictionary is key: m/z , value: probability of m/z
    private readonly Dictionary<double, double> _peakDictionary;
    public WeightedMs2Provider(MsDataFile dataFile, Dictionary<double, double> peakDictionary, int maxToProvide) : base(dataFile, maxToProvide)
    {
        _peakDictionary = peakDictionary;
    }

    public override IEnumerable<MzSpectrum> GetSpectra()
    {
        int count = 0;
        foreach (var scan in DataFile)
        {
            if (scan.MsnOrder == 2)
            {
                var spectrum = scan.MassSpectrum;
                double totalIntensity = 0;
                foreach (var peak in spectrum.XArray)
                {
                    if (_peakDictionary.TryGetValue(peak, out double intensity))
                    {
                        totalIntensity += intensity;
                    }
                }
                if (totalIntensity > 0)
                {
                    yield return spectrum;
                    count++;
                }
            }
            if (count >= MaxToProvide)
                yield break;
        }
    }
}



