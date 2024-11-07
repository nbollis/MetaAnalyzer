using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MzLibUtil;

namespace ResultAnalyzerUtil
{
    public class CommaDelimitedToStringListTypeConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            return text.Split(',').Where(p => p != "").ToList();
        }

        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            var list = value as IEnumerable<string> ?? throw new MzLibException("Cannot convert input to IEnumerable<string>");
            return string.Join(',', list);
        }
    }
}
