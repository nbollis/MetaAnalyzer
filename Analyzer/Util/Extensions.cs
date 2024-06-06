using CsvHelper;
using Proteomics.PSM;

namespace Analyzer.Util
{
    public static class Extensions
    {
        public static bool IsDecoy(this PsmFromTsv psm) => psm.DecoyContamTarget == "D";

        public static bool ValidateMyColumn(this IReaderRow row)
        {
            // if I remove the HasHeaderRecord check here and set the CsvConfig HasHeaderRecord = false
            // the code all works I would have originally expected, e.g. header row gets ignored and all othe
            // rows are included.
            if (row.Configuration.HasHeaderRecord && row.Parser.Row == 1)
            {
                return true;
            }

            // Do other checks, for example:

            if (int.TryParse(row[0], out var _))
            {
                return true;
            }

            // Logging to objectForLogRef
            return false;
        }

        public static List<double> CalculateRollingAverage(this List<double> numbers, int windowSize)
        {
            var result = new List<double>();

            for (int i = 0; i < numbers.Count - windowSize + 1; i++)
            {
                var window = numbers.Skip(i).Take(windowSize);
                result.Add(window.Average());
            }

            return result;
        }

        public static List<double> CalculateRollingAverage(this List<int> numbers, int windowSize)
        {
            var result = new List<double>();

            for (int i = 0; i < numbers.Count - windowSize + 1; i++)
            {
                var window = numbers.Skip(i).Take(windowSize);
                result.Add(window.Average());
            }

            return result;
        }
    }
}
