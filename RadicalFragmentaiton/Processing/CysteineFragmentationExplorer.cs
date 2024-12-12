using Omics.Modifications;
using Proteomics;
using Proteomics.AminoAcidPolymer;

namespace RadicalFragmentation.Processing;

internal class CysteineFragmentationExplorer : RadicalFragmentationExplorer
{
    public override string AnalysisType => "Cysteine";
    public double CysteineToSelect = 1;

    public CysteineFragmentationExplorer(string databasePath, int numberOfMods, string species, int ambiguityLevel, int fragmentationEvents, string? baseDirectory = null, int allowedMissedMonos = 0)
        : base(databasePath, numberOfMods, species, fragmentationEvents, ambiguityLevel, baseDirectory, allowedMissedMonos)
    {
    }

    public override IEnumerable<PrecursorFragmentMassSet> GeneratePrecursorFragmentMasses(Protein protein)
    {
        var random = new Random();

        // add on the modifications
        foreach (var proteoform in protein.Digest(PrecursorDigestionParams, fixedMods, variableMods)
                     .DistinctBy(p => p.FullSequence).Where(p => p.MonoisotopicMass < StaticVariables.MaxPrecursorMass))
        {
            var mods = proteoform.AllModsOneIsNterminus
                .ToDictionary(p => p.Key, p => new List<Modification>() { p.Value });

            // get a random number between 3 and cysMax to split on 
            var cysCount = proteoform.BaseSequence.Count(p => p == 'C');
            if (cysCount == 0)
            {
                yield return new PrecursorFragmentMassSet(proteoform.MonoisotopicMass, proteoform.Protein.Accession,
                    new List<double> { proteoform.MonoisotopicMass }, proteoform.FullSequence);
            }
            else
            {
                // select a cysteine at random 
                int cysIndex;
                do
                {
                    cysIndex = proteoform.BaseSequence
                        .IndexOf('C', random.Next(0, proteoform.BaseSequence.Length));
                } while (cysIndex == -1);


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
                    fragmentMasses.OrderBy(p => p).Append(proteoform.MonoisotopicMass).ToList(), proteoform.FullSequence);
            }
        }
    }

    /// <summary>
    /// Extracts base sequence from full sequence by ignoring anything found in square braackets and counts occurances of 'C'
    /// </summary>
    public void CountCysteines()
    {
        foreach(var proteoform in PrecursorFragmentMassFile)
        {
            if (proteoform.CysteineCount != 0)
                continue;

            var baseSequence = new string(proteoform.FullSequence
                .Where(c => c != '[' && c != ']').ToArray());
            var cleanedSequence = System.Text.RegularExpressions.Regex.Replace(baseSequence, @"\[.*?\]", string.Empty);
            var cysteineCount = cleanedSequence.Count(c => c == 'C');
            proteoform.CysteineCount = cysteineCount;
        }
    }
}
