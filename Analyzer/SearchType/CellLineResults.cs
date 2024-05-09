﻿using System.Collections;
using Analyzer.FileTypes.External;
using Analyzer.FileTypes.Internal;
using Analyzer.Util;
using Proteomics.PSM;
using Readers;

namespace Analyzer.SearchType;

public class CellLineResults : IEnumerable<BulkResult>
{
    public string DirectoryPath { get; set; }
    public bool Override { get; set; } = false;
    public string SearchResultsDirectoryPath { get; set; }
    public string CellLine { get; set; }
    public List<BulkResult> Results { get; set; }
    public string DatasetName { get; set; }

    private string[] _dataFilePaths;

    public CellLineResults(string directoryPath)
    {
        DirectoryPath = directoryPath;
        SearchResultsDirectoryPath = Path.Combine(DirectoryPath, "SearchResults"); /*directoryPath*/;
        CellLine = Path.GetFileName(DirectoryPath);
        Results = new List<BulkResult>();

    
        if (directoryPath.Contains("TopDown") || directoryPath.Contains("PEP"))
        {
            var calibAveragedDir = Path.Combine(@"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\SearchResults", "MetaMorpheus", "Task2-AveragingTask");
            _dataFilePaths = Directory.GetFiles(calibAveragedDir, "*.mzML", SearchOption.AllDirectories);
        }
        else
        {
            var man11Directory = @"B:\RawSpectraFiles\Mann_11cell_lines";
            var cellLineDirectory = Directory.GetDirectories(man11Directory).First(p => p.Contains(CellLine));
            var caliAvgDirectory = Directory.GetDirectories(cellLineDirectory).First(p =>
                p.Contains("calibratedaveraged", StringComparison.InvariantCultureIgnoreCase));
            _dataFilePaths = Directory.GetFiles(caliAvgDirectory, "*.mzML", SearchOption.AllDirectories);
        }
        
        foreach (var directory in Directory.GetDirectories(SearchResultsDirectoryPath).Where(p => !p.Contains("maxquant")))
        {
            if (Directory.GetFiles(directory, "meta.bin", SearchOption.AllDirectories).Any()
                && !Directory.GetFiles(directory, "combined_peptide.tsv").Any())
                continue; // fragger currently running
            if (Directory.GetFiles(directory, "*.psmtsv", SearchOption.AllDirectories).Any())
            {
                var files = Directory.GetFiles(directory, "*.psmtsv", SearchOption.AllDirectories);
                if (directory.Contains("Fragger") && Directory.GetDirectories(directory).Length > 2)
                {
                    var directories = Directory.GetDirectories(directory);
                    Results.Add(new MetaMorpheusResult(directories.First(p => p.Contains("NoChimera"))) { DataFilePaths = _dataFilePaths });
                    Results.Add(new MetaMorpheusResult(directories.First(p => p.Contains("WithChimera"))) { DataFilePaths = _dataFilePaths });
                }
                //else if (directory.Contains("PEP"))
                //    continue;
                else if (!files.Any(p => p.Contains("AllProteoforms") || p.Contains("AllPSMs")) && !files.Any(p => p.Contains("AllProteinGroups")))
                    continue;
                else
                    Results.Add(new MetaMorpheusResult(directory) { DataFilePaths = _dataFilePaths });
            }
            else if (Directory.GetFiles(directory, "*IcTda.tsv", SearchOption.AllDirectories).Any())
            {
                if (Directory.GetFiles(directory, "*IcTda.tsv", SearchOption.AllDirectories).Count() is 20 or 10 or 43) // short circuit fi searching is not yet finishedes from parsing
                    Results.Add(new MsPathFinderTResults(directory));
            }
            else if (Directory.GetFiles(directory, "*.fp-manifest", SearchOption.AllDirectories).Any())
                Results.Add(new MsFraggerResult(directory));
            else if (Directory.GetFiles(directory, "*.tdReport").Any())
                if (Directory.GetFiles(directory, "*.txt").Length == 4)
                    Results.Add(new ProteomeDiscovererResult(directory));
        }
    }

    public CellLineResults(string directorypath, List<BulkResult> results)
    {
        DirectoryPath = directorypath;
        SearchResultsDirectoryPath = Path.Combine(DirectoryPath);
        CellLine = Path.GetFileName(DirectoryPath);
        Results = results;
    }

    private string _chimeraCountingPath => Path.Combine(DirectoryPath, $"{CellLine}_PSM_{FileIdentifiers.ChimeraCountingFile}");
    private ChimeraCountingFile _chimeraCountingFile;
    public ChimeraCountingFile ChimeraCountingFile => _chimeraCountingFile ??= CountChimericPsms();

    public ChimeraCountingFile CountChimericPsms()
    {
        if (!Override && File.Exists(_chimeraCountingPath))
        {
            var result = new ChimeraCountingFile(_chimeraCountingPath);
            if (result.Results.DistinctBy(p => p.Software).Count() == Results.Count)
                return result;
        }

        List<ChimeraCountingResult> results = new List<ChimeraCountingResult>();
        foreach (var result in Results)
        {
            results.AddRange(result.ChimeraPsmFile.Results);
        }

        var chimeraCountingFile = new ChimeraCountingFile(_chimeraCountingPath) { Results = results };
        chimeraCountingFile.WriteResults(_chimeraCountingPath);
        return chimeraCountingFile;
    }

    private string _chimeraPeptidePath => Path.Combine(DirectoryPath, $"{CellLine}_Peptide_{FileIdentifiers.ChimeraCountingFile}");
    private ChimeraCountingFile _chimeraPeptideFile;
    public ChimeraCountingFile ChimeraPeptideFile => _chimeraPeptideFile ??= CountChimericPeptides();
    public ChimeraCountingFile CountChimericPeptides()
    {
        if (!Override && File.Exists(_chimeraPeptidePath))
        {
            var result = new ChimeraCountingFile(_chimeraPeptidePath);
            if (result.Results.DistinctBy(p => p.Software).Count() == Results.Count)
                return result;
        }

        List<ChimeraCountingResult> results = new List<ChimeraCountingResult>();
        foreach (var bulkResult in Results.Where(p => p is MetaMorpheusResult))
        {
            var result = (MetaMorpheusResult)bulkResult;
            results.AddRange(result.ChimeraPeptideFile.Results);
        }

        var chimeraPeptideFile = new ChimeraCountingFile(_chimeraPeptidePath) { Results = results };
        chimeraPeptideFile.WriteResults(_chimeraPeptidePath);
        return chimeraPeptideFile;
    }

    private string _chimeraBreakdownFilePath => Path.Combine(DirectoryPath, $"{CellLine}_{FileIdentifiers.ChimeraBreakdownComparison}");
    private ChimeraBreakdownFile _chimeraBreakdownFile;
    public ChimeraBreakdownFile ChimeraBreakdownFile => _chimeraBreakdownFile ??= GetChimeraBreakdownFile();

    public ChimeraBreakdownFile GetChimeraBreakdownFile()
    {
        if (!Override && File.Exists(_chimeraBreakdownFilePath))
            return new ChimeraBreakdownFile(_chimeraBreakdownFilePath);
        
        List<ChimeraBreakdownRecord> results = new List<ChimeraBreakdownRecord>();
        foreach (var bulkResult in Results.Where(p => p is MetaMorpheusResult))
        {
            var result = (MetaMorpheusResult)bulkResult;
            switch (result.IsTopDown)
            {
                case true when result.Condition != "MetaMorpheus":
                case false when result.Condition != "MetaMorpheusWithLibrary":
                    continue;
                default:
                    results.AddRange(result.ChimeraBreakdownFile.Results);
                    break;
            }
        }

        var chimeraBreakdownFile = new ChimeraBreakdownFile(_chimeraBreakdownFilePath) { Results = results };
        chimeraBreakdownFile.WriteResults(_chimeraBreakdownFilePath);
        return chimeraBreakdownFile;
    }

    private string _bulkResultCountComparisonPath => Path.Combine(DirectoryPath, $"{CellLine}_{FileIdentifiers.BottomUpResultComparison}");
    private BulkResultCountComparisonFile _bulkResultCountComparisonFile;
    public BulkResultCountComparisonFile BulkResultCountComparisonFile => _bulkResultCountComparisonFile ??= GetBulkResultCountComparisonFile();
    public BulkResultCountComparisonFile GetBulkResultCountComparisonFile()
    {
        if (!Override && File.Exists(_bulkResultCountComparisonPath))
        {
            var result = new BulkResultCountComparisonFile(_bulkResultCountComparisonPath);
            if (result.Results.DistinctBy(p => p.Condition).Count() == Results.Count)
                return result;
        }

        List<BulkResultCountComparison> results = new List<BulkResultCountComparison>();
        foreach (var result in Results)
        {
            results.AddRange(result.BulkResultCountComparisonFile.Results);
        }

        var bulkResultCountComparisonFile = new BulkResultCountComparisonFile(_bulkResultCountComparisonPath) { Results = results };
        bulkResultCountComparisonFile.WriteResults(_bulkResultCountComparisonPath);
        return bulkResultCountComparisonFile;
    }

    private string _individualFilePath => Path.Combine(DirectoryPath, $"{CellLine}_{FileIdentifiers.IndividualFileComparison}");
    private BulkResultCountComparisonFile _individualFileComparison;
    public BulkResultCountComparisonFile IndividualFileComparisonFile => _individualFileComparison ??= IndividualFileComparison();
    public BulkResultCountComparisonFile IndividualFileComparison()
    {
        if (!Override && File.Exists(_individualFilePath))
        {
            var result = new BulkResultCountComparisonFile(_individualFilePath);
            if (result.Results.DistinctBy(p => p.Condition).Count() == Results.Count)
                return result;
        }

        List<BulkResultCountComparison> results = new List<BulkResultCountComparison>();
        foreach (var result in Results.Where(p => p.IndividualFileComparisonFile != null))
        {
            results.AddRange(result.IndividualFileComparisonFile.Results);
        }

        var individualFileComparison = new BulkResultCountComparisonFile(_individualFilePath) { Results = results };
        individualFileComparison.WriteResults(_individualFilePath);
        return individualFileComparison;
    }

    private string _baseSeqIndividualFilePath => Path.Combine(DirectoryPath, $"{CellLine}_BaseSeq_{FileIdentifiers.IndividualFileComparison}");
    private BulkResultCountComparisonFile _baseSeqIndividualFileComparison;
    public BulkResultCountComparisonFile BaseSeqIndividualFileComparisonFile => _baseSeqIndividualFileComparison ??= IndividualFileComparisonBaseSeq();

    public BulkResultCountComparisonFile IndividualFileComparisonBaseSeq()
    {
        if (!Override && File.Exists(_baseSeqIndividualFilePath))
        {
            var result = new BulkResultCountComparisonFile(_baseSeqIndividualFilePath);
            if (result.Results.DistinctBy(p => p.Condition).Count() == Results.Count)
                return result;
        }

        List<BulkResultCountComparison> results = new List<BulkResultCountComparison>();
        foreach (var result in Results)
        {
            if (result.BaseSeqIndividualFileComparisonFile != null)
                results.AddRange(result.BaseSeqIndividualFileComparisonFile.Results);
        }

        var individualFileComparison = new BulkResultCountComparisonFile(_baseSeqIndividualFilePath) { Results = results };
        individualFileComparison.WriteResults(_baseSeqIndividualFilePath);
        return individualFileComparison;
    }



   



    public void FileComparisonDifferentTypes(string outPath)
    {
        var sw = new StreamWriter(outPath);
        sw.WriteLine("DatasetName,FileName,Condition,Peptides,Base Sequence,Full Sequence,1% Peptides, 1% Base Sequence, 1% Full Sequence, 1% No Chimeras");
        foreach (var result in Results)
        {
            int mmPeptides,
                mmPeptidesBaseSeq,
                mmPeptidesFullSeq,
                fraggerPeptides,
                fraggerPeptidesBaseSeq,
                fraggerPeptidesFullSeq,
                fraggerPeptidesOnePercent,
                fraggerPeptidesOnePercentBaseSeq,
                fraggerPeptidesOnePercentFullSeq;
            string file;

            if (result is MsFraggerResult frag)
            {
                foreach (var individualFile in frag.IndividualFileResults)
                {
                    file = Path.GetFileNameWithoutExtension((string)individualFile.PsmFile.First().FileNameWithoutExtension);
                    var peptides = individualFile.PeptideFile;
                    fraggerPeptides = peptides.Count();
                    fraggerPeptidesBaseSeq = peptides.DistinctBy(p => p.BaseSequence).Count();

                    fraggerPeptidesFullSeq = peptides.GroupBy(p => p,
                        CustomComparer<MsFraggerPeptide>.MsFraggerPeptideDistinctComparer).Count();

                    var onePercentPeptides = peptides.Where(p => p.Probability >= 0.99).ToList();
                    fraggerPeptidesOnePercent = onePercentPeptides.Count();
                    fraggerPeptidesOnePercentBaseSeq = onePercentPeptides.DistinctBy(p => p.BaseSequence).Count();
                    fraggerPeptidesOnePercentFullSeq = onePercentPeptides.GroupBy(p => p,
                        CustomComparer<MsFraggerPeptide>.MsFraggerPeptideDistinctComparer).Count();

                    sw.WriteLine(
                        $"MsFragger,{file},{frag.Condition},{fraggerPeptides},{fraggerPeptidesBaseSeq},{fraggerPeptidesFullSeq},{fraggerPeptidesOnePercent},{fraggerPeptidesOnePercentBaseSeq},{fraggerPeptidesOnePercentFullSeq}");
                }
            }
            else if (result is MetaMorpheusResult mm)
            {
                var indFileDir =
                    Directory.GetDirectories(mm.DirectoryPath, "Individual File Results", SearchOption.AllDirectories);
                if (indFileDir.Length == 0)
                    continue;

                var indFileDirectory = indFileDir.First();
                foreach (var peptideFile in Directory.GetFiles(indFileDirectory, "*Peptides.psmtsv"))
                {
                    var peptides = SpectrumMatchTsvReader.ReadPsmTsv(peptideFile, out _)
                        .Where(p => p.DecoyContamTarget == "T" && p.PEP_QValue <= 0.01);
                    file = peptides.First().FileNameWithoutExtension.Split('-')[0];
                    mmPeptides = peptides.Count();
                    mmPeptidesBaseSeq = peptides.DistinctBy(p => p.BaseSeq).Count();
                    mmPeptidesFullSeq = peptides.GroupBy(p => p.FullSequence).Count();
                    var mmNoChimeraCount = peptides.DistinctBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer).Count();
                    sw.WriteLine(
                        $"MetaMorpheus,{file},{mm.Condition},{mmPeptides},{mmPeptidesBaseSeq},{mmPeptidesFullSeq},{mmPeptides},{mmPeptidesBaseSeq},{mmPeptidesFullSeq},{mmNoChimeraCount}");
                }
            }
        }
        sw.Dispose();
    }

    public IEnumerator<BulkResult> GetEnumerator()
    {
        return Results.GetEnumerator();
    }

    public override string ToString() => CellLine;
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}