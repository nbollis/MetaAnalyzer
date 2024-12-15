using MzLibUtil;
using Omics.SpectrumMatch;

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

    public static bool ContainsWithin(this IEnumerable<double> list, double value, Tolerance tolerance)
    {
        foreach (var item in list)
        {
            if (tolerance.Within(item, value))
            {
                return true;
            }
        }
        return false;
    }

    // returns if list contains all values within tolerance
    public static bool ListContainsWithin(this IEnumerable<double> list, List<double> values, Tolerance tolerance)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (!list.ContainsWithin(values[index], tolerance))
                return false;
        }

        return true;
    }

    // returns if list contains all values within tolerance
    public static bool ListContainsWithin(this IEnumerable<double> list, double[] values, Tolerance tolerance)
    {
        for (var index = 0; index < values.Length; index++)
        {
            if (!list.ContainsWithin(values[index], tolerance))
                return false;
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
            while (File.Exists(filePath) || ClaimedFilePaths.Contains(filePath))
            {
                var toInsert = $"({index})";
                int indexToInsert = filePath.IndexOf(extension, StringComparison.InvariantCulture);

                // if first time needing to add an integer to filename
                if (index == 1)
                {
                    filePath = filePath.Insert(indexToInsert, toInsert);
                }
                else
                {
                    var lastInsertIndex = filePath.LastIndexOf($"({index - 1})", StringComparison.Ordinal);
                    filePath = filePath.Remove(lastInsertIndex, $"({index - 1})".Length)
                        .Insert(indexToInsert - toInsert.Length, toInsert);
                }


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

        while (File.Exists(filePath))
        {
            var toInsert = $"({index})";
            int indexToInsert = filePath.IndexOf(extension, StringComparison.InvariantCulture);

            // if first time needing to add an integer to filename
            if (index == 1)
            {
                filePath = filePath.Insert(indexToInsert, toInsert);
            }
            else
            {
                var lastInsertIndex = filePath.LastIndexOf($"({index - 1})", StringComparison.Ordinal);
                filePath = filePath.Remove(lastInsertIndex, $"({index - 1})".Length)
                    .Insert(indexToInsert - toInsert.Length, toInsert);
            }
            index++;
        }

        return filePath;
    }
}