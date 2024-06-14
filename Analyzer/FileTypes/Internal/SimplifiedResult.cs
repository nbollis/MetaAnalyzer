using Analyzer.Util;
using CsvHelper.Configuration;
using System.Globalization;

namespace Analyzer.FileTypes.Internal
{
    public class SimplifiedResultRecord
    {
        public static CsvConfiguration CsvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true,
        };

        // identifiers
        public string Dataset { get; set; }
        public string FileName { get; set; }
        public string Condition { get; set; }
        public int Ms2ScanNumber { get; set; }
        public ResultType Type { get; set; }

        // results
        public string BaseSequence { get; set; }
        public string FullSequence { get; set; }
        public int IdsPerSpectrum { get; set; }

    }
}
