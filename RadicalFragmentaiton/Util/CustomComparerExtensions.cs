using ResultAnalyzerUtil;

namespace RadicalFragmentation;

internal static class CustomComparerExtensions
{
    internal static Func<PrecursorFragmentMassSet, object>[] PrecursorFragmentSetSelector =
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

    internal static CustomComparer<PrecursorFragmentMassSet> PrecursorFragmentMassComparer =>
        new(PrecursorFragmentSetSelector);

    internal static CustomComparer<PrecursorFragmentMassSet> LevelOneComparer => new
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

    internal static CustomComparer<PrecursorFragmentMassSet> LevelTwoComparer => new
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
}