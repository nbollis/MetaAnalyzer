using Proteomics.PSM;
using Readers;

namespace Calibrator;

public class CalibratorClass
{
    public Dictionary<string, double> LibraryRetentionTimes = new Dictionary<string, double>();
    public List<FileLogger> FileLoggers = new List<FileLogger>();

    public CalibratorClass(string filePath)
    {
        // read the psmtsv psmFile
        var psmtsv = new PsmFromTsvFile(filePath);
        psmtsv.LoadResults();

        // make the calibration logger
        FileLoggers.Add(new FileLogger(psmtsv));
    }

    public CalibratorClass(List<string> filesPaths)
    {
        foreach (var filePath in filesPaths)
        {
            // read the psmtsv psmFile
            var psmtsv = new PsmFromTsvFile(filePath);
            psmtsv.LoadResults();

            // make the calibration logger
            FileLoggers.Add(new FileLogger(psmtsv));
        }
    }

    // TODO: constructo from psm objects
    public CalibratorClass(List<PsmFromTsv> allPsms)
    {
        foreach (var psmFromTsvs in allPsms.GroupBy(p => p.FileNameWithoutExtension))
        {
            
        }
    }

    public void Calibrate()
    {
        foreach (var fileLogger in FileLoggers)
        {
            fileLogger.Calibrate();
        }
    }

   
}