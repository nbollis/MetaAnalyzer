using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.SearchType;
using Analyzer.Util;

namespace Test
{
    internal class PaperNumberGetter
    {
        public static List<string> AllPaperNumbers { get; private set; } = new();
        public static string PaperNumberOutFilePath =>
            @"C:\Users\Nic\OneDrive - UW-Madison\ChimeraPaper\PaperNumbers.txt";

        #region Input Directories

        public static string Mann11FdrRunPath => 
            MiscTaskRunner.Man11FDRRunPath;

        public static string Mann11AllRunPath => 
            MiscTaskRunner.Man11AllResultsPath;

        public static string TdJurkatFdrRunPath => 
            MiscTaskRunner.TopDownJurkatFDRRunPath;

        public static string TdJurkatAllRunPath => 
            MiscTaskRunner.TopDownDirectoryPath;


        #endregion

        [Test]
        public static void GetPaperNumbers()
        {
            GetFdrRunsPaperNumbers();
            GetFraggerComparisonNumbers();
            GetInternalMetaMorpheusComparisonNumbers();

            using var sw = new StreamWriter(File.Create(PaperNumberOutFilePath));
            AllPaperNumbers.ForEach(sw.WriteLine);
        }

        private static void GetFdrRunsPaperNumbers()
        {
            var buFdrResults = new MetaMorpheusResult(Mann11FdrRunPath);
            var buValue = buFdrResults.GetFractionContainingSinglePrecursorLeadingToConfidentId();
            AllPaperNumbers.Add($"Bottom Up\t% Scans Containing Single Identified Feature\t{buValue}");



            var tdFdrResults = new MetaMorpheusResult(TdJurkatFdrRunPath);
            var tdValue = tdFdrResults.GetFractionContainingSinglePrecursorLeadingToConfidentId();
            AllPaperNumbers.Add($"Top Down\t% Scans Containing Single Identified Feature\t{tdValue}");
        }

        private static void GetInternalMetaMorpheusComparisonNumbers()
        {

        }

        private static void GetFraggerComparisonNumbers()
        {

        }
        

    }
}
