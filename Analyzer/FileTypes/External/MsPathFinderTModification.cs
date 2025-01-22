using AnalyzerCore;

namespace Analyzer.FileTypes.External;

public class MsPathFinderTModification(string modName, int modLocation, char modifiedResidue, int nominalMass) 
    : ILocalizedModification
{
    public string Name { get; } = modName;
    public int OneBasedLocalization { get; } = modLocation;
    public char ModifiedResidue { get; } = modifiedResidue;
    public int NominalMass { get; } = nominalMass;

    public bool Equals(ILocalizedModification? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return OneBasedLocalization == other.OneBasedLocalization
               && Name == other.Name
               && ModifiedResidue == other.ModifiedResidue;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ILocalizedModification)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(OneBasedLocalization, Name, ModifiedResidue);
    }
    public override string ToString()
    {
        return $"{OneBasedLocalization}{ModifiedResidue}-{Name}";
    }
}