using Analyzer.SearchType;
using Analyzer.Util;
using Plotly.NET;
using Plotly.NET.ImageExport;
using Plotting.Util;
using ResultAnalyzerUtil;

namespace Analyzer.Plotting.Util
{
    public static class ExportLocators
    {
        #region Generic

        public static void SaveInCellLineOnly(this GenericChart.GenericChart chart, CellLineResults cellLine,
            string exportName, int? width = null, int? height = null)
        {
            var cellLineDirectory = cellLine.FigureDirectory;
            var outPath = Path.Combine(cellLineDirectory, exportName);
            TryAllTheExports(chart, outPath, width, height);
        }

        public static void SaveInCellLineOnly(this GenericChart.GenericChart chart, SingleRunResults runResult,
            string exportName, int? width = null, int? height = null)
        {
            var cellLineDirectory = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(runResult.DirectoryPath)),
                "Figures");
            var outPath = Path.Combine(cellLineDirectory, exportName);
            TryAllTheExports(chart, outPath, width, height);
        }

        public static void SaveInRunResultOnly(this GenericChart.GenericChart chart, SingleRunResults runResult,
            string exportName, int? width = null, int? height = null)
        {
            var runResultDirectory = runResult.FigureDirectory;
            var outPath = Path.Combine(runResultDirectory, exportName);
            TryAllTheExports(chart, outPath, width, height);
        }

        public static void SaveInAllResultsOnly(this GenericChart.GenericChart chart, AllResults allResults,
            string exportName, int? width = null, int? height = null)
        {
            var allResultsDirectory = allResults.GetChimeraPaperFigureDirectory();
            var outPath = Path.Combine(allResultsDirectory, exportName);
            TryAllTheExports(chart, outPath, width, height);
        }

        public static void SaveInAllResultsOnly(this GenericChart.GenericChart chart, CellLineResults cellLine,
            string exportName, int? width = null, int? height = null)
        {
            var allResultsDirectory = Path.GetDirectoryName(cellLine.DirectoryPath).GetDirectories()
                .First(p => p.Contains("Figures"));
            var outPath = Path.Combine(allResultsDirectory, exportName);
            TryAllTheExports(chart, outPath, width, height);
        }

        public static void TryAllTheExports(GenericChart.GenericChart chart, string outPath, int? width, int? height)
        {
            try
            {
                chart.SavePNG(outPath, null, width, height);
            }
            catch
            {
                try
                {
                    chart.SaveJPG(outPath, null, width, height);
                }
                catch
                {
                    try
                    {
                        chart.SaveSVG(outPath, null, width, height);
                    }
                    catch
                    {
                        try
                        {
                            Plotly.NET.CSharp.GenericChartExtensions.SaveHtml(chart, outPath, false);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Idk man, we tried them all to {outPath}: {e}");
                        }
                    }
                }
            }
        }

        #endregion

        #region ChimeraPaper

        public static void SaveInMan11Only(this GenericChart.GenericChart chart, CellLineResults cellLine,
            string exportName, int? width = null, int? height = null)
        {
            var mann11Directory = cellLine.GetChimeraPaperFigureDirectory();
            chart.SavePNG(Path.Combine(mann11Directory, exportName), null, width, height);
        }

        public static void SaveInCellLineAndMann11Directories(this GenericChart.GenericChart chart, CellLineResults cellLine, string exportName,
            int? width = null, int? height = null)
        {
            chart.SaveInCellLineOnly(cellLine, exportName, width, height);
            chart.SaveInMan11Only(cellLine, exportName, width, height);
        }

        public static string GetChimeraPaperFigureDirectory(this AllResults allResults)
        {
            var directory = Path.Combine(allResults.DirectoryPath, "Figures");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            return directory;
        }

        public static string GetChimeraPaperFigureDirectory(this CellLineResults cellLine)
        {
            string directory = cellLine.DirectoryPath.Contains("PEPTesting") ?
                Path.Combine(cellLine.DirectoryPath, "Figures")
                : Path.Combine(Path.GetDirectoryName(cellLine.DirectoryPath)!, "Figures");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            return directory;
        }

        public static string GetChimeraPaperFigureDirectory(this MetaMorpheusResult result)
        {
            string directory = result.DirectoryPath.Contains("PEPTesting") ?
                Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(result.DirectoryPath)), "Figures")
                : Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(result.DirectoryPath)))!, "Figures");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            return directory;
        }

        #endregion


    }
}
