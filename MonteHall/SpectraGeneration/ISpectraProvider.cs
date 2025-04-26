using MassSpectrometry;
using Readers;

namespace MonteCarlo;

public interface ISpectraProvider
{
    int Count { get; }
    int SpectraPerIteration { get; set; }
    IEnumerable<MzSpectrum> GetSpectra();
    void ConfigureSpectraPerIteration(int totalIterations, int maxSpectraPerIteration)
    {
        // Maximize spectra per iteration while ensuring we don't exceed the total iterations
        SpectraPerIteration = Math.Min(Math.Max(1, Count / totalIterations), maxSpectraPerIteration);
    }
}

public enum SpectraProviderType
{
    AllMs2,
    ProbabilisticMs2
}

public static class SpectraProviderFactory
{
    public static ISpectraProvider CreateSpectraProvider(SpectraProviderType type, int spectraPerIteration, string[] dataFilePaths)
    {
        var msDataFiles = dataFilePaths.Select(MsDataFileReader.GetDataFile).ToArray();
        return type switch
        {
            SpectraProviderType.AllMs2 => new AllMs2Provider(msDataFiles, spectraPerIteration),
            SpectraProviderType.ProbabilisticMs2 => new WeightedMs2Provider(msDataFiles, spectraPerIteration),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}


