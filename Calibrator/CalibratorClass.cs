using Analyzer.FileTypes.External;
using Proteomics.PSM;
using Readers;
using MsFraggerPsm = Analyzer.FileTypes.External.MsFraggerPsm;
using MsFraggerPsmFile = Analyzer.FileTypes.External.MsFraggerPsmFile;

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

    public CalibratorClass(IEnumerable<string> filesPaths)
    {
        if (filesPaths.All(p => p.EndsWith("psmtsv")))
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
        else if (filesPaths.All(p => p.EndsWith("psm.tsv")))
        {
            List<MsFraggerPsm> allPsms = new();
            foreach (var filePath in filesPaths)
                allPsms.AddRange(new MsFraggerPsmFile(filePath).Results);

            var tempFile = new MsFraggerPsmFile("dont yell at me please")
            {
                Results = allPsms
            };

            FileLoggers.Add(new FileLogger(tempFile));
        }
        else
        {
            throw new Exception("All files must be of the same type");
        }
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