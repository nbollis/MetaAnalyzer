using NUnit.Framework;
using RadicalFragmentation.Util;
using System;
using Chemistry;

namespace Test
{
    [TestFixture]
    public class MissedMonoisotopicToleranceTests
    {
        [Test]
        public void Within_ExactMatch_ReturnsTrue()
        {
            var tolerance = new MissedMonoisotopicTolerance(10);
            Assert.IsTrue(tolerance.Within(100, 100));
        }

        [Test]
        public void Within_WithinTolerance_ReturnsTrue()
        {
            var tolerance = new MissedMonoisotopicTolerance(10);
            Assert.IsTrue(tolerance.Within(100.0000055, 100));
        }

        [Test]
        public void Within_OutsideTolerance_ReturnsFalse()
        {
            var tolerance = new MissedMonoisotopicTolerance(10);
            Assert.IsFalse(tolerance.Within(111, 100));
        }

        [Test]
        public void Within_WithMissedMonoisotopics_ReturnsTrue()
        {
            var tolerance = new MissedMonoisotopicTolerance(10, 1);
            Assert.IsTrue(tolerance.Within(100 + Constants.C13MinusC12, 101.0033));
        }

        [Test]
        public void Within_WithMultipleMissedMonoisotopics_ReturnsTrue()
        {
            var tolerance = new MissedMonoisotopicTolerance(10, 2);
            Assert.IsTrue(tolerance.Within(100 + Constants.C13MinusC12, 101.0033));
            Assert.IsTrue(tolerance.Within(100 + 2 * Constants.C13MinusC12, 102.0066));
        }

        [Test]
        public void Within_WithMissedMonoisotopicsOutsideTolerance_ReturnsFalse()
        {
            var tolerance = new MissedMonoisotopicTolerance(10, 1);
            Assert.IsFalse(tolerance.Within(100 + Constants.C13MinusC12 + 2, 100));
        }

        [Test]
        public void Within_WithNegativeTolerance_ReturnsFalse()
        {
            var tolerance = new MissedMonoisotopicTolerance(-10);
            Assert.IsFalse(tolerance.Within(100, 130));
        }
    }
}
