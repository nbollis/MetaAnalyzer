﻿using System.Text;
using Analyzer.SearchType;
using Analyzer.Util.TypeConverters;
using Chemistry;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Omics.Modifications;
using Readers;
using ResultAnalyzerUtil;

namespace Analyzer.FileTypes.External
{
    public class ProteomeDiscovererPsmRecord : IEquatable<ProteomeDiscovererPsmRecord>, ISpectralMatch
    {


        #region Static

        public static CsvConfiguration CsvConfiguration => new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            Delimiter = "\t",
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null,
            MissingFieldFound = null,
            HeaderValidated = null,
        };

        private static Dictionary<ProteomeDiscovererMod, Modification> _modConversionDictionary;
        static ProteomeDiscovererPsmRecord()
        {
            _modConversionDictionary = new Dictionary<ProteomeDiscovererMod, Modification>();
        }

        public static string ConvertProteomeDiscovererModification(ProteomeDiscovererMod pdMod, IEnumerable<Modification> allKnownMods)
        {
            
            var targetResidueMatching = allKnownMods.Where(p => p.Target.ToString().Contains(pdMod.ModifiedResidue) || p.Target.ToString().Contains("on X"))
                .ToArray();
            var nameMatching = allKnownMods.Where(p => 
                    p.IdWithMotif.Contains(pdMod.ModName.Replace("-L-lysine", "")) 
                    && (p.IdWithMotif.Contains($" on {pdMod.ModifiedResidue}") || p.IdWithMotif.Contains(" on X"))                      
                    /*&& !p.OriginalId.Contains("DTT")*/)
                .ToArray();

            Modification modToReturn;
            if (nameMatching.Length != 1) // if multiple match by name, go with lowest uniprot reference number
            {
                int lowestRef = int.MaxValue;
                Modification? lowestReference = null;
                foreach (var modification in nameMatching)
                {
                    if (modification.DatabaseReference.TryGetValue("Unimod", out var values))
                    {
                        foreach (var value in values)
                        {
                            if (int.TryParse(value, out var result) && result < lowestRef)
                            {
                                lowestRef = result;
                                lowestReference = modification;
                            }
                        }
                    }
                    else
                    {
                        //Debugger.Break();
                    }
                }

                if (lowestReference is null)
                    modToReturn = nameMatching.MinBy(p => p.IdWithMotif.Length)!;
                else
                    modToReturn = lowestReference;
            }
            else
                modToReturn = nameMatching[0];

            string category = modToReturn.ModificationType switch
            {
                "Unimod" when modToReturn.OriginalId.Contains("Carbamido") => "Common Fixed",
                "Unimod" when modToReturn.OriginalId.Contains("Oxidation") => "Common Variable",
                "Unimod" when modToReturn.OriginalId.Contains("Phosphoryl") => "Common Biological",
                "Unimod" when modToReturn.OriginalId.Contains("Acetyl") => "Common Biological",
                "Unimod" when modToReturn.OriginalId.Contains("Methy") => "Common Biological",
                _ => modToReturn.ModificationType
            };

            return $"[{category}:{modToReturn.OriginalId} on {modToReturn.Target}]";
        }

        #endregion

        #region ISpectralMatch Members

        [Ignore] public int OneBasedScanNumber => int.Parse(Ms2ScanNumber);
        [Ignore] public double RetentionTime => RT;

        [Ignore] private string? _fullSequence;
        [Ignore] public string FullSequence
        {
            get
            {
                if (_fullSequence != null)
                    return _fullSequence;
                if (!Modifications.Any())
                    return _fullSequence = BaseSequence;

                var sb = new StringBuilder();

                if (Modifications.Any(p => p.ModLocation == 0))
                {

                }
                for (int i = 0; i < BaseSequence.Length; i++)
                {
                    var residue = BaseSequence[i];
                    sb.Append(residue);

                    var potentialMod = Modifications.FirstOrDefault(p => p.ModLocation == i + 1);
                    if (potentialMod is null) continue;

                    var mmMod = ConvertProteomeDiscovererModification(potentialMod, GlobalVariables.AllModsKnown);
                    sb.Append(mmMod);
                }

                return sb.ToString();
            }
        }
        [Ignore] public string FileNameWithoutExtension { get; }
        [Ignore] public double MonoisotopicMass => TheoreticalMass;
        [Ignore] public string ProteinAccession => ProteinAccessions;

        // decoys are not reported by default
        [Ignore] public bool IsDecoy => false;
        [Ignore] public double ConfidenceMetric => NegativeLogEValue;
        [Ignore] public double SecondaryConfidenceMetric => QValue;
        [Ignore] public bool PassesConfidenceFilter => SecondaryConfidenceMetric <= 0.01;

        #endregion

        [Name("Checked")]
        [Optional]
        public bool Checked { get; set; }

        [Name("Confidence")]
        public string Confidence { get; set; }

        [Name("Detected Ion Count")]
        public int DetectedIonCount { get; set; }

        [Name("Proteoform Level")]
        public int ProteoformLevel { get; set; }

        [Name("PTMs Localized")]
        public string PTMsLocalized { get; set; }

        [Name("PTMs Identified")]
        public string PTMsIdentified { get; set; }

        [Name("Sequence Defined")]
        public string SequenceDefined { get; set; }

        [Name("Gene Identified")]
        public string GeneIdentified { get; set; }

        [Name("Identifying Node")]
        public string IdentifyingNode { get; set; }

        [Name("Annotated Sequence")]
        public string AnnotatedSequence { get; set; }

        [Name("Annotated Sequence")]
        [TypeConverter(typeof(ProteomeDiscovererAnnotatedToBaseSequenceConverter))]
        public string BaseSequence { get; set; }

        [Name("Modifications")]
        [TypeConverter(typeof(ProteomeDiscovererPSMModToProteomeDiscovererModificationArrayConverter))]
        public ProteomeDiscovererMod[] Modifications { get; set; }

        [Name("# Proteins")]
        public int ProteinCount { get; set; }

        [Name("Master Protein Accessions", "Protein Accessions")]
        public string ProteinAccessions { get; set; }

        [Name("Original Precursor Charge", "Charge")]
        public int Charge { get; set; }

        [Name("Rank")]
        public int Rank { get; set; }

        [Name("Search Engine Rank")]
        public int SearchEngineRank { get; set; }

        [Name("m/z [Da]")]
        public double Mz { get; set; }

        [Name("Mass [Da]", "MH+ [Da]")]
        public double PrecursorMass { get; set; }

        [Name("Theo. Mass [Da]", "Theo. MH+ [Da]")]
        public double TheoreticalMass { get; set; }

        [Name("DeltaMass [ppm]", "DeltaM [ppm]")]
        public double DeltaMassPpm { get; set; }

        [Name("DeltaMass [Da]")]
        public double DeltaMassDa { get; set; }

        [Name("Deltam/z [Da]")]
        public double DeltaMz { get; set; }

        [Name("Matched Ions")]
        public string MatchedIons { get; set; }

        [Name("Activation Type")]
        public string ActivationType { get; set; }

        [Name("NCE [%]")]
        public double NCE { get; set; }

        [Name("MS Order")]
        [TypeConverter(typeof(PSPDMsOrderConverter))]
        public int MSOrder { get; set; }

        [Name("Ion Inject Time [ms]")]
        public double IonInjectTime { get; set; }

        [Name("RT [min]")]
        public double RT { get; set; }

        [Name("Predicted RT {min}")]
        public double PredictedRT { get; set; }

        [Name("Apex RT [min]")]
        public string ApexRT { get; set; }

        [Name("DeltaRT")]
        public double DeltaRT { get; set; }

        [Name("Fragmentation Scan(s)", "First Scan")]
        public string Ms2ScanNumber { get; set; }

        [Name("# Fragmentation Scans")]
        public int FragmentationScans { get; set; }

        [Name("# Precursor Scans")]
        public int PrecursorScans { get; set; }

        [Name("File ID")]
        public string FileID { get; set; }

        [Name("-Log P-Score")]
        public double NegativeLogPScore { get; set; }

        [Name("-Log E-Value")]
        public double NegativeLogEValue { get; set; }

        [Name("C-Score")]
        public double CScore { get; set; }

        [Name("Q-value", "q-Value")]
        public double QValue { get; set; }

        [Name("PEP")]
        public double PEP { get; set; }

        [Name("SVM Score")]
        public double SVMScore { get; set; }

        [Name("Precursor Abundance")]
        public string PrecursorAbundance { get; set; }

        [Name("% Residue Cleavages")]
        public double PercentResidueCleavages { get; set; }

        [Name("Corrected Delta Mass (Da)")]
        public double CorrectedDeltaMassDa { get; set; }

        [Name("Corrected Delta Mass (ppm)")]
        public double CorrectedDeltaMassPpm { get; set; }

        [Name("Compensation Voltage")]
        public double CompensationVoltage { get; set; }

        [Name("PSM Ambiguity")]
        [Optional]
        public string PsmAmbiguity { get; set; }

        [Name("I/L Ambiguity")]
        [Optional]
        public string ILAmbiguity { get; set; }

        [Name("# Missed Cleavages")]
        [Optional]
        public int MissedCleavages { get; set; }

        [Name("DeltaScore")]
        public string DeltaScore { get; set; }

        [Name("DeltaCn")]
        public double DeltaCn { get; set; }

        [Name("Intensity")]
        public double Intensity { get; set; }

        [Name("Normalized CHIMERYS Coefficient")]
        public double NormalizedChimeraCoefficient { get; set; }

        [Name("Master Scan(s)")]
        [TypeConverter(typeof(SemiColonDelimitedToDoubleArrayConverter))]
        public double[] PrecursorScanNumbers { get; set; }

        [Name("# Protein Groups")]
        public int ProteinGroupCount { get; set; }

        [Ignore] private double? _calculatedMz;
        [Ignore] public double CalculatedMz => _calculatedMz ??= Mz.ToMz(Charge);

        public bool Equals(ProteomeDiscovererPsmRecord? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return AnnotatedSequence == other.AnnotatedSequence && ProteinAccessions == other.ProteinAccessions && Ms2ScanNumber == other.Ms2ScanNumber && FileID == other.FileID;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ProteomeDiscovererPsmRecord)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AnnotatedSequence, ProteinAccessions, Ms2ScanNumber, FileID);
        }
    }

    public class ProteomeDiscovererPsmFile : ResultFile<ProteomeDiscovererPsmRecord>, IResultFile
    {
        public ProteomeDiscovererPsmFile(string filePath) : base(filePath)
        {
        }

        public ProteomeDiscovererPsmFile()
        {
        }
        public override SupportedFileType FileType => SupportedFileType.Tsv_FlashDeconv;
        public override Software Software { get; set; }

        private List<ProteomeDiscovererPsmRecord>? _filteredResults;
        public List<ProteomeDiscovererPsmRecord> FilteredResults => _filteredResults ??= Results.Where(p => p.QValue <= 0.01 && (p.NegativeLogEValue >= 5 || FilePath.Contains("Chimerys"))).ToList();
        public override void LoadResults()
        {
            using var csv = new CsvReader(new StreamReader(FilePath), ProteomeDiscovererPsmRecord.CsvConfiguration);
            Results = csv.GetRecords<ProteomeDiscovererPsmRecord>().ToList();
        }

        public override void WriteResults(string outputPath)
        {
            using var csv = new CsvWriter(new StreamWriter(File.Create(outputPath)), ProteomeDiscovererPsmRecord.CsvConfiguration);

            csv.WriteHeader<ProteomeDiscovererPsmRecord>();
            foreach (var result in Results)
            {
                csv.NextRecord();
                csv.WriteRecord(result);
            }
        }
    }
}
