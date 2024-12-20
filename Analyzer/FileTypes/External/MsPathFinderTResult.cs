﻿using Analyzer.Util.TypeConverters;
using Chemistry;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Easy.Common.Extensions;
using Readers;

namespace Analyzer.FileTypes.External
{


    public class MsPathFinderTResultFile : ResultFile<MsPathFinderTResult>, IResultFile
    {
        public override SupportedFileType FileType => SupportedFileType.Tsv_FlashDeconv;
        public override Software Software { get; set; }

        private List<MsPathFinderTResult>? _filteredResults;
        public List<MsPathFinderTResult> FilteredResults => _filteredResults ??=
            Results.Where(p => p is { SpecEValue: <= 0.01, Probability: >= 0.5 }).ToList();

        public MsPathFinderTResultFile(string filePath) : base(filePath, Software.Unspecified)
        {
            try
            {
                if (Results.First().FileNameWithoutExtension.IsNullOrEmpty())
                    Results.ForEach(p => p.FileNameWithoutExtension = string.Join("_", Path.GetFileNameWithoutExtension(filePath).Split('_')[..^1]));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public MsPathFinderTResultFile() : base()
        {
        }

        public override void LoadResults()
        {
            using var csv = new CsvReader(new StreamReader(FilePath), MsPathFinderTResult.CsvConfiguration);
            Results = csv.GetRecords<MsPathFinderTResult>().ToList();
        }

        public override void WriteResults(string outputPath)
        {
            if (!CanRead(outputPath))
                outputPath += FileType.GetFileExtension();

            using (var csv = new CsvWriter(new StreamWriter(File.Create(outputPath)),
                       MsPathFinderTResult.CsvConfiguration))
            {
                csv.WriteHeader<MsPathFinderTResult>();
                foreach (var result in Results)
                {
                    csv.NextRecord();
                    csv.WriteRecord(result);
                }
            }
            Thread.Sleep(1000);
        }
    }

    public class MsPathFinderTResult : ISpectralMatch
    {
        public static CsvConfiguration CsvConfiguration { get; } =
            new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                Delimiter = "\t",
                HasHeaderRecord = true,
                IgnoreBlankLines = true,
                TrimOptions = CsvHelper.Configuration.TrimOptions.InsideQuotes,
                BadDataFound = null,
            };


        [Name("Scan")] public int OneBasedScanNumber { get; set; }

        [Name("Pre")] public char PreviousResidue { get; set; }

        [Name("Sequence")] public string BaseSequence { get; set; }

        [Name("Post")] public char NextResidue { get; set; }

        [Name("Modifications")] public string Modifications { get; set; }

        [Name("Composition")]
        [TypeConverter(typeof(MsPathFinderTCompositionToChemicalFormulaConverter))]
        public ChemicalFormula ChemicalFormula { get; set; }

        [Name("ProteinName")] public string ProteinName { get; set; }

        [Name("ProteinDesc")] public string ProteinDescription { get; set; }

        [Name("ProteinLength")] public int Length { get; set; }

        [Name("Start")] public int OneBasedStartResidue { get; set; }

        [Name("End")] public int OneBasedEndResidue { get; set; }

        [Name("Charge")] public int Charge { get; set; }

        [Name("MostAbundantIsotopeMz")] public double MostAbundantIsotopeMz { get; set; }

        [Name("Mass")] public double MonoisotopicMass { get; set; }

        [Name("Ms1Features")] public int Ms1Features { get; set; }

        [Name("#MatchedFragments")] public int NumberOfMatchedFragments { get; set; }

        [Name("Probability")] public double Probability { get; set; }

        [Name("SpecEValue")] public double SpecEValue { get; set; }

        [Name("EValue")] public double EValue { get; set; }

        [Name("QValue")] [Optional] public double QValue { get; set; }

        [Name("PepQValue")] [Optional] public double PepQValue { get; set; }

        #region InterpretedFields

        [Ignore] private string _accession = null;
        [Ignore] public string Accession => _accession ??= ProteinName.Split('|')[1].Trim();

        [Ignore] private bool? _isDecoy = null;
        [Ignore] public bool IsDecoy => _isDecoy ??= ProteinName.StartsWith("XXX");

        [Optional] public string FileNameWithoutExtension { get; set; }

        #endregion

        #region Interface Fields

        public string FullSequence => throw new NotImplementedException();
        public double ConfidenceMetric => SpecEValue;
        public double SecondaryConfidenceMetric => Probability;
        public bool PassesConfidenceFilter => SpecEValue <= 0.01 && Probability >= 0.5;
        public string ProteinAccession => Accession;
        public double RetentionTime => throw new NotImplementedException();

        #endregion
    }
}
