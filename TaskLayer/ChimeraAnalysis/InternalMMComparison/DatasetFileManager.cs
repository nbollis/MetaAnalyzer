using Analyzer.Plotting.Util;
using System.Diagnostics;

namespace TaskLayer.ChimeraAnalysis;

internal class DatasetFileManager
{
    public string Dataset { get; init; }
    public bool IsTopDown { get; init; }
    public string DirectoryPath { get; init; }
    public string DataDirectoryPath { get; init; }

    public List<CellLineFileManager> CellLines { get; init; }
    private Dictionary<string, string>? calibratedAveragedFilePaths;

    public Dictionary<string, string> CalibratedAveragedFilePaths
    {
        get
        {
            if (calibratedAveragedFilePaths == null)
            {
                calibratedAveragedFilePaths = new Dictionary<string, string>();
                foreach (var replicate in CellLines)
                {
                    foreach (var kvp in replicate.CalibratedAveragedFilePaths)
                    {
                        calibratedAveragedFilePaths.Add(kvp.Key, kvp.Value);
                    }
                }
            }
            return calibratedAveragedFilePaths;
        }
    }

    public DatasetFileManager(string dataset, bool isTopDown, string directoryPath, string dataDirectoryPath)
    {
        Dataset = dataset;
        IsTopDown = isTopDown;
        DirectoryPath = directoryPath;
        DataDirectoryPath = dataDirectoryPath;


        CellLines = new();
        foreach (var cellLineDataDir in Directory.GetDirectories(dataDirectoryPath))
        {
            var cellLineName = Path.GetFileNameWithoutExtension(cellLineDataDir);
            CellLines.Add(new CellLineFileManager(cellLineName, cellLineDataDir));
        }
    }
}

internal class CellLineFileManager
{
    public string DataDirectoryPath { get; init; }
    public string CellLine { get; init; }
    public List<ReplicateFileManager> Replicates { get; init; }

    private Dictionary<string, string>? calibratedAveragedFilePaths;
    public Dictionary<string, string> CalibratedAveragedFilePaths
    {
        get
        {
            if (calibratedAveragedFilePaths == null)
            {
                calibratedAveragedFilePaths = new Dictionary<string, string>();
                foreach (var replicate in Replicates)
                {
                    foreach (var kvp in replicate.CalibratedAveragedFilePaths)
                    {
                        calibratedAveragedFilePaths.Add(kvp.Key, kvp.Value);
                    }
                }
            }
            return calibratedAveragedFilePaths;
        }
    }

    public CellLineFileManager(string cellLine, string dataDirectoryPath)
    {
        CellLine = cellLine;
        DataDirectoryPath = dataDirectoryPath;

        Replicates = new();
        var calibratedAveragedDir = Path.Combine(dataDirectoryPath, "CalibratedAveraged");
        foreach (var calibAvgRepGroup in Directory.GetFiles(calibratedAveragedDir, "*.mzML")
                     .GroupBy(p => int.Parse(Path.GetFileNameWithoutExtension(p).ConvertFileName().Split('_')[1])))
        {
            if (calibAvgRepGroup.Count() != 6)
                Debugger.Break();
            Replicates.Add(new ReplicateFileManager(calibAvgRepGroup.Key, calibAvgRepGroup.ToArray()));
        }
    }
}

internal class ReplicateFileManager
{
    public int Replicate { get; init; }
    public Dictionary<string, string> CalibratedAveragedFilePaths { get; init; }

    public ReplicateFileManager(int replicate, string[] calibAveragedFilePaths)
    {
        Replicate = replicate;
        CalibratedAveragedFilePaths = new();
        foreach (var file in calibAveragedFilePaths)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            CalibratedAveragedFilePaths.Add(fileName.ConvertFileName(), file);
        }
    }
}