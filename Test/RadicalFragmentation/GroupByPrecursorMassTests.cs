using MzLibUtil;
using RadicalFragmentation;
using RadicalFragmentation.Processing;

namespace Test;

[TestFixture]
public class GroupByPrecursorMassTests
{

    public static string TestingDirectory;

    [OneTimeSetUp]
    public static void Setup()
    {
        TestingDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "GroupingTests");
        if (Directory.Exists(TestingDirectory))
        {
            Directory.Delete(TestingDirectory, true);
        }

        if (!Directory.Exists(TestingDirectory))
        {
            Directory.CreateDirectory(TestingDirectory);
        }
    }

    [OneTimeTearDown]
    public static void CleanUp()
    {
        if (Directory.Exists(TestingDirectory))
        {
            Directory.Delete(TestingDirectory, true);
        }
    }


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
    public static void StreamGroupsByTolerance_AllWorkingSetValuesIncluded()
    {
        var filePath = Path.Combine(TestingDirectory, "StreamGroupsByTolerance_AllWorkingSetValuesIncluded.csv");
        var precursorFragmentMassSets = new List<PrecursorFragmentMassSet>
        {
            new PrecursorFragmentMassSet(100.0, "P1", new List<double> { 100.0 }, "SEQ1"),
            new PrecursorFragmentMassSet(105.0, "P2", new List<double> { 105.0 }, "SEQ2"),
            new PrecursorFragmentMassSet(110.0, "P3", new List<double> { 110.0 }, "SEQ3"),
            new PrecursorFragmentMassSet(115.0, "P4", new List<double> { 115.0 }, "SEQ4"),
            new PrecursorFragmentMassSet(120.0, "P5", new List<double> { 120.0 }, "SEQ5")
        };

        // Write test data to CSV file
        using (var writer = new StreamWriter(filePath))
        using (var csv = new CsvHelper.CsvWriter(writer, PrecursorFragmentMassSet.CsvConfiguration))
        {
            csv.WriteRecords(precursorFragmentMassSets);
        }

        var tolerance = new AbsoluteTolerance(100);
        var chunkSize = 2;
        var ambiguityLevel = 1;

        using var precursorFragmentMassFile = new PrecursorFragmentMassFile(filePath);
        int index = 0;
        foreach (var indResult in precursorFragmentMassFile.StreamGroupsByTolerance(tolerance, chunkSize, ambiguityLevel))
        {
            // one result per precursor
            Assert.That(indResult.Item1, Is.EqualTo(precursorFragmentMassSets[index]));

            // all are included in each set
            Assert.That(indResult.Item2.Count, Is.EqualTo(4));
            Assert.That(indResult.Item2, Is.EquivalentTo(precursorFragmentMassSets.Where((_, i) => i != index)));

            // set does not contain original
            Assert.That(indResult.Item2, Has.None.EqualTo(precursorFragmentMassSets[index]));
            Assert.That(indResult.Item2, Has.None.EqualTo(indResult));
            index++;
        }
        Assert.That(index, Is.EqualTo(precursorFragmentMassSets.Count));
    }

    [Test]
    public static void StreamGroupsByTolerance_AllWorkingSetValuesIncluded_Level2Ambiguity()
    {
        var filePath = Path.Combine(TestingDirectory, "StreamGroupsByTolerance_AllWorkingSetValuesIncluded.csv");
        var precursorFragmentMassSets = new List<PrecursorFragmentMassSet>
        {
            new PrecursorFragmentMassSet(100.0, "P1", new List<double> { 100.0 }, "SEQ1"),
            new PrecursorFragmentMassSet(105.0, "P1", new List<double> { 105.0 }, "SEQ1"),
            new PrecursorFragmentMassSet(110.0, "P2", new List<double> { 110.0 }, "SEQ2"),
            new PrecursorFragmentMassSet(115.0, "P2", new List<double> { 115.0 }, "SEQ2"),
            new PrecursorFragmentMassSet(120.0, "P3", new List<double> { 120.0 }, "SEQ3"),
            new PrecursorFragmentMassSet(120.0, "P4", new List<double> { 120.0 }, "SEQ3"),
        };

        // Write test data to CSV file
        using (var writer = new StreamWriter(filePath))
        using (var csv = new CsvHelper.CsvWriter(writer, PrecursorFragmentMassSet.CsvConfiguration))
        {
            csv.WriteRecords(precursorFragmentMassSets);
        }

        var tolerance = new AbsoluteTolerance(100);
        var chunkSize = 2;
        var ambiguityLevel = 2;

        using var precursorFragmentMassFile = new PrecursorFragmentMassFile(filePath);
        int index = 0;
        foreach (var indResult in precursorFragmentMassFile.StreamGroupsByTolerance(tolerance, chunkSize, ambiguityLevel))
        {
            // one result per precursor
            Assert.That(indResult.Item1, Is.EqualTo(precursorFragmentMassSets[index]));

            // all are included in each set
            Assert.That(indResult.Item2.Count, Is.EqualTo(4));

            // set does not contain original
            Assert.That(indResult.Item2, Has.None.EqualTo(precursorFragmentMassSets[index]));
            Assert.That(indResult.Item2, Has.None.EqualTo(indResult));

            // set contains none that share a sequence or an accession
            foreach (var innerResult in indResult.Item2)
            {
                Assert.That(innerResult.Accession, Is.Not.EqualTo(indResult.Item1.Accession));
                Assert.That(innerResult.FullSequence, Is.Not.EqualTo(indResult.Item1.FullSequence));
            }
            index++;
        }
        Assert.That(index, Is.EqualTo(precursorFragmentMassSets.Count));
    }

    //[Test]
    //public static void StreamGroupsByTolerance_AllWorkingSetValuesIncluded_Parallel()
    //{
    //    var filePath = Path.Combine(TestingDirectory, "StreamGroupsByTolerance_AllWorkingSetValuesIncluded.csv");
    //    var precursorFragmentMassSets = new List<PrecursorFragmentMassSet>
    //    {
    //        new PrecursorFragmentMassSet(100.0, "P1", new List<double> { 100.0 }, "SEQ1"),
    //        new PrecursorFragmentMassSet(105.0, "P2", new List<double> { 105.0 }, "SEQ2"),
    //        new PrecursorFragmentMassSet(110.0, "P3", new List<double> { 110.0 }, "SEQ3"),
    //        new PrecursorFragmentMassSet(115.0, "P4", new List<double> { 115.0 }, "SEQ4"),
    //        new PrecursorFragmentMassSet(120.0, "P5", new List<double> { 120.0 }, "SEQ5")
    //    };

    //    // Write test data to CSV file
    //    using (var writer = new StreamWriter(filePath))
    //    using (var csv = new CsvHelper.CsvWriter(writer, PrecursorFragmentMassSet.CsvConfiguration))
    //    {
    //        csv.WriteRecords(precursorFragmentMassSets);
    //    }

    //    var tolerance = new AbsoluteTolerance(100);
    //    var chunkSize = 2;
    //    var ambiguityLevel = 1;

    //    using var precursorFragmentMassFile = new PrecursorFragmentMassFile(filePath);

    //    int index = 0;
    //    foreach(var indResult in precursorFragmentMassFile.StreamByPrecursorWithinToleranceParallelStream(tolerance, chunkSize, ambiguityLevel))
    //    {
    //        // one result per precursor
    //        Assert.That(indResult.Item1, Is.EqualTo(precursorFragmentMassSets[index]));

    //        // all are included in each set
    //        Assert.That(indResult.Item2.Count, Is.EqualTo(4));
    //        Assert.That(indResult.Item2, Is.EquivalentTo(precursorFragmentMassSets.Where((_, i) => i != index)));

    //        // set does not contain original
    //        Assert.That(indResult.Item2, Has.None.EqualTo(precursorFragmentMassSets[index]));
    //        Assert.That(indResult.Item2, Has.None.EqualTo(indResult));

    //        index++;
    //    }
    //    Assert.That(index, Is.EqualTo(precursorFragmentMassSets.Count));
    //}

    //[Test]
    //public static void StreamGroupsByTolerance_AllWorkingSetValuesIncluded_Level2Ambiguity_Parallel()
    //{
    //    var filePath = Path.Combine(TestingDirectory, "StreamGroupsByTolerance_AllWorkingSetValuesIncluded.csv");
    //    var precursorFragmentMassSets = new List<PrecursorFragmentMassSet>
    //    {
    //        new PrecursorFragmentMassSet(100.0, "P1", new List<double> { 100.0 }, "SEQ1"),
    //        new PrecursorFragmentMassSet(105.0, "P1", new List<double> { 105.0 }, "SEQ1"),
    //        new PrecursorFragmentMassSet(110.0, "P2", new List<double> { 110.0 }, "SEQ2"),
    //        new PrecursorFragmentMassSet(115.0, "P2", new List<double> { 115.0 }, "SEQ2"),
    //        new PrecursorFragmentMassSet(120.0, "P3", new List<double> { 120.0 }, "SEQ3"),
    //        new PrecursorFragmentMassSet(120.0, "P4", new List<double> { 120.0 }, "SEQ3"),
    //    };

    //    // Write test data to CSV file
    //    using (var writer = new StreamWriter(filePath))
    //    using (var csv = new CsvHelper.CsvWriter(writer, PrecursorFragmentMassSet.CsvConfiguration))
    //    {
    //        csv.WriteRecords(precursorFragmentMassSets);
    //    }

    //    var tolerance = new AbsoluteTolerance(100);
    //    var chunkSize = 2;
    //    var ambiguityLevel = 2;

    //    using var precursorFragmentMassFile = new PrecursorFragmentMassFile(filePath);

    //    int index = 0;
    //    foreach (var indResult in precursorFragmentMassFile.StreamByPrecursorWithinToleranceParallelStream(tolerance, chunkSize, ambiguityLevel))
    //    {
    //        // one result per precursor
    //        Assert.That(indResult.Item1, Is.EqualTo(precursorFragmentMassSets[index]));

    //        // all are included in each set
    //        Assert.That(indResult.Item2.Count, Is.EqualTo(4));

    //        // set does not contain original
    //        Assert.That(indResult.Item2, Has.None.EqualTo(precursorFragmentMassSets[index]));
    //        Assert.That(indResult.Item2, Has.None.EqualTo(indResult));

    //        // set contains none that share a sequence or an accession
    //        foreach (var innerResult in indResult.Item2)
    //        {
    //            Assert.That(innerResult.Accession, Is.Not.EqualTo(indResult.Item1.Accession));
    //            Assert.That(innerResult.FullSequence, Is.Not.EqualTo(indResult.Item1.FullSequence));
    //        }
    //        index++;
    //    }
    //    Assert.That(index, Is.EqualTo(precursorFragmentMassSets.Count));
    //}
}
