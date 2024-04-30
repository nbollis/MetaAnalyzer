using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MzLibUtil;

namespace ResultAnalyzer.Util.TypeConverters
{
    public class CommaDelimitedToIntegerArrayTypeConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            var splits = text.Split(',');
            var toReturn = splits.Where(p => p != "");
            return toReturn.Select(int.Parse).ToArray();
        }

        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            var list = value as IEnumerable<int> ?? throw new MzLibException("Cannot convert input to IEnumerable<double>");
            return string.Join(',', list);
        }
    }
}
