using NUnit.Framework;
using RadicalFragmentation.Processing;
using System.Collections.Generic;
using MzLibUtil;
using RadicalFragmentation;

namespace Test
{
    [TestFixture]
    public class MinFragmentMassesToDifferentiateTests
    {
        [Test]
        public void MinFragmentMassesToDifferentiate_IdenticalProteoforms_ReturnsNegativeOne()
        {
            var targetProteoform = new HashSet<double> { 100.0, 200.0, 300.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 200.0, 300.0 }, "SEQ1")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance);

            Assert.That(result, Is.EqualTo(-1));
        }

        [Test] // Peak at 200 is enough to distinguish it from both of the below
        public void MinFragmentMassesToDifferentiate_MultipleProteoforms_ReturnsCorrectCount()
        {
            var targetProteoform = new HashSet<double> { 100.0, 200.0, 300.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 150.0, 250.0, 350.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 100.0, 250.0, 350.0 }, "SEQ2")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance);

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void MinFragmentMassesToDifferentiate_UniqueFragment_ReturnsOne()
        {
            var targetProteoform = new HashSet<double> { 100.0, 200.0, 300.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 200.0, 350.0 }, "SEQ1")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance);

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void MinFragmentMassesToDifferentiate_NoUniqueFragment_NoGoodCombo_ReturnsNegativeOne()
        {
            var targetProteoform = new HashSet<double> { 100.0, 200.0, 300.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 200.0, 300.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 100.0, 200.0, 300.0 }, "SEQ2")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance);

            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void MinFragmentMassesToDifferentiate_NoUniqueFragment_OneGoodComboOfTwo_ReturnsTwo()
        {
            var targetProteoform = new HashSet<double> { 100.0, 200.0, 300.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 200.0, 350.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 100.0, 250.0, 300.0 }, "SEQ2")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance);

            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        public void MinFragmentMassesToDifferentiate_NoUniqueFragment_OneGoodComboOfThree_ReturnsThree()
        {
            var targetProteoform = new HashSet<double> { 100.0, 200.0, 300.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 200.0, 350.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 100.0, 250.0, 300.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(600.0, "P3", new List<double> { 150.0, 200.0, 300.0 }, "SEQ3"),
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance);

            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        public void MinFragmentMassesToDifferentiate_ComplexMixtureWithNoUniqueFragment_ReturnsMinusOne()
        {
            var targetProteoform = new HashSet<double> { 150.0, 250.0, 350.0, 450.0, 550.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 150.0, 250.0, 350.0, 450.0, 550.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 150.0, 250.0, 350.0, 450.0, 550.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(700.0, "P3", new List<double> { 150.0, 250.0, 350.0, 450.0, 550.0 }, "SEQ3"),
                new PrecursorFragmentMassSet(800.0, "P4", new List<double> { 150.0, 250.0, 350.0, 450.0, 550.0 }, "SEQ4"),
                new PrecursorFragmentMassSet(900.0, "P5", new List<double> { 150.0, 250.0, 350.0, 450.0, 550.0 }, "SEQ5")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance);

            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void MinFragmentMassesToDifferentiate_ComplexMixtureWithUniqueFragment_ReturnsOne()
        {
            var targetProteoform = new HashSet<double> { 100.0, 200.0, 300.0, 400.0, 500.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 150.0, 200.0, 350.0, 450.0, 550.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 150.0, 250.0, 350.0, 450.0, 550.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(700.0, "P3", new List<double> { 150.0, 250.0, 300.0, 450.0, 550.0 }, "SEQ3"),
                new PrecursorFragmentMassSet(800.0, "P4", new List<double> { 150.0, 200.0, 300.0, 400.0, 550.0 }, "SEQ4"),
                new PrecursorFragmentMassSet(900.0, "P5", new List<double> { 150.0, 200.0, 300.0, 400.0, 500.0 }, "SEQ5")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance);

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void MinFragmentMassesToDifferentiate_ComplexMixtureWithNoUniqueFragment_FrontModified_ReturnsTwo()
        {
            var targetProteoform = new HashSet<double> { 150.0, 250.0, 350.0, 450.0, 550.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 250.0, 350.0, 450.0, 550.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 150.0, 200.0, 350.0, 450.0, 550.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(700.0, "P3", new List<double> { 150.0, 200.0, 350.0, 450.0, 550.0 }, "SEQ3"),
                new PrecursorFragmentMassSet(800.0, "P4", new List<double> { 150.0, 200.0, 350.0, 450.0, 550.0 }, "SEQ4"),
                new PrecursorFragmentMassSet(900.0, "P5", new List<double> { 150.0, 200.0, 350.0, 450.0, 550.0 }, "SEQ5")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance);

            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        public void MinFragmentMassesToDifferentiate_ComplexMixtureWithNoUniqueFragment_ReturnsTwo()
        {
            var targetProteoform = new HashSet<double> { 150.0, 250.0, 350.0, 450.0, 550.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 150.0, 250.0, 350.0, 450.0, 500.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 150.0, 250.0, 350.0, 400.0, 550.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(700.0, "P3", new List<double> { 150.0, 250.0, 350.0, 400.0, 550.0 }, "SEQ3"),
                new PrecursorFragmentMassSet(800.0, "P4", new List<double> { 150.0, 250.0, 350.0, 400.0, 550.0 }, "SEQ4"),
                new PrecursorFragmentMassSet(900.0, "P5", new List<double> { 150.0, 250.0, 350.0, 400.0, 550.0 }, "SEQ5")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance);

            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        public void MinFragmentMassesToDifferentiate_ComplexMixtureWithNoUniqueFragment_FrontModified_ReturnsThree()
        {
            var targetProteoform = new HashSet<double> { 150.0, 250.0, 350.0, 450.0, 550.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 250.0, 350.0, 450.0, 550.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 100.0, 250.0, 300.0, 450.0, 550.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(700.0, "P3", new List<double> { 150.0, 250.0, 300.0, 450.0, 550.0 }, "SEQ3"),
                new PrecursorFragmentMassSet(800.0, "P4", new List<double> { 150.0, 200.0, 300.0, 450.0, 550.0 }, "SEQ4"),
                new PrecursorFragmentMassSet(900.0, "P5", new List<double> { 150.0, 200.0, 350.0, 450.0, 550.0 }, "SEQ5")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance);

            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        public void MinFragmentMassesToDifferentiate_ComplexMixtureWithNoUniqueFragment_ReturnsThree()
        {
            var targetProteoform = new HashSet<double> { 150.0, 250.0, 350.0, 450.0, 550.0 };
            var otherProteoforms = new List<PrecursorFragmentMassSet>
            {
                new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 150.0, 250.0, 350.0, 400.0, 550.0 }, "SEQ1"),
                new PrecursorFragmentMassSet(600.0, "P2", new List<double> { 150.0, 250.0, 350.0, 400.0, 550.0 }, "SEQ2"),
                new PrecursorFragmentMassSet(700.0, "P3", new List<double> { 150.0, 250.0, 300.0, 450.0, 550.0 }, "SEQ3"),
                new PrecursorFragmentMassSet(800.0, "P4", new List<double> { 150.0, 250.0, 350.0, 450.0, 500.0 }, "SEQ4"),
                new PrecursorFragmentMassSet(900.0, "P5", new List<double> { 150.0, 250.0, 350.0, 450.0, 500.0 }, "SEQ5")
            };
            var tolerance = new PpmTolerance(10);

            int result = RadicalFragmentationExplorer.MinFragmentMassesToDifferentiate(targetProteoform, otherProteoforms, tolerance);

            Assert.That(result, Is.EqualTo(3));
        }
    }

    [TestFixture]
    public class GroupByPrecursorMassTests
    {
        [Test]
        public void GroupByPrecursorMass_SingleEntry_ReturnsSingleGroup()
        {
            var precursorMassSet = new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 200.0, 300.0 }, "SEQ1");
            var tolerance = new PpmTolerance(10);
            var result = RadicalFragmentationExplorer.GroupByPrecursorMass(new List<PrecursorFragmentMassSet> { precursorMassSet }, tolerance);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Item1, Is.EqualTo(precursorMassSet));
            Assert.That(result[0].Item2.Count, Is.EqualTo(0));
        }

        [Test]
        public void GroupByPrecursorMass_MultipleEntriesWithinTolerance_ReturnsNonePerGroup()
        {
            var precursorMassSet1 = new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 200.0, 300.0 }, "SEQ1");
            var precursorMassSet2 = new PrecursorFragmentMassSet(505.0, "P2", new List<double> { 150.0, 250.0, 350.0 }, "SEQ2");
            var tolerance = new PpmTolerance(10);
            var result = RadicalFragmentationExplorer.GroupByPrecursorMass(new List<PrecursorFragmentMassSet> { precursorMassSet1, precursorMassSet2 }, tolerance);

            Assert.That(result.Count, Is.EqualTo(2));

            Assert.That(result[0].Item2.Count, Is.EqualTo(0));
            Assert.That(result[1].Item2.Count, Is.EqualTo(0));
        }


        [Test]
        public void GroupByPrecursorMass_MultipleEntriesWithinTolerance_ReturnsSinglePerGroup()
        {
            var precursorMassSet1 = new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 200.0, 300.0 }, "SEQ1");
            var precursorMassSet2 = new PrecursorFragmentMassSet(500.000001, "P2", new List<double> { 150.0, 250.0, 350.0 }, "SEQ2");
            var tolerance = new PpmTolerance(10);
            var result = RadicalFragmentationExplorer.GroupByPrecursorMass(new List<PrecursorFragmentMassSet> { precursorMassSet1, precursorMassSet2 }, tolerance);

            Assert.That(result.Count, Is.EqualTo(2));

            Assert.That(result[0].Item2.Count, Is.EqualTo(1));
            Assert.That(result[1].Item2.Count, Is.EqualTo(1));
        }

        [Test]
        public void GroupByPrecursorMass_AmbiguityLevelTwo_SkipsSameAccession()
        {
            var precursorMassSet1 = new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 200.0, 300.0 }, "SEQ1");
            var precursorMassSet2 = new PrecursorFragmentMassSet(500.000001, "P1", new List<double> { 150.0, 250.0, 350.0 }, "SEQ1");
            var tolerance = new PpmTolerance(10);
            var result = RadicalFragmentationExplorer.GroupByPrecursorMass(new List<PrecursorFragmentMassSet> { precursorMassSet1, precursorMassSet2 }, tolerance, 2);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Item2.Count, Is.EqualTo(0));
            Assert.That(result[1].Item2.Count, Is.EqualTo(0));
        }


        [Test]
        public void GroupByPrecursorMass_MultipleEntriesWithinTolerance_ReturnsMultiplePerGroup()
        {
            var precursorMassSet1 = new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 200.0, 300.0 }, "SEQ1");
            var precursorMassSet2 = new PrecursorFragmentMassSet(500.0, "P2", new List<double> { 150.0, 250.0, 350.0 }, "SEQ2");
            var precursorMassSet3 = new PrecursorFragmentMassSet(500.0, "P3", new List<double> { 150.0, 250.0, 350.0 }, "SEQ2");
            var tolerance = new PpmTolerance(10);
            var result = RadicalFragmentationExplorer.GroupByPrecursorMass(new List<PrecursorFragmentMassSet> { precursorMassSet1, precursorMassSet2, precursorMassSet3 }, tolerance);

            Assert.That(result.Count, Is.EqualTo(3));

            Assert.That(result[0].Item2.Count, Is.EqualTo(2));
            Assert.That(result[1].Item2.Count, Is.EqualTo(2));
        }
    }
}
