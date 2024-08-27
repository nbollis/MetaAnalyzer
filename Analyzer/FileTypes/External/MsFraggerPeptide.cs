using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using Analyzer.Util.TypeConverters;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Easy.Common.Extensions;

namespace Analyzer.FileTypes.External
{
    public class MsFraggerPeptide : ISpectralMatch
    {
        public static CsvConfiguration CsvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = "\t",
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null,
            MissingFieldFound = null,
            HeaderValidated = null,
        };

        #region ISpectralMatch Members

        [Ignore] public int OneBasedScanNumber { get; }
        [Ignore] public double RetentionTime { get; }
        [Ignore] private string? _fullSequence;

        [Ignore]
        public string FullSequence
        {
            get
            {
                if (_fullSequence != null)
                    return _fullSequence;

                if (!ObservedModifications.Any() && !AssignedModifications.Any())
                    return _fullSequence = BaseSequence;

                //if (ObservedModifications.Length != AssignedModifications.Length)
                //Debugger.Break();

                var observedMods = AssignedModifications
                    .Select(p => MsFraggerPsm.ParseString(p.Trim()))
                    .OrderBy(p => p.Item1)
                    .ToArray();

                string workingSequence = "";
                if (observedMods.First().Item1 == 0)
                {
                    workingSequence +=
                        MsFraggerPsm.FraggerToMetaMorpheusModDict[
                            (observedMods.First().Item3, observedMods.First().Item2)];
                }

                int currentResidue = 1;
                foreach (var residue in BaseSequence)
                {
                    workingSequence += residue;
                    if (observedMods.Any(p => p.Item1 == currentResidue))
                    {
                        var mod = observedMods.First(p => p.Item1 == currentResidue);
                        workingSequence += MsFraggerPsm.FraggerToMetaMorpheusModDict[(mod.Item3, mod.Item2)];
                    }


                    currentResidue++;
                }

                return _fullSequence ??= workingSequence;
            }
        }

        [Ignore] public double MonoisotopicMass { get; }

        private int _charge;
        [Ignore] int ISpectralMatch.Charge => _charge;

        [Ignore] public string DecoyLabel { get; set; } = "rev_";
        [Ignore] public bool IsDecoy => ProteinAccession.Contains(DecoyLabel, StringComparison.InvariantCulture);
        [Ignore] public double ConfidenceMetric => Probability;
        [Ignore] public double SecondaryConfidenceMetric { get; }
        [Ignore] public bool PassesConfidenceFilter => Probability >= 0.99;

        #endregion


        [Name("Peptide", "Sequence")] public string BaseSequence { get; set; }

        [Name("Prev AA")][Optional] public char PreviousAminoAcid { get; set; }

        [Name("Next AA")][Optional] public char NextAminoAcid { get; set; }

        [Ignore] private int _peptideLength;

        [Name("Peptide Length")]
        [Optional]
        public int PeptideLength
        {
            get => _peptideLength.IsDefault() ? BaseSequence.Length : _peptideLength;
            set => _peptideLength = value;
        }

        [Name("Protein Start")][Optional] public int OneBasedStartResidueInProtein { get; set; }

        [Name("Protein End")][Optional] public int OneBasedEndResidueInProtein { get; set; }

        [Name("Charges", "Charge States")]
        [TypeConverter(typeof(CommaDelimitedToIntegerArrayTypeConverter))]
        public int[] Charge { get; set; }

        [Name("Probability")] public double Probability { get; set; }

        [Name("Spectral Count")][Optional] public int SpectralCount { get; set; }

        [Name("Intensity")][Optional] public double Intensity { get; set; }

        [Name("Assigned Modifications")]
        [TypeConverter(typeof(CommaDelimitedToStringArrayTypeConverter))]
        public string[] AssignedModifications { get; set; }

        [Name("Observed Modifications")]
        [Optional]
        [TypeConverter(typeof(CommaDelimitedToStringArrayTypeConverter))]
        public string[] ObservedModifications { get; set; }

        [Name("Protein")] public string Protein { get; set; }

        [Name("Protein ID")][Optional] public string ProteinAccession { get; set; }
        

        [Ignore] private string _proteinName;

        [Name("Entry Name")]
        [Optional]
        public string ProteinName
        {
            get => _proteinName.IsDefault() ? Protein.Split('|').Last().Trim() : _proteinName;
            set => _proteinName = value;
        }

        [Name("Gene")]
        public string Gene { get; set; }

        [Name("Protein Description")]
        public string ProteinDescription { get; set; }

        [Name("Mapped Genes")]
        [Optional]
        [TypeConverter(typeof(CommaDelimitedToStringArrayTypeConverter))]
        public string[] MappedGenes { get; set; }

        [Name("Mapped Proteins")]
        [Optional]
        [TypeConverter(typeof(CommaDelimitedToStringArrayTypeConverter))]
        public string[] MappedProteins { get; set; }

        [Name("File Name")]
        [Optional]
        public string FileNameWithoutExtension { get; set; }


        [NotMapped]
        [Optional]
        public Dictionary<string, int> FileToPsmCount { get; set; }

        [NotMapped]
        [Optional]
        public Dictionary<string, double> IntensityByFile { get; set; }

        public MsFraggerPeptide()
        {
            FileToPsmCount = new();
            IntensityByFile = new();
        }
    }
}
