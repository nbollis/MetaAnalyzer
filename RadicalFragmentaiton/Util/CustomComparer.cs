namespace RadicalFragmentation;

public class CustomComparer<T> : IEqualityComparer<T>
{
    private readonly Func<T, object>[] propertySelectors;

    public CustomComparer(params Func<T, object>[] propertySelectors)
    {
        this.propertySelectors = propertySelectors;
    }

    public bool Equals(T x, T y)
    {
        if (x == null && y == null)
            return true;
        if (x == null || y == null)
            return false;

        foreach (var selector in propertySelectors)
        {
            if (selector.Target is IEnumerable<double> enumerable)
            {
                if (!enumerable.SequenceEqual((IEnumerable<double>)selector(y)))
                    return false;
            }
            else if (!Equals(selector(x), selector(y)))
                return false;
        }

        return true;
    }

    public int GetHashCode(T obj)
    {
        unchecked
        {
            int hash = 17;
            foreach (var selector in propertySelectors)
            {
                hash = hash * 23 + (selector(obj)?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }


    #region Custom Implementations

    // Radical Fragmentation
    private static Func<PrecursorFragmentMassSet, object>[] PrecursorFragmentSetSelector =
    {
            protein => protein.Accession,
            protein => protein.PrecursorMass,
            protein => protein.FragmentCount,
            protein => protein.FragmentMasses.FirstOrDefault(),
            protein => protein.FragmentMasses.LastOrDefault(),
            protein => protein.FragmentMasses[protein.FragmentMasses.Count / 2],
            protein => protein.FragmentMasses[protein.FragmentMasses.Count / 3],
            protein => protein.FragmentMasses[protein.FragmentMasses.Count / 4],
            protein => protein.FullSequence
        };

    public static CustomComparer<PrecursorFragmentMassSet> PrecursorFragmentMassComparer =>
        new(PrecursorFragmentSetSelector);

    public static CustomComparer<PrecursorFragmentMassSet> LevelOneComparer => new
    (
        protein => protein.Accession,
        protein => protein.PrecursorMass,
        protein => protein.FragmentCount,
        protein => protein.FullSequence,
        protein => protein.FragmentMasses.FirstOrDefault(),
        protein => protein.FragmentMasses.LastOrDefault(),
        protein => protein.FragmentMasses[protein.FragmentMasses.Count / 2],
        protein => protein.FragmentMasses[protein.FragmentMasses.Count / 3],
        protein => protein.FragmentMasses[protein.FragmentMasses.Count / 4]
    );

    public static CustomComparer<PrecursorFragmentMassSet> LevelTwoComparer => new
    (
        protein => protein.Accession,
        protein => protein.PrecursorMass,
        protein => protein.FragmentCount,
        protein => protein.FragmentMasses.FirstOrDefault(),
        protein => protein.FragmentMasses.LastOrDefault(),
        protein => protein.FragmentMasses[protein.FragmentMasses.Count / 2],
        protein => protein.FragmentMasses[protein.FragmentMasses.Count / 3],
        protein => protein.FragmentMasses[protein.FragmentMasses.Count / 4]
    );

    #endregion
}