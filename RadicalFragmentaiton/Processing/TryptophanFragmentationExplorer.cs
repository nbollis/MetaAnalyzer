﻿using Omics.Modifications;
using Proteomics;
using Proteomics.ProteolyticDigestion;
using ResultAnalyzerUtil;

namespace RadicalFragmentation.Processing;

internal class TryptophanFragmentationExplorer : RadicalFragmentationExplorer
{
    #region Bulk Results
    private string _bulkResultsFragmentHistogramFilepath => Path.Combine(DirectoryPath, $"Combined_{FileIdentifiers.FragmentCountHistogram}");
    private FragmentHistogramFile _bulkResultsFragmentHistogramFile;
    public FragmentHistogramFile BulkResultsFragmentHistogramFile => _bulkResultsFragmentHistogramFile ??= CombineFragmentHistograms();
    public FragmentHistogramFile CombineFragmentHistograms()
    {
        if (!Override && File.Exists(_bulkResultsFragmentHistogramFilepath))
            return new FragmentHistogramFile(_bulkResultsFragmentHistogramFilepath);

        List<FragmentHistogramRecord> results = new List<FragmentHistogramRecord>();
        foreach (var item in Directory.GetFiles(DirectoryPath, $"*{FileIdentifiers.FragmentCountHistogram}"))
        {
            results.AddRange(new FragmentHistogramFile(item).Results);
        }

        var fragmentHistogramFile = new FragmentHistogramFile(_bulkResultsFragmentHistogramFilepath) { Results = results };
        fragmentHistogramFile.WriteResults(_bulkResultsFragmentHistogramFilepath);
        return fragmentHistogramFile;
    }

    private string _bulkResultsMinFragmentsNeededFilepath => Path.Combine(DirectoryPath, $"Combined_{FileIdentifiers.MinFragmentNeeded}");
    private FragmentsToDistinguishFile _bulkResultsMinFragmentsNeededFile;
    public FragmentsToDistinguishFile BulkResultsMinFragmentsNeededFile => _bulkResultsMinFragmentsNeededFile ??= CombineMinFragmentsNeeded();

    public FragmentsToDistinguishFile CombineMinFragmentsNeeded()
    {
        if (!Override && File.Exists(_bulkResultsMinFragmentsNeededFilepath))
            return new FragmentsToDistinguishFile(_bulkResultsMinFragmentsNeededFilepath);

        List<FragmentsToDistinguishRecord> results = new List<FragmentsToDistinguishRecord>();
        foreach (var item in Directory.GetFiles(DirectoryPath, $"*{FileIdentifiers.MinFragmentNeeded}"))
        {
            results.AddRange(new FragmentsToDistinguishFile(item).Results);
        }

        var fragmentsToDistinguishFile = new FragmentsToDistinguishFile(_bulkResultsMinFragmentsNeededFilepath) { Results = results };
        fragmentsToDistinguishFile.WriteResults(_bulkResultsMinFragmentsNeededFilepath);
        return fragmentsToDistinguishFile;
    }

    #endregion

    public override string AnalysisType => "Tryptophan";

    public TryptophanFragmentationExplorer(string databasePath, int numberOfMods, string species, int ambiguityLevel = 1, string? baseDirectory = null)
        : base(databasePath, numberOfMods, species, int.MaxValue, ambiguityLevel, baseDirectory)
    {
        digestionParameters = new DigestionParams("tryptophan oxidation", 0, 7, int.MaxValue, 100000,
            InitiatorMethionineBehavior.Retain, NumberOfMods);
    }

    public DigestionParams digestionParameters;
    public override IEnumerable<PrecursorFragmentMassSet> GeneratePrecursorFragmentMasses(Protein protein)
    {
        // add the modifications to the protein
        foreach (var proteoform in protein.Digest(PrecursorDigestionParams, fixedMods, variableMods)
                     .DistinctBy(p => p.FullSequence).Where(p => p.MonoisotopicMass < 60000))
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
            var fragments = peps.Select(p => p.MonoisotopicMass).ToList();
            fragments.Add(proteoform.MonoisotopicMass);

            yield return new PrecursorFragmentMassSet(proteoform.MonoisotopicMass, proteoform.Protein.Accession, fragments, proteoform.FullSequence);
        }
    }
}