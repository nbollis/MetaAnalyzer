namespace TaskLayer.JenkinsLikePEPTesting;

public class JenkinsLikeRunParserTaskParameters : BaseResultAnalyzerTaskParameters
{
    public bool RunChimeraBreakdown { get; set; }
    public JenkinsLikeRunParserTaskParameters(string inputDirectoryPath, bool overrideFiles, bool runChimeraBreakdown = false) : base(inputDirectoryPath, overrideFiles, true)
    {
        RunChimeraBreakdown = runChimeraBreakdown;
    }
}