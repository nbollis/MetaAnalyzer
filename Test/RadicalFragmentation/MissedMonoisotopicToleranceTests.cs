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
            var tolerance = new MissedMonoisotopicTolerance(10, 3);

            var value = 100;
            var missedMons = 3;
            var missedMonValues = new double[missedMons + 1];
            for (int i = 0; i < missedMons + 1; i++)
            {
                missedMonValues[i] = value + i * Constants.C13MinusC12;
            }

            for (int i = 0; i < missedMons + 1; i++)
            {
                Assert.IsTrue(tolerance.Within(value, missedMonValues[i]));
            }

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
        [Test]
        public void GetMinimumValue_NoMissedMonoisotopics_ReturnsCorrectValue()
        {
            var tolerance = new MissedMonoisotopicTolerance(10);
            double mean = 100;
            double expectedMinValue = mean * (1.0 - 10 / 1000000.0);
            Assert.AreEqual(expectedMinValue, tolerance.GetMinimumValue(mean));
        }

        [Test]
        public void GetMinimumValue_WithMissedMonoisotopics_ReturnsCorrectValue()
        {
            var tolerance = new MissedMonoisotopicTolerance(10, 2);
            double mean = 100;
            double expectedMinValue = (mean) * (1.0 - 10 / 1000000.0);
            Assert.AreEqual(expectedMinValue, tolerance.GetMinimumValue(mean));
        }

        [Test]
        public void GetMaximumValue_NoMissedMonoisotopics_ReturnsCorrectValue()
        {
            var tolerance = new MissedMonoisotopicTolerance(10);
            double mean = 100;
            double expectedMaxValue = mean * (1.0 + 10 / 1000000.0);
            Assert.AreEqual(expectedMaxValue, tolerance.GetMaximumValue(mean));
        }

        [Test]
        public void GetMaximumValue_WithMissedMonoisotopics_ReturnsCorrectValue()
        {
            var tolerance = new MissedMonoisotopicTolerance(10, 2);
            double mean = 100;
            double expectedMaxValue = (mean + 2 * Constants.C13MinusC12) * (1.0 + 10 / 1000000.0);
            Assert.AreEqual(expectedMaxValue, tolerance.GetMaximumValue(mean));
        }
    }
}
