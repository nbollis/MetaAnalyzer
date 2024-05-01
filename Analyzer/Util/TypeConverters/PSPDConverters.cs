using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Analyzer.Util.TypeConverters
{
    public class PSPDMsOrderConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            return int.Parse(text.Last().ToString());
        }

        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            return $"MS{value}";
        }
    }
}
