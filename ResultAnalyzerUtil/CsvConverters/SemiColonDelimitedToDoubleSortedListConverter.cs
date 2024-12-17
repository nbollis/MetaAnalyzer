using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MzLibUtil;

namespace ResultAnalyzerUtil.CsvConverters;

public class SemiColonDelimitedToDoubleSortedListConverter : DefaultTypeConverter
{
    public override object ConvertFromString(
        string text,
        IReaderRow row,
        MemberMapData memberMapData)
    {
        return text.Split(';')
            .Select(double.Parse).OrderBy(p => p).ToList();
    }

    public override string ConvertToString(
        object value,
        IWriterRow row,
        MemberMapData memberMapData)
    {
        return value is IEnumerable<double> values
            ? string.Join(';', values)
            : throw new MzLibException("Cannot convert input to IEnumerable<double>");
    }
}