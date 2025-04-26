using MassSpectrometry;

namespace MonteCarlo;

public class MsDataFileScraper
{
    /// <summary>
    /// Generates a dictionary of m/z and summed relative intensity of all peaks of that m/z across all scans. 
    /// </summary>
    /// <param name="dataFile"></param>
    /// <param name="msnOrder"></param>
    /// <returns></returns>
    public static Dictionary<double, double> Scrape(MsDataFile[] dataFiles, int msnOrder)
    {
        Dictionary<double, double> allPeakDictionary = new(128000);
        foreach (var dataFile in dataFiles)
        { 
            foreach (var scan in dataFile.GetAllScansList())
            {
                if (scan.MsnOrder != msnOrder)
                    continue;

                var mzArray = scan.MassSpectrum.XArray;
                var intensityArray = scan.MassSpectrum.YArray;
                double normalizationFactor = scan.MassSpectrum.SumOfAllY;

                for (int i = 0; i < mzArray.Length; i++)
                {
                    double normalizedIntensity = intensityArray[i] / normalizationFactor;

                    if (allPeakDictionary.TryGetValue(mzArray[i], out double runningSum))
                    {
                        allPeakDictionary[mzArray[i]] = runningSum + normalizedIntensity;
                    }
                    else
                    {
                        allPeakDictionary[mzArray[i]] = normalizedIntensity;
                    }
                }
            }
        }
        return allPeakDictionary;
    }
}


