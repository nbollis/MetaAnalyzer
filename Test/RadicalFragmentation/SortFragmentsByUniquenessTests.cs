using RadicalFragmentation.Processing;
using MzLibUtil;
using RadicalFragmentation;

namespace Test
{
    [TestFixture]
    public class SortFragmentsByUniquenessTests
    {
        [Test]
        public void SortFragmentsByUniqueness_UniqueFragments_SortsCorrectly()
        {
            var targetProteoform = new List<double> { 100.0, 200.0, 300.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 150.0, 250.0, 300.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 100.0, 250.0, 300.0 }, "SEQ2")
            };
            var tolerance = new PpmTolerance(10);

            RadicalFragmentationExplorer.SortFragmentsByUniqueness(targetProteoform, otherProteoforms, tolerance);

            Assert.That(targetProteoform, Is.EqualTo(new List<double> { 200.0, 100.0, 300.0 }));
        }

        [Test]
        public void SortFragmentsByUniqueness_NoUniqueFragments_SortsCorrectly()
        {
            var targetProteoform = new List<double> { 100.0, 200.0, 300.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 200.0, 300.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 100.0, 200.0, 300.0 }, "SEQ2")
            };
            var tolerance = new PpmTolerance(10);

            RadicalFragmentationExplorer.SortFragmentsByUniqueness(targetProteoform, otherProteoforms, tolerance);

            Assert.That(targetProteoform, Is.EqualTo(new List<double> { 100.0, 200.0, 300.0 }));
        }

        [Test]
        public void SortFragmentsByUniqueness_MixedFragments_SortsCorrectly()
        {
            var targetProteoform = new List<double> { 100.0, 200.0, 300.0, 400.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 250.0, 350.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 200.0, 300.0, 400.0 }, "SEQ2")
            };
            var tolerance = new PpmTolerance(10);

            RadicalFragmentationExplorer.SortFragmentsByUniqueness(targetProteoform, otherProteoforms, tolerance);

            Assert.That(targetProteoform, Is.EqualTo(new List<double> { 100.0, 200.0, 300.0, 400.0 }));
        }
    }
}
