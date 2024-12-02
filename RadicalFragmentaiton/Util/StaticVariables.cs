namespace RadicalFragmentation;

internal static class StaticVariables
{
    internal static double MaxPrecursorMass { get; private set; } = 60000;
    internal static double DefaultPpmTolerance { get; private set; } = 10;
    internal static int MaxThreads { get; set; } = 1;
}