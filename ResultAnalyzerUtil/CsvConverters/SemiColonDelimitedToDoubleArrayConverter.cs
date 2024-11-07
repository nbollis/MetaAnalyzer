using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Easy.Common.Extensions;

namespace ResultAnalyzerUtil
{
    public class SemiColonDelimitedToDoubleArrayConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (text.IsNullOrEmpty())
                return Array.Empty<double>();
            var splits = text.Split(';');
            var toReturn = splits.Where(p => p != "");
            return toReturn.Select(double.Parse).ToArray();
        }

        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value is null)
                return "";
            var list = value as IEnumerable<double> ?? throw new MzLibUtil.MzLibException("Cannot convert input to IEnumerable<double>");
            return string.Join(';', list);
        }

        public class SemiColonDelimitedToIntegerArrayConverter : DefaultTypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                if (text.IsNullOrEmpty())
                    return Array.Empty<int>();
                var splits = text.Split(';');
                var toReturn = splits.Where(p => p != "");
                return toReturn.Select(int.Parse).ToArray();
            }

            public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
            {
                if (value is null)
                    return "";
                var list = value as IEnumerable<int> ?? throw new MzLibUtil.MzLibException("Cannot convert input to IEnumerable<double>");
                return string.Join(';', list);
            }
        }
    }
}
