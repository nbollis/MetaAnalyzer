using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MathNet.Numerics;

namespace ResultAnalyzerUtil
{
    public class AnonymousDoubleArrayToStringConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            double[][] tdRatioAtFdr = text.Split(';').Select(x => x.Split(',').Select(double.Parse).ToArray()).ToArray();

            return tdRatioAtFdr.Select(x => (x[0], x[1])).ToArray();
        }

        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value is (double, double )[] tdRatioAtFdr)
            {
                return string.Join(";", tdRatioAtFdr.Select(x => $"{x.Item1.Round(5)},{x.Item2.Round(5)}"));
            }
            else
            {
                throw new FormatException("TdRatioAtFdr must be a tuple array");
            }
        }
    }
}
