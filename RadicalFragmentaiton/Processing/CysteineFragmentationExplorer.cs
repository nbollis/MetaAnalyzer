﻿using Omics.Modifications;
using Proteomics;
using Proteomics.AminoAcidPolymer;
using System.Collections.Concurrent;

namespace RadicalFragmentation.Processing;

internal class CysteineFragmentationExplorer : RadicalFragmentationExplorer
{
    public override string AnalysisType => "Cysteine";
    public override bool ResortNeeded => false;
    public double CysteineToSelect = 1;
    public ConcurrentDictionary<string, int> BaseSequenceToIndexDictionary;

    public CysteineFragmentationExplorer(string databasePath, int numberOfMods, string species, int ambiguityLevel, int fragmentationEvents, string? baseDirectory = null, int allowedMissedMonos = 0, double? ppmTolerance = null)
        : base(databasePath, numberOfMods, species, fragmentationEvents, ambiguityLevel, baseDirectory, allowedMissedMonos, ppmTolerance)
    {
        BaseSequenceToIndexDictionary = new ConcurrentDictionary<string, int>();
    }

    public override IEnumerable<PrecursorFragmentMassSet> GeneratePrecursorFragmentMasses(Protein protein)
    {
        var random = new Random();

        // ensure that when we split the same sequence, we split in the same region
        // this assumption is made due to labeling chemistry. 
        // If it only labels one site, I assume it will label the same site for the same sequence
        // add on the modifications
        var proteoforms = protein.Digest(PrecursorDigestionParams, fixedMods, variableMods)
            .DistinctBy(p => p.FullSequence)
            .Where(p => p.MonoisotopicMass < StaticVariables.MaxPrecursorMass)
            .ToList();
        foreach (var proteoform in proteoforms)
        {
            var mods = proteoform.AllModsOneIsNterminus
                .ToDictionary(p => p.Key, p => new List<Modification>() { p.Value });

            // get a random number between 3 and cysMax to split on 
            var cysCount = proteoform.BaseSequence.Count(p => p == 'C');
            if (cysCount == 0)
            {
                yield return new PrecursorFragmentMassSet(proteoform.MonoisotopicMass, proteoform.Protein.Accession,
                    new List<double> { proteoform.MonoisotopicMass }, proteoform.FullSequence)
                { 
                    CysteineCount = cysCount
                };
            }
            else
            {
                if (!BaseSequenceToIndexDictionary.TryGetValue(proteoform.BaseSequence, out var cysIndex))
                {
                    // select a cysteine at random 
                    do
                    {
                        cysIndex = proteoform.BaseSequence
                            .IndexOf('C', random.Next(0, proteoform.BaseSequence.Length));
                    } while (cysIndex == -1);

                    BaseSequenceToIndexDictionary.TryAdd(proteoform.BaseSequence, cysIndex);
                }
                
                // select all indices within +- 4 residues of the cysteine
                var indicesToFragment = new List<int>();
                for (int i = cysIndex - 4; i <= cysIndex + 4; i++)
                {
                    if (i >= 0 && i < proteoform.BaseSequence.Length && i != cysIndex)
                    {
                        indicesToFragment.Add(i);
                    }
                }
                var maxFrag = indicesToFragment.Count;
                maxFrag = Math.Min(maxFrag, proteoform.BaseSequence.Length - 1);
                
                // split the protein sequence and mods based upon indices to fragment
                // foreach split there will be 2 masses, one the left side and one the right side
                // the mass for each split will be the sum of the sequence and then the sum of mods
                double[] fragmentMasses = new double[maxFrag * 2+1];
                for (int i = 0; i < maxFrag; i++)
                {
                    var leftSide = proteoform.BaseSequence.Substring(0, indicesToFragment[i]);
                    var leftMods = mods
                        .Where(p => p.Key < indicesToFragment[i])
                        .ToDictionary(p => p.Key, p => p.Value);
                    var leftSequence = new Peptide(leftSide);
                    var leftMass = leftSequence.MonoisotopicMass +
                                   leftMods.Values.Sum(p => p.Sum(m => m.MonoisotopicMass))!.Value;

                    var rightSide = proteoform.BaseSequence.Substring(indicesToFragment[i]);
                    var rightMods = mods
                        .Where(p => p.Key >= indicesToFragment[i])
                        .ToDictionary(p => p.Key - indicesToFragment[i], p => p.Value);
                    var rightSequence = new Peptide(rightSide);
                    var rightMass = rightSequence.MonoisotopicMass +
                                    rightMods.Values.Sum(p => p.Sum(m => m.MonoisotopicMass))!.Value;

                    fragmentMasses[i * 2] = leftMass;
                    fragmentMasses[i * 2 + 1] = rightMass;
                }
                
                yield return new PrecursorFragmentMassSet(proteoform.MonoisotopicMass, proteoform.Protein.Accession,
                    fragmentMasses.Where(p => p != 0).OrderBy(p => p).ToList(), proteoform.FullSequence)
                {
                    CysteineCount = cysCount
                };
            }
        }
    }

    public static int GetCysteineCountFromFullSequence(string fullSequence)
    {
        // Use a StringBuilder to build the cleaned sequence
        var baseSequenceBuilder = new System.Text.StringBuilder(fullSequence.Length);
        int bracketCount = 0;
        foreach (var c in fullSequence)
        {
            switch (c)
            {
                case '[':
                    bracketCount++;
                    continue;
                case ']':
                    bracketCount--;
                    continue;
                default:
                {
                    if (bracketCount == 0)
                        baseSequenceBuilder.Append(c);
                    break;
                }
            }
        }

        // Use Span<T> to process the sequence in-place
        var span = baseSequenceBuilder.ToString().AsSpan();
        int cysteineCount = 0;
        foreach (var character in span)
            if (character == 'C')
                cysteineCount++;
        return cysteineCount;
    }

    /// <summary>
    /// Extracts base sequence from full sequence by ignoring anything found in square brackets and counts occurrences of 'C'
    /// </summary>
    public void CountCysteines()
    {
        foreach(var proteoform in PrecursorFragmentMassFile)
        {
            proteoform.CysteineCount ??= GetCysteineCountFromFullSequence(proteoform.FullSequence);
        }
    }
}
