using Calibrator;
using ResultAnalyzerUtil;
using ResultAnalyzerUtil.CommandLine;
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
        IsCmdTask = false;

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