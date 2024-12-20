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
    static RadicalFragmentationExplorer()
    {
        HashSetPool = new HashSetPool<double>(128);
        ListPool = new ListPool<double>(8);
        WriteLock = new object();
    }

    protected static readonly object WriteLock;
    protected static readonly HashSetPool<double> HashSetPool;
    protected static readonly ListPool<double> ListPool;
    public bool Override { get; set; } = false;
    protected string BaseDirectorPath { get; init; }

    public abstract string AnalysisType { get; }
    public abstract bool ResortNeeded { get; }

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
    protected string IndexDirectoryPath => Path.Combine(BaseDirectorPath, "IndexedFragments", AnalysisType);
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
        int ambiguityLevel = 1, string? baseDirectory = null, int allowedMissedMonos = 0, double? ppmTolerance = null)
    {
        var ppm = ppmTolerance ?? StaticVariables.DefaultPpmTolerance;
        DatabasePath = databasePath;
        NumberOfMods = numberOfMods;
        Species = species;
        MaximumFragmentationEvents = maximumFragmentationEvents;
        AmbiguityLevel = ambiguityLevel;
        BaseDirectorPath = baseDirectory ?? @"D:\Projects\RadicalFragmentation\FragmentAnalysis";
        MissedMonoIsotopics = allowedMissedMonos;
        PrecursorMassTolerance = allowedMissedMonos == 0 
            ? new PpmTolerance(ppm) 
            : new MissedMonoisotopicTolerance(ppm, allowedMissedMonos);
        FragmentMassTolerance = new PpmTolerance(ppm);

        fixedMods = new List<Modification>();
        variableMods = new List<Modification>();
        proteolysisProducts = new List<ProteolysisProduct>();
        disulfideBonds = new List<DisulfideBond>();
    }

    #region Result Files

    protected string _precursorFragmentMassFilePath => Path.Combine(IndexDirectoryPath,
        $"{Species}_{NumberOfMods}Mods_{MaxFragmentString}_{FileIdentifiers.FragmentIndex}.csv");
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
        $"{Species}_{NumberOfMods}Mods_{MaxFragmentString}_Level({AmbiguityLevel})Ambiguity_{FileIdentifiers.FragmentCountHistogram}.csv");
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
        $"{Species}_{NumberOfMods}Mods_{MaxFragmentString}_Level({AmbiguityLevel})Ambiguity_{FileIdentifiers.MinFragmentNeeded}.csv");
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
    public static EventHandler<ProgressBarEventArgs> ProgressBarHandler = null!;
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

    protected void UpdateProgressBar(string progressBarName, double progress)
    {
        if (progress > 1 || progress < 0)
            throw new ArgumentOutOfRangeException(nameof(progress), "Progress must be between 0 and 1");

        ProgressBarHandler?.Invoke(this, new ProgressBarEventArgs(progressBarName, progress));
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

        if (!Directory.Exists(IndexDirectoryPath))
            Directory.CreateDirectory(IndexDirectoryPath);

        int toProcess;
        int currentCount = 0;
        UpdateProgressBar($"Creating Index File for {AnalysisLabel}", 0);

        var sets = new List<PrecursorFragmentMassSet>();
        var modifications = NumberOfMods == 0 ? new List<Modification>() : GlobalVariables.AllModsKnown;
        var proteins = ProteinDbLoader.LoadProteinXML(DatabasePath, true, DecoyType.None, modifications,
            false, new List<string>(), out var um);

        toProcess = proteins.Count;
        Parallel.ForEach(Partitioner.Create(0, proteins.Count), new ParallelOptions() { MaxDegreeOfParallelism = StaticVariables.MaxThreads }, range =>
        {
            var localSets = new List<PrecursorFragmentMassSet>(100);
            for (int i = range.Item1; i < range.Item2; i++)
            {
                var generatedSets = GeneratePrecursorFragmentMasses(proteins[i]);
                localSets.AddRange(generatedSets);

                // report progress every 1% of data
                if (Interlocked.Increment(ref currentCount) % (toProcess / 100) == 0)
                {
                    UpdateProgressBar($"Creating Index File for {AnalysisLabel}", (double)currentCount / toProcess);
                }
            }

            lock (sets)
            {
                sets.AddRange(localSets);
            }
        });
        
        var uniqueSets = sets.DistinctBy(p => p, CustomComparerExtensions.LevelOneComparer).ToList();
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
            .DistinctBy(p => p, AmbiguityLevel == 1 
                ? CustomComparerExtensions.LevelOneComparer 
                : CustomComparerExtensions.LevelTwoComparer)
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

        UpdateProgressBar($"Finding Fragments Needed for {AnalysisLabel}", 0.00001);
        bool isCysteine = this is CysteineFragmentationExplorer;
        var writeTasks = new List<Task>(16);
        var results = new ConcurrentQueue<FragmentsToDistinguishRecord>();
        int toProcess = PrecursorFragmentMassFile.Results.Count;
        int current = 0;


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

                // Protein level analysis and all other proteoforms are of the same protein
                // Theoretically already handled in GroupByPrecursorMass, but this is a just in case check
                else if (AmbiguityLevel == 2 && result.Item2.All(other => other.Accession == result.Item1.Accession))
                    minFragments = 0;
                // if the only peak is the precursor, it cannot be differentiated
                else if (result.Item1.FragmentMasses.Count == 1 && result.Item1.FragmentMasses.BinaryContainsWithin(result.Item1.PrecursorMass, FragmentMassTolerance))
                    minFragments = -1;
                // all same mass and have no cysteines
                else if (isCysteine && result.Item1.CysteineCount == 0
                                    && result.Item2.All(other => other.CysteineCount == result.Item1.CysteineCount))
                    minFragments = -1;

                minFragments ??= MinFragmentMassesToDifferentiate(result.Item1.FragmentMasses, result.Item2,
                    FragmentMassTolerance, ResortNeeded);
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

                    if (Monitor.TryEnter(WriteLock))
                    {
                        try
                        {
                            // after lock is released, all other threads will flood in here
                            // this check ensures they don't dequeue the empty queue
                            if (results.Count < 100000) return;

                            toWrite = new List<FragmentsToDistinguishRecord>(128000);
                            while (results.TryDequeue(out var item))
                            {
                                toWrite.Add(item);
                            }

                            writeTasks.Add(Task.Run(() =>
                            {
                                var tempFile = new FragmentsToDistinguishFile()
                                {
                                    Results = toWrite
                                };
                                var path = _minFragmentNeededTempBasePath.GenerateUniqueFilePathThreadSafe();
                                tempFile.WriteResults(path);
                                //FinishedWritingFile(path);
                            }));
                        }
                        finally
                        {
                            Monitor.Exit(WriteLock);
                        }
                    }
                }

                // report progress every 1% of data
                if (Interlocked.Increment(ref current) % (toProcess / 100) == 0)
                {
                    UpdateProgressBar($"Finding Fragments Needed for {AnalysisLabel}", (double)current / toProcess);
                }
            });

        UpdateProgressBar($"Finding Fragments Needed for {AnalysisLabel}", 1);

        Task.WaitAll(writeTasks.ToArray());

        // combine all temporary temporaryFiles into a single file and delete the temp temporaryFiles
        var allResults = new List<FragmentsToDistinguishRecord>();
        allResults.AddRange(results); // add those that did not go to a temp file


        Task.WaitAll(writeTasks.ToArray());
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


    public static int MinFragmentMassesToDifferentiate(List<double> targetProteoform, List<PrecursorFragmentMassSet> otherProteoforms, Tolerance tolerance, bool sortByUniqueness = false)
    {
        // check to see if target proteoform has a fragment that is unique to all other proteoform fragments within tolerance
        if (HasUniqueFragment(targetProteoform, otherProteoforms, tolerance))
            return 1;

        var uniqueTargetFragmentList = ListPool.Get();
        try
        {
            // add all peaks from targetProteoform that are not found in every otherProteoform
            foreach (var frag in targetProteoform)
            {
                bool all = true;
                
                foreach (var otherFrag in otherProteoforms)
                {
                    if (otherFrag.FragmentMasses.BinaryContainsWithin(frag, tolerance)) 
                        continue;

                    all = false;
                    break;
                }

                if (all)
                    continue;
                uniqueTargetFragmentList.Add(frag);
            }

            // If unique target list is empty, then all fragments are shared
            if (uniqueTargetFragmentList.Count == 0)
                return -1;

            // if any other proteoform contains all unique ions, then we cannot differentiate
            if (otherProteoforms.Any(p => p.FragmentMasses.ListContainsWithin(uniqueTargetFragmentList, tolerance)))
                return -1;

            // reorder unique target list to be have those shared by the least other proteoforms first
            if (sortByUniqueness)
                uniqueTargetFragmentList.Sort((a, b) => otherProteoforms.Count(p => p.FragmentMasses.BinaryContainsWithin(a, tolerance))
                    .CompareTo(otherProteoforms.Count(p => p.FragmentMasses.BinaryContainsWithin(b, tolerance))));

            // Order by count of fragment masses and check to see if they can differentiate the target
            for (int count = 2; count <= uniqueTargetFragmentList.Count; count++)
            {
                bool uniquePlusOneFound = false;
                foreach (var combination in GenerateCombinationsWithCount(uniqueTargetFragmentList, count))
                {
                    // Get those that can be explained by these fragments
                    var matchingProteoforms = otherProteoforms
                        .Where(p => p.FragmentMasses.ListContainsWithin(combination, tolerance, !sortByUniqueness))
                        .ToList();

                    if (matchingProteoforms.Count == 0)
                        return combination.Count;

                    // if unique plus one has been found, no need to check again
                    // however, we do need to ensure that one of the current number analyzed combinations is unique
                    if (uniquePlusOneFound)
                        continue;

                    if (HasUniqueFragment(uniqueTargetFragmentList, matchingProteoforms, tolerance))
                        uniquePlusOneFound = true;
                }
                if (uniquePlusOneFound)
                    return count + 1;
            }

            return -1;
        }
        finally
        {
            ListPool.Return(uniqueTargetFragmentList);
        }
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
            Parallel.ForEach(Partitioner.Create(0, orderedResults.Count), new ParallelOptions { MaxDegreeOfParallelism = Math.Max(StaticVariables.MaxThreads / 2, 1) }, range =>
            {
                for (int index = range.Item1; index < range.Item2; index++)
                {
                    var outerResult = orderedResults[index];
                    int lookupStart = BinarySearch(orderedResults, outerResult.PrecursorMass, tolerance, true);
                    var innerResults = new List<PrecursorFragmentMassSet>(64);

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

                        if (tolerance.Within( outerResult.PrecursorMass, innerResult.PrecursorMass))
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

    protected static bool HasUniqueFragment(List<double> targetProteoform, List<PrecursorFragmentMassSet> otherProteoforms,
        Tolerance tolerance)
    {
        var sortedOtherFragments = ListPool.Get();
        try
        {
            foreach (var otherProteoform in otherProteoforms)
            {
                sortedOtherFragments.AddRange(otherProteoform.FragmentMasses); // Assuming these are sorted
            }
            sortedOtherFragments.Sort(); // Sort once if not already sorted

            // Step 2: Check for unique fragments using binary search
            foreach (var targetFragment in targetProteoform)
            {
                if (!sortedOtherFragments.BinaryContainsWithin(targetFragment, tolerance))
                {
                    return true; // Found a unique fragment
                }
            }

            return false; // No unique fragment found
        }
        finally
        {
            ListPool.Return(sortedOtherFragments);
        }
    }

    // Function to generate combinations of a specific count from a given list
    // retains the order of the imput fragmentMasses
    protected static IEnumerable<List<double>> GenerateCombinationsWithCount(List<double> fragmentMasses, int count)
    {
        int n = fragmentMasses.Count;
        var indices = new int[count];
        for (int i = 0; i < count; i++)
            indices[i] = i;

        while (indices[0] < n - count + 1)
        {
            var combination = new List<double>(count);
            for (int i = 0; i < count; i++)
                combination.Add(fragmentMasses[indices[i]]);
            yield return combination;

            int t = count - 1;
            while (t != 0 && indices[t] == n - count + t)
                t--;

            indices[t]++;
            for (int i = t + 1; i < count; i++)
                indices[i] = indices[i - 1] + 1;
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
            if (tolerance.Within(targetMass, orderedResults[mid].PrecursorMass))
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