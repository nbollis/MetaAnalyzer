using GradientDevelopment;

namespace Test.GradientDevelopment
{
    [TestFixture]
    public class RunInformationTests
    {
        [Test]
        public void CalculatePercentage_CorrectlyCalculatesPercentage()
        {
            double result = RunInformation.CalculatePercentage(50, 100);
            Assert.That(result, Is.EqualTo(0.5));

            result = RunInformation.CalculatePercentage(0, 100);
            Assert.That(result, Is.EqualTo(0));

            result = RunInformation.CalculatePercentage(50, 0);
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        [TestCase("CAAAAAAAAUUAC[Biological:Methylation on C]CAAUAAUC[Biological:Methylation on C]GAC", 5, 2, 3)]
        [TestCase("AAAACUCUCUUC[Biological:Methylation on C]AAUUCUUUAUG", 5, 1, 4)]
        [TestCase("AAAACUCUC[Biological:Methylation on C]UUC[Biological:Methylation on C]AAUUCUUUAUG", 5, 2, 3)]
        [TestCase("C[Biological:Methylation on C]AAAAAAAAUUACC[Biological:Methylation on C]AAUAAUCCAG", 5, 2, 3)]
        [TestCase("AAAAC[Biological:Methylation on C]UCUCUUC[Biological:Methylation on C]AAUUCUUUAUG", 5, 2, 3)]
        [TestCase("C[Biological:Methylation on C]AAAAA[Metal:Sodium on A]AAAUUACC[Biological:Methylation on C]AAUAAUC[Biological:Methylation on C]CAG", 5, 3, 2)]
        [TestCase("C[Biological:Methylation on C]AAAAAAAAUUAC[Biological:Methylation on C]CAAUAAUCCAG", 5, 2, 3)]
        [TestCase("AAAAC[Biological:Methylation on C]UCUC[Biological:Methylation on C]UUCAAUUCUUUAUG", 5, 2, 3)]
        [TestCase("CAAAAAAAAUUACC[Biological:Methylation on C]AAUAAUCCAG[Digestion Termini:Cyclic Phosphate on X]", 5, 1, 4)]
        public void CountCytosinesInSequence_CorrectlyCountsCytosines(string sequence, int expectedTotal, int expectedMethylated, int expectedUnmethylated)
        {
            var result = RunInformation.CountCytosinesInSequence(sequence);
            Assert.That(result.localCCount, Is.EqualTo(expectedTotal));
            Assert.That(result.localMethylatedCCount, Is.EqualTo(expectedMethylated));
            Assert.That(result.localUnmethylatedCCount, Is.EqualTo(expectedUnmethylated));
        }

        [Test]
        public void CountCytosines_CorrectlyCountsCytosinesInSequences()
        {
            var sequences = new List<string>
            {
                "CAAAAAAAAUUAC[Biological:Methylation on C]CAAUAAUC[Biological:Methylation on C]GAC",
                "AAAACUCUCUUC[Biological:Methylation on C]AAUUCUUUAUG",
                "AAAACUCUC[Biological:Methylation on C]UUC[Biological:Methylation on C]AAUUCUUUAUG",
                "AAAAUUC[Biological:Methylation on C]UUAAUUUUUAUG",
                "AAAAUUCUUAAUUUUUAUG"
            };

            var result = RunInformation.CountCytosines(sequences);

            Assert.That(result.total, Is.EqualTo(17));
            Assert.That(result.methylated, Is.EqualTo(6));
            Assert.That(result.unmethylated, Is.EqualTo(11));
            Assert.That(result.totalGreaterThanOne, Is.EqualTo(15));
            Assert.That(result.methylatedGreaterThanOne, Is.EqualTo(5));
            Assert.That(result.unmethylatedGreaterThanOne, Is.EqualTo(10));
        }
    }
}
