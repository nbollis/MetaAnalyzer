using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Plotting.Util
{
    public enum TargetDecoyCurveMode
    {
        Score,
        QValue,
        PepQValue,
        Pep,
    }

    public enum Kernels
    {
        Gaussian,
        Epanechnikov,
        Triangular,
        Uniform
    }
}
