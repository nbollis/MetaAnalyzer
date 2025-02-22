using System.Collections;
using System.Collections.Generic;

namespace GradientDevelopment;

public class ExperimentalBatch : IEnumerable<RunInformation>
{
    private readonly bool _overrideParsedResults;
    private readonly string _extractedInfoPath;
    private readonly string _cytosineMethylPath;
    private ExtractedInformationFile? _extractedInformationFile;
    private CytosineInformationFile? _cytosineInformationFile;

    public string Identifier { get; init; }
    public string ProcessedResultsDirectory { get; init; }
    public List<RunInformation> AllRuns { get; init; }
    public ExtractedInformationFile ExtractedInformationFile => _extractedInformationFile ??= GetExtractedInformation();

    public CytosineInformationFile CytosineInformationFile => _cytosineInformationFile ??= GetCytosineInformation();

    public ExperimentalBatch(string identifier, string processedResultsDirectory, List<RunInformation> allRuns, bool overrideParsedResults = false)
    {
        Identifier = identifier;
        ProcessedResultsDirectory = processedResultsDirectory;
        AllRuns = allRuns;

        _extractedInfoPath = Path.Combine(processedResultsDirectory, "ExtractedGradientData.tsv");
        _cytosineMethylPath = Path.Combine(processedResultsDirectory, "CytosineMethylData.csv");
        _overrideParsedResults = overrideParsedResults;
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

    public IEnumerator<RunInformation> GetEnumerator()
    {
        return AllRuns.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}