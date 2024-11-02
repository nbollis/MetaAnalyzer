using Readers;
using ThermoFisher.CommonCore.Data.Business;
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

        public RunInformation(string dataFilePath, string gradientPath, string searchResultPath, string mobilePhaseB)
        {
            DataFilePath = dataFilePath;
            GradientPath = gradientPath;
            SearchResultPath = searchResultPath;
            MobilePhaseB = mobilePhaseB;
            DataFileName = Path.GetFileNameWithoutExtension(dataFilePath);
        }

        public ExtractedInformation GetExtractedRunInformation()
        {
            var grad = new Gradient(GradientPath).GetGradient();
            var dataFile = MsDataFileReader.GetDataFile(DataFilePath).LoadAllStaticData();
            var tic = dataFile.Scans
                .Where(p => p.MsnOrder == 1)
                .Select(p => (p.RetentionTime, p.TotalIonCurrent))
                .ToArray();
            double minRt = 0;
            double maxRt = tic.Max(p => p.RetentionTime);

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

            var allOsms = osmInfo.ToArray();
            var filteredOsms = osmInfo.Where(p => p.Q <= 0.05).ToArray();

            // Interpolation
            var xValues = Enumerable.Range(0, (int)((maxRt - minRt) / 0.1) + 1).Select(i => minRt + i * 0.1).ToArray();
            var interpolatedTic = Interpolate(tic, xValues);
            var interpolatedGrad = Interpolate(grad, xValues);
            var interpolatedAllOsms = Interpolate(allOsms, xValues);
            var interpolatedFilteredOsms = Interpolate(filteredOsms, xValues);

            var info = new ExtractedInformation(DataFileName, MobilePhaseB, interpolatedTic, interpolatedGrad, interpolatedAllOsms, interpolatedFilteredOsms);
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
