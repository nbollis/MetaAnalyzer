using System.Collections;
using System.Collections.Generic;

namespace GradientDevelopment;


/// <summary>
/// Class representing a batch of experimental data to be parsed and compared as one. 
/// </summary>
public class ExperimentalBatch : IEnumerable<RunInformation>
{
    private readonly bool _overrideParsedResults;
    private readonly string _extractedInfoPath;
    private readonly string _cytosineMethylPath;
    private readonly string _cytosineMethylByFdrPath;
    private ExtractedInformationFile? _extractedInformationFile;
    private CytosineInformationFile? _cytosineInformationFile;
    private CytosineInformationFile? _cytosineInformationByFdrFile;

    public string Identifier { get; init; }
    public string ProcessedResultsDirectory { get; init; }
    public List<RunInformation> AllRuns { get; init; }
    public ExtractedInformationFile ExtractedInformationFile => _extractedInformationFile ??= GetExtractedInformation();
    public CytosineInformationFile CytosineInformationFile => _cytosineInformationFile ??= GetCytosineInformation();
    public CytosineInformationFile CytosineInformationByFdrFile => _cytosineInformationByFdrFile ??= GetCysteineInformationFileByFdr();

    public ExperimentalBatch(string identifier, string processedResultsDirectory, List<RunInformation> allRuns, bool overrideParsedResults = false)
    {
        _extractedInfoPath = Path.Combine(processedResultsDirectory, "ExtractedGradientData.tsv");
        _cytosineMethylPath = Path.Combine(processedResultsDirectory, "CytosineMethylData.csv");
        _cytosineMethylByFdrPath = Path.Combine(processedResultsDirectory, "CytosineMethylDataByFdr.csv");
        _overrideParsedResults = overrideParsedResults;

        Identifier = identifier;
        AllRuns = allRuns;
        ProcessedResultsDirectory = processedResultsDirectory;

        if (!Directory.Exists(processedResultsDirectory))
            Directory.CreateDirectory(processedResultsDirectory);
    }

    private ExtractedInformationFile GetExtractedInformation()
    {
        bool wasUpdated = false;
        ExtractedInformationFile file = null!;
        if (!File.Exists(_extractedInfoPath) || _overrideParsedResults) // create from nothing or ovverwrite
        {
            wasUpdated = true;
            file = new ExtractedInformationFile(_extractedInfoPath)
            {
                Results = AllRuns.Select(p => p.GetExtractedRunInformation()).ToList()
            };
        }
        else 
        {
            // load in file
            file = new ExtractedInformationFile(_extractedInfoPath);

            // check if any runs are not in file
            List<ExtractedInformation> toAppend = [];
            foreach (var run in AllRuns)
            {
                if (file.Any(p => p.DataFileName == run.DataFileName))
                    continue;

                var extractedInfo = run.GetExtractedRunInformation();
                toAppend.Add(extractedInfo);
            }


            // add runs not infile
            if (toAppend.Any())
            {
                wasUpdated = true;
                file.Results.AddRange(toAppend);
            }
        }

        // overwrite if updated
        if (wasUpdated)
            file.WriteResults(_extractedInfoPath);

        return file;
    }

    private CytosineInformationFile GetCytosineInformation()
    {
        bool wasUpdated = false;
        CytosineInformationFile file = null!;
        if (!File.Exists(_cytosineMethylPath) || _overrideParsedResults)// create from nothing or overwrite
        {
            wasUpdated = true;
            file = new CytosineInformationFile(_cytosineMethylPath)
            {
                Results = AllRuns.Select(p => p.ExtractMethylationInformation()).ToList()
            };
        }
        else
        {
            // load in file
            file = new CytosineInformationFile(_cytosineMethylPath);

            // check if any runs are not in file
            List<CytosineInformation> toAppend = [];
            foreach (var run in AllRuns)
            {
                if (file.Any(p => p.DataFileName == run.DataFileName))
                    continue;

                var methylInfo = run.ExtractMethylationInformation();
                toAppend.Add(methylInfo);
            }

            // add runs not infile
            if (toAppend.Any())
            {
                wasUpdated = true;
                file.Results.AddRange(toAppend);
            }
        }

        // overwrite if updated
        if (wasUpdated)
            file.WriteResults(_cytosineMethylPath);

        return file;
    }

    private CytosineInformationFile GetCysteineInformationFileByFdr()
    {
        bool wasUpdated = false;
        CytosineInformationFile file = null!;
        double[] fdrValuesToCollect = [0.01, 0.02, 0.03, 0.04, 0.05, 0.06, 0.07, 0.08, 0.09, 0.1, 0.11, 0.12, 0.13, 0.14, 0.15, 0.2, 0.25, 0.30, 0.35, 0.4, 0.45, 0.5];

        if (!File.Exists(_cytosineMethylByFdrPath) || _overrideParsedResults)// create from nothing or overwrite
        {
            wasUpdated = true;
            file = new CytosineInformationFile(_cytosineMethylByFdrPath)
            {
                Results = AllRuns.SelectMany(run => fdrValuesToCollect.Select(fdr => run.ExtractMethylationInformation(fdr))).ToList()
            };
        }
        else
        {
            // load in file
            file = new CytosineInformationFile(_cytosineMethylByFdrPath);

            // check if any runs are not in file
            List<CytosineInformation> toAppend = [];
            foreach (var run in AllRuns)
            {
                foreach (var fdrValue in fdrValuesToCollect)
                {
                    if (file.Any(p => p.DataFileName == run.DataFileName && Math.Abs(p.FdrCutoff - fdrValue) < 0.001))
                        continue;

                    var methylInfo = run.ExtractMethylationInformation(fdrValue);
                    toAppend.Add(methylInfo);
                }
            }

            // add runs not infile
            if (toAppend.Any())
            {
                wasUpdated = true;
                file.Results.AddRange(toAppend);
            }
        }

        // overwrite if updated
        if (wasUpdated)
            file.WriteResults(_cytosineMethylByFdrPath);

        return file;
    }

    public IEnumerator<RunInformation> GetEnumerator()
    {
        return AllRuns.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}