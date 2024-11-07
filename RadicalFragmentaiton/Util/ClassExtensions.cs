using MzLibUtil;
using Plotly.NET;

namespace RadicalFragmentation;

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

    #region Plotting Colors

    static ClassExtensions()
    {
        ColorQueue = new Queue<Color>(new[]
        {
            Color.fromKeyword(ColorKeyword.Blue),
            Color.fromKeyword(ColorKeyword.Green),
            Color.fromKeyword(ColorKeyword.Purple),
            Color.fromKeyword(ColorKeyword.Orange),
            Color.fromKeyword(ColorKeyword.Yellow),
            Color.fromKeyword(ColorKeyword.Cyan),
            Color.fromKeyword(ColorKeyword.Magenta),
            Color.fromKeyword(ColorKeyword.Lime),
            Color.fromKeyword(ColorKeyword.Pink),
            Color.fromKeyword(ColorKeyword.Teal),
            Color.fromKeyword(ColorKeyword.Lavender),
            Color.fromKeyword(ColorKeyword.Brown),
            Color.fromKeyword(ColorKeyword.Beige),
            Color.fromKeyword(ColorKeyword.Maroon),
            Color.fromKeyword(ColorKeyword.Olive),
            Color.fromKeyword(ColorKeyword.Coral),
            Color.fromKeyword(ColorKeyword.Navy),
            Color.fromKeyword(ColorKeyword.Grey),
            Color.fromKeyword(ColorKeyword.White),
            Color.fromKeyword(ColorKeyword.Black),
            Color.fromKeyword(ColorKeyword.Purple),
            Color.fromKeyword(ColorKeyword.Indigo),
            Color.fromKeyword(ColorKeyword.Turquoise),
            Color.fromKeyword(ColorKeyword.DarkOrange),
            Color.fromKeyword(ColorKeyword.DarkBlue),
            Color.fromKeyword(ColorKeyword.DarkRed),
            Color.fromKeyword(ColorKeyword.DarkGreen),
            Color.fromKeyword(ColorKeyword.DarkViolet),
            Color.fromKeyword(ColorKeyword.DarkCyan),
            Color.fromKeyword(ColorKeyword.DarkMagenta),
            Color.fromKeyword(ColorKeyword.DarkGrey),
        });
    }

    #endregion

    public static Queue<Color> ColorQueue { get; }

    public static Dictionary<string, Color> ConditionToColorDictionary = new()
    {
        {"Peptide", Color.fromKeyword(ColorKeyword.LightCoral)},
        {"Peptidoform", Color.fromKeyword(ColorKeyword.Crimson)},
        {"Proteoform", Color.fromKeyword(ColorKeyword.CornflowerBlue)},
        {"Protein", Color.fromKeyword(ColorKeyword.Navy)},
    };

    private static Dictionary<string, string> ConditionNameConversionDictionary = new()
    {

    };

    public static Color ConvertConditionToColor(this string condition)
    {
        if (ConditionToColorDictionary.TryGetValue(condition, out var color))
            return color;
        else if (ConditionToColorDictionary.TryGetValue(condition.Trim(), out color))
            return color;
        else
        {
            if (ConditionNameConversionDictionary.ContainsValue(condition))
            {
                var key = ConditionNameConversionDictionary.FirstOrDefault(x => x.Value == condition).Key;
                if (key is null)
                    return Color.fromKeyword(ColorKeyword.Black);
                if (ConditionToColorDictionary.TryGetValue(key, out color))
                    return color;
            }
            else
            {
                ConditionToColorDictionary.Add(condition, ColorQueue.Dequeue());
                return ConditionToColorDictionary[condition];
            }
        }

        return Color.fromKeyword(ColorKeyword.Black);
    }
}