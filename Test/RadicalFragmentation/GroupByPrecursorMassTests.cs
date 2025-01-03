using MzLibUtil;
using RadicalFragmentation;
using RadicalFragmentation.Processing;

namespace Test;

[TestFixture]
public class GroupByPrecursorMassTests
{
    [Test]
    public void GroupByPrecursorMass_SingleEntry_ReturnsSingleGroup()
    {
        var precursorMassSet = new PrecursorFragmentMassSet(500.0, "P1", new List<double> { 100.0, 200.0, 300.0 }, "SEQ1");
        var tolerance = new PpmTolerance(10);
        var result = RadicalFragmentationExplorer
            .GroupByPrecursorMass(new List<PrecursorFragmentMassSet> { precursorMassSet }, tolerance)
            .ToList();

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
        var result = RadicalFragmentationExplorer
            .GroupByPrecursorMass(new List<PrecursorFragmentMassSet> { precursorMassSet1, precursorMassSet2 }, tolerance)
            .ToList();

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
        var result = RadicalFragmentationExplorer
            .GroupByPrecursorMass(new List<PrecursorFragmentMassSet> { precursorMassSet1, precursorMassSet2 }, tolerance)
            .ToList();

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
        var result = RadicalFragmentationExplorer
            .GroupByPrecursorMass(new List<PrecursorFragmentMassSet> { precursorMassSet1, precursorMassSet2 }, tolerance, 2)
            .ToList();

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
        var result = RadicalFragmentationExplorer
            .GroupByPrecursorMass(new List<PrecursorFragmentMassSet> { precursorMassSet1, precursorMassSet2, precursorMassSet3 }, tolerance)
            .ToList();

        Assert.That(result.Count, Is.EqualTo(3));

        Assert.That(result[0].Item2.Count, Is.EqualTo(2));
        Assert.That(result[1].Item2.Count, Is.EqualTo(2));
    }


    [Test]
    public void GroupByPrecursorMass_MemoryMappedFile_ReturnsExpectedGroups()
    {
        // Arrange
        var filePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "RadicalFragmentation", "Tryptophan", "Human_0Mods_All_FragmentIndexFile.csv");
        var precursorFragmentMassFile = new PrecursorFragmentMassFile(filePath);
        var tolerance = new PpmTolerance(10);

        // Act
        var result = RadicalFragmentationExplorer
            .GroupByPrecursorMass(precursorFragmentMassFile, tolerance)
            .ToList();

        // Assert
        // Add appropriate assertions based on the expected results
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.GreaterThan(0));
        // Additional assertions can be added here based on the expected behavior
    }

}