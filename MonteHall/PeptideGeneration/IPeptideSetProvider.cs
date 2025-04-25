using Omics;

namespace MonteCarlo;

public interface IPeptideSetProvider
{
    IEnumerable<IBioPolymerWithSetMods> GetPeptides();
}
