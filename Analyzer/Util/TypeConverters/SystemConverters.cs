using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using CsvHelper;
using MzLibUtil;

namespace Analyzer.Util.TypeConverters;

/// <summary>
/// Converts a list of doubles delimited by semicolons to a list of doubles
/// To be used with CsvHelper
/// </summary>
internal class SemicolonDelimitedToDoubleListConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        var splits = text.Split(';');
        return splits.Select(p => double.Parse(p)).ToList();
    }

    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
    {
        var list = value as IEnumerable<double> ?? throw new MzLibException("Cannot convert input to IEnumerable<double>");
        return string.Join(';', list);
    }
}

/// <summary>
/// Converts a list of integers delimited by semicolons to an array of integers
/// To be used with CsvHelper
/// </summary>
internal class SemicolonDelimitedToIntegerArrayConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        var splits = text.Split(';');
        return splits.Select(int.Parse).ToArray();
    }

    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
    {
        var list = value as int[] ?? throw new MzLibException("Cannot convert input to int[]");
        return string.Join(';', list);
    }
}

/// <summary>
/// Converts a list of strings delimited by semicolons to an array of strings
/// To be used with CsvHelper
/// </summary>
internal class SemicolonDelimitedToStringArrayConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        return text.Split(';');
    }

    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
    {
        var list = value as string[] ?? throw new MzLibException("Cannot convert input to string[]");
        return string.Join(';', list);
    }
}

/// <summary>
/// Converts a list of numbers delimited by semicolons to an array of longs
/// To be used with CsvHelper
/// </summary>
internal class SemicolonDelimitedToLongArrayConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        var splits = text.Split(';');
        return splits.Select(long.Parse).ToArray();
    }

    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
    {
        var list = value as long[] ?? throw new MzLibException("Cannot convert input to long[]");
        return string.Join(';', list);
    }
}