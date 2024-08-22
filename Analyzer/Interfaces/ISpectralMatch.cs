using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer
{
    public interface ISpectralMatch
    {
        int OneBasedScanNumber { get; }

        /// <summary>
        /// The retention time of the identified species, ensure to convert to minutes
        /// </summary>
        double RetentionTime { get; }
        string BaseSequence { get; }

        /// <summary>
        /// Primary sequence with modifications, preferably converted to MetaMorpheus Notation
        /// </summary>
        string FullSequence { get; }
        string FileNameWithoutExtension { get; }


        double MonoisotopicMass { get; }
        int Charge { get; }

        string ProteinAccession { get; }
        bool IsDecoy { get; }


        double ConfidenceMetric { get; }
        double SecondaryConfidenceMetric { get; }
        bool PassesConfidenceFilter { get; }
    }
}
