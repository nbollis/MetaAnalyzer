using AnalyzerCore;

namespace Analyzer.FileTypes.External;

public class ChimerysModification(string modName, int modLocation, char modifiedResidue, int nominalMass)
    : ILocalizedModification
{
    public string Name { get; } = modName;
    public char ModifiedResidue { get; } = modifiedResidue;
    public int NominalMass { get; } = nominalMass;
    public int OneBasedLocalization { get; } = modLocation;

    public bool Equals(ILocalizedModification? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return OneBasedLocalization == other.OneBasedLocalization
               && Name == other.Name
               && ModifiedResidue == other.ModifiedResidue
               && NominalMass == other.NominalMass;
    }
}