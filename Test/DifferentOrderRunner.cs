using Analyzer.SearchType;
using Easy.Common.Extensions;
using static NUnit.Framework.Assert;
using Directory = System.IO.Directory;

namespace Test
{
    internal class DifferentOrderRunner
    {
        internal static string DirectoryPath = @"B:\Users\Nic\Chimeras\TopDown_Analysis\JurkatDifferentTaskOrder";

        private static AllResults? _allResults;

        internal static AllResults AllResults
        {
            get
            {
                List<CellLineResults> differentRunResults = new();
                var searchResultsDir = Directory.GetDirectories(DirectoryPath).First(p => p.Contains("Search"));
                foreach (var specificRunDirectory in Directory.GetDirectories(searchResultsDir))
                {
                    var firstSearch = Directory.GetDirectories(specificRunDirectory).FirstOrDefault(p => p.Contains("Task1-Ini"));
                    var postGptmd = Directory.GetDirectories(specificRunDirectory).FirstOrDefault(p => p.Contains("Task3-PostG"));

                    if (firstSearch is null || postGptmd is null)
                        continue;

                    var first = new MetaMorpheusResult(firstSearch);
                    var second = new MetaMorpheusResult(postGptmd);
                    differentRunResults.Add(new CellLineResults(specificRunDirectory, new List<SingleRunResults>() { first, second }));
                }

                var results = new AllResults(DirectoryPath, differentRunResults);
                return _allResults ??= results;
            }
        }

        [Test]
        public static void EnsureCorrectDatabaseWasUsed()
        {
            foreach (var cellLine in AllResults)
            {
                bool isXml = cellLine.CellLine.Contains("xml");
                var proseFile = Directory.GetFiles(cellLine.First().DirectoryPath, "*Prose.txt").First();
                var lines = File.ReadAllLines(proseFile);
                var dbIndex = lines.IndexOf("Databases:");
                var db = lines[dbIndex + 1].Split(' ').Select(p => p.Trim()).ToArray();
                var fastaCount = db.Count(p => p.EndsWith(".fasta"));
                var xmlCount = db.Count(p => p.EndsWith(".xml"));
                var mspCout = db.Count(p => p.EndsWith(".msp"));
                if (isXml)
                {
                    That(xmlCount, Is.EqualTo(1));
                    That(fastaCount, Is.EqualTo(0));
                }
                else
                {
                    That(fastaCount, Is.EqualTo(1));
                    That(xmlCount, Is.EqualTo(0));
                }
                That(mspCout, Is.EqualTo(1));
            }
        }
    }
}
