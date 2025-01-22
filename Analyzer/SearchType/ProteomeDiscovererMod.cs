using AnalyzerCore;

namespace Analyzer.SearchType;

public class ProteomeDiscovererMod(int modLocation, string modName, char modifiedResidue)
    : IEquatable<ProteomeDiscovererMod>, ILocalizedModification
{
    public int OneBasedLocalization { get; } = modLocation;
    public string Name { get; } = modName;
    public char ModifiedResidue { get; } = modifiedResidue;

    public bool Equals(ProteomeDiscovererMod? other)
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
        return Equals((ProteomeDiscovererMod)obj);
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