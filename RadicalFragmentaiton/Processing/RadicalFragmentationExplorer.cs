﻿using MzLibUtil;
using Omics.Modifications;
using Proteomics;
using Proteomics.ProteolyticDigestion;
using RadicalFragmentation.Util;
using ResultAnalyzerUtil;
using ResultAnalyzerUtil.CommandLine;
using System.Collections.Concurrent;
using UsefulProteomicsDatabases;

namespace RadicalFragmentation.Processing;

public abstract class RadicalFragmentationExplorer
{
    public bool Override { get; set; } = false;
    protected string BaseDirectorPath { get; init; }

    public abstract string AnalysisType { get; }

    private string? _analysisLabel;
    public string AnalysisLabel
    {
        get
        {
            if (_analysisLabel is not null) return _analysisLabel;
            if (PrecursorMassTolerance is MissedMonoisotopicTolerance tol)
            {
                _analysisLabel = $"{AnalysisType}_{tol.MissedMonoisotpics}MissedMonos";
            }
            else
            {
                _analysisLabel = $"{AnalysisType}";
            }
            return _analysisLabel;
        }
    }
    public string DirectoryPath => Path.Combine(BaseDirectorPath, AnalysisLabel);
    public string FigureDirectory => Path.Combine(BaseDirectorPath, "Figure");
    protected string IndexDirectoryPath => Path.Combine(BaseDirectorPath, "IndexedFragments", AnalysisLabel);
    public int AmbiguityLevel { get; set; }
    public string Species { get; set; }
    public int NumberOfMods { get; set; }
    public string DatabasePath { get; set; }
    public int MissedMonoIsotopics { get; set; }


    protected int MaximumFragmentationEvents { get; set; }
    protected string MaxFragmentString => MaximumFragmentationEvents == int.MaxValue ? "All" : MaximumFragmentationEvents.ToString();
    protected Tolerance PrecursorMassTolerance { get; set; }
    protected Tolerance FragmentMassTolerance { get; set; }

    protected List<Modification> fixedMods;
    protected List<Modification> variableMods;
    protected List<ProteolysisProduct> proteolysisProducts;
    protected List<DisulfideBond> disulfideBonds;

    protected RadicalFragmentationExplorer(string databasePath, int numberOfMods, string species, int maximumFragmentationEvents = int.MaxValue,
        int ambiguityLevel = 1, string? baseDirectory = null, int allowedMissedMonos = 0)
    {
        DatabasePath = databasePath;
        NumberOfMods = numberOfMods;
        Species = species;
        MaximumFragmentationEvents = maximumFragmentationEvents;
        AmbiguityLevel = ambiguityLevel;
        BaseDirectorPath = baseDirectory ?? @"D:\Projects\RadicalFragmentation\FragmentAnalysis";
        MissedMonoIsotopics = allowedMissedMonos;
        PrecursorMassTolerance = allowedMissedMonos == 0 
            ? new PpmTolerance(StaticVariables.DefaultPpmTolerance) 
            : new MissedMonoisotopicTolerance(StaticVariables.DefaultPpmTolerance, allowedMissedMonos);
        FragmentMassTolerance = new PpmTolerance(StaticVariables.DefaultPpmTolerance);

        fixedMods = new List<Modification>();
        variableMods = new List<Modification>();
        proteolysisProducts = new List<ProteolysisProduct>();
        disulfideBonds = new List<DisulfideBond>();
    }

    #region Result Files

    protected string _precursorFragmentMassFilePath => Path.Combine(IndexDirectoryPath,
        $"{Species}_{NumberOfMods}Mods_{MaxFragmentString}_Level({AmbiguityLevel})Ambiguity_{FileIdentifiers.FragmentIndex}");
    protected PrecursorFragmentMassFile _precursorFragmentMassFile;
    public PrecursorFragmentMassFile PrecursorFragmentMassFile
    {
        get
        {
            if (_precursorFragmentMassFile != null) return _precursorFragmentMassFile;
            if (File.Exists(_precursorFragmentMassFilePath))
            {
                _precursorFragmentMassFile = new PrecursorFragmentMassFile(_precursorFragmentMassFilePath);
            }
            else
            {
                _precursorFragmentMassFile = CreateIndexedFile();
            }

            return _precursorFragmentMassFile;
        }
    }

    protected string _fragmentHistogramFilePath => Path.Combine(DirectoryPath,
        $"{Species}_{NumberOfMods}Mods_{MaxFragmentString}_Level({AmbiguityLevel})Ambiguity_{FileIdentifiers.FragmentCountHistogram}");
    protected FragmentHistogramFile _fragmentHistogramFile;
    public FragmentHistogramFile FragmentHistogramFile
    {
        get
        {
            if (_fragmentHistogramFile != null) return _fragmentHistogramFile;
            if (File.Exists(_fragmentHistogramFilePath))
            {
                _fragmentHistogramFile = new FragmentHistogramFile(_fragmentHistogramFilePath);
            }
            else
            {
                CreateFragmentHistogramFile();
                _fragmentHistogramFile = new FragmentHistogramFile(_fragmentHistogramFilePath);
            }

            return _fragmentHistogramFile;
        }
    }

    protected string _minFragmentNeededFilePath => Path.Combine(DirectoryPath,
        $"{Species}_{NumberOfMods}Mods_{MaxFragmentString}_Level({AmbiguityLevel})Ambiguity_{FileIdentifiers.MinFragmentNeeded}");
    protected FragmentsToDistinguishFile _minFragmentNeededFile;
    public FragmentsToDistinguishFile MinFragmentNeededFile
    {
        get
        {
            if (_minFragmentNeededFile != null) return _minFragmentNeededFile;
            if (File.Exists(_minFragmentNeededFilePath))
            {
                _minFragmentNeededFile = new FragmentsToDistinguishFile(_minFragmentNeededFilePath);
            }
            else
            {
                FindNumberOfFragmentsNeededToDifferentiate();
                _minFragmentNeededFile = new FragmentsToDistinguishFile(_minFragmentNeededFilePath);
            }

            return _minFragmentNeededFile;
        }
    }

    #endregion

    #region

    public static EventHandler<StringEventArgs> LogHandler = null!;
    public static EventHandler<StringEventArgs> WarnHandler = null!;
    public static EventHandler<SingleFileEventArgs> FileWrittenHandler = null!;
    public static EventHandler<SubProcessEventArgs> StartingSubProcessHandler = null!;
    public static EventHandler<SubProcessEventArgs> FinishedSubProcessHandler = null!;

    protected void Log(string message)
    {
        LogHandler?.Invoke(this, new StringEventArgs(message));
    }

    protected void Warn(string message)
    {
        WarnHandler?.Invoke(this, new StringEventArgs(message));
    }

    protected void StartingSubProcess(string label)
    {
        StartingSubProcessHandler?.Invoke(this, new SubProcessEventArgs(label));
    }

    protected void FinishedSubProcess(string label)
    {
        FinishedSubProcessHandler?.Invoke(this, new SubProcessEventArgs(label));
    }

    protected void FinishedWritingFile(string writtenFile)
    {
        FileWrittenHandler?.Invoke(this, new SingleFileEventArgs(writtenFile));
    }

    #endregion

    #region Methods
    protected DigestionParams PrecursorDigestionParams => new DigestionParams("top-down", 0, 2, int.MaxValue, 100000,
        InitiatorMethionineBehavior.Retain, NumberOfMods);

    public PrecursorFragmentMassFile CreateIndexedFile()
    {
        if (!Override && File.Exists(_precursorFragmentMassFilePath))
        {
            Log($"File Found: loading in index file for {AnalysisLabel}");
            return PrecursorFragmentMassFile;
        }

        Log($"Creating Index File for {AnalysisLabel}");
        if (!Directory.Exists(IndexDirectoryPath))
            Directory.CreateDirectory(IndexDirectoryPath);

        CustomComparer<PrecursorFragmentMassSet> comparer = AmbiguityLevel switch
        {
            1 => CustomComparerExtensions.LevelOneComparer,
            2 => CustomComparerExtensions.LevelTwoComparer,
            _ => throw new Exception("Ambiguity level not supported")
        };

        var sets = new List<PrecursorFragmentMassSet>();
        var level1Path = Path.Combine(IndexDirectoryPath,
            $"{Species}_{NumberOfMods}Mods_{MaxFragmentString}_Level(1)Ambiguity_{FileIdentifiers.FragmentIndex}");
        if (AmbiguityLevel == 2 && File.Exists(level1Path))
        {
            sets = new PrecursorFragmentMassFile(level1Path).Results.DistinctBy(p => p, comparer)
                    .ToList();
        }
        else
        {
            var modifications = NumberOfMods == 0 ? new List<Modification>() : GlobalVariables.AllModsKnown;
            var proteins = ProteinDbLoader.LoadProteinXML(DatabasePath, true, DecoyType.None, modifications, false, new List<string>(), out var um);

            Parallel.ForEach(proteins, new ParallelOptions() { MaxDegreeOfParallelism = StaticVariables.MaxThreads }, protein =>
            {
                var generatedSets = GeneratePrecursorFragmentMasses(protein);
                lock (sets)
                {
                    sets.AddRange(generatedSets);
                }
            });
        }


        var uniqueSets = sets.DistinctBy(p => p, comparer).ToList();
        var file = new PrecursorFragmentMassFile()
        {
            FilePath = _precursorFragmentMassFilePath,
            Results = uniqueSets
        };
        file.WriteResults(_precursorFragmentMassFilePath);

        FinishedWritingFile(_precursorFragmentMassFilePath);
        return _precursorFragmentMassFile = file;
    }
    public abstract IEnumerable<PrecursorFragmentMassSet> GeneratePrecursorFragmentMasses(Protein protein);

    public FragmentHistogramFile CreateFragmentHistogramFile()
    {
        if (!Override && File.Exists(_fragmentHistogramFilePath))
        {
            Log($"File Found: loading in fragment histogram file for {AnalysisLabel}");
            return FragmentHistogramFile;
        }

        Log($"Creating fragment histogram file for {AnalysisLabel}");
        if (!Directory.Exists(DirectoryPath))
            Directory.CreateDirectory(DirectoryPath);

        var fragmentCounts = PrecursorFragmentMassFile.Results
            .GroupBy(p => p.FragmentCount)
            .OrderBy(p => p.Key)
            .Select(p => new FragmentHistogramRecord
            {
                Species = Species,
                NumberOfMods = NumberOfMods,
                MaxFragments = MaximumFragmentationEvents,
                AnalysisType = AnalysisLabel,
                AmbiguityLevel = AmbiguityLevel,
                FragmentCount = p.Key,
                ProteinCount = p.Count()
            }).ToList();
        var file = new FragmentHistogramFile(_fragmentHistogramFilePath) { Results = fragmentCounts };
        file.WriteResults(_fragmentHistogramFilePath);

        FinishedWritingFile(_minFragmentNeededFilePath);
        return _fragmentHistogramFile = file;
    }


    public FragmentsToDistinguishFile FindNumberOfFragmentsNeededToDifferentiate()
    {
        if (!Override && File.Exists(_minFragmentNeededFilePath))
        {
            Log($"File Found: loading in fragments needed file for {AnalysisLabel}");
            return MinFragmentNeededFile;
        }

        Log($"Creating fragments needed file for {AnalysisLabel}");
        var dataSplits = 10;

        string[] tempFilePaths = new string[dataSplits];
        for (int i = 0; i < dataSplits; i++)
            tempFilePaths[i] = _minFragmentNeededFilePath.Replace(".csv", $"_{i}.csv");

        // split processed data into n chunks
        var toSplit = GroupByPrecursorMass(PrecursorFragmentMassFile.Results, PrecursorMassTolerance, AmbiguityLevel);
        var toProcess = toSplit.Split(dataSplits).ToList();
        
        // Process a single chunk at a time
        for (int i = 0; i < dataSplits; i++)
        {
            var currentIteration = i;
            if (File.Exists(tempFilePaths[i]))
                continue;
            StartingSubProcess($"Processing Precursor Chunk {currentIteration + 1} of {dataSplits}");
            var results = new FragmentsToDistinguishRecord[toProcess[i].Count];
            for (int j = 0; j < toProcess[i].Count; j++)
            {
                var result = toProcess[currentIteration][j];
                var minFragments = result.Item2.Count != 0
                    ? MinFragmentMassesToDifferentiate(result.Item1.FragmentMassesHashSet, result.Item2, FragmentMassTolerance)
                    : 0;

                var record = new FragmentsToDistinguishRecord
                {
                    Species = Species,
                    NumberOfMods = NumberOfMods,
                    MaxFragments = MaximumFragmentationEvents,
                    AnalysisType = AnalysisLabel,
                    AmbiguityLevel = AmbiguityLevel,
                    Accession = result.Item1.Accession,
                    NumberInPrecursorGroup = result.Item2.Count,
                    FragmentsAvailable = result.Item1.FragmentMasses.Count,
                    FragmentCountNeededToDifferentiate = minFragments
                };

                results[j] = record;
            }

            // write that chunk
            var tempFile = new FragmentsToDistinguishFile(tempFilePaths[currentIteration]) { Results = results.ToList() };
            tempFile.WriteResults(tempFilePaths[currentIteration]);
            FinishedWritingFile(tempFilePaths[currentIteration]);
            FinishedSubProcess($"Finished Processing Precursor Chunk {currentIteration + 1} of {dataSplits}");
            toProcess[i].Clear();
        }

        // combine all temporary files into a single file and delete the temp files
        var allResults = new List<FragmentsToDistinguishRecord>();
        foreach (var tempFile in tempFilePaths)
            allResults.AddRange(new FragmentsToDistinguishFile(tempFile).Results);

        var fragmentsToDistinguishFile = new FragmentsToDistinguishFile(_minFragmentNeededFilePath) { Results = allResults };
        fragmentsToDistinguishFile.WriteResults(_minFragmentNeededFilePath);
        FinishedWritingFile(_minFragmentNeededFilePath);

        foreach (var tempFile in tempFilePaths)
            File.Delete(tempFile);

        return _minFragmentNeededFile = fragmentsToDistinguishFile;
    }


    public static int MinFragmentMassesToDifferentiate(HashSet<double> targetProteoform, List<PrecursorFragmentMassSet> otherProteoforms, Tolerance tolerance)
    {
        // check to see if target proteoform has a fragment that is unique to all other proteoform fragments within tolerance
        if (HasUniqueFragment(targetProteoform, otherProteoforms, tolerance))
            return 1;

        // remove all ions that are shared across every otherProteoform 
        var uniqueTargetFragmentList = targetProteoform
            .Where(frag => !otherProteoforms.All(p => p.FragmentMassesHashSet.Contains(frag)))
            .ToList();


        // If unique target list is empty, then all fragments are shared
        if (uniqueTargetFragmentList.Count == 0)
            return -1;

        // Generate all combinations of fragment masses from the target otherProteoform
        var combinations = GenerateCombinations(uniqueTargetFragmentList)
            .Where(p => p.Count > 1)
            .GroupBy(p => p.Count)
            .Select(group => new
            {
                Count = group.Key,
                Combinations = group.OrderBy(combination => combination.Sum(fragment =>
                    otherProteoforms.Count(p => p.FragmentMassesHashSet.Contains(fragment))))
            });

        // Order by count of fragment masses and check to see if they can differentiate the target
        foreach (var combinationGroup in combinations)
        {
            bool uniquePlusOneFound = false;
            foreach (var combination in combinationGroup.Combinations)
            {
                // Get those that can be explained by these fragments
                var matchingProteoforms = otherProteoforms
                    .Where(p => p.FragmentMassesHashSet.ListContainsWithin(combination, tolerance))
                    .ToList();

                if (matchingProteoforms.Count == 0)
                    return combination.Count;

                // if unique plus one has been found, no need to check again
                // however, we do need to ensure that one of the current analyzed combinations is unique
                if (uniquePlusOneFound)
                    continue;

                if (HasUniqueFragment(targetProteoform, matchingProteoforms, tolerance))
                    uniquePlusOneFound = true;
            }
            if (uniquePlusOneFound)
                return combinationGroup.Count + 1;
        }

        return -1;
    }

    /// <summary>
    /// Takes a large group of Precursor fragment mass sets and returns a list with an element for each allResultsToGroup
    /// The element is a tuple with the first element being the PrecursorFragmentMassSet and the second element being a list of all other PrecursorFragmentMassSets whose precursor mass falls within the tolerance
    /// </summary>
    /// <param name="allResultsToGroup"></param>
    /// <param name="tolerance"></param>
    /// <param name="ambiguityLevel"></param>
    /// <returns></returns>
    public static List<(PrecursorFragmentMassSet, List<PrecursorFragmentMassSet>)> GroupByPrecursorMass(List<PrecursorFragmentMassSet> allResultsToGroup, Tolerance tolerance, int ambiguityLevel = 1)
    {
        var orderedResults = allResultsToGroup.OrderBy(p => p.PrecursorMass).ToList();
        var groupedResults = new ConcurrentBag<(PrecursorFragmentMassSet, List<PrecursorFragmentMassSet>)>();

        Parallel.ForEach(Partitioner.Create(0, orderedResults.Count), new ParallelOptions() { MaxDegreeOfParallelism = StaticVariables.MaxThreads },
            range =>
        {
            for (int index = range.Item1; index < range.Item2; index++)
            {
                var outerResult = orderedResults[index];
                var firstIndex = orderedResults.FindIndex(p => tolerance.Within(p.PrecursorMass, outerResult.PrecursorMass));

                if (firstIndex == -1)
                    continue;

                var innerResults = new List<PrecursorFragmentMassSet>();
                for (int i = firstIndex; i < orderedResults.Count; i++)
                {
                    if (orderedResults[i].Equals(outerResult))
                        continue;
                    switch (ambiguityLevel)
                    {
                        case 2 when orderedResults[i].Accession == outerResult.Accession:
                        case 2 when orderedResults[i].FullSequence == outerResult.FullSequence:
                        continue;
                    }

                    if (tolerance.Within(orderedResults[i].PrecursorMass, orderedResults[firstIndex].PrecursorMass))
                        innerResults.Add(orderedResults[i]);
                    else
                        break;
                }

                groupedResults.Add((outerResult, innerResults));
            }
        });

        return groupedResults.ToList();
    }

    protected static bool HasUniqueFragment(HashSet<double> targetProteoform, List<PrecursorFragmentMassSet> otherProteoforms,
        Tolerance tolerance)
    {
        // Estimate the size of the HashSet to avoid resizing
        int estimatedSize = otherProteoforms.Sum(p => p.FragmentMassesHashSet.Count);
        var otherFragments = new HashSet<double>(estimatedSize);

        foreach (var otherProteoform in otherProteoforms)
        {
            foreach (var fragment in otherProteoform.FragmentMassesHashSet)
            {
                otherFragments.Add(fragment);
            }
        }

        // check to see if target proteoform has a fragment that is unique to all other proteoform fragments within tolerance
        foreach (var targetFragment in targetProteoform)
        {
            bool isUniqueFragment = true;
            foreach (var otherFragment in otherFragments)
            {
                if (tolerance.Within(targetFragment, otherFragment))
                {
                    isUniqueFragment = false;
                    break;
                }
            }
            if (isUniqueFragment)
                return true;
        }

        return false;
    }


    // Function to generate all combinations of fragment masses from a given list
    protected static IEnumerable<List<double>> GenerateCombinations(List<double> fragmentMasses)
    {
        int n = fragmentMasses.Count;
        var combinations = new List<List<double>>(1 << n);
        for (int i = 0; i < 1 << n; i++)
        {
            List<double> combination = new List<double>();
            for (int j = 0; j < n; j++)
            {
                if ((i & 1 << j) != 0)
                {
                    combination.Add(fragmentMasses[j]);
                }
            }
            combinations.Add(combination);
        }
        return combinations;
    }

    #endregion

}