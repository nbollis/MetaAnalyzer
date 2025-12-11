using Chemistry;
using Omics.Modifications;
using Proteomics.ProteolyticDigestion;
using Readers;
using System.Text;
using ResultAnalyzerUtil;

namespace RetentionTimePrediction;

/// <summary>
/// Represents an entity for which retention time can be predicted.
/// Designed for minimal allocation and no dependencies on higher layers (e.g., Omics).
/// </summary>
public interface IRetentionPredictable
{
    /// <summary>
    /// Gets the base (unmodified) sequence
    /// </summary>
    string BaseSequence { get; }

    /// <summary>
    /// Gets the full sequence representation with modification identifiers
    /// e.g., "PEPTIDE[Variable:Oxidation on M]K[Variable:Acetylation on K]"
    /// </summary>
    string FullSequence { get; }

    /// <summary>
    /// Gets the monoisotopic mass of the peptide.
    /// Required for CZE electrophoretic mobility predictions.
    /// </summary>
    double MonoisotopicMass { get; }

    /// <summary>
    /// Builds a sequence string with mass shifts for modifications.
    /// Format: "PEPTIDE[+15.995]K[+42.011]" or "PEPTIDE[-17.026]K"
    /// This is used by predictors that work with mass-based representations (e.g., Chronologer).
    /// </summary>
    /// <returns>Sequence with mass shift annotations, or null if not applicable</returns>
    string FullSequenceWithMassShifts { get; }
}

public class PredicablePsm : IRetentionPredictable
{
    private string? _fullSequenceWithMassShifts = null!;

    public PredicablePsm(PsmFromTsv psm)
    {
        Psm = psm;
        BaseSequence = psm.BaseSequence.Split('|')[0];
        FullSequence = psm.FullSequence.Split('|')[0];
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