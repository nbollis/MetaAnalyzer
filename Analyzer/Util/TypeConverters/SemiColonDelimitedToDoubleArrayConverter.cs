using CsvHelper.TypeConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace Analyzer.Util.TypeConverters
{
    public class SemiColonDelimitedToDoubleArrayConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            var splits = text.Split(';');
            var toReturn = splits.Where(p => p != "");
            return toReturn.Select(double.Parse).ToArray();
        }

        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            var list = value as IEnumerable<double> ?? throw new MzLibUtil.MzLibException("Cannot convert input to IEnumerable<double>");
            return string.Join(';', list);
        }

        public class SemiColonDelimitedToIntegerArrayConverter : DefaultTypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                var splits = text.Split(';');
                var toReturn = splits.Where(p => p != "");
                return toReturn.Select(int.Parse).ToArray();
            }

            public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
            {
                var list = value as IEnumerable<int> ?? throw new MzLibUtil.MzLibException("Cannot convert input to IEnumerable<double>");
                return string.Join(';', list);
            }
        }
    }
}
