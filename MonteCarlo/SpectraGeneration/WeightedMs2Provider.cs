using MassSpectrometry;

namespace MonteCarlo;

//public class WeightedMs2Provider : MsDataFileSpectraProvider
//{
//    // peak dictionary is key: m/z , value: probability of m/z
//    private readonly Dictionary<double, double> _peakDictionary;
//    private readonly int PeaksPerSpectrum = 75;
//    private readonly Random Rand = new();
//    public override int Count => int.MaxValue;

//    public WeightedMs2Provider(MsDataFile[] dataFile, int spectraPerIteration) : base(dataFile, spectraPerIteration)
//    {
//        _peakDictionary = MsDataFileScraper.Scrape(dataFile, 2);
//    }

//    // use the peak dictionary to generate a spectrum.
//    // We will use the value to determine probaility of selection of said peak and
//    // the key to set the mz of the peak. All intesnities will be set to 1. 
//    public override IEnumerable<MzSpectrum> GetSpectra()
//    {
//        for (int i = 0; i < SpectraPerIteration; i++)
//        {
//            var selectedPeaks = new List<(double mz, double intensity)>();
//            var totalProbability = _peakDictionary.Values.Sum();

//            // Select peaks based on their probability
//            for (int j = 0; j < PeaksPerSpectrum; j++)
//            {
//                double randomValue = Rand.NextDouble() * totalProbability;
//                double cumulativeProbability = 0;

//                foreach (var peak in _peakDictionary)
//                {
//                    cumulativeProbability += peak.Value;
//                    if (randomValue <= cumulativeProbability)
//                    {
//                        selectedPeaks.Add((peak.Key, 1.0)); // Intensity is set to 1.0
//                        break;
//                    }
//                }
//            }

//            // Create and yield the spectrum
//            var mzArray = selectedPeaks.Select(p => p.mz).ToArray();
//            var intensityArray = selectedPeaks.Select(p => p.intensity).ToArray();
//            yield return new MzSpectrum(mzArray, intensityArray, true);
//        }
//    }
//}



