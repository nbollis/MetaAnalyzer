using MathNet.Numerics;
using Readers;
using Range = System.Range;
using StreamReader = System.IO.StreamReader;

namespace GradientDevelopment
{
    public class RunInformation
    {
        internal string MobilePhaseB { get; init; }
        internal string DataFileName { get; init; }
        internal string DataFilePath { get; init; }
        internal string GradientPath { get; init; }
        internal string SearchResultPath { get; init; }
        internal Range? MinMaxToDisplay { get; init; }

        public RunInformation(string dataFilePath, string gradientPath, string searchResultPath, string mobilePhaseB, Range? minMax = null)
        {
            DataFilePath = dataFilePath;
            GradientPath = gradientPath;
            SearchResultPath = searchResultPath;
            MobilePhaseB = mobilePhaseB;
            DataFileName = Path.GetFileNameWithoutExtension(dataFilePath);
            MinMaxToDisplay = minMax;
        }

        public ExtractedInformation GetExtractedRunInformation()
        {
            var resultsTxtPath = Directory.GetParent(SearchResultPath)!.GetFiles( "results.txt").First();
            
            // Result file lines
            var lines = File.ReadAllLines(resultsTxtPath.FullName);
            var relevant = lines.Where(p => p.Contains(DataFileName)).ToArray();
            var ms2Scans = relevant.First(p => p.Contains("MS2 spectra in"));
            var precursors = relevant.First(p => p.Contains("Precursors fragmented"));
            var osmLine = relevant.First(p => p.Contains("target PSMs"));
            var oligLine = relevant.First(p => p.Contains("oligos with q"));
            var ms2ScanCount = int.Parse(ms2Scans.Split(':')[1].Trim());
            var precursorCount = int.Parse(precursors.Split(':')[1].Trim());
            var osmCount = int.Parse(osmLine.Split(':')[1].Trim());
            var oligoCount = int.Parse(oligLine.Split(':')[1].Trim());

            // Gradient
            var grad = new Gradient(GradientPath).GetGradient();
            var dataFile = MsDataFileReader.GetDataFile(DataFilePath).LoadAllStaticData();
            var tic = dataFile.Scans
                .Where(p => p.MsnOrder == 1)
                .Select(p => (p.RetentionTime, p.TotalIonCurrent))
                .ToArray();


            // Spectral Matches
            var osmInfo = new List<(double Rt, double Q)>();
            using (var sw = new StreamReader(File.OpenRead(SearchResultPath)))
            {
                var header = sw.ReadLine();
                if (header == null)
                    throw new InvalidOperationException("Search result file is empty or invalid.");

                var headerSplit = header.Split('\t');
                var qValueIndex = Array.IndexOf(headerSplit, "QValue");
                var decoyIndex = Array.IndexOf(headerSplit, "Decoy");
                var rtIndex = Array.IndexOf(headerSplit, "Scan Retention Time");

                while (!sw.EndOfStream)
                {
                    var line = sw.ReadLine();

                    if (line is null || !line.Contains(DataFileName))
                        continue;

                    var values = line.Split('\t');
                    if (values[decoyIndex] == "Y")
                        continue;

                    osmInfo.Add((double.Parse(values[rtIndex]), double.Parse(values[qValueIndex])));
                }
            }

            var allOsms = osmInfo.GroupBy(p => p.Rt.Round(2))
                .Select(p => (p.Key, (double)p.Count()))
                .OrderBy(p => p.Key)
                .ToArray();
            var filteredOsms = osmInfo.Where(p => p.Q <= 0.05)
                .GroupBy(p => p.Rt.Round(2))
                .OrderBy(p => p.Key)
                .Select(p => (p.Key, (double)p.Count()))
                .ToArray();

            var gradName = Path.GetFileNameWithoutExtension(GradientPath);
            var info = new ExtractedInformation(DataFileName, MobilePhaseB, gradName, tic, grad, allOsms, filteredOsms, ms2ScanCount, precursorCount, osmCount, oligoCount);
            return info;
        }

        private (double, double)[] Interpolate((double X, double Y)[] data, double[] xValues)
        {
            var interpolated = new List<(double, double)>();
            for (int i = 0; i < xValues.Length; i++)
            {
                double x = xValues[i];
                var lower = data.LastOrDefault(p => p.X <= x);
                var upper = data.FirstOrDefault(p => p.X >= x);

                if (lower.Equals(default((double, double))) || upper.Equals(default((double, double))))
                {
                    interpolated.Add((x, 0));
                }
                else if (lower.Equals(upper))
                {
                    interpolated.Add((x, lower.Y));
                }
                else
                {
                    double slope = (upper.Y - lower.Y) / (upper.X - lower.X);
                    double y = lower.Y + slope * (x - lower.X);
                    interpolated.Add((x, y));
                }
            }
            return interpolated.ToArray();
        }
    }
}
