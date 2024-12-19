using MzLibUtil;
using Omics.SpectrumMatch;
using System.Collections.Generic;

namespace ResultAnalyzerUtil;

public static class ClassExtensions
{
    static ClassExtensions()
    {
        FilePathLock = new object();
        ClaimedFilePaths = new List<string>(64);
    }

    public static IEnumerable<List<T>> Split<T>(this IEnumerable<T> list, int parts)
    {
        int i = 0;
        var splits = from item in list
            group item by i++ % parts into part
            select part.ToList();
        return splits;
    }

    public static bool ContainsWithin(this IList<double> list, double value, Tolerance tolerance)
    {
        foreach (var item in list)
        {
            if (tolerance.Within(value, item))
            {
                return true;
            }
        }
        return false;
    }

    public static bool BinaryContainsWithin(this IList<double> sortedList, double value, Tolerance tolerance)
    {
        int low = 0, high = sortedList.Count - 1;

        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            double midValue = sortedList[mid];

            if (tolerance.Within( value, midValue))
            {
                return true; // Found a match within tolerance
            }
            else if (midValue < value)
            {
                low = mid + 1; // Search right
            }
            else
            {
                high = mid - 1; // Search left
            }
        }

        return false; // No match found within tolerance
    }

    // returns if list contains all values within tolerance
    public static bool ListContainsWithin(this IList<double> list, IList<double> values, Tolerance tolerance, bool valuesAreSored = true)
    {
        if (values.Count > list.Count)
            return false;

        if (values.Count > 6)
            if (valuesAreSored)
                return list.IsSuperSetSorted(values, tolerance);
            else 
                return list.IsSuperSet_SubsetNotSorted(values, tolerance);

        for (var index = 0; index < values.Count; index++)
        {
            if (!list.BinaryContainsWithin(values[index], tolerance))
                return false;
        }

        return true;
    }

    // assumes both are ordred
    public static bool IsSuperSetSorted(this IList<double> superset, IList<double> subset, Tolerance tolerance)
    {
        if (superset.Count < subset.Count)
            return false;

        int i = 0, j = 0;
        while (i < superset.Count && j < subset.Count)
        {
            if (tolerance.Within(superset[i], subset[j]))
            {
                // Match found within tolerance, move to the next subset element
                j++;
            }
            else if (superset[i] <= tolerance.GetMinimumValue(subset[j]))
            {
                // Superset value is too small, move to the next superset element
                i++;
            }
            else
            {
                // Subset element not found within current superset range
                return false;
            }
        }

        return j == subset.Count; // If we matched all subset elements, return true
    }

    public static bool IsSuperSet_SubsetNotSorted(this IList<double> superset, IList<double> subset, Tolerance tolerance)
    {
        if (superset.Count < subset.Count)
            return false;

        var matchedIndices = new HashSet<int>();
        foreach (var subsetValue in subset)
        {
            bool matchFound = false;
            for (int i = 0; i < superset.Count; i++)
            {
                if (!matchedIndices.Contains(i) && tolerance.Within(superset[i], subsetValue))
                {
                    matchedIndices.Add(i);
                    matchFound = true;
                    break;
                }
            }
            if (!matchFound)
            {
                return false;
            }
        }

        return true;
    }






    private static readonly object FilePathLock;
    private static readonly List<string> ClaimedFilePaths;
    public static string GenerateUniqueFilePathThreadSafe(this string filePath)
    {
        var extension = System.IO.Path.GetExtension(filePath);
        int index = 1;

        lock (FilePathLock)
        {
            var toInsert = $"({index})";
            while (File.Exists(filePath) || ClaimedFilePaths.Contains(filePath))
            {
                var previous = toInsert;
                toInsert = $"({index})";

                // if first time needing to add an integer to filename
                if (index != 1)
                {
                    var lastInsertIndex = filePath.LastIndexOf(previous, StringComparison.Ordinal);
                    filePath = filePath.Remove(lastInsertIndex, previous.Length);
                }

                int indexToInsert = filePath.IndexOf(extension, StringComparison.InvariantCulture);
                filePath = filePath.Insert(indexToInsert, toInsert);
                index++;
            }
            ClaimedFilePaths.Add(filePath);
        }
        return filePath;
    }

    public static string GenerateUniqueFilePath(this string filePath)
    {
        var extension = System.IO.Path.GetExtension(filePath);
        int index = 1;

        var toInsert = $"({index})";
        while (File.Exists(filePath))
        {
            var previous = toInsert;
            toInsert = $"({index})";

            // if first time needing to add an integer to filename
            if (index != 1)
            {
                var lastInsertIndex = filePath.LastIndexOf(previous, StringComparison.Ordinal);
                filePath = filePath.Remove(lastInsertIndex, previous.Length);
            }

            int indexToInsert = filePath.IndexOf(extension, StringComparison.InvariantCulture);
            filePath = filePath.Insert(indexToInsert, toInsert);
            index++;
        }

        return filePath;
    }
}