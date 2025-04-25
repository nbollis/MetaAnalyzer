using MassSpectrometry;
using Readers;

namespace MonteCarlo;

public interface ISpectraProvider
{
    int SpectraPerIteration { get; }
    IEnumerable<MzSpectrum> GetSpectra();
}

public enum SpectraProviderType
{
    AllMs2,
    ProbabilisticMs2
}

public static class SpectraProviderFactory
{
    public static ISpectraProvider CreateSpectraProvider(SpectraProviderType type, int spectraPerIteration, string dataFilePath)
    {
        var msDataFile = MsDataFileReader.GetDataFile(dataFilePath);
        return type switch
        {
            SpectraProviderType.AllMs2 => new AllMs2Provider(msDataFile, spectraPerIteration),
            SpectraProviderType.ProbabilisticMs2 => new WeightedMs2Provider(msDataFile, spectraPerIteration),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}


