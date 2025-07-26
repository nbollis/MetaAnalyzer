using RadicalFragmentation.Processing;
using MzLibUtil;
using RadicalFragmentation;

namespace Test
{
    [TestFixture]
    public class MinFragmentMassesToDifferentiateTests
    {
        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void MinFragmentMassesToDifferentiate_IdenticalProteoforms_ReturnsNegativeOne(bool greedAndResort)
        {
            var targetProteoform = new List<double> { 100.0, 200.0, 300.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 200.0, 300.0 }, "SEQ1")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance, greedAndResort, greedAndResort);

            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void MinFragmentMassesToDifferentiate_MultipleProteoforms_ReturnsCorrectCount(bool greedAndResort)
        {
            var targetProteoform = new List<double> { 100.0, 200.0, 300.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 150.0, 250.0, 350.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 100.0, 250.0, 350.0 }, "SEQ2")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance, greedAndResort, greedAndResort);

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void MinFragmentMassesToDifferentiate_UniqueFragment_ReturnsOne(bool greedAndResort)
        {
            var targetProteoform = new List<double> { 100.0, 200.0, 300.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 200.0, 350.0 }, "SEQ1")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance, greedAndResort, greedAndResort);

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void MinFragmentMassesToDifferentiate_NoUniqueFragment_NoGoodCombo_ReturnsNegativeOne(bool greedAndResort)
        {
            var targetProteoform = new List<double> { 100.0, 200.0, 300.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 200.0, 300.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 100.0, 200.0, 300.0 }, "SEQ2")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance, greedAndResort, greedAndResort);

            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void MinFragmentMassesToDifferentiate_NoUniqueFragment_OneGoodComboOfTwo_ReturnsTwo(bool greedAndResort)
        {
            var targetProteoform = new List<double> { 100.0, 200.0, 300.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 200.0, 350.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 100.0, 250.0, 300.0 }, "SEQ2")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance, greedAndResort, greedAndResort);

            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void MinFragmentMassesToDifferentiate_NoUniqueFragment_OneGoodComboOfThree_ReturnsThree(bool greedAndResort)
        {
            var targetProteoform = new List<double> { 100.0, 200.0, 300.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 200.0, 350.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 100.0, 250.0, 300.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(600.0, "P3", new List<double> { 150.0, 200.0, 300.0 }, "SEQ3"),
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance, greedAndResort, greedAndResort);

            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void MinFragmentMassesToDifferentiate_ComplexMixtureWithNoUniqueFragment_ReturnsMinusOne(bool greedAndResort)
        {
            var targetProteoform = new List<double> { 150.0, 250.0, 350.0, 450.0, 550.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 150.0, 250.0, 350.0, 450.0, 550.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 150.0, 250.0, 350.0, 450.0, 550.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(700.0, "P3", new List<double> { 150.0, 250.0, 350.0, 450.0, 550.0 }, "SEQ3"),
                new PrecursorFragmentMassSet(800.0, "P4", new List<double> { 150.0, 250.0, 350.0, 450.0, 550.0 }, "SEQ4"),
                new PrecursorFragmentMassSet(900.0, "P5", new List<double> { 150.0, 250.0, 350.0, 450.0, 550.0 }, "SEQ5")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance, greedAndResort, greedAndResort);

            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void MinFragmentMassesToDifferentiate_ComplexMixtureWithUniqueFragment_ReturnsOne(bool greedAndResort)
        {
            var targetProteoform = new List<double> { 100.0, 200.0, 300.0, 400.0, 500.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 150.0, 200.0, 350.0, 450.0, 550.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 150.0, 250.0, 350.0, 450.0, 550.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(700.0, "P3", new List<double> { 150.0, 250.0, 300.0, 450.0, 550.0 }, "SEQ3"),
                new PrecursorFragmentMassSet(800.0, "P4", new List<double> { 150.0, 200.0, 300.0, 400.0, 550.0 }, "SEQ4"),
                new PrecursorFragmentMassSet(900.0, "P5", new List<double> { 150.0, 200.0, 300.0, 400.0, 500.0 }, "SEQ5")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance, greedAndResort, greedAndResort);

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void MinFragmentMassesToDifferentiate_ComplexMixtureWithNoUniqueFragment_FrontModified_ReturnsTwo(bool greedAndResort)
        {
            var targetProteoform = new List<double> { 150.0, 250.0, 350.0, 450.0, 550.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 250.0, 350.0, 450.0, 550.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 150.0, 200.0, 350.0, 450.0, 550.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(700.0, "P3", new List<double> { 150.0, 200.0, 350.0, 450.0, 550.0 }, "SEQ3"),
                new PrecursorFragmentMassSet(800.0, "P4", new List<double> { 150.0, 200.0, 350.0, 450.0, 550.0 }, "SEQ4"),
                new PrecursorFragmentMassSet(900.0, "P5", new List<double> { 150.0, 200.0, 350.0, 450.0, 550.0 }, "SEQ5")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance, greedAndResort, greedAndResort);

            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void MinFragmentMassesToDifferentiate_ComplexMixtureWithNoUniqueFragment_ReturnsTwo(bool greedAndResort)
        {
            var targetProteoform = new List<double> { 150.0, 250.0, 350.0, 450.0, 550.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 150.0, 250.0, 350.0, 450.0, 500.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 150.0, 250.0, 350.0, 400.0, 550.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(700.0, "P3", new List<double> { 150.0, 250.0, 350.0, 400.0, 550.0 }, "SEQ3"),
                new PrecursorFragmentMassSet(800.0, "P4", new List<double> { 150.0, 250.0, 350.0, 400.0, 550.0 }, "SEQ4"),
                new PrecursorFragmentMassSet(900.0, "P5", new List<double> { 150.0, 250.0, 350.0, 400.0, 550.0 }, "SEQ5")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance, greedAndResort, greedAndResort);

            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void MinFragmentMassesToDifferentiate_ComplexMixtureWithNoUniqueFragment_FrontModified_ReturnsThree(bool greedAndResort)
        {
            var targetProteoform = new List<double> { 150.0, 250.0, 350.0, 450.0, 550.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 250.0, 350.0, 450.0, 550.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 100.0, 250.0, 300.0, 450.0, 550.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(700.0, "P3", new List<double> { 150.0, 250.0, 300.0, 450.0, 550.0 }, "SEQ3"),
                new PrecursorFragmentMassSet(800.0, "P4", new List<double> { 150.0, 200.0, 300.0, 450.0, 550.0 }, "SEQ4"),
                new PrecursorFragmentMassSet(900.0, "P5", new List<double> { 150.0, 200.0, 350.0, 450.0, 550.0 }, "SEQ5")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance, greedAndResort, greedAndResort);

            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void MinFragmentMassesToDifferentiate_ComplexMixtureWithNoUniqueFragment_ReturnsThree(bool greedAndResort)
        {
            var targetProteoform = new List<double> { 150.0, 250.0, 350.0, 450.0, 550.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 150.0, 250.0, 350.0, 400.0, 550.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 150.0, 250.0, 350.0, 400.0, 550.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(700.0, "P3", new List<double> { 150.0, 250.0, 300.0, 450.0, 550.0 }, "SEQ3"),
                new PrecursorFragmentMassSet(800.0, "P4", new List<double> { 150.0, 250.0, 350.0, 450.0, 500.0 }, "SEQ4"),
                new PrecursorFragmentMassSet(900.0, "P5", new List<double> { 150.0, 250.0, 350.0, 450.0, 500.0 }, "SEQ5")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance, greedAndResort, greedAndResort);

            Assert.That(result, Is.EqualTo(3));
        }
    }
}
