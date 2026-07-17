using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Analyzer.FileTypes.Internal;
using Analyzer.Plotting;
using Analyzer.SearchType;
using Plotly.NET;
using Plotly.NET.ImageExport;
using ResultAnalyzerUtil;

namespace TaskLayer.ChimeraAnalysis;

public class RevisionsParsingTask : BaseResultAnalyzerTask
{
    public override MyTask MyTask => MyTask.ChimeraRevisionsAnalysis;
    public override BaseResultAnalyzerTaskParameters Parameters { get; }

    public RevisionsParsingTask(BaseResultAnalyzerTaskParameters parameters)
    {
        Parameters = parameters;
    }

    protected override void RunSpecific()
    {
        // ── 1. Resolve paths ──────────────────────────────────────────────
        var searchTaskDir = Parameters.InputDirectoryPath;

        if (!Directory.Exists(searchTaskDir))
            throw new DirectoryNotFoundException(
                $"Search task directory not found: {searchTaskDir}");

        var datasetRoot = Directory.GetParent(searchTaskDir)?.Parent?.FullName
            ?? throw new InvalidOperationException(
                $"Cannot resolve dataset root from search task directory: {searchTaskDir}. " +
                "Expected input to be 2 levels below the dataset root (e.g., .../MixedSpeciesFirstLook/Task3-SearchTask).");

        var designPath = Path.Combine(datasetRoot, "ExperimentalDesign.tsv");
        if (!File.Exists(designPath))
            throw new FileNotFoundException(
                $"ExperimentalDesign.tsv not found at expected location: {designPath}");

        // ── 2. Parse experimental design ──────────────────────────────────
        var designRows = ParseExperimentalDesign(designPath);
        Log($"Parsed {designRows.Count} design rows from ExperimentalDesign.tsv");

        // ── 3. Construct MetaMorpheusResult to obtain IndividualFileResults ──
        var mmResult = new MetaMorpheusResult(searchTaskDir);
        if (mmResult.IndividualFileResults.Count == 0)
            throw new InvalidOperationException(
                $"MetaMorpheusResult found zero IndividualFileResults at {searchTaskDir}. " +
                "Check that the 'Individual File Results' subdirectory exists and contains *PSMs.psmtsv files.");

        Log($"Discovered {mmResult.IndividualFileResults.Count} individual search-file results");

        // ── 4. Build lookup from search results by normalized join key ─────
        // The MetaMorpheusIndividualFileResult.FileName is already the normalized
        // stem (strips -calib, -averaged, _PSMs, _Peptides, _ProteinGroups, _Proteoforms).
        // For design filenames we strip .raw to produce the same key.
        var searchFileLookup = mmResult.IndividualFileResults
            .ToDictionary(f => f.FileName, StringComparer.OrdinalIgnoreCase);

        // ── 5. Join design rows with search results, preserving design order ─
        var joinedRows = new List<DesignJoinRecord>(designRows.Count);
        foreach (var (design, index) in designRows.Select((d, i) => (d, i)))
        {
            var normalizedKey = NormalizeDesignFileName(design.RawFileName);

            if (!searchFileLookup.TryGetValue(normalizedKey, out var searchFile))
            {
                var availableKeys = string.Join(", ", searchFileLookup.Keys.OrderBy(k => k));
                throw new InvalidOperationException(
                    $"No matching search file found for design entry '{design.RawFileName}' " +
                    $"(normalized join key: '{normalizedKey}'). " +
                    $"Available search-file keys [{searchFileLookup.Count}]: {availableKeys}");
            }

            joinedRows.Add(new DesignJoinRecord
            {
                DesignOrder = index + 1,
                Condition = design.Condition,
                Biorep = design.Biorep,
                Fraction = design.Fraction,
                Techrep = design.Techrep,
                RawFileName = design.RawFileName,
                SearchFileName = searchFile.FileName,
                NormalizedJoinKey = normalizedKey,
            });
        }

        // ── 5b. Check for unmatched search files (fail fast) ───────────────
        var matchedKeys = new HashSet<string>(
            joinedRows.Select(r => r.NormalizedJoinKey),
            StringComparer.OrdinalIgnoreCase);
        var unmatchedSearchFiles = mmResult.IndividualFileResults
            .Where(f => !matchedKeys.Contains(f.FileName))
            .ToList();
        if (unmatchedSearchFiles.Count > 0)
        {
            throw new InvalidOperationException(
                $"Found {unmatchedSearchFiles.Count} search file(s) not matched to any design entry: " +
                string.Join(", ", unmatchedSearchFiles.Select(f => f.FileName)));
        }

        // ── 6. Validate 4/4/5 condition cardinality ───────────────────────
        var conditionCounts = joinedRows
            .GroupBy(r => r.Condition)
            .ToDictionary(g => g.Key, g => g.Count());

        if (conditionCounts.Count != 3)
            throw new InvalidOperationException(
                $"Expected exactly 3 conditions but found {conditionCounts.Count}. " +
                $"Distribution: {FormatCounts(conditionCounts)}");

        // Expected: Condition "1" → 4, Condition "2" → 4, Condition "3" → 5
        var expectedCounts = new Dictionary<string, int>
        {
            ["1"] = 4,
            ["2"] = 4,
            ["3"] = 5,
        };

        foreach (var (cond, expected) in expectedCounts)
        {
            if (!conditionCounts.TryGetValue(cond, out var actual))
                throw new InvalidOperationException(
                    $"Condition '{cond}' is missing from the joined results. " +
                    $"Distribution: {FormatCounts(conditionCounts)}");

            if (actual != expected)
                throw new InvalidOperationException(
                    $"Condition '{cond}' has {actual} rows but expected {expected}. " +
                    $"Distribution: {FormatCounts(conditionCounts)}");
        }

        Log($"Condition cardinality validated: 4 / 4 / 5 ✓");

        // ── 7. Write the design-join summary TSV ──────────────────────────
        var outputDir = Path.Combine(searchTaskDir, "Figures", "RevisionsParsing");
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, "MixedSpeciesRevisions_DesignJoinSummary.tsv");

        using (var writer = new StreamWriter(outputPath))
        {
            writer.WriteLine("DesignOrder\tCondition\tBiorep\tFraction\tTechrep\tRawFileName\tSearchFileName\tNormalizedJoinKey");
            foreach (var row in joinedRows)
            {
                writer.WriteLine(
                    $"{row.DesignOrder}\t{row.Condition}\t{row.Biorep}\t{row.Fraction}\t{row.Techrep}" +
                    $"\t{row.RawFileName}\t{row.SearchFileName}\t{row.NormalizedJoinKey}");
            }
        }

        Log($"Design-join summary written: {outputPath}");
        Log($"Total rows: {joinedRows.Count}");
        Log($"Condition distribution: {FormatCounts(conditionCounts)}");

        // ── 8. Generate per-file count bar charts ──────────────────────────
        // Reuse MetaMorpheusResult.GetIndividualFileComparison() for the underlying counts.
        var individualFileComparison = mmResult.GetIndividualFileComparison()
            ?? throw new InvalidOperationException("GetIndividualFileComparison returned null");

        // IndividualFileResultBarChart sorts Results by FileName.ConvertFileName() internally,
        // which alphabetically orders as A1-A4, B1-B4, C1-C5 — matching the canonical design order.
        var comparisonFiles = new List<BulkResultCountComparisonFile> { individualFileComparison };

        var psmChart = AnalyzerGenericPlots.IndividualFileResultBarChart(
            comparisonFiles, out int psmWidth, out int psmHeight,
            title: "Mixed Species Revisions", isTopDown: false, resultType: ResultType.Psm);
        var psmChartPath = Path.Combine(outputDir, "MixedSpeciesRevisions_Counts_PSM.png");
        psmChart.SavePNG(psmChartPath, null, psmWidth, psmHeight);
        Log($"PSM count chart saved: {psmChartPath}");

        var peptideChart = AnalyzerGenericPlots.IndividualFileResultBarChart(
            comparisonFiles, out int pepWidth, out int pepHeight,
            title: "Mixed Species Revisions", isTopDown: false, resultType: ResultType.Peptide);
        var peptideChartPath = Path.Combine(outputDir, "MixedSpeciesRevisions_Counts_Peptide.png");
        peptideChart.SavePNG(peptideChartPath, null, pepWidth, pepHeight);
        Log($"Peptide count chart saved: {peptideChartPath}");

        var proteinChart = AnalyzerGenericPlots.IndividualFileResultBarChart(
            comparisonFiles, out int protWidth, out int protHeight,
            title: "Mixed Species Revisions", isTopDown: false, resultType: ResultType.Protein);
        var proteinChartPath = Path.Combine(outputDir, "MixedSpeciesRevisions_Counts_Protein.png");
        proteinChart.SavePNG(proteinChartPath, null, protWidth, protHeight);
        Log($"Protein count chart saved: {proteinChartPath}");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Helper types and methods
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses the standard MetaMorpheus ExperimentalDesign.tsv format.
    /// Expected header: FileName\tCondition\tBiorep\tFraction\tTechrep
    /// </summary>
    private static List<ExperimentalDesignRow> ParseExperimentalDesign(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2)
            throw new InvalidDataException(
                $"ExperimentalDesign.tsv has no data rows (only {lines.Length} line(s)).");

        // Validate header
        var header = lines[0].Trim().Split('\t');
        var expectedHeader = new[] { "FileName", "Condition", "Biorep", "Fraction", "Techrep" };
        if (header.Length < expectedHeader.Length
            || !header.Take(expectedHeader.Length).SequenceEqual(expectedHeader))
        {
            throw new InvalidDataException(
                $"Unexpected ExperimentalDesign.tsv header. " +
                $"Found: [{string.Join(", ", header)}]. " +
                $"Expected first {expectedHeader.Length} columns: [{string.Join(", ", expectedHeader)}]");
        }

        var rows = new List<ExperimentalDesignRow>(lines.Length - 1);
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            var parts = line.Split('\t');
            if (parts.Length < expectedHeader.Length)
                throw new InvalidDataException(
                    $"Line {i + 1} of ExperimentalDesign.tsv has {parts.Length} columns (expected ≥ {expectedHeader.Length}): {line}");

            rows.Add(new ExperimentalDesignRow(
                RawFileName: parts[0],
                Condition: parts[1],
                Biorep: int.Parse(parts[2]),
                Fraction: int.Parse(parts[3]),
                Techrep: int.Parse(parts[4])
            ));
        }

        return rows;
    }

    /// <summary>
    /// Normalizes a .raw filename from the design file to the join key.
    /// E.g., "210820_Grad090_LFQ_A_01.raw" → "210820_Grad090_LFQ_A_01"
    /// </summary>
    private static string NormalizeDesignFileName(string rawFileName)
    {
        const string rawSuffix = ".raw";
        if (rawFileName.EndsWith(rawSuffix, StringComparison.OrdinalIgnoreCase))
            return rawFileName[..^rawSuffix.Length];
        return rawFileName;
    }

    private static string FormatCounts(Dictionary<string, int> counts) =>
        string.Join(" / ", counts.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"));
}

/// <summary>
/// Lightweight record for a parsed ExperimentalDesign.tsv row.
/// </summary>
internal sealed record ExperimentalDesignRow(
    string RawFileName,
    string Condition,
    int Biorep,
    int Fraction,
    int Techrep
);

/// <summary>
/// Output record for one row of the design-join summary TSV.
/// </summary>
internal sealed class DesignJoinRecord
{
    public int DesignOrder { get; init; }
    public string Condition { get; init; } = "";
    public int Biorep { get; init; }
    public int Fraction { get; init; }
    public int Techrep { get; init; }
    public string RawFileName { get; init; } = "";
    public string SearchFileName { get; init; } = "";
    public string NormalizedJoinKey { get; init; } = "";
}
