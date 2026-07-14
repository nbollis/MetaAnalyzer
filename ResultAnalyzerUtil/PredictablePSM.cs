using Chromatography.RetentionTimePrediction;
using Omics.Modifications;
using Proteomics.ProteolyticDigestion;
using System.Text;
using Chemistry;

namespace ResultAnalyzerUtil;
public class PredicablePsm : IRetentionPredictable
{
    private string? _fullSequenceWithMassShifts = null!;

    public PredicablePsm(PsmFromTsv psm)
    {
        Psm = psm;
        BaseSequence = psm.BaseSequence.Split('|')[0];
        FullSequence = psm.FullSequence.Split('|')[0];
        _fullSequenceWithMassShifts = SetFullSequenceWithMassShifts(GlobalVariables.AllModsKnownDictionary);
    }

    public PsmFromTsv Psm { get; }
    public string BaseSequence { get; }
    public string FullSequence { get; }
    public double MonoisotopicMass { get; private set; }
    public string FullSequenceWithMassShifts => _fullSequenceWithMassShifts ??= SetFullSequenceWithMassShifts(GlobalVariables.AllModsKnownDictionary);

    public string SetFullSequenceWithMassShifts(Dictionary<string, Modification> allKnownMods)
    {
        var withSetMods = new PeptideWithSetModifications(FullSequence.Split('|')[0], allKnownMods);
        MonoisotopicMass = withSetMods.MonoisotopicMass;
        var subsequence = new StringBuilder();

        // modification on peptide N-terminus
        if (withSetMods.AllModsOneIsNterminus.TryGetValue(1, out Modification? mod))
        {
            if (mod.MonoisotopicMass > 0)
                subsequence.Append($"[+{mod.MonoisotopicMass.RoundedDouble(6)}]");
            else
                subsequence.Append($"[{mod.MonoisotopicMass.RoundedDouble(6)}]");
        }

        for (int r = 0; r < withSetMods.Length; r++)
        {
            subsequence.Append(withSetMods[r]);

            // modification on this residue
            if (withSetMods.AllModsOneIsNterminus.TryGetValue(r + 2, out mod))
            {
                if (mod.MonoisotopicMass > 0)
                {
                    subsequence.Append($"[+{mod.MonoisotopicMass.RoundedDouble(6)}]");
                }
                else
                {
                    subsequence.Append($"[{mod.MonoisotopicMass.RoundedDouble(6)}]");
                }
            }
        }

        // modification on peptide C-terminus
        if (withSetMods.AllModsOneIsNterminus.TryGetValue(withSetMods.Length + 2, out mod))
        {
            if (mod.MonoisotopicMass > 0)
            {
                subsequence.Append($"-[+{mod.MonoisotopicMass.RoundedDouble(6)}]");
            }
            else
            {
                subsequence.Append($"-[{mod.MonoisotopicMass.RoundedDouble(6)}]");
            }
        }
        return subsequence.ToString();
    }
}
