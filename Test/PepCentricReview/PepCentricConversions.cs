using Analyzer;
using Omics.Modifications;
using Proteomics.ProteolyticDigestion;
using ResultAnalyzerUtil;
using UsefulProteomicsDatabases;

namespace Test.PepCentricReview;

public static class PepCentricConversions
{
    static PepCentricConversions()
    {
        var modificationTypeProperty = typeof(Modification).GetProperty("ModificationType");
        var oxidativeMethionine = GlobalVariables.AllModsKnown.First(b => b is { IdWithMotif: "Oxidation on M" });
        modificationTypeProperty.SetValue(oxidativeMethionine, "Common Variable");

        var carbamimidoMethyl = GlobalVariables.AllModsKnown.First(b => b is { IdWithMotif: "Carbamidomethyl on C" });
        modificationTypeProperty.SetValue(carbamimidoMethyl, "Common Fixed");

        // Set up the fixed modifications
        FixedMods = new List<Modification>()
        {
            carbamimidoMethyl
        };

        // Set up the variable modifications
        VariableMods = new List<Modification>()
        {
            oxidativeMethionine
        };
    }
    internal static string FastaPath = @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\UP000005640_reviewed.fasta";
    internal static string XmlPath = @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";
    internal static DigestionParams DigestionParams = new DigestionParams("trypsin", 2, 7, int.MaxValue, 1024);
    public static List<Modification> FixedMods { get; set; }
    public static List<Modification> VariableMods { get; set; }

    public static IEnumerable<PepCentricReviewRecord> GetRecordsFromProforma(ProformaFile proformaFile)
    {
        var temp = proformaFile.GroupBy(p => p.BaseSequence).Select((group) =>
            {
                int count = group.Count();
                int fullSequenceCount = group.Select(p => p.FullSequence).Distinct().Count();

                return new
                {
                    BaseSequence = group.First().BaseSequence,
                    GroupCount = count,
                    FullSequenceCount = fullSequenceCount,
                    AllResults = group.ToList(),
                    Accession = group.First().ProteinAccession,
                    FullSequenceGroup = group.GroupBy(p => p.FullSequence).ToDictionary(p => p.Key, p => p.Count())
                };
            })
            .OrderByDescending(p => p.GroupCount)
            .ThenByDescending(p => p.FullSequenceCount)
            .ToList();

        var xmlFullSequences = new HashSet<string>(GetXmlFullSequences());
        var fastaFullSequences = new HashSet<string>(GetFastaFullSequences());

        var records = new List<PepCentricReviewRecord>();
        foreach (var baseSequenceGroup in temp)
        {
            foreach (var fullSeqGroup in baseSequenceGroup.FullSequenceGroup)
            {

                bool isFasta = fastaFullSequences.Contains(fullSeqGroup.Key);
                bool isXml = xmlFullSequences.Contains(fullSeqGroup.Key);
                bool isGptmd = !fastaFullSequences.Contains(fullSeqGroup.Key) && !xmlFullSequences.Contains(fullSeqGroup.Key);

                IdClassification idClassification;
                if (isFasta)
                {
                    idClassification = IdClassification.Fasta;
                }
                else if (isXml)
                {
                    idClassification = IdClassification.Xml;
                }
                else if (isGptmd)
                {
                    idClassification = IdClassification.Gptmd;
                }
                else
                {
                    idClassification = IdClassification.Unknown;
                }

                var record = new PepCentricReviewRecord
                {
                    BaseSequence = baseSequenceGroup.BaseSequence,
                    FullSequence = fullSeqGroup.Key,
                    BaseSequenceCount = baseSequenceGroup.GroupCount,
                    FullSequenceCount = fullSeqGroup.Value,
                    FullSequenceWithMassShifts = PepCentricReviewRecord.GetFullSequenceWithMassShift(fullSeqGroup.Key),
                    Accession = baseSequenceGroup.Accession,
                    IdClassification = idClassification
                };
                records.Add(record);
            }
        }
        return records.OrderByDescending(p => p.BaseSequenceCount)
            .ThenByDescending(p => p.FullSequenceCount);
    }
    public static IEnumerable<string> GetFastaFullSequences()
    {
        var proteins = ProteinDbLoader.LoadProteinFasta(FastaPath, true, DecoyType.None, false, out _);

        foreach (var peptideWithSetModifications in proteins.SelectMany(p => p.Digest(DigestionParams, FixedMods, VariableMods)))
        {
            yield return peptideWithSetModifications.FullSequence;
        }
    }

    public static IEnumerable<string> GetXmlFullSequences()
    {
        var proteins = ProteinDbLoader.LoadProteinXML(XmlPath, true, DecoyType.None, GlobalVariables.AllModsKnown, false, [], out _);

        foreach (var peptideWithSetModifications in proteins.SelectMany(p => p.Digest(DigestionParams, FixedMods, VariableMods)))
        {
            yield return peptideWithSetModifications.FullSequence;
        }
    }
}