using Proteomics.PSM;
using Readers;
using ThermoFisher.CommonCore.Data;

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
        List<PsmFromTsv> allPsms = new();
        foreach (var filePath in filesPaths)
            allPsms.AddRange(new PsmFromTsvFile(filePath).Results);

        var tempFile = new PsmFromTsvFile("dont yell at me please")
        {
            Results = allPsms
        };

        FileLoggers.Add(new FileLogger(tempFile));
    }

    public void Calibrate()
    {
        foreach (var fileLogger in FileLoggers)
        {
            fileLogger.Calibrate();
        }
    }

    public void WriteFile(string outPath)
    {
        FileLoggers.First().WriteOutput(outPath);
    }

   
}