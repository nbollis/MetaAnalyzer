using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proteomics.PSM;

namespace ResultAnalyzer.Util
{
    public static class Extensions
    {
        public static bool IsDecoy(this PsmFromTsv psm) => psm.DecoyContamTarget == "D";
    }
}
