using MassSpectrometry;

namespace MonteCarlo;

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



