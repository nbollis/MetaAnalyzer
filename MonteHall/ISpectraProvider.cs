using MassSpectrometry;

namespace MonteCarlo;

public interface ISpectraProvider
{
    int MaxToProvide { get; }
    IEnumerable<MzSpectrum> GetSpectra();
}

public abstract class MsDataFileSpectraProvider : ISpectraProvider
{
    protected readonly MsDataFile DataFile;
    public int MaxToProvide { get; }

    public MsDataFileSpectraProvider(MsDataFile dataFile, int maxToProvide)
    {
        DataFile = dataFile;
        MaxToProvide = maxToProvide;
    }

    public abstract IEnumerable<MzSpectrum> GetSpectra();
}

public class AllMs2Provider : MsDataFileSpectraProvider
{
    public AllMs2Provider(MsDataFile dataFile, int maxToProvide) : base(dataFile, maxToProvide)
    {
    }

    public override IEnumerable<MzSpectrum> GetSpectra()
    {
        int count = 0;
        foreach (var scan in DataFile)
        {
            if (scan.MsnOrder == 2)
            {
                yield return scan.MassSpectrum;
                count++;
            }

            if (count >= MaxToProvide)
                yield break;
        }
    }
}

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



