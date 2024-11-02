using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GradientDevelopment;

namespace Test
{
    internal class Gradient
    {

        static string GradientDevelopmentDirectory = @"B:\Users\Nic\RNA\FLuc\GradientDevelopment";

        [Test]
        public static void FuckThatRunThat()
        {
            var outPath = Path.Combine(GradientDevelopmentDirectory, "FirstGradientAnalysis.tsv");

            var results = new List<ExtractedInformation>();
            foreach (var runInformation in StoredInformation.RunInformationList)
            {
                results.Add(runInformation.GetExtractedRunInformation());
            }

            var resultFile = new ExtractedInformationFile(outPath) { Results = results };
            resultFile.WriteResults(outPath);
        }
    }
}
