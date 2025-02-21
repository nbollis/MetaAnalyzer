using GradientDevelopment.Temporary;
using MassSpectrometry;
using MathNet.Numerics;
using MzLibUtil;
using Readers;
using Ms1FeatureFile = GradientDevelopment.Temporary.Ms1FeatureFile;
using Range = System.Range;
using SpectrumMatchTsvReader = GradientDevelopment.Temporary.SpectrumMatchTsvReader;
using StreamReader = System.IO.StreamReader;

namespace GradientDevelopment
{
    public class RunInformation
    {
        private static double qValueCutoff = 0.05;

        private string _featureFilePath;
        private Ms1FeatureFile? _featureFile;
        private string _gradientFilePath;
        private Gradient _gradient = null!;
        private string _dataFilePath;
        private MsDataFile msDataFile = null!;
        private string _osmPath;
        private List<OsmFromTsv> osmFromTsvs = null!;

        internal string MobilePhaseB { get; init; }
        public string DataFileName { get; init; }

        public Gradient Gradient => _gradient ??= new Gradient(_gradientFilePath);
        public List<OsmFromTsv> OsmFromTsv => osmFromTsvs ??= SpectrumMatchTsvReader.ReadOsmTsv(_osmPath, out _);
        public MsDataFile MsDataFile => msDataFile ??= MsDataFileReader.GetDataFile(_dataFilePath).LoadAllStaticData();
        public Ms1FeatureFile Ms1FeatureFile => _featureFile ??= new Ms1FeatureFile(_featureFilePath);


        public string ParentDirectory { get; init; }
        internal DoubleRange? MinMaxToDisplay { get; init; }

        public RunInformation(string dataFilePath, string gradientPath, string searchResultPath, string featurePath, string mobilePhaseB, DoubleRange? minMax = null)
        {
            _dataFilePath = dataFilePath;
            _gradientFilePath = gradientPath;
            _osmPath = searchResultPath;

            MobilePhaseB = mobilePhaseB;
            DataFileName = Path.GetFileNameWithoutExtension(dataFilePath);
            MinMaxToDisplay = minMax;

            // Assumption given folder structure
            ParentDirectory = Path.GetDirectoryName(dataFilePath)!;
        }

        public ExtractedInformation GetExtractedRunInformation()
        {
            var resultsTxtPath = Directory.GetParent(_osmPath)!.GetFiles( "results.txt").First();
            
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
            var grad = Gradient.GetGradient();
            var tic = MsDataFile.Scans
                .Where(p => p.MsnOrder == 1)
                .Select(p => (p.RetentionTime, p.TotalIonCurrent))
                .ToArray();


            // Spectral Matches
            var osmInfo = new List<(double Rt, double Q)>();
            using (var sw = new StreamReader(File.OpenRead(_osmPath)))
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

            var gradName = Path.GetFileNameWithoutExtension(_gradientFilePath);
            var info = new ExtractedInformation(DataFileName, MobilePhaseB, gradName, tic, grad, 
                allOsms, filteredOsms, ms2ScanCount, precursorCount, osmCount, 
                oligoCount, MinMaxToDisplay?.Minimum, MinMaxToDisplay?.Maximum);
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


        public CytosineInformation ExtractMethylationInformation() 
        {
            int totalCytosinesTargets = 0;
            int methylatedCytosinesTargets = 0;
            int unmethylatedCytosinesTargets = 0;

            int totalCytosinesDecoys = 0;
            int methylatedCytosinesDecoys = 0;
            int unmethylatedCytosinesDecoys = 0;


            foreach (var osmToCheck in OsmFromTsv.Where(p => p.QValue <= qValueCutoff))
            {
                string sequence = osmToCheck.FullSequence;
                bool inBracket = false;
                bool isDecoy = osmToCheck.DecoyContamTarget == "D";

                for (int i = 0; i < sequence.Length; i++)
                {
                    if (sequence[i] == '[')
                    {
                        inBracket = true;
                    }
                    else if (sequence[i] == ']')
                    {
                        inBracket = false;
                    }
                    else if (sequence[i] == 'C')
                    {
                        if (isDecoy)
                            totalCytosinesDecoys++;
                        else
                            totalCytosinesTargets++;

                        if (inBracket)
                        {
                            if (isDecoy)
                                methylatedCytosinesDecoys++;
                            else
                                methylatedCytosinesTargets++;
                        }
                        else
                        {
                            if (isDecoy)
                                unmethylatedCytosinesDecoys++;
                            else
                                unmethylatedCytosinesTargets++;
                        }
                    }
                }
            }

            return new CytosineInformation(DataFileName, totalCytosinesTargets, totalCytosinesDecoys,
                methylatedCytosinesTargets, methylatedCytosinesDecoys,
                unmethylatedCytosinesTargets, unmethylatedCytosinesDecoys);
        }
    }
}
