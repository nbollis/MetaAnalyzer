using MzLibUtil;
using RadicalFragmentation.Processing;
using RadicalFragmentation;

namespace Test
{
    [TestFixture]
    public class BinarySearchTests
    {
        [Test]
        public void BinarySearch_TargetMassFound_ReturnsCorrectIndex()
        {
            var orderedResults = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(100.0, "P1", new List<double> { 100.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(200.0, "P2", new List<double> { 200.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(300.0, "P3", new List<double> { 300.0 }, "SEQ3")
            };
            var tolerance = new PpmTolerance(10);
            int result = RadicalFragmentationExplorer.BinarySearch(orderedResults, 200.0, tolerance, true);
            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void BinarySearch_TargetMassNotFound_ReturnsNegativeOne()
        {
            var orderedResults = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(100.0, "P1", new List<double> { 100.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(200.0, "P2", new List<double> { 200.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(300.0, "P3", new List<double> { 300.0 }, "SEQ3")
            };
            var tolerance = new PpmTolerance(10);
            int result = RadicalFragmentationExplorer.BinarySearch(orderedResults, 400.0, tolerance, true);
            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void BinarySearch_FindFirst_ReturnsFirstOccurrence()
        {
            var orderedResults = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(100.0, "P1", new List<double> { 100.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(200.0, "P2", new List<double> { 200.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(200.0, "P3", new List<double> { 200.0 }, "SEQ3"),
                new PrecursorFragmentMassSet(300.0, "P4", new List<double> { 300.0 }, "SEQ4")
            };
            var tolerance = new PpmTolerance(10);
            int result = RadicalFragmentationExplorer.BinarySearch(orderedResults, 200.0, tolerance, true);
            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void BinarySearch_FindLast_ReturnsLastOccurrence()
        {
            var orderedResults = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(100.0, "P1", new List<double> { 100.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(200.0, "P2", new List<double> { 200.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(200.0, "P3", new List<double> { 200.0 }, "SEQ3"),
                new PrecursorFragmentMassSet(300.0, "P4", new List<double> { 300.0 }, "SEQ4")
            };
            var tolerance = new PpmTolerance(10);
            int result = RadicalFragmentationExplorer.BinarySearch(orderedResults, 200.0, tolerance, false);
            Assert.That(result, Is.EqualTo(2));
        }
    }
}

