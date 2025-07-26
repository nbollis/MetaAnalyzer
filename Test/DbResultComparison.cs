using TaskLayer;

namespace Test
{
    [TestFixture]
    public static class DbResultComparison
    {
        public static string AllDirPath = @"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask";
        public static string OneDirPath = @"B:\RawSpectraFiles\JurkatTopDown\107_CalibratedAveraged_Rep2\Task1-AveragingTask\GptmdSearch_111";

        public static IEnumerable<string> GetAllRelevantDirectories() => Directory.GetDirectories(AllDirPath)
            .Where(p => p.Contains("GptmdSearch"));

        [Test]
        public static void CompareDbResults()
        {
            var gptmdSearch = new GptmdSearchResult(OneDirPath);

            gptmdSearch.ParseModifications();
        }

        [Test]
        public static void RunAll()
        {
            foreach (var dir in GetAllRelevantDirectories())
            {
                var gptmdSearch = new GptmdSearchResult(dir);
                var file = gptmdSearch.SearchTaskResults.GetBulkResultCountComparisonFile();
            }
        }
    }
}
