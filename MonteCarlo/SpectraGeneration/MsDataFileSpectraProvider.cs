using MassSpectrometry;

namespace MonteCarlo;

public abstract class MsDataFileSpectraProvider : ISpectraProvider
{
    protected readonly MsDataFile[] DataFiles;
    public int SpectraPerIteration { get; set; } 

    public MsDataFileSpectraProvider(MsDataFile[] dataFiles, int spectraPerIteration)
    {
        DataFiles = dataFiles;
        SpectraPerIteration = spectraPerIteration;
    }

    public abstract int Count { get; }
    public abstract IEnumerable<MsDataScan> GetSpectra();
}



