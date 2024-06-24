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

        // Chimera analysis specific

        [Option("rc", HelpText = "The output Directory", Required = false, Default = false)]
        public bool RunChimeraBreakdown { get; set; }

        [Option("ra", HelpText = "Run on all", Required = false, Default = false)]
        public bool RunOnAll { get; set; }

        [Option("rfdr", HelpText = "Run FDR analysis", Required = false, Default = false)]
        public bool RunFdrAnalysis { get; set; }

        [Option("rrc", HelpText = "Run result counting", Required = false, Default = false)]
        public bool RunResultCounting { get; set; }

        [Option("rcc", HelpText = "Count chimeric results", Required = false, Default = false)]
        public bool RunChimericCounting { get; set; }


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
