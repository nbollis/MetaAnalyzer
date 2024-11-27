using Omics.Fragmentation;
using Omics.Modifications;
using Proteomics;

namespace RadicalFragmentation.Processing;

internal class EtdFragmentationExplorer : RadicalFragmentationExplorer
{
    public EtdFragmentationExplorer(string databasePath, int numberOfMods, string species,  int ambiguityLevel = 1, string? baseDirectory = null, int allowedMissedMonos = 0) 
        : base(databasePath, numberOfMods, species, Int32.MaxValue, ambiguityLevel, baseDirectory, allowedMissedMonos)
    {
    }

    public override string AnalysisType { get; } = "ETD";
    public override IEnumerable<PrecursorFragmentMassSet> GeneratePrecursorFragmentMasses(Protein protein)
    {
        List<Product> masses = new();
        foreach (var proteoform in protein.Digest(PrecursorDigestionParams, fixedMods, variableMods)
                     .DistinctBy(p => p.FullSequence).Where(p => p.MonoisotopicMass < StaticVariables.MaxPrecursorMass))
        {
            masses.Clear();
            proteoform.Fragment(MassSpectrometry.DissociationType.ETD, Omics.Fragmentation.FragmentationTerminus.Both, masses);
            var fragments = masses.Select(p => p.MonoisotopicMass).ToList();
            
            yield return new PrecursorFragmentMassSet(proteoform.MonoisotopicMass, proteoform.Protein.Accession, fragments, proteoform.FullSequence);
        }
    }
}
