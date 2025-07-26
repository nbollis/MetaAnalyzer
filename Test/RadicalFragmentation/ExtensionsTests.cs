using RadicalFragmentation;

namespace Test
{
    [TestFixture]
    public class ExtensionsTests
    {
        [Test]
        public void ToFragmentsNeededSummaryRecords_SingleRecord_ReturnsSingleSummary()
        {
            var explorer = new TestFragmentationExplorer("testPath", 2, "TestSpecies", int.MaxValue, 1, "", 1, 0.1)
            {
                MinFragmentNeededFile = new FragmentsToDistinguishFile
                {
                    Results = new List<FragmentsToDistinguishRecord>
                    {
                        new FragmentsToDistinguishRecord { AnalysisType = "Type1", Species = "Species1", AmbiguityLevel = 1, FragmentCountNeededToDifferentiate = 2 }
                    }
                },
            };

            var summary = explorer.ToFragmentsNeededSummaryRecords().ToList();

            Assert.That(summary.Count, Is.EqualTo(1));
            Assert.That(summary[0].FragmentsNeeded, Is.EqualTo(2));
            Assert.That(summary[0].Count, Is.EqualTo(1));
        }

        [Test]
        public void ToPrecursorCompetitionSummaryRecords_SingleRecord_ReturnsSingleSummary()
        {
            var explorer = new TestFragmentationExplorer("testPath", 2, "TestSpecies", int.MaxValue, 1, "", 1, 0.1)
            {
                MinFragmentNeededFile = new FragmentsToDistinguishFile
                {
                    Results = new List<FragmentsToDistinguishRecord>
                    {
                        new FragmentsToDistinguishRecord { AnalysisType = "Type1", Species = "Species1", AmbiguityLevel = 1, NumberInPrecursorGroup = 2 }
                    }
                },
            };

            var summary = explorer.ToPrecursorCompetitionSummaryRecords().ToList();

            Assert.That(summary.Count, Is.EqualTo(1));
            Assert.That(summary[0].PrecursorsInGroup, Is.EqualTo(2));
            Assert.That(summary[0].Count, Is.EqualTo(1));
        }

        [Test]
        public void ToPrecursorCompetitionSummaryRecords_ValidData_ReturnsCorrectSummary()
        {
            var explorer = new TestFragmentationExplorer("testPath", 2, "TestSpecies", int.MaxValue, 1, "", 1, 0.1)
            {
                MinFragmentNeededFile = new FragmentsToDistinguishFile
                {
                    Results = new List<FragmentsToDistinguishRecord>
                    {
                        new FragmentsToDistinguishRecord { AnalysisType = "Type1", Species = "Species1", AmbiguityLevel = 1, NumberInPrecursorGroup = 2 },
                        new FragmentsToDistinguishRecord { AnalysisType = "Type1", Species = "Species1", AmbiguityLevel = 1, NumberInPrecursorGroup = 3 },
                        new FragmentsToDistinguishRecord { AnalysisType = "Type1", Species = "Species1", AmbiguityLevel = 1, NumberInPrecursorGroup = 2 }
                    }
                },
            };

            var summary = explorer.ToPrecursorCompetitionSummaryRecords().ToList();

            Assert.That(summary.Count, Is.EqualTo(2));
            Assert.That(summary[0].PrecursorsInGroup, Is.EqualTo(2));
            Assert.That(summary[0].Count, Is.EqualTo(2));
            Assert.That(summary[1].PrecursorsInGroup, Is.EqualTo(3));
            Assert.That(summary[1].Count, Is.EqualTo(1));
        }
    }
}
