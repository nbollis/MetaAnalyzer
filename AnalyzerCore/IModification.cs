namespace AnalyzerCore
{
    public interface IModification
    {
        string Name { get; }
        char ModifiedResidue { get; }
        int NominalMass { get; }
    }

    public interface ILocalizedModification : IModification
    {
        int OneBasedLocalization { get; }
    }
}
