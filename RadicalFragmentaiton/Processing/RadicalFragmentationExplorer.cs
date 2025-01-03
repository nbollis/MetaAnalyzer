using Easy.Common.Extensions;
using MzLibUtil;
using Omics.Modifications;
using Proteomics;
using Proteomics.ProteolyticDigestion;
using RadicalFragmentation.Util;
using ResultAnalyzerUtil;
using ResultAnalyzerUtil.CommandLine;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UsefulProteomicsDatabases;

namespace RadicalFragmentation.Processing;

public abstract class RadicalFragmentationExplorer
{
    // thread safety
    protected static readonly object WriteLock;
    protected static readonly HashSetPool<double> HashSetPool;
    protected static readonly ListPool<double> ListPool;
    protected static readonly DictionaryPool<int, int> FragmentCacheDictionaryPool;
    static RadicalFragmentationExplorer()
    {
        HashSetPool = new HashSetPool<double>(128);
        ListPool = new ListPool<double>(8);
        WriteLock = new object();
        FragmentCacheDictionaryPool = new DictionaryPool<int, int>(128);
    }

    // Identifiers
    public abstract string AnalysisType { get; }

    private string? _analysisLabel;
    public string AnalysisLabel
    {
        get
        {
            if (_analysisLabel is not null) return _analysisLabel;

            string suffix = "";
            if (PrecursorMassTolerance is MissedMonoisotopicTolerance tol)
                suffix = $"_{tol.MissedMonoisotpics}MissedMonos";
            if (Math.Abs(PrecursorMassTolerance.Value - 10) > 0.0000001)
                suffix += $"_{PrecursorMassTolerance.Value}ppm";

            return _analysisLabel = $"{AnalysisType}{suffix}";
        }
    }

    // params
    public abstract bool ResortNeeded { get; }
    public bool Override { get; set; } = false;
    public readonly int NumberOfMods;
    public readonly int AmbiguityLevel;
    public readonly string Species;
    public readonly string DatabasePath;
    public readonly int MissedMonoIsotopics;
    public readonly double Tolerance;
    protected int MaximumFragmentationEvents { get; set; }
    protected string MaxFragmentString => MaximumFragmentationEvents == int.MaxValue ? "All" : MaximumFragmentationEvents.ToString();
    protected readonly Tolerance PrecursorMassTolerance;
    protected readonly Tolerance FragmentMassTolerance;

    // paths
    public readonly string DirectoryPath;
    public readonly string FigureDirectory;
    protected readonly string BaseDirectorPath;
    protected readonly string IndexDirectoryPath;


    // empty and used in digestion
    protected readonly List<Modification> fixedMods;
    protected readonly List<Modification> variableMods;
    protected readonly List<ProteolysisProduct> proteolysisProducts;
    protected readonly List<DisulfideBond> disulfideBonds;

    protected RadicalFragmentationExplorer(string databasePath, int numberOfMods, string species, int maximumFragmentationEvents = int.MaxValue,
        int ambiguityLevel = 1, string? baseDirectory = null, int allowedMissedMonos = 0, double? ppmTolerance = null)
    {
        Tolerance = ppmTolerance ?? StaticVariables.DefaultPpmTolerance;
        DatabasePath = databasePath;
        NumberOfMods = numberOfMods;
        Species = species;
        MaximumFragmentationEvents = maximumFragmentationEvents;
        AmbiguityLevel = ambiguityLevel;
        MissedMonoIsotopics = allowedMissedMonos;
        PrecursorMassTolerance = allowedMissedMonos == 0 
            ? new PpmTolerance(Tolerance) 
            : new MissedMonoisotopicTolerance(Tolerance, allowedMissedMonos);
        FragmentMassTolerance = new PpmTolerance(StaticVariables.DefaultPpmTolerance);

        fixedMods = new List<Modification>();
        variableMods = new List<Modification>();
        proteolysisProducts = new List<ProteolysisProduct>();
        disulfideBonds = new List<DisulfideBond>();


        // Set readonly properties
        BaseDirectorPath = baseDirectory ?? @"D:\Projects\RadicalFragmentation\FragmentAnalysis";
        DirectoryPath = Path.Combine(BaseDirectorPath, AnalysisLabel);
        FigureDirectory = Path.Combine(BaseDirectorPath, "Figure");
        IndexDirectoryPath = Path.Combine(BaseDirectorPath, "IndexedFragments", AnalysisType);
        PrecursorFragmentMassFilePath = Path.Combine(IndexDirectoryPath, $"{Species}_{NumberOfMods}Mods_{MaxFragmentString}_{FileIdentifiers.FragmentIndex}.csv");
        FragmentHistogramFilePath = Path.Combine(DirectoryPath, $"{Species}_{NumberOfMods}Mods_{MaxFragmentString}_Level({AmbiguityLevel})Ambiguity_{FileIdentifiers.FragmentCountHistogram}.csv");
        MinFragmentNeededFilePath = Path.Combine(DirectoryPath,
            $"{Species}_{NumberOfMods}Mods_{MaxFragmentString}_Level({AmbiguityLevel})Ambiguity_{FileIdentifiers.MinFragmentNeeded}.csv");
        MinFragmentNeededTempBasePath = MinFragmentNeededFilePath.Replace(".csv", "_temp.csv");

    }

    #region Result Files

    protected readonly string PrecursorFragmentMassFilePath;
    protected readonly string FragmentHistogramFilePath;
    protected readonly string MinFragmentNeededTempBasePath;
    protected readonly string MinFragmentNeededFilePath;

    protected PrecursorFragmentMassFile? _precursorFragmentMassFile;
    public PrecursorFragmentMassFile PrecursorFragmentMassFile
    {
        get
        {
            if (_precursorFragmentMassFile != null) return _precursorFragmentMassFile;
            if (File.Exists(PrecursorFragmentMassFilePath))
            {
                _precursorFragmentMassFile = new PrecursorFragmentMassFile(PrecursorFragmentMassFilePath);
            }
            else
            {
                _precursorFragmentMassFile = CreateIndexedFile();
            }

            return _precursorFragmentMassFile;
        }
    }

    protected FragmentHistogramFile? _fragmentHistogramFile;
    public FragmentHistogramFile FragmentHistogramFile
    {
        get
        {
            if (_fragmentHistogramFile != null) return _fragmentHistogramFile;
            if (File.Exists(FragmentHistogramFilePath))
            {
                _fragmentHistogramFile = new FragmentHistogramFile(FragmentHistogramFilePath);
            }
            else
            {
                CreateFragmentHistogramFile();
                _fragmentHistogramFile = new FragmentHistogramFile(FragmentHistogramFilePath);
            }

            return _fragmentHistogramFile;
        }
    }

    protected FragmentsToDistinguishFile? _minFragmentNeededFile;
    public FragmentsToDistinguishFile MinFragmentNeededFile
    {
        get
        {
            if (_minFragmentNeededFile != null) return _minFragmentNeededFile;
            if (File.Exists(MinFragmentNeededFilePath))
            {
                _minFragmentNeededFile = new FragmentsToDistinguishFile(MinFragmentNeededFilePath);
            }
            else
            {
                FindNumberOfFragmentsNeededToDifferentiate();
                _minFragmentNeededFile = new FragmentsToDistinguishFile(MinFragmentNeededFilePath);
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
        if (!Override && File.Exists(PrecursorFragmentMassFilePath))
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
            FilePath = PrecursorFragmentMassFilePath,
            Results = uniqueSets
        };
        file.WriteResults(PrecursorFragmentMassFilePath);

        FinishedWritingFile(PrecursorFragmentMassFilePath);
        return _precursorFragmentMassFile = file;
    }
    public abstract IEnumerable<PrecursorFragmentMassSet> GeneratePrecursorFragmentMasses(Protein protein);

    public FragmentHistogramFile CreateFragmentHistogramFile()
    {
        if (!Override && File.Exists(FragmentHistogramFilePath))
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
        var file = new FragmentHistogramFile(FragmentHistogramFilePath) { Results = fragmentCounts };
        file.WriteResults(FragmentHistogramFilePath);

        FinishedWritingFile(MinFragmentNeededFilePath);
        return _fragmentHistogramFile = file;
    }


    public FragmentsToDistinguishFile FindNumberOfFragmentsNeededToDifferentiate()
    {
        if (!Override && File.Exists(MinFragmentNeededFilePath))
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
        int maxBeforeTempFile = 250000;

        if (this is CysteineFragmentationExplorer cys)
            cys.CountCysteines();

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
                int fragAvailable = result.Item1.FragmentMasses.Count;

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

                // method may be destructive to lists
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
                    FragmentsAvailable = fragAvailable,
                    FragmentCountNeededToDifferentiate = minFragments.Value
                };
                results.Enqueue(record);

                // Write intermediate results to temporary file
                if (results.Count >= maxBeforeTempFile)
                {
                    List<FragmentsToDistinguishRecord>? toWrite;

                    if (Monitor.TryEnter(WriteLock))
                    {
                        try
                        {
                            // after lock is released, all other threads will flood in here
                            // this check ensures they don't dequeue the empty queue
                            if (results.Count < maxBeforeTempFile) return;

                            toWrite = new List<FragmentsToDistinguishRecord>((int)(maxBeforeTempFile * 1.1));
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
                                var path = MinFragmentNeededTempBasePath.GenerateUniqueFilePathThreadSafe();
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

        var fragmentsToDistinguishFile = new FragmentsToDistinguishFile(MinFragmentNeededFilePath) { Results = allResults };
        fragmentsToDistinguishFile.WriteResults(MinFragmentNeededFilePath);
        FinishedWritingFile(MinFragmentNeededFilePath);

        foreach (var tempFile in temporaryFiles)
            File.Delete(tempFile);

        return _minFragmentNeededFile = fragmentsToDistinguishFile;
    }

    // works well for smaller sets
    public static int MinFragmentMassesToDifferentiate(List<double> targetProteoform,
        List<PrecursorFragmentMassSet> otherProteoforms, Tolerance tolerance, bool sortByUniqueness = false,
        bool useGreed = false)
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
            {
                // Precompute uniqueness scores for all fragments
                var fragmentUniqueness = uniqueTargetFragmentList.ToDictionary(
                    frag => frag,
                    frag => otherProteoforms.Count(p => p.FragmentMasses.BinaryContainsWithin(frag, tolerance))
                );

                // Sort based on precomputed uniqueness scores
                uniqueTargetFragmentList.Sort((a, b) => fragmentUniqueness[a].CompareTo(fragmentUniqueness[b]));

            }

            if (useGreed)
                return FindMinGreedy(uniqueTargetFragmentList, otherProteoforms, tolerance);
            else
                return FindMinBruteForce(uniqueTargetFragmentList, otherProteoforms, tolerance, sortByUniqueness);

        }
        finally
        {
            ListPool.Return(uniqueTargetFragmentList);
        }
    }

    public static int FindMinBruteForce(List<double> uniqueTargetFragmentList, List<PrecursorFragmentMassSet> otherProteoforms, 
        Tolerance tolerance, bool resort)
    {
        // Order by count of fragment masses and check to see if they can differentiate the target

        var matchingProteoforms = new List<PrecursorFragmentMassSet>();
        for (int count = 2; count <= uniqueTargetFragmentList.Count; count++)
        {
            bool uniquePlusOneFound = false;
            foreach (var combination in GenerateCombinationsWithCount(uniqueTargetFragmentList, count))
            {
                matchingProteoforms.Clear();

                // Get those that can be explained by these fragments
                matchingProteoforms.AddRange(otherProteoforms
                    .Where(p => p.FragmentMasses.ListContainsWithin(combination, tolerance, !resort)));

                if (matchingProteoforms.Count == 0)
                    return combination.Count;

                // If unique plus one has been found, no need to check again
                // However, we do need to ensure that one of the current number analyzed combinations is unique
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

    public static int FindMinGreedy(
    List<double> targetProteoform,
    List<PrecursorFragmentMassSet> otherProteoforms,
    Tolerance tolerance)
    {
        int fragmentCount = 0; // To track the number of selected fragments

        while (otherProteoforms.Count > 0)
        {
            // Select the most useful fragment
            var bestFragment = targetProteoform.FirstOrDefault(f =>
                otherProteoforms.Any(p => !p.FragmentMasses.BinaryContainsWithin(f, tolerance)));

            if (bestFragment == default)
                return -1; // Return -1 if no suitable fragment is found

            fragmentCount++;

            // Remove resolved proteoforms
            otherProteoforms.RemoveAll(p => !p.FragmentMasses.BinaryContainsWithin(bestFragment, tolerance));

            // Remove the selected fragment from the target proteoform
            targetProteoform.Remove(bestFragment);

            // Periodic rescoring for dynamic adaptability
            if (fragmentCount % 2 == 0)
            {
                // Precompute uniqueness scores for all fragments
                var fragmentUniqueness = targetProteoform.ToDictionary(
                    frag => frag,
                    frag => otherProteoforms.Count(p => p.FragmentMasses.BinaryContainsWithin(frag, tolerance))
                );

                // Sort based on precomputed uniqueness scores
                targetProteoform.Sort((a, b) => fragmentUniqueness[a].CompareTo(fragmentUniqueness[b]));
            }
        }

        return fragmentCount;
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
        var uniqueFragments = HashSetPool.Get();
        var sortedUniqueFragments = ListPool.Get();
        try
        {
            foreach (var otherProteoform in otherProteoforms)
            {
                foreach (var fragment in otherProteoform.FragmentMasses)
                {
                    uniqueFragments.Add(fragment); // Add fragments to HashSet to ensure uniqueness
                }
            }

            sortedUniqueFragments.AddRange(uniqueFragments);
            sortedUniqueFragments.Sort(); // Sort the unique fragments

            // Step 2: Check for unique fragments using binary search
            foreach (var targetFragment in targetProteoform)
            {
                if (!sortedUniqueFragments.BinaryContainsWithin(targetFragment, tolerance))
                {
                    return true; // Found a unique fragment
                }
            }

            return false; // No unique fragment found
        }
        finally
        {
            HashSetPool.Return(uniqueFragments);
            ListPool.Return(sortedUniqueFragments);
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

    private static int GetHashCodeForSet(HashSet<double> set)
    {
        int hash = 17;
        foreach (var item in set)
        {
            hash = hash * 31 + item.GetHashCode();
        }
        return hash;
    }


}