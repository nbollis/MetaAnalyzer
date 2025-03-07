using System.Diagnostics;
using MassSpectrometry;
using Plotting.Util;

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
        if (isTopDown)
        {
            CellLines.Add(new CellLineFileManager("Jurkat", dataDirectoryPath, true));
        }
        else
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
            if (calibratedAveragedFilePaths != null) return calibratedAveragedFilePaths;
            calibratedAveragedFilePaths = new Dictionary<string, string>();
            foreach (var kvp in Replicates
                         .SelectMany(replicate => replicate.CalibratedAveragedFilePaths))
            {
                calibratedAveragedFilePaths.Add(kvp.Key, kvp.Value);
            }
            return calibratedAveragedFilePaths;
        }
    }

    private Dictionary<string, string>? calibratedAveragedSetPrecursorFilePaths;
    public Dictionary<string, string> CalibratedAveragedSetPrecursorFilePaths
    {
        get
        {
            if (calibratedAveragedSetPrecursorFilePaths != null) return calibratedAveragedSetPrecursorFilePaths;
            calibratedAveragedSetPrecursorFilePaths = new Dictionary<string, string>();
            foreach (var kvp in Replicates
                         .SelectMany(replicate => replicate.CalibratedAveragedFilePaths))
            {
                calibratedAveragedSetPrecursorFilePaths.Add(kvp.Key, kvp.Value);
            }
            return calibratedAveragedSetPrecursorFilePaths;
        }
    }

    // When they were calibrated and averaged all together. 
    //public CellLineFileManager(string cellLine, string dataDirectoryPath, bool useSetPrecursor = false)
    //{
    //    CellLine = cellLine;
    //    DataDirectoryPath = dataDirectoryPath;

    //    Replicates = new();
    //    var calibratedAveragedDir = Path.Combine(dataDirectoryPath, $"{InternalMetaMorpheusAnalysisTask.Version}_CalibratedAveraged");
    //    var calibAveragedSetPrecursorDir = Path.Combine(dataDirectoryPath, "CalibratedAveragedSetPrecursor");

    //    // top-down jurkat
    //    if (useSetPrecursor)
    //    {
    //        var calibratedFiles = Directory.GetFiles(calibratedAveragedDir, "*.mzML");
    //        var calibratedSetPrecursorFiles = Directory.GetFiles(calibAveragedSetPrecursorDir, "*.mzML");

    //        List<(string original, string setPrecursor)> files = new();
    //        foreach (var item in calibratedFiles.Zip(calibratedSetPrecursorFiles, (calibrated, setPrecursor) => (calibrated, setPrecursor)))
    //        {
    //            files.Add((item.Item1, item.Item2));
    //        }

    //        foreach (var replicateFileGroup in files.GroupBy(p =>
    //                     int.Parse(Path.GetFileNameWithoutExtension(p.Item1).ConvertFileName().Split('_')[0])))
    //        {
    //            if (replicateFileGroup.Count() != 10)
    //                Debugger.Break();

    //            Replicates.Add(new ReplicateFileManager(replicateFileGroup.Key,
    //                replicateFileGroup.Select(p => p.original).ToArray(),
    //                replicateFileGroup.Select(p => p.setPrecursor).ToArray()));
    //        }
    //    }
    //    else
    //    {
    //        foreach (var calibAvgRepGroup in Directory.GetFiles(calibratedAveragedDir, "*.mzML")
    //                     .GroupBy(p => int.Parse(Path.GetFileNameWithoutExtension(p).ConvertFileName().Split('_')[1])))
    //        {
    //            if (calibAvgRepGroup.Count() != 6)
    //                Debugger.Break();

    //            Replicates.Add(new ReplicateFileManager(calibAvgRepGroup.Key, calibAvgRepGroup.ToArray()));
    //        }
    //    }
    //}

    // when they were calibrated and averaged in their replicates. 
    public CellLineFileManager(string cellLine, string dataDirectoryPath, bool useSetPrecursor = false)
    {
        CellLine = cellLine;
        DataDirectoryPath = dataDirectoryPath;

        Replicates = new();
        var calibratedAveragedDirs = Directory.GetDirectories(dataDirectoryPath, "*_CalibratedAveraged*");
        var toUse = calibratedAveragedDirs.Where(p => p.Contains($"{InternalMetaMorpheusAnalysisTask.Version}")).ToList();
        var calibAveragedSetPrecursorDir = Path.Combine(dataDirectoryPath, "CalibratedAveragedSetPrecursor");

        // top-down jurkat
        if (useSetPrecursor)
        {
            //if (toUse.Count != 3)
            //    Debugger.Break();

            var calibratedSetPrecursorFiles = Directory.GetFiles(calibAveragedSetPrecursorDir, "*.mzML");

            foreach (var calibAverageReplicateDir in toUse)
            {
                int replicate = int.Parse(calibAverageReplicateDir.Split('_').Last().Replace("Rep",""));
                var msDataFiles = Directory.GetFiles(calibAverageReplicateDir, "*.mzML", SearchOption.AllDirectories);

                if (msDataFiles.Length != 10)
                    Debugger.Break();

                List<string> setPrecursorFiles = new(10);
                foreach (var file in calibratedSetPrecursorFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file).Replace("_SetPrecursor", "").ConvertFileName();
 
                    if (fileName.StartsWith(replicate.ToString()))
                        setPrecursorFiles.Add(file);
                }
                if (setPrecursorFiles.Count != 10)
                    Debugger.Break();

                Replicates.Add(new ReplicateFileManager(replicate, msDataFiles, setPrecursorFiles.ToArray()));
            }
        }
        else
        {
            if (toUse.Count != 3)
                Debugger.Break();

            foreach (var calibAverageReplicateDir in toUse)
            {
                int replicate = int.Parse(calibAverageReplicateDir.Split('_').Last());
                var files = Directory.GetFiles(calibAverageReplicateDir, "*.mzML", SearchOption.AllDirectories);
                if (files.Length != 6)
                    Debugger.Break();
                Replicates.Add(new ReplicateFileManager(replicate, files));
            }
        }
    }
}

internal class ReplicateFileManager
{
    public int Replicate { get; init; }
    public Dictionary<string, string> CalibratedAveragedFilePaths { get; init; }
    public Dictionary<string, string> CalibratedAveragedSetPrecursorFilePaths { get; init; }


    public ReplicateFileManager(int replicate, string[] calibAveragedFilePaths,
        string[]? calibAveragedSetPrecursorFilePaths = null)
    {
        Replicate = replicate;
        CalibratedAveragedFilePaths = new();
        foreach (var file in calibAveragedFilePaths)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            CalibratedAveragedFilePaths.Add(fileName.ConvertFileName(), file);
        }

        CalibratedAveragedSetPrecursorFilePaths = new();
        if (calibAveragedSetPrecursorFilePaths == null) return;
        foreach (var file in calibAveragedSetPrecursorFilePaths)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            CalibratedAveragedSetPrecursorFilePaths.Add(fileName.ConvertFileName(), file);
        }
    }
}