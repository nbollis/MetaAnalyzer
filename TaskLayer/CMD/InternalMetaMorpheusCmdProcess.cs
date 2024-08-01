using System.Diagnostics;
using System.Text;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using Calibrator;
using Easy.Common.Extensions;
using TaskLayer.ChimeraAnalysis;

namespace TaskLayer.CMD;


public class ResultAnalyzerTaskToCmdProcessAdaptor : CmdProcess
{
    private bool _hasStarted;
    private bool _isComplete;

    public override string Prompt { get; }
    public BaseResultAnalyzerTask Task { get; }
    public ResultAnalyzerTaskToCmdProcessAdaptor(BaseResultAnalyzerTask task, string summaryText, double weight, string workingDir, string? quickName = null, string programExe = "CMD.exe") 
        : base(summaryText, weight, workingDir, quickName, programExe)
    {
        _hasStarted = false;

        Prompt = "";
        Task = task;
    }

    public async Task RunTask()
    {
        _hasStarted = true;
        if (!IsCompleted())
        {
            await Task.Run();
            _isComplete = true;
        }
    }

    public override bool HasStarted() => _hasStarted;

    public override bool IsCompleted()
    {
        if (_isComplete) 
            return _isComplete;

        try
        {
            string? pathToCheck = Task switch
            {
                SingleRunSpectralAngleComparisonTask s => Directory.GetFiles(s.Parameters.InputDirectoryPath,
                    $"*{FileIdentifiers.SpectralAngleFigure}_{s.Parameters.PlotType.ToString()}.png",
                    SearchOption.AllDirectories).FirstOrDefault(),

                SingleRunChimeraRetentionTimeDistribution c => Directory.GetFiles(c.Parameters.InputDirectoryPath,
                    $"*{FileIdentifiers.RetentionTimeFigure}_{c.Parameters.PlotType.ToString()}.png",
                    SearchOption.AllDirectories).FirstOrDefault(),

                SingleRunChimericSpectrumSummaryTask css => Directory.GetFiles(css.Parameters.InputDirectoryPath,
                    $"*{FileIdentifiers.ChimericSpectrumSummary}",
                    SearchOption.AllDirectories).FirstOrDefault(),

                SingleRunRetentionTimeCalibrationTask rtc => Directory.GetFiles(rtc.Parameters.InputDirectoryPath,
                    $"*{FileIdentifiers.CalibratedRetentionTimeFile}",
                    SearchOption.AllDirectories).FirstOrDefault(),
                _ => null
            };
            if (pathToCheck is null)
                _isComplete = false;
            if (File.Exists(pathToCheck))
            {
                _isComplete = true;
            }
        }
        catch (Exception)
        {
            _isComplete = false;
        }
     
        return _isComplete;
       
    }
}




public class InternalMetaMorpheusCmdProcess : CmdProcess
{
    public override string Prompt
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append($" -t {GptmdTask} {SearchTask}");
            sb.Append($" -s {string.Join(" ", SpectraPaths)}");
            sb.Append($" -o {OutputDirectory}");
            sb.Append($" -d {DatabasePath}");
            if (Dependency != null)
            {
                sb.Append($" {Dependency.Task.Result}");
            }

            var promptstring = sb.ToString();
            var start = OutputDirectory.Substring(0, 3);
            promptstring = promptstring.Replace(@"B:\", start);

            return promptstring;
        }
    }
    public string OutputDirectory { get; }
    public string[] SpectraPaths { get; }
    public string DatabasePath { get; }
    public string GptmdTask { get; }
    public string SearchTask { get; }

    /// <summary>
    /// For use in the creation of the internal MM comparison searches
    /// </summary>
    /// <param name="spectraPaths"></param>
    /// <param name="dbPath"></param>
    /// <param name="gptmd"></param>
    /// <param name="search"></param>
    /// <param name="outputPath"></param>
    /// <param name="summaryText"></param>
    /// <param name="weight"></param>
    /// <param name="workingDir"></param>
    /// <param name="quickName"></param>
    /// <param name="programExe"></param>
    public InternalMetaMorpheusCmdProcess(string[] spectraPaths, string dbPath, string gptmd, string search, string outputPath,
        string summaryText, double weight, string workingDir, string? quickName = null, string programExe = "CMD.exe")
        : base(summaryText, weight, workingDir, quickName, programExe)
    {
        SpectraPaths = spectraPaths;
        DatabasePath = dbPath;
        GptmdTask = gptmd;
        SearchTask = search;
        OutputDirectory = outputPath;
    }

    public override bool IsCompleted()
    {
        var spectraFiles = SpectraPaths.Length;
        if (!HasStarted()) return false;

        if (Directory.GetFiles(OutputDirectory, "*.psmtsv", SearchOption.AllDirectories).Length <
            spectraFiles + 3) return false;

        // build library tasks also need to wait on the msp being written out
        if (!SearchTask.Contains("Build")) return true;
        var filePath = Directory.GetFiles(OutputDirectory, "*.msp", SearchOption.AllDirectories).FirstOrDefault();
        return filePath != null;
    }

    public override bool HasStarted()
    {
        return Directory.Exists(OutputDirectory);
    }
}