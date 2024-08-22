using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.FileTypes.External;
using Readers;

namespace Test
{
    public class TestProteomeDiscovererPsmRecord
    {
        public static string PsmFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData",
            "ProteomeDiscoverer_TestData_Psms.txt");
        public static string PeptideFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData",
                           "ProteomeDiscoverer_TestData_PeptideGroups.txt");

        [Test]
        public static void TestFullSequenceConversion()
        {
            var psms = new ProteomeDiscovererPsmFile(PsmFilePath);
            psms.LoadResults();
            


        }
    }
}
