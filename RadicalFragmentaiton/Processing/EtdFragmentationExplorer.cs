using MathNet.Numerics;
using Omics.Fragmentation;
using Proteomics;

namespace RadicalFragmentation.Processing;

internal class EtdFragmentationExplorer : RadicalFragmentationExplorer
{
    public override string AnalysisType { get; } = "ETD";
    public override bool ResortNeeded => true;
    public EtdFragmentationExplorer(string databasePath, int numberOfMods, string species,  int ambiguityLevel = 1, string? baseDirectory = null, int allowedMissedMonos = 0, double? ppmTolerance = null) 
        : base(databasePath, numberOfMods, species, Int32.MaxValue, ambiguityLevel, baseDirectory, allowedMissedMonos, ppmTolerance)
    {
    }

    
    public override IEnumerable<PrecursorFragmentMassSet> GeneratePrecursorFragmentMasses(Protein protein)
    {
        List<Product> masses = new();
        var proteoforms = protein.Digest(PrecursorDigestionParams, fixedMods, variableMods)
            .DistinctBy(p => p.FullSequence)
            .Where(p => p.MonoisotopicMass < StaticVariables.MaxPrecursorMass)
            .ToList();

        foreach (var proteoform in proteoforms)
        {
            masses.Clear();
            proteoform.Fragment(MassSpectrometry.DissociationType.ETD, Omics.Fragmentation.FragmentationTerminus.Both, masses);
            var fragments = masses.Select(p => p.MonoisotopicMass.Round(3)).OrderBy(p => p).ToList();
            
            yield return new PrecursorFragmentMassSet(proteoform.MonoisotopicMass, proteoform.Protein.Accession, fragments, proteoform.FullSequence);
        }
    }
}
