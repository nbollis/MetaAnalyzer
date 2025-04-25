using MassSpectrometry;

namespace MonteCarlo;

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



