using System.Collections.Generic;
using System.Linq;
using GradientDevelopment.Temporary;

namespace GradientDevelopment;
/// <summary>
/// Generates CytosineInformation files from arbitrary OSM lists, grouped by DataFileName.
/// </summary>
public class CytosineBatchProcessor
{
    private readonly IEnumerable<OsmFromTsv> _osms;
    private readonly IEnumerable<string> _dataFileNames;
    private readonly string _outputDirectory;
    private readonly string _cytosineMethylPath;
    private readonly string _cytosineMethylByFdrPath;

    public CytosineBatchProcessor(IEnumerable<OsmFromTsv> osms, IEnumerable<string> dataFileNames, string outputDirectory)
    {
        _osms = osms;
        _dataFileNames = dataFileNames;
        _outputDirectory = outputDirectory;
        _cytosineMethylPath = Path.Combine(outputDirectory, "CytosineMethylData.csv");
        _cytosineMethylByFdrPath = Path.Combine(outputDirectory, "CytosineMethylDataByFdr.csv");

        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);
    }

    public void WriteCytosineInformationFiles(bool overwrite = false, string condition = "")
    {
        WriteCytosineInformationFile(_cytosineMethylPath, overwrite, condition);
        WriteCytosineInformationByFdrFile(_cytosineMethylByFdrPath, overwrite, condition);
    }

    private void WriteCytosineInformationFile(string path, bool overwrite, string condition = "")
    {
        if (File.Exists(path) && !overwrite)
            return;

        var results = _dataFileNames
            .Select(name => ExtractMethylationInformation(_osms, name, 0.05, condition))
            .ToList();

        var file = new CytosineInformationFile(path) { Results = results };
        file.WriteResults(path);
    }

    private void WriteCytosineInformationByFdrFile(string path, bool overwrite, string condition = "")
    {
        if (File.Exists(path) && !overwrite)
            return;

        double[] fdrValuesToCollect = [0.01, 0.02, 0.03, 0.04, 0.05, 0.06, 0.07, 0.08, 0.09, 0.1, 0.11, 0.12, 0.13, 0.14, 0.15, 0.2, 0.25, 0.30, 0.35, 0.4, 0.45, 0.5];

        var results = _dataFileNames
            .SelectMany(name => fdrValuesToCollect.Select(fdr => ExtractMethylationInformation(_osms, name, fdr, condition)))
            .ToList();

        var file = new CytosineInformationFile(path) { Results = results };
        file.WriteResults(path);
    }

    public static CytosineInformation ExtractMethylationInformation(
        IEnumerable<OsmFromTsv> osms,
        string dataFileName,
        double fdrCutoff = 0.05, string condition = "")
    {
        var filtered = osms.Where(p => p.FileNameWithoutExtension == dataFileName && p.QValue <= fdrCutoff).ToList();
        var targets = filtered.Where(RunInformation.IsMajorityTarget).ToList();
        var decoys = filtered.Where(p => !RunInformation.IsMajorityTarget(p)).ToList();

        var targetCounts = RunInformation.CountCytosines(targets.Select(p => p.FullSequence));
        var decoyCounts = RunInformation.CountCytosines(decoys.Select(p => p.FullSequence));

        double percentMethylatedTargets = RunInformation.CalculatePercentage(targetCounts.methylated, targetCounts.total);
        double percentMethylatedDecoys = RunInformation.CalculatePercentage(decoyCounts.methylated, decoyCounts.total);
        double percentMethylatedTargetsGreaterThanOne = RunInformation.CalculatePercentage(targetCounts.methylatedGreaterThanOne, targetCounts.totalGreaterThanOne);
        double percentMethylatedDecoysGreaterThanOne = RunInformation.CalculatePercentage(decoyCounts.methylatedGreaterThanOne, decoyCounts.totalGreaterThanOne);

        return new CytosineInformation(dataFileName, fdrCutoff, targetCounts.total, decoyCounts.total,
            targetCounts.methylated, decoyCounts.methylated,
            targetCounts.unmethylated, decoyCounts.unmethylated, percentMethylatedTargets,
            percentMethylatedDecoys, percentMethylatedTargetsGreaterThanOne, percentMethylatedDecoysGreaterThanOne, condition);
    }
}
