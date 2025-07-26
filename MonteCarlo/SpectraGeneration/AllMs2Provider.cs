using MassSpectrometry;

namespace MonteCarlo;

public class AllMs2Provider : MsDataFileSpectraProvider
{
    public override int Count => SpectrumList.Count;
    protected CircularLinkedList<MsDataScan> SpectrumList;
    public AllMs2Provider(MsDataFile[] dataFile, int spectraPerIteration) : base(dataFile, spectraPerIteration)
    {
        // scramble the order of the spectra randomly
        var random = new Random();
        var randomOrderSpectra = DataFiles.SelectMany(p => p.GetAllScansList())
            .Where(p => p.MsnOrder == 2)
            .OrderBy(_ => random.Next());

        SpectrumList = new();
        foreach (var spectrum in randomOrderSpectra)
        {
            SpectrumList.Add(spectrum);
        }
    }

    public override IEnumerable<MsDataScan> GetSpectra()
    {
        int count = SpectraPerIteration;
        while (count > 0)
        {
            yield return SpectrumList.GetNext();
            count--;
        }
    }
}



