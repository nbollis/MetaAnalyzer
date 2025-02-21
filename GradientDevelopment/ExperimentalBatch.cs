using System.Collections.Generic;

namespace GradientDevelopment;

public class ExperimentalBatch
{
    private string _extractedInfoPath;
    private string _cytosineMethylPath;
    private ExtractedInformationFile? _extractedInformationFile;
    private CytosineInformationFile? _cytosineInformationFile;

    public string Identifier { get; init; }
    public string DirectoryPath { get; init; }
    public List<RunInformation> AllRuns { get; init; }
    public ExtractedInformationFile ExtractedInformationFile => _extractedInformationFile ??= GetExtractedInformation();

    public ExperimentalBatch(string identifier, string directoryPath, List<RunInformation> allRuns)
    {
        Identifier = identifier;
        DirectoryPath = directoryPath;
        AllRuns = allRuns;

        _extractedInfoPath = Path.Combine(directoryPath, "ExtractedGradientData.tsv");
    }

    private ExtractedInformationFile GetExtractedInformation()
    {
        bool wasUpdated = false;
        ExtractedInformationFile file = null!;
        if (File.Exists(_extractedInfoPath))
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
        else // create from nothing
        {
            wasUpdated = true;
            file = new ExtractedInformationFile(_extractedInfoPath)
            {
                Results = AllRuns.Select(p => p.GetExtractedRunInformation()).ToList()
            };
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
        if (File.Exists(_extractedInfoPath))
        {
            // load in file
            file = new CytosineInformationFile(_extractedInfoPath);

            // check if any runs are not in file
            List<CytosineInformation> toAppend = [];
            foreach (var run in AllRuns)
            {
                if (file.Any(p => p.DataFileName == run.DataFileName))
                    continue;

                var extractedInfo = run.ExtractMethylationInformation();
                toAppend.Add(extractedInfo);
            }


            // add runs not infile
            if (toAppend.Any())
            {
                wasUpdated = true;
                file.Results.AddRange(toAppend);
            }
        }
        else // create from nothing
        {
            wasUpdated = true;
            file = new CytosineInformationFile(_extractedInfoPath)
            {
                Results = AllRuns.Select(p => p.ExtractMethylationInformation()).ToList()
            };
        }

        // overwrite if updated
        if (wasUpdated)
            file.WriteResults(_extractedInfoPath);

        return file;
    }
}