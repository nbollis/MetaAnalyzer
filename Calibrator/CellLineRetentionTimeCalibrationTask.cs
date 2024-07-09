using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using TaskLayer;
using TaskLayer.ChimeraAnalysis;

namespace Calibrator
{
    public class CellLineRetentionTimeCalibrationTask : BaseResultAnalyzerTask
    {
        public override MyTask MyTask => MyTask.RetentionTimeAlignment;
        public override CellLineAnalysisParameters Parameters { get; }

        public CellLineRetentionTimeCalibrationTask(CellLineAnalysisParameters parameters)
        {
            Parameters = parameters;
        }

        protected override void RunSpecific()
        {
            var potentialRuns = Parameters.CellLine
                .Where(p => Parameters.CellLine.GetSingleResultSelector().Contains(p.Condition)).ToList();
            if (!potentialRuns.Any())
                throw new ArgumentException($"Cell Line does not contain result in single run selector");
            if (potentialRuns.Count() > 1) 
                throw new ArgumentException("Cell Line contains multiple results in single run selector");

            var run = potentialRuns.First();

            if (run is not MetaMorpheusResult mm)
                throw new ArgumentException("Selected run is not from MetaMorpheus");

            if (!mm.IndividualFileResults.Any())
                throw new ArgumentException("Selected run does not contain any individual file results");

            var peptideFilePaths = mm.IndividualFileResults.Select(p => p.PeptidePath).ToList();
            var calibrator = new CalibratorClass(peptideFilePaths);
            calibrator.Calibrate();

            var outPath = Path.Combine(Parameters.CellLine.DirectoryPath, "AdjustedRetentionTimes.tsv");
            calibrator.WriteFile(outPath);
            var results = calibrator.FileLoggers.First().FileWiseCalibrations;


        }
    }
}
