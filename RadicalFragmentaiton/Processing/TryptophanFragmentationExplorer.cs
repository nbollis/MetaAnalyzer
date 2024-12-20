using Omics.Modifications;
using Proteomics;
using Proteomics.ProteolyticDigestion;

namespace RadicalFragmentation.Processing;

internal class TryptophanFragmentationExplorer : RadicalFragmentationExplorer
{
    
    public override string AnalysisType => "Tryptophan";
    public override bool ResortNeeded => false;

    public TryptophanFragmentationExplorer(string databasePath, int numberOfMods, string species, int ambiguityLevel = 1, string? baseDirectory = null, int allowedMissedMonos = 0, double? ppmTolerance = null)
        : base(databasePath, numberOfMods, species, int.MaxValue, ambiguityLevel, baseDirectory, allowedMissedMonos, ppmTolerance)
    {
        digestionParameters = new DigestionParams("tryptophan oxidation", 0, 7, int.MaxValue, 100000,
            InitiatorMethionineBehavior.Retain, NumberOfMods);
    }

    public DigestionParams digestionParameters;
    public override IEnumerable<PrecursorFragmentMassSet> GeneratePrecursorFragmentMasses(Protein protein)
    {
        // add the modifications to the protein
        var proteoforms = protein.Digest(PrecursorDigestionParams, fixedMods, variableMods)
            .DistinctBy(p => p.FullSequence)
            .Where(p => p.MonoisotopicMass < StaticVariables.MaxPrecursorMass)
            .ToList();

        foreach (var proteoform in proteoforms)
        {
            var mods = proteoform.AllModsOneIsNterminus
                .ToDictionary(p => p.Key, p => new List<Modification>() { p.Value });
            var proteinReconstruction = new Protein(proteoform.BaseSequence, proteoform.Protein.Accession,
                proteoform.Protein.Organism, proteoform.Protein.GeneNames.ToList(),
                mods, proteolysisProducts, proteoform.Protein.Name, proteoform.Protein.FullName,
                proteoform.Protein.IsDecoy, proteoform.Protein.IsContaminant,
                proteoform.Protein.DatabaseReferences.ToList(), proteoform.Protein.SequenceVariations.ToList(),
                proteoform.Protein.AppliedSequenceVariations, proteoform.Protein.SampleNameForVariants,
                disulfideBonds, proteoform.Protein.SpliceSites.ToList(),
                proteoform.Protein.DatabaseFilePath, false
            );

            // split the protein at each W and record fragment masses
            var peps = proteinReconstruction.Digest(digestionParameters, fixedMods, variableMods);
            var fragments = peps.Select(p => p.MonoisotopicMass).OrderBy(p => p).ToList();
            fragments.Add(proteoform.MonoisotopicMass);

            yield return new PrecursorFragmentMassSet(proteoform.MonoisotopicMass, proteoform.Protein.Accession, fragments, proteoform.FullSequence);
        }
    }
}