using CommandLine;
using ResultAnalyzerUtil.CommandLine;

namespace RadicalFragmentation
{
    public class CommandLineArguments : ICommandLineParameters
    {
        public string ProgramName => "RadicalFragmentation";
        public VerbosityType Verbosity { get; } = VerbosityType.Normal;

        [Option('d', HelpText = "The database path", Required = true)]
        public string DatabasePath { get; set; }

        [Option('m', HelpText = "Mods To Consider", Required = true)]
        public int ModsToConsider { get; set; }

        [Option('a', HelpText = "Ambiguity Level\n1-> Proteoform Resolution\n2-> Protein Resolution", Required = true)]
        public int AmbiguityLevel { get; set; }

        [Option('f', Required = true, Default = 1, HelpText = "Type of fragmentation to perform\n1 -> Tryptophan\n2 -> Cysteine\n3 -> ETD")]
        public int FragmentAmbiguityType { get; set; }

        internal FragmentExplorerType FragmentExplorerType => (FragmentExplorerType)FragmentAmbiguityType;

        [Option('o', HelpText = "The output Directory", Required = false, Default = false)]
        public string? OutputDirectory { get; set; }

        [Option('l', HelpText="Label", Required = false, Default = "Human")]
        public string? Label { get; set; }

        [Option('i', HelpText = "MissedMonoIsotopics", Required = false, Default = 0)]
        public int MissedMonoIsoTopics { get; set; }

        [Option('t', HelpText = "Maximum threads for RadicalFragmentor to use", Required = false, Default = 1)]
        public int MaxThreads { get; set; }

        public void ValidateCommandLineSettings()
        {
            if (string.IsNullOrEmpty(DatabasePath))
                throw new Exception("No database path specified");
            else if (!File.Exists(DatabasePath))
                throw new Exception($"Database at {DatabasePath} does not exist");

            if (ModsToConsider < 0)
                throw new Exception("Mods to consider must be greater than zero");

            if (AmbiguityLevel < 1 || AmbiguityLevel > 2)
                throw new Exception("Ambiguity level must be 1 or 2");

            if (MissedMonoIsoTopics < 0)
                throw new Exception("Missed Mono Isotopics must be greater than or equal to zero");

            if (MaxThreads < 1)
                throw new Exception("Max threads must be greater than zero");
            if (MaxThreads > Environment.ProcessorCount)
                MaxThreads = Environment.ProcessorCount;

            if (string.IsNullOrEmpty(DatabasePath))
                OutputDirectory = Directory.GetDirectoryRoot(DatabasePath);
            else if (!Directory.Exists(OutputDirectory))
                Directory.CreateDirectory(OutputDirectory);
        }
    }
}
