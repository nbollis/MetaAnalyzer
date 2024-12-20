using RadicalFragmentation.Processing;
using Proteomics;
using RadicalFragmentation;

namespace Test
{
    internal class TestFragmentationExplorer : RadicalFragmentationExplorer
    {
        public override string AnalysisType => "Test";
        public override bool ResortNeeded => false;

        public TestFragmentationExplorer(string databasePath, int numberOfMods, string species, int maximumFragmentationEvents = Int32.MaxValue, int ambiguityLevel = 1, string? baseDirectory = null, int allowedMissedMonos = 0, double? tolerance = null)
            : base(databasePath, numberOfMods, species, maximumFragmentationEvents, ambiguityLevel, baseDirectory, allowedMissedMonos, tolerance)
        {
        }

        public override IEnumerable<PrecursorFragmentMassSet> GeneratePrecursorFragmentMasses(Protein protein)
        {
            return new List<PrecursorFragmentMassSet>();
        }

        public new FragmentsToDistinguishFile MinFragmentNeededFile
        {
            get => _minFragmentNeededFile ?? new();
            set => _minFragmentNeededFile = value;
        }
    }
}
