using Analyzer.Plotting.Util;
using CommandLine;
using TaskLayer;

namespace CMD
{
    internal class CommandLineArguments
    {

        public List<MyTask> Tasks { get; set; }

        [Option('t', HelpText = "The task to run; comma-delimited", Required = true)]
        public IEnumerable<int> _taskIntegers { get; set; }

        [Option('i', HelpText = "The input Directory", Required = true)]
        public string InputDirectory { get; set; }

        [Option('o', HelpText = "The output Directory", Required = false, Default = false)]
        public bool OverrideFiles { get; set; }

        [Option('d', HelpText = "The database path", Required = false, Default = false)]
        public string DatabasePath { get; set; }

        [Option('s', HelpText = "The spectra paths", Required = false, Default = false)]
        public string SpectraFileDirectory { get; set; }

        [Option('w', HelpText = "The working directory", Required = false, Default = false)]
        public string OutputDirectory { get; set; }

        [Option('~', HelpText = "The MetaMorpheus path", Required = false, Default = false)]
        public string MetaMorpheusPath { get; set; }


        // Chimera analysis specific

        [Option('b', HelpText = "The output Directory", Required = false, Default = false)]
        public bool RunChimeraBreakdown { get; set; }

        [Option('a', HelpText = "Run on all", Required = false, Default = false)]
        public bool RunOnAll { get; set; }

        [Option('f', HelpText = "Run FDR analysis", Required = false, Default = false)]
        public bool RunFdrAnalysis { get; set; }

        [Option('r', HelpText = "Run result counting", Required = false, Default = false)]
        public bool RunResultCounting { get; set; }

        [Option('c', HelpText = "Count chimeric results", Required = false, Default = false)]
        public bool RunChimericCounting { get; set; }

        [Option('m', HelpText = "Run modification analysis", Required = false, Default = false)]
        public bool RunModificationAnalysis { get; set; }

        [Option('p', HelpText = "The plot type", Required = false, Default = DistributionPlotTypes.ViolinPlot)]
        public DistributionPlotTypes PlotType { get; set; }


        public void ValidateCommandLineSettings()
        {
            if (_taskIntegers == null)
            {
                throw new Exception("No tasks specified");
            }

            Tasks = _taskIntegers.Select(p => (MyTask)p).ToList();

            if (string.IsNullOrWhiteSpace(InputDirectory))
            {
                throw new Exception("No input directory specified");
            }
        }
    }
}
