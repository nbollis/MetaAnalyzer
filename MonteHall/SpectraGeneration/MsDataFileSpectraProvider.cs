using MassSpectrometry;

namespace MonteCarlo;

public abstract class MsDataFileSpectraProvider : ISpectraProvider
{
    protected readonly MsDataFile DataFile;
    public int SpectraPerIteration { get; } 

    public MsDataFileSpectraProvider(MsDataFile dataFile, int spectraPerIteration)
    {
        DataFile = dataFile.LoadAllStaticData();
        SpectraPerIteration = spectraPerIteration;
    }

    public abstract IEnumerable<MzSpectrum> GetSpectra();
}



