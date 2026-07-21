using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Analyzer.FileTypes.Internal;
using Analyzer.Plotting;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.SearchType;
using Plotly.NET;
using Plotly.NET.ImageExport;
using Plotting.Util;
using ResultAnalyzerUtil;
using Chart = Plotly.NET.CSharp.Chart;

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

        // Enforce design ordering for count plots: reorder the Results sequence
        // to match joinedRows so the validation below can check consistency.
        {
            var resultsByKey = individualFileComparison.Results
                .ToDictionary(r => r.FileName, StringComparer.OrdinalIgnoreCase);

            var reordered = joinedRows
                .Select(r => resultsByKey.GetValueOrDefault(r.NormalizedJoinKey))
                .Where(r => r is not null)
                .ToList();

            if (reordered.Count != individualFileComparison.Results.Count)
            {
                var missing = joinedRows
                    .Where(r => !resultsByKey.ContainsKey(r.NormalizedJoinKey))
                    .Select(r => r.NormalizedJoinKey);
                throw new InvalidOperationException(
                    $"Count-plot results missing entries for design keys: [{string.Join(", ", missing)}]");
            }

            individualFileComparison.Results = reordered;
        }

        // Emit the three count charts using labels explicitly derived from
        // joinedRows design order — not from any helper's internal sort.
        var designLabels = joinedRows.Select(r => r.NormalizedJoinKey).ToList();
        EmitCountChartFromDesignOrder(
            individualFileComparison.Results, designLabels,
            title: "Mixed Species Revisions", resultType: ResultType.Psm,
            filePathNoExt: Path.Combine(outputDir, "MixedSpeciesRevisions_Counts_PSM"));
        EmitCountChartFromDesignOrder(
            individualFileComparison.Results, designLabels,
            title: "Mixed Species Revisions", resultType: ResultType.Peptide,
            filePathNoExt: Path.Combine(outputDir, "MixedSpeciesRevisions_Counts_Peptide"));
        EmitCountChartFromDesignOrder(
            individualFileComparison.Results, designLabels,
            title: "Mixed Species Revisions", resultType: ResultType.Protein,
            filePathNoExt: Path.Combine(outputDir, "MixedSpeciesRevisions_Counts_Protein"));

        // ── 9. Generate chimera composition stacked-column plots ────────────
        // PLAN STATED: Reuse MetaMorpheusResult.GetChimericSpectrumSummaryFile() +
        //              ChimericSpectrumSummaryFile.ToChimeraBreakDownRecords().
        // BLOCKER:     GetChimericSpectrumSummaryFile() in MetaMorpheusResult.cs is
        //              hardcoded to legacy B:\-drive paths (Ecoli / Jurkat / Chimeras /
        //              Mann_11cell_lines) and fails for the MixedSpeciesFirstLook
        //              dataset (DirectoryNotFoundException at line 1230). The function
        //              also requires deconvoluted ms1.feature files that were not
        //              produced for this search task.
        // DEVIATION:   We use MetaMorpheusResult.GetChimeraBreakdownFile() instead,
        //              which generates the same ChimeraBreakdownRecord data type from
        //              the search task's PSM/Peptide TSVs and the mzML data files in
        //              the calibrated task directory. No new chimera-classification
        //              logic is introduced — the existing ChimeraComparer grouping
        //              inside GetChimeraBreakdownFile() is reused as-is. The plot
        //              helper and record type are identical to the planned path.
        // PSM-only filter is applied via ResultType.Psm inside the helper, so we do
        // not need to pre-filter peptide records.
        // CACHE NOTE:  GetChimeraBreakdownFile() early-returns a cached CSV via CsvHelper
        //              (line 322-323). A previously truncated 79MB
        //              ChimeraBreakdownComparison.csv was left in the search task dir
        //              from a prior interrupted run; CsvHelper throws on the truncated
        //              last row. Setting mmResult.Override = Parameters.Override makes
        //              the function regenerate the file when -o true is passed, sidestepping
        //              the broken cache read.
        mmResult.Override = Parameters.Override;
        var chimeraBreakdownFile = mmResult.GetChimeraBreakdownFile();
        var chimeraBreakdownRecords = chimeraBreakdownFile.Results.ToList();
        Log($"Chimera breakdown records: {chimeraBreakdownRecords.Count}");

        // 9a. Aggregate across the entire dataset
        EmitChimeraCompositionPlot(
            chimeraBreakdownRecords,
            title: "Mixed Species Revisions — Aggregate (PSM)",
            filePathNoExt: Path.Combine(outputDir, "MixedSpeciesRevisions_Chimera_Aggregate_PSM"));

        // 9b. Per-file (13 files) — iterate joinedRows in canonical design order
        //     so file names follow the verified 4 / 4 / 5 design join.
        foreach (var row in joinedRows)
        {
            var perFileRecords = chimeraBreakdownRecords
                .Where(p => p.FileName.Equals(row.NormalizedJoinKey, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var perFileNoExt = Path.Combine(
                outputDir,
                $"MixedSpeciesRevisions_Chimera_File_{Path.GetFileNameWithoutExtension(row.RawFileName)}");

            EmitChimeraCompositionPlot(
                perFileRecords,
                title: $"Mixed Species Revisions — File {Path.GetFileNameWithoutExtension(row.RawFileName)} (PSM)",
                filePathNoExt: perFileNoExt);
        }

        // 9c. Aggregate-by-condition — summed membership follows the verified
        //     13-row design join (4 / 4 / 5), not the per-file source path.
        var conditionGroups = joinedRows
            .GroupBy(r => r.Condition)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        foreach (var conditionGroup in conditionGroups)
        {
            var allowedKeys = new HashSet<string>(
                conditionGroup.Select(r => r.NormalizedJoinKey),
                StringComparer.OrdinalIgnoreCase);

            var perConditionRecords = chimeraBreakdownRecords
                .Where(p => allowedKeys.Contains(p.FileName))
                .ToList();

            var conditionFileName = $"MixedSpeciesRevisions_Chimera_Condition_{ConditionLetterFor(conditionGroup.Key)}_PSM";
            var conditionNoExt = Path.Combine(outputDir, conditionFileName);

            EmitChimeraCompositionPlot(
                perConditionRecords,
                title: $"Mixed Species Revisions — Condition {ConditionLetterFor(conditionGroup.Key)} (PSM)",
                filePathNoExt: conditionNoExt);
        }

        // ── 10. Condition-by-replicate overview grid (15-slot, 3×5) ────────
        // 13 design rows + 2 empty slots (A5, B5) → 15 panels in a 3-row by
        // 5-column grid. Rows are conditions (A, B, C in canonical 1/2/3
        // order); columns are replicate slots 1-5. Per-file records are
        // pulled from the same in-memory chimeraBreakdownRecords used in
        // Step 9b — no image stitching and no re-reads from disk. Empty
        // slots render a minimal "n/a (no replicate)" annotation so the
        // grid preserves the empty cells visibly.
        const int gridCols = 5;
        var conditionLetters = new[] { "A", "B", "C" };

        // Build panel records in canonical (Condition letter, ReplicateSlot) order.
        var panelRecords = new List<PanelLayoutRecord>(gridCols * conditionLetters.Length);
        var panelCharts = new List<GenericChart.GenericChart>(gridCols * conditionLetters.Length);

        for (int rowIdx = 0; rowIdx < conditionLetters.Length; rowIdx++)
        {
            var conditionLetter = conditionLetters[rowIdx];
            var conditionDigit = ConditionDigitFor(conditionLetter);

            // Files belonging to this condition in canonical design order.
            var filesInCondition = joinedRows
                .Where(r => r.Condition == conditionDigit)
                .OrderBy(r => r.DesignOrder)
                .ToList();

            for (int slot = 1; slot <= gridCols; slot++)
            {
                // Find the design row (if any) that occupies this slot.
                var matched = filesInCondition.FirstOrDefault(f => f.Biorep == slot);

                if (matched is not null)
                {
                    var perFileRecords = chimeraBreakdownRecords
                        .Where(p => p.FileName.Equals(matched.NormalizedJoinKey, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    var chart = ChimeraBreakdownPlots.GetChimeraBreakDownStackedColumn(
                        perFileRecords, ResultType.Psm, isTopDown: false, out int _,
                        extraTitle: $"{conditionLetter}{slot}");

                    panelCharts.Add(chart);
                    panelRecords.Add(new PanelLayoutRecord(
                        GridRow: rowIdx + 1,
                        GridColumn: slot,
                        Condition: conditionLetter,
                        ReplicateSlot: slot,
                        PanelType: "Data",
                        SearchFileName: matched.SearchFileName));
                }
                else
                {
                    // Preserved empty slot — render minimal annotation chart.
                    panelCharts.Add(BuildPlaceholderPanel(conditionLetter, slot));
                    panelRecords.Add(new PanelLayoutRecord(
                        GridRow: rowIdx + 1,
                        GridColumn: slot,
                        Condition: conditionLetter,
                        ReplicateSlot: slot,
                        PanelType: "Placeholder",
                        SearchFileName: ""));
                }
            }
        }

        // Sanity guard: 15 slots total, 13 data + 2 placeholders.
        if (panelRecords.Count != 15
            || panelRecords.Count(r => r.PanelType == "Data") != 13
            || panelRecords.Count(r => r.PanelType == "Placeholder") != 2)
        {
            throw new InvalidOperationException(
                $"Overview grid expected 15 panels (13 Data + 2 Placeholder) " +
                $"but got {panelRecords.Count} ({panelRecords.Count(r => r.PanelType == "Data")} Data, " +
                $"{panelRecords.Count(r => r.PanelType == "Placeholder")} Placeholder).");
        }

        // Compose the 3×5 overview grid. Chart.Grid with Pattern=Independent
        // gives each subplot its own axes (mirrors SummaryPlots.cs).
        var overviewGrid = Chart.Grid(
                panelCharts,
                conditionLetters.Length, gridCols,
                Pattern: StyleParam.LayoutGridPattern.Independent,
                YGap: 0.05)
            .WithTitle("Mixed Species Revisions — Chimera Overview Grid (PSM)")
            .WithSize(2000, 1400)
            .WithLayout(PlotlyBase.DefaultLayout);

        var overviewGridPath = Path.Combine(outputDir, "MixedSpeciesRevisions_Chimera_OverviewGrid_PSM");
        overviewGrid.SavePNG(overviewGridPath, null, 2000, 1400);
        Log($"Overview grid saved: {overviewGridPath}");

        // ── 10b. Emit the 15-row machine-readable panel-layout TSV ────────
        var panelLayoutPath = Path.Combine(outputDir, "MixedSpeciesRevisions_PanelLayout.tsv");
        using (var writer = new StreamWriter(panelLayoutPath))
        {
            writer.WriteLine("GridRow\tGridColumn\tCondition\tReplicateSlot\tPanelType\tSearchFileName");
            foreach (var panel in panelRecords)
            {
                writer.WriteLine(
                    $"{panel.GridRow}\t{panel.GridColumn}\t{panel.Condition}\t{panel.ReplicateSlot}\t{panel.PanelType}\t{panel.SearchFileName}");
            }
        }
        Log($"Panel layout TSV saved: {panelLayoutPath}");
        Log($"Panel layout: 15 rows total (13 Data + 2 Placeholder)");

        // ── 11. Emit deterministic output manifest (24 entries) ───────────
        // Machine-readable inventory of every artifact produced by this task.
        // RelativePath values are relative to the RevisionsParsing output directory.
        var manifestEntries = new List<(string ArtifactName, string ArtifactType)>();

        // 3 machine-readable TSVs
        manifestEntries.Add(("MixedSpeciesRevisions_DesignJoinSummary.tsv", "TSV"));
        manifestEntries.Add(("MixedSpeciesRevisions_PanelLayout.tsv", "TSV"));
        manifestEntries.Add(("MixedSpeciesRevisions_OutputManifest.tsv", "TSV"));

        // 3 per-file count bar charts (PSM, Peptide, Protein)
        manifestEntries.Add(("MixedSpeciesRevisions_Counts_PSM.png", "PNG"));
        manifestEntries.Add(("MixedSpeciesRevisions_Counts_Peptide.png", "PNG"));
        manifestEntries.Add(("MixedSpeciesRevisions_Counts_Protein.png", "PNG"));

        // 1 aggregate chimera composition plot (PSM)
        manifestEntries.Add(("MixedSpeciesRevisions_Chimera_Aggregate_PSM.png", "PNG"));

        // 3 condition-level chimera composition plots (A, B, C)
        manifestEntries.Add(("MixedSpeciesRevisions_Chimera_Condition_A_PSM.png", "PNG"));
        manifestEntries.Add(("MixedSpeciesRevisions_Chimera_Condition_B_PSM.png", "PNG"));
        manifestEntries.Add(("MixedSpeciesRevisions_Chimera_Condition_C_PSM.png", "PNG"));

        // 13 per-file chimera composition plots — iterate joinedRows in canonical design order
        // so the manifest order matches the per-file emit order in Step 9b.
        foreach (var row in joinedRows)
        {
            var fileStem = Path.GetFileNameWithoutExtension(row.RawFileName);
            manifestEntries.Add(
                ($"MixedSpeciesRevisions_Chimera_File_{fileStem}.png", "PNG"));
        }

        // 1 overview grid (15-slot, 3×5)
        manifestEntries.Add(("MixedSpeciesRevisions_Chimera_OverviewGrid_PSM.png", "PNG"));

        // Assert the hard contract: exactly 24 entries.
        if (manifestEntries.Count != 24)
        {
            throw new InvalidOperationException(
                $"Output manifest expected exactly 24 entries but built {manifestEntries.Count}. " +
                $"Breakdown: 3 TSV + 3 count PNG + 1 aggregate + 3 condition + " +
                $"{joinedRows.Count} per-file + 1 overview grid.");
        }

        var manifestPath = Path.Combine(outputDir, "MixedSpeciesRevisions_OutputManifest.tsv");
        using (var writer = new StreamWriter(manifestPath))
        {
            writer.WriteLine("ArtifactName\tRelativePath\tArtifactType");
            foreach (var (name, type) in manifestEntries)
            {
                // All artifacts are emitted directly into the output directory,
                // so the relative path is simply the filename.
                writer.WriteLine($"{name}\t{name}\t{type}");
            }
        }

        Log($"Output manifest written: {manifestPath}");
        Log($"Manifest entries: {manifestEntries.Count} " +
            $"(3 TSV + 3 count PNG + 1 aggregate + 3 condition + " +
            $"{joinedRows.Count} per-file + 1 overview grid)");
    }

    /// <summary>
    /// Renders a PSM-level chimera composition stacked-column chart and saves it
    /// to PNG. Thin orchestration wrapper around
    /// <see cref="ChimeraBreakdownPlots.GetChimeraBreakDownStackedColumn"/> that
    /// centralises the ResultType.Psm filter, default sizing, and
    /// no-extra-suffix path convention (SavePNG appends .png).
    /// </summary>
    private static void EmitChimeraCompositionPlot(
        List<ChimeraBreakdownRecord> records,
        string title,
        string filePathNoExt)
    {
        if (records.Count == 0)
            throw new InvalidOperationException(
                $"Cannot emit chimera composition plot — zero ChimeraBreakdownRecord entries. " +
                $"Title: \"{title}\"");

        var chart = ChimeraBreakdownPlots.GetChimeraBreakDownStackedColumn(
            records, ResultType.Psm, isTopDown: false, out int width, extraTitle: title);

        chart.WithTitle(title).SavePNG(filePathNoExt, null, width, PlotlyBase.DefaultHeight);
    }

    /// <summary>
    /// Emits a per-file count bar chart using design-order labels from
    /// joinedRows for the x-axis sequence, replacing the generic helper's
    /// internal alphabetical sort with explicit design-order ordering.
    /// </summary>
    private static void EmitCountChartFromDesignOrder(
        List<BulkResultCountComparison> results,
        List<string> designOrderLabels,
        string title,
        ResultType resultType,
        string filePathNoExt)
    {
        var selector = AnalyzerGenericPlots.ResultSelector(resultType);
        var values = results.Select(selector).ToList();

        var displayLabels = designOrderLabels
            .Select(l => l.ConvertFileName())
            .ToList();

        var width = Math.Max(50 * displayLabels.Count + 10, 800);

        var chart = Chart2D.Chart.Column<int, string, string, int, int>(
                values, displayLabels, null,
                results.First().Condition.ConvertConditionName(),
                MarkerColor: results.First().Condition.ConvertConditionToColor(),
                MultiText: values.Select(p => p.ToString()).ToArray())
            .WithTitle($"{title} 1% FDR {Labels.GetLabel(false, resultType)}")
            .WithXAxisStyle(Title.init("File"))
            .WithYAxisStyle(Title.init("Count"))
            .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
            .WithSize(width, PlotlyBase.DefaultHeight);

        chart.SavePNG(filePathNoExt, null, width, PlotlyBase.DefaultHeight);
        Log($"{resultType} count chart saved: {filePathNoExt}");
    }

    /// <summary>
    /// Maps the design-join Condition string ("1", "2", "3") to the canonical
    /// plot-file letter ("A", "B", "C") used in the per-file filenames and
    /// ExperimentalDesign.tsv.
    /// </summary>
    private static string ConditionLetterFor(string condition) => condition switch
    {
        "1" => "A",
        "2" => "B",
        "3" => "C",
        _ => condition,
    };

    /// <summary>
    /// Inverse of <see cref="ConditionLetterFor"/>: maps "A"/"B"/"C" to the
    /// design-join Condition string ("1"/"2"/"3").
    /// </summary>
    private static string ConditionDigitFor(string conditionLetter) => conditionLetter switch
    {
        "A" => "1",
        "B" => "2",
        "C" => "3",
        _ => conditionLetter,
    };

    /// <summary>
    /// Builds a minimal "n/a (no replicate)" placeholder chart used to
    /// preserve empty slots (A5, B5) inside the 3×5 overview grid. The
    /// chart has no data points and uses the cell title to communicate
    /// that no replicate exists. We deliberately avoid Plotly.NET's
    /// Annotation.init API here because the strong-typed generic argument
    /// list varies between Plotly.NET versions and is brittle to maintain.
    /// </summary>
    private static GenericChart.GenericChart BuildPlaceholderPanel(string conditionLetter, int slot)
    {
        var empty = Chart.Scatter<double, double, string>(
            new double[0], new double[0],
            StyleParam.Mode.Markers,
            Name: "placeholder")
            .WithTitle($"{conditionLetter}{slot}\n(n/a — no replicate)")
            .WithXAxis(Plotly.NET.LayoutObjects.LinearAxis.init<double, double, double, double, double, double>(
                ShowGrid: false, ShowLine: false, ZeroLine: false, ShowTickLabels: false))
            .WithYAxis(Plotly.NET.LayoutObjects.LinearAxis.init<double, double, double, double, double, double>(
                ShowGrid: false, ShowLine: false, ZeroLine: false, ShowTickLabels: false))
            .WithLayout(Layout.init<string>(ShowLegend: false));

        return empty;
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

/// <summary>
/// One row of the overview-grid panel-layout TSV. Records the
/// (GridRow, GridColumn) cell, the condition letter, the replicate slot,
/// the PanelType (Data or Placeholder), and — for Data panels — the
/// SearchFileName produced by the design-join step.
/// </summary>
internal sealed record PanelLayoutRecord(
    int GridRow,
    int GridColumn,
    string Condition,
    int ReplicateSlot,
    string PanelType,
    string SearchFileName
);
