using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.FileTypes.Internal
{
    public class MaximumChimeraEstimation
    {
        public static CsvConfiguration CsvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true,
        };

        public string FileName { get; set; }
        public string CellLine { get; set; }
        public int Ms2ScanNumber { get; set; }

        public int PossibleFeatureCount { get; set; }
        public int PsmCount_MetaMorpheus { get; set; }
        public int PsmCount_Fragger { get; set; }
        
    }
}
