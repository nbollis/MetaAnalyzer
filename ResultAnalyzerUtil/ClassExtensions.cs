using MzLibUtil;
using Omics.SpectrumMatch;

namespace ResultAnalyzerUtil;

public static class ClassExtensions
{
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

    public static string GetUniqueFilePath(this string filePath)
    {
        var extension = System.IO.Path.GetExtension(filePath);
        int index = 1;

        while (File.Exists(filePath))
        {
            int indexToInsert = filePath.IndexOf(extension, StringComparison.InvariantCulture);

            // if first time needing to add an integer to filename
            filePath = index == 1
                ? filePath.Insert(indexToInsert, $"({index})")
                : filePath.Replace($"({index - 1})", $"({index})");

            index++;
        }

        return filePath;
    }
}