using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Util;
using Analyzer.Util.TypeConverters;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Easy.Common.Extensions;
using Proteomics;
using Proteomics.PSM;
using Readers;
using TorchSharp;

namespace Analyzer.FileTypes.Internal
{
    public class ProteinCountingRecord
    {
        public static CsvConfiguration CsvContext =>
            new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                Delimiter = "\t",
                HasHeaderRecord = true
            };

        public string Condition { get; set; }
        public string ProteinAccession { get; set; }
        public string AnnotatedSequence { get; set; } 
        public double SequenceCoverage { get; set; }
        public int PsmCount { get; set; }
        public int UniqueBaseSequences { get; set; }
        public int UniqueFullSequences { get; set; }

        [TypeConverter(typeof(CommaDelimitedToStringListTypeConverter))]
        public List<string> BaseSequences { get; set; }
        [TypeConverter(typeof(CommaDelimitedToStringListTypeConverter))]
        public List<string> FullSequences { get; set; }

        public ProteinCountingRecord()
        {
            BaseSequences = new List<string>();
            FullSequences = new List<string>();
        }

        internal void Resolve()
        {
            BaseSequences = BaseSequences.Distinct().ToList();
            FullSequences = FullSequences.Distinct().ToList();
            UniqueBaseSequences = BaseSequences.Count;
            UniqueFullSequences = FullSequences.Count;
            BaseSequences.ForEach(seq => 
                AnnotatedSequence = AnnotatedSequence.Replace(seq, seq.ToUpper(), StringComparison.InvariantCultureIgnoreCase));
            SequenceCoverage = Math.Round(AnnotatedSequence.Count(char.IsUpper) / (double)AnnotatedSequence.Length * 100, 2);
        }

        public static ProteinCountingRecord operator +(ProteinCountingRecord a, ProteinCountingRecord b)
        {
            if (a.ProteinAccession != b.ProteinAccession)
                throw new ArgumentException("Protein Accessions must be the same to add counts");
            if (a.Condition != b.Condition)
                throw new ArgumentException("Conditions must be the same to add counts");


            var newRecord =  new ProteinCountingRecord
            {
                Condition = a.Condition,
                ProteinAccession = a.ProteinAccession,
                AnnotatedSequence = a.AnnotatedSequence,
                PsmCount = a.PsmCount + b.PsmCount,
                BaseSequences = a.BaseSequences.Union(b.BaseSequences).ToList(),
                FullSequences = a.FullSequences.Union(b.FullSequences).ToList()
            };

            newRecord.Resolve();
            return newRecord;
        }

        public static List<ProteinCountingRecord> GetRecords(List<PsmFromTsv> psms, List<Protein> databaseProteins, string condition = "")
        {
            var filtered = psms.Where(p => p.PassesConfidenceFilter())
                .ToList();

            Dictionary<string, ProteinCountingRecord> resultDict = new();

            // set up result dict
            foreach (var accession in filtered.SelectMany(p => p.ProteinAccession.Split('|')).Distinct())
            {
                var protein = databaseProteins.FirstOrDefault(prot => prot.Accession == accession);
                if (protein is null)
                    continue;

                resultDict.Add(accession, new ProteinCountingRecord()
                {
                    ProteinAccession = accession,
                    AnnotatedSequence = protein.BaseSequence.ToLower(),
                    Condition = condition
                });
            }

            // add psm result
            foreach (var psm in filtered)
            {
                foreach (var accession in psm.ProteinAccession.Split('|'))
                {
                    resultDict[accession].PsmCount++;
                    foreach (var baseSeq in psm.BaseSeq.Split('|'))
                        if (!resultDict[accession].BaseSequences.Contains(baseSeq))
                            resultDict[accession].BaseSequences.Add(baseSeq);
                    foreach (var fullSeq in psm.FullSequence.Split('|'))
                        if (!resultDict[accession].FullSequences.Contains(fullSeq))
                            resultDict[accession].FullSequences.Add(fullSeq);
                }
            }

            resultDict.ForEach(p => p.Value.Resolve());
            return resultDict.Values.ToList();
        }

        public static List<ProteinCountingRecord> GetRecords(List<ISpectralMatch> psms, List<Protein> databaseProteins, string condition = "")
        {

            var filtered = psms.Where(p => p.PassesConfidenceFilter)
                .ToList();

            Dictionary<string, ProteinCountingRecord> resultDict = new();

            // set up result dict
            foreach (var accession in filtered.SelectMany(p => p.ProteinAccession.Split('|', ';')).Distinct())
            {
                var protein = databaseProteins.FirstOrDefault(prot => prot.Accession == accession);
                if (protein is null)
                    continue;

                resultDict.Add(accession, new ProteinCountingRecord()
                {
                    ProteinAccession = accession,
                    AnnotatedSequence = protein.BaseSequence.ToLower(),
                    Condition = condition
                });
            }

            // add psm result
            foreach (var psm in filtered)
            {
                foreach (var accession in psm.ProteinAccession.Split('|', ';'))
                {
                    resultDict[accession].PsmCount++;
                    foreach (var baseSeq in psm.BaseSequence.Split('|', ';'))
                        if (!resultDict[accession].BaseSequences.Contains(baseSeq))
                            resultDict[accession].BaseSequences.Add(baseSeq);
                    foreach (var fullSeq in psm.FullSequence.Split('|', ';'))
                        if (!resultDict[accession].FullSequences.Contains(fullSeq))
                            resultDict[accession].FullSequences.Add(fullSeq);
                }
            }

            resultDict.ForEach(p => p.Value.Resolve());
            return resultDict.Values.ToList();
        }
    }

    public class ProteinCountingFile : ResultFile<ProteinCountingRecord>, IResultFile
    {
        public ProteinCountingFile(string filepath) : base(filepath) { }
        public ProteinCountingFile() : base() { }

        public override void LoadResults()
        {
            using var csv = new CsvReader(new StreamReader(FilePath), ProteinCountingRecord.CsvContext);
            Results = csv.GetRecords<ProteinCountingRecord>().ToList();
        }

        public override void WriteResults(string outputPath)
        {
            Results.ForEach(p => p.Resolve());
            using (var csv = new CsvWriter(new StreamWriter(File.Create(outputPath)), ProteinCountingRecord.CsvContext))
            {
                csv.WriteHeader<ProteinCountingRecord>();
                foreach (var result in Results)
                {
                    csv.NextRecord();
                    csv.WriteRecord(result);
                }
            }
        }

        public override SupportedFileType FileType { get; }
        public override Software Software { get; set; }

        /// <summary>
        /// Combine the results from a and b into one list, adding them if they share an accession
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static ProteinCountingFile operator +(ProteinCountingFile a, ProteinCountingFile b)
        {
            var results = a.Results.Concat(b.Results)
                .GroupBy(p => p.ProteinAccession)
                .Select(p => p.Aggregate((x, y) => x + y))
                .ToList();

            return new ProteinCountingFile {Results = results};
        }
    }
}
