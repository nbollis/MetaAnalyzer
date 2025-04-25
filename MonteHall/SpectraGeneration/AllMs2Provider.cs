using MassSpectrometry;

namespace MonteCarlo;

public class AllMs2Provider : MsDataFileSpectraProvider
{
    protected Queue<MzSpectrum> spectrumQueue = new Queue<MzSpectrum>();
    public AllMs2Provider(MsDataFile dataFile, int spectraPerIteration) : base(dataFile, spectraPerIteration)
    {
        // scramble the order of the spectra randomly
        var random = new Random();
        var randomOrderSpectra = DataFile.Where(p => p.MsnOrder == 2)
            .Select(p => p.MassSpectrum)
            .OrderBy(_ => random.Next());

        foreach (var spectrum in randomOrderSpectra)
        {
            spectrumQueue.Enqueue(spectrum);
        }
    }

    public override IEnumerable<MzSpectrum> GetSpectra()
    {
        int count = SpectraPerIteration;
        while (spectrumQueue.Count > 0 && count > 0)
        {
            yield return spectrumQueue.Dequeue();
            count--;
        }
    }
}



