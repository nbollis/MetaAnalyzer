using MassSpectrometry;

namespace MonteCarlo;

public interface ISpectraProvider
{
    int MaxToProvide { get; }
    IEnumerable<MzSpectrum> GetSpectra();
}



