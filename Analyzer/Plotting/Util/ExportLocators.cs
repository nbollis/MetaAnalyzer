using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.SearchType;
using Plotly.NET;
using Plotly.NET.ImageExport;

namespace Analyzer.Plotting.Util
{
    public static class ExportLocators
    {
        #region Generic

        public static void SaveInCellLineOnly(this GenericChart.GenericChart chart, CellLineResults cellLine,
            string exportName, int? width = null, int? height = null)
        {
            var cellLineDirectory = cellLine.FigureDirectory;
            chart.SavePNG(Path.Combine(cellLineDirectory, exportName), null, width, height);
        }

        public static void SaveInRunResultOnly(this GenericChart.GenericChart chart, BulkResult runResult,
            string exportName, int? width = null, int? height = null)
        {
            var runResultDirectory = runResult.FigureDirectory;
            chart.SavePNG(Path.Combine(runResultDirectory, exportName), null, width, height);
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
