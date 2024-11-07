using MzLibUtil;

namespace ResultAnalyzerUtil;

public static class ClassExtensions
{
    public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> list, int parts)
    {
        int i = 0;
        var splits = from item in list
            group item by i++ % parts into part
            select part.AsEnumerable();
        return splits;
    }

    public static bool ContainsWithin(this IEnumerable<double> list, double value, Tolerance tolerance)
    {
        return list.Any(p => tolerance.Within(p, value));
    }

    public static bool ListContainsWithin(this IEnumerable<double> list, List<double> values, Tolerance tolerance)
    {
        return values.All(p => list.ContainsWithin(p, tolerance));
    }
}