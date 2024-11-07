using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace Analyzer.FileTypes.External
{
    public class MsFraggerPsm : ISpectralMatch
    {
        public static CsvConfiguration CsvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = "\t",
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null,
            MissingFieldFound = null,
        };

        #region MsFragger Fields

        [Name("Spectrum")]
        public string Spectrum { get; set; }

        [Name("Spectrum File")]
        public string SpectrumFilePath { get; set; }

        [Name("Peptide")]
        public string BaseSequence { get; set; }

        [Name("Modified Peptide")]
        public string ModifiedSequence { get; set; }

        [Name("Extended Peptide")]
        public string ExtendedSequence { get; set; }

        [Name("Prev AA")]
        public char PreviousAminoAcid { get; set; }

        [Name("Next AA")]
        public char NextAminoAcid { get; set; }

        [Name("Peptide Length")]
        public int PeptideLength { get; set; }

        [Name("Charge")]
        public int Charge { get; set; }

        [Name("Retention")]
        public double RetentionTimeInSeconds { get; set; }

        [Ignore] private double? _retentionTime;
        [Ignore] public double RetentionTime
        {
            get => _retentionTime ??= RetentionTimeInSeconds / 60;
            set => _retentionTime = value;
        }

        [Name("Observed Mass")]
        public double ObservedMass { get; set; }

        [Name("Calibrated Observed Mass")]
        public double CalibratedObservedMass { get; set; }

        [Name("Observed M/Z")]
        public double ObservedMz { get; set; }

        [Name("Calibrated Observed M/Z")]
        public double CalibratedObservedMz { get; set; }

        [Name("Calculated Peptide Mass")]
        public double CalculatedPeptideMass { get; set; }

        [Name("Calculated M/Z")]
        public double CalculatedMz { get; set; }

        [Name("Delta Mass")]
        public double DeltaMass { get; set; }

        [Name("Expectation")]
        public double Expectation { get; set; }

        [Name("Hyperscore")]
        public double HyperScore { get; set; }

        [Name("Nextscore")]
        public double NextScore { get; set; }

        [Name("PeptideProphet Probability")]
        public double PeptideProphetProbability { get; set; }

        [Name("Number of Enzymatic Termini")]
        public int NumberOfEnzymaticTermini { get; set; }

        [Name("Number of Missed Cleavages")]
        public int NumberOfMissedCleavages { get; set; }

        [Name("Protein Start")]
        public int ProteinStart { get; set; }

        [Name("Protein End")]
        public int ProteinEnd { get; set; }

        [Name("Intensity")]
        public double Intensity { get; set; }

        [Name("Assigned Modifications")]
        public string AssignedModifications { get; set; }

        [Name("Observed Modifications")]
        public string ObservedModifications { get; set; }

        [Name("Purity")]
        public double Purity { get; set; }

        [Name("Is Unique")]
        public bool IsUnique { get; set; }

        [Name("Protein")]
        public string Protein { get; set; }

        [Name("Protein ID")]
        public string ProteinAccession { get; set; }

        [Name("Entry Name")]
        public string EntryName { get; set; }

        [Name("Gene")]
        public string Gene { get; set; }

        [Name("Protein Description")]
        public string ProteinDescription { get; set; }

        [Name("Mapped Genes")]
        public string MappedGenes { get; set; }

        [Name("Mapped Proteins")]
        public string MappedProteins { get; set; }

        #endregion

        #region Interpreted Fields

        [Ignore] private string _fileNameWithoutExtension;

        [Name("File Name")]
        [Optional]
        public string FileNameWithoutExtension
        {
            get => _fileNameWithoutExtension ??= Spectrum.Split('.')[0];
            set => _fileNameWithoutExtension = value;
        }

        [Ignore] private int _oneBasedScanNumber;
        [Ignore] public int OneBasedScanNumber =>
            _oneBasedScanNumber != 0 ? _oneBasedScanNumber : int.Parse(Spectrum.Split('.')[1]);
        [Ignore] public double MonoisotopicMass => ObservedMass;
        [Ignore] public string DecoyLabel { get; set; } = "rev_";
        [Ignore] public bool IsDecoy => ProteinAccession.Contains(DecoyLabel, StringComparison.InvariantCulture);
        [Ignore] public double ConfidenceMetric => PeptideProphetProbability;
        [Ignore] public double SecondaryConfidenceMetric => Expectation;
        [Ignore] public bool PassesConfidenceFilter => PeptideProphetProbability >= 0.99;

        [Ignore] private string? _fullSequence;

        [Ignore]
        public string FullSequence
        {
            get
            {
                if (_fullSequence != null)
                    return _fullSequence;

                if (ModifiedSequence == "" && AssignedModifications == "")
                    return _fullSequence = BaseSequence;

                //if (ObservedModifications.Length != AssignedModifications.Length)
                    //Debugger.Break();

                var observedMods = AssignedModifications.Split(',')
                    .Select(p => ParseString(p.Trim()))
                    .OrderBy(p => p.Item1)
                    .ToArray();

                string workingSequence = "";
                if (observedMods.First().Item1 == 0)
                {
                    workingSequence+= FraggerToMetaMorpheusModDict[(observedMods.First().Item3, observedMods.First().Item2)];
                }
                int currentResidue = 1;
                foreach (var residue in BaseSequence)
                {
                    workingSequence += residue;
                    if (observedMods.Any(p => p.Item1 == currentResidue))
                    {
                        var mod = observedMods.First(p => p.Item1 == currentResidue);
                        workingSequence += FraggerToMetaMorpheusModDict[(mod.Item3, mod.Item2)];
                    }


                    currentResidue++;
                }

                return _fullSequence ??= workingSequence;
            }
        }

        [Ignore]
        internal static Dictionary<(double, char), string> FraggerToMetaMorpheusModDict => new()
        {
            { (57.0214, 'C'), "[Common Biological : Carbamidomethyl on C]" },
            { (15.9949, 'M'), "[Common Variable : Oxidation on M]" },
            { (79.9663, 'S'), "[Common Biological : Phosphorylation on S]" },
            { (79.9663, 'T'), "[Common Biological : Phosphorylation on T]" },
            { (79.9663, 'Y'), "[Common Biological : Phosphorylation on Y]" },
            { (14.0156,'K'),  "[Common Biological : Methylation on K]" },
            { (14.0156,'R'),  "[Common Biological : Methylation on R]" },
            { (42.0106, 'K'), "[Common Biological : Acetylation on K]" },
            { (42.0106, 'N'), "[Common Biological : Acetylation on X]" },
        };

        public static (int, char, double) ParseString(string input)
        {
            // Regular expression to match the first pattern: leading number, middle character, number in parenthesis
            var regex1 = new Regex(@"(\d+)([A-Z])\(([\d\.]+)\)");
            var match1 = regex1.Match(input);

            if (match1.Success)
            {
                int leadingNumber = int.Parse(match1.Groups[1].Value);
                char middleCharacter = char.Parse(match1.Groups[2].Value);
                double numberInParenthesis = double.Parse(match1.Groups[3].Value);

                return (leadingNumber, middleCharacter, numberInParenthesis);
            }

            // Regular expression to match the second pattern: "N-term", number in parenthesis
            var regex2 = new Regex(@"N-term\(([\d\.]+)\)");
            var match2 = regex2.Match(input);

            if (match2.Success)
            {
                int leadingNumber = 0;
                char middleCharacter = 'N';
                double numberInParenthesis = double.Parse(match2.Groups[1].Value);

                return (leadingNumber, middleCharacter, numberInParenthesis);
            }

            throw new ArgumentException("Input string does not match the required format.");
        }

        #endregion
    }
}
