using Easy.Common.Extensions;
using MzLibUtil;
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
    protected static readonly HashSetPool<double> HashSetPool = new(128);
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

    protected string _minFragmentNeededTempBasePath => _minFragmentNeededFilePath.Replace(".csv", "_temp.csv");
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

    #region CMD

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

            Parallel.ForEach(Partitioner.Create(0, proteins.Count), new ParallelOptions() { MaxDegreeOfParallelism = StaticVariables.MaxThreads }, range =>
            {
                var localSets = new List<PrecursorFragmentMassSet>(100);
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    var generatedSets = GeneratePrecursorFragmentMasses(proteins[i]);
                    localSets.AddRange(generatedSets);
                }

                lock (sets)
                {
                    sets.AddRange(localSets);
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
        bool isCysteine = this is CysteineFragmentationExplorer;

        var writeLock = new object();
        var writeTasks = new List<Task>(16);
        var results = new ConcurrentQueue<FragmentsToDistinguishRecord>();

        Parallel.ForEach(Partitioner.Create
            (
                GroupByPrecursorMass(PrecursorFragmentMassFile.Results, PrecursorMassTolerance, AmbiguityLevel),
                EnumerablePartitionerOptions.NoBuffering
            ),
            new ParallelOptions() { MaxDegreeOfParallelism = StaticVariables.MaxThreads },
            result =>
            {
                int? minFragments = null;
                int numberInGroup = result.Item2.Count;

                // no other proteoform with precursor mass in range
                if (result.Item2.Count == 0)
                    minFragments = 0;

                // all same mass and have no cysteines
                else if (isCysteine && result.Item1.CysteineCount == 0
                                    && result.Item2.All(other => other.CysteineCount == result.Item1.CysteineCount))
                    minFragments = -1;

                minFragments ??= MinFragmentMassesToDifferentiate(result.Item1.FragmentMassesHashSet, result.Item2,
                    FragmentMassTolerance);
                var record = new FragmentsToDistinguishRecord
                {
                    Species = Species,
                    NumberOfMods = NumberOfMods,
                    MaxFragments = MaximumFragmentationEvents,
                    AnalysisType = AnalysisLabel,
                    AmbiguityLevel = AmbiguityLevel,
                    Accession = result.Item1.Accession,
                    NumberInPrecursorGroup = numberInGroup,
                    FragmentsAvailable = result.Item1.FragmentMasses.Count,
                    FragmentCountNeededToDifferentiate = minFragments.Value
                };
                results.Enqueue(record);

                // Write intermediate results to temporary file
                if (results.Count >= 100000)
                {
                    List<FragmentsToDistinguishRecord>? toWrite;

                    lock (writeLock)
                    {
                        // after lock is released, all other threads will flood in here
                        // this check ensures they don't dequeue the empty queue
                        if (results.Count < 100000) return;

                        toWrite = new List<FragmentsToDistinguishRecord>(101000);
                        while (results.TryDequeue(out var item))
                        {
                            toWrite.Add(item);
                        }
                    }

                    writeTasks.Add(Task.Run(() =>
                    {
                        var tempFile = new FragmentsToDistinguishFile()
                        {
                            Results = toWrite
                        };
                        var path = _minFragmentNeededTempBasePath.GetUniqueFilePath();
                        tempFile.WriteResults(path);
                        FinishedWritingFile(path);

                        toWrite = null;
                    }));
                }
            });
        

        Task.WaitAll(writeTasks.ToArray());

        // combine all temporary temporaryFiles into a single file and delete the temp temporaryFiles
        var allResults = new List<FragmentsToDistinguishRecord>();
        allResults.AddRange(results); // add those that did not go to a temp file

        var temporaryFiles = Directory.GetFiles(DirectoryPath)
            .Where(p => p.Contains(FileIdentifiers.MinFragmentNeeded) && p.Contains("temp"))
            .ToArray();
        foreach (var file in temporaryFiles)
        {
            allResults.AddRange(new FragmentsToDistinguishFile(file).Results);
        }

        var fragmentsToDistinguishFile = new FragmentsToDistinguishFile(_minFragmentNeededFilePath) { Results = allResults };
        fragmentsToDistinguishFile.WriteResults(_minFragmentNeededFilePath);
        FinishedWritingFile(_minFragmentNeededFilePath);

        foreach (var tempFile in temporaryFiles)
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
            .Where(frag => !otherProteoforms.All(p => p.FragmentMassesHashSet.ContainsWithin(frag, tolerance)))
            .ToArray();

        // If unique target list is empty, then all fragments are shared
        if (uniqueTargetFragmentList.Length == 0)
            return -1;

        // if any other proteoform contains all unique ions, then we cannot differentiate
        if (otherProteoforms.Any(p => p.FragmentMassesHashSet.ListContainsWithin(uniqueTargetFragmentList, tolerance)))
            return -1;

        // Generate all combinations of fragment masses from the target otherProteoform in order of uniqueness
        var combinations = GenerateCombinations(uniqueTargetFragmentList)
            .Where(p => p.Count > 1)
            .GroupBy(p => p.Count)
            .OrderBy(group => group.Key);

        // Order by count of fragment masses and check to see if they can differentiate the target
        foreach (var combinationGroup in combinations)
        {
            bool uniquePlusOneFound = false;
            foreach (var combination in combinationGroup.OrderBy(combination => combination.Sum(fragment =>
                         otherProteoforms.Count(p => p.FragmentMassesHashSet.Contains(fragment)))))
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
                return combinationGroup.Key + 1;
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
    public static IEnumerable<(PrecursorFragmentMassSet, List<PrecursorFragmentMassSet>)> GroupByPrecursorMass(List<PrecursorFragmentMassSet> allResultsToGroup, Tolerance tolerance, int ambiguityLevel = 1)
    {
        var orderedResults = allResultsToGroup.OrderBy(p => p.PrecursorMass).ToList();
        var results = new BlockingCollection<(PrecursorFragmentMassSet, List<PrecursorFragmentMassSet>)>();
        var producerTask = Task.Run(() =>
        {
            Parallel.ForEach(Partitioner.Create(0, orderedResults.Count), new ParallelOptions { MaxDegreeOfParallelism = StaticVariables.MaxThreads }, range =>
            {
                for (int index = range.Item1; index < range.Item2; index++)
                {
                    var outerResult = orderedResults[index];
                    int lookupStart = BinarySearch(orderedResults, outerResult.PrecursorMass, tolerance, true);
                    var innerResults = new List<PrecursorFragmentMassSet>(4);

                    for (int i = lookupStart; i < orderedResults.Count; i++)
                    {
                        var innerResult = orderedResults[i];
                        if (innerResult.Equals(outerResult))
                            continue;
                        switch (ambiguityLevel)
                        {
                            case 2 when innerResult.Accession == outerResult.Accession:
                            case 2 when innerResult.FullSequence == outerResult.FullSequence:
                                continue;
                        }

                        if (tolerance.Within(innerResult.PrecursorMass, outerResult.PrecursorMass))
                            innerResults.Add(innerResult);
                        else
                            break;
                    }

                    results.Add((outerResult, innerResults));
                }
            });

            results.CompleteAdding();
        });

        foreach (var result in results.GetConsumingEnumerable())
        {
            yield return result;
        }

        producerTask.Wait();
    }

    protected static bool HasUniqueFragment(HashSet<double> targetProteoform, List<PrecursorFragmentMassSet> otherProteoforms,
        Tolerance tolerance)
    {
        var otherFragments = HashSetPool.Get();
        try
        {
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
        finally
        {
            HashSetPool.Return(otherFragments);
        }
    }

    // Function to generate all combinations of fragment masses from a given list
    protected static IEnumerable<List<double>> GenerateCombinations(double[] fragmentMasses)
    {
        int n = fragmentMasses.Length;
        for (int i = 1; i < 1 << n; i++)// Start from 1 to avoid empty combination
        {
            var combination = new List<double>();
            for (int j = 0; j < n; j++)
            {
                if ((i & 1 << j) != 0)
                {
                    combination.Add(fragmentMasses[j]);
                }
            }
            yield return combination;
        }
    }



    #endregion

    public static int BinarySearch(List<PrecursorFragmentMassSet> orderedResults, double targetMass, Tolerance tolerance, bool findFirst)
    {
        int left = 0;
        int right = orderedResults.Count - 1;
        int result = -1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            if (tolerance.Within(orderedResults[mid].PrecursorMass, targetMass))
            {
                result = mid;
                if (findFirst)
                    right = mid - 1;
                else
                    left = mid + 1;
            }
            else if (orderedResults[mid].PrecursorMass < targetMass)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }

        return result;
    }
}