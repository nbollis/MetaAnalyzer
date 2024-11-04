using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace RadicalFragmentation
{
    public class CommandLineArguments
    {
        [Option('d', HelpText = "The database path", Required = true)]
        public string DatabasePath { get; set; }

        [Option('m', HelpText = "Mods To Consider", Required = true)]
        public int ModsToConsider { get; set; }

        [Option('o', HelpText = "The output Directory", Required = false, Default = false)]
        public string OutputDirectory { get; set; }

        [Option('a', HelpText = "Ambiguity Level\n1-> Proteoform Resolution\n2-> Protein Resolution", Required = true)]
        public int AmbiguityLevel { get; set; }
    }
}
