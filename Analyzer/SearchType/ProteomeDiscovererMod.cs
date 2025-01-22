using AnalyzerCore;

namespace Analyzer.SearchType;

public class ProteomeDiscovererMod : IEquatable<ProteomeDiscovererMod>, ILocalizedModification
{
    public int ModLocation { get; }
    public string ModName { get; }
    public char ModifiedResidue { get; }

    public ProteomeDiscovererMod(int modLocation, string modName, char modifiedResidue)
    {
        ModLocation = modLocation;
        ModName = modName;
        ModifiedResidue = modifiedResidue;
    }


    public bool Equals(ProteomeDiscovererMod? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ModLocation == other.ModLocation 
            && ModName == other.ModName 
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
        return HashCode.Combine(ModLocation, ModName, ModifiedResidue);
    }

    public override string ToString()
    {
        return $"{ModLocation}{ModifiedResidue}-{ModName}";
    }
}