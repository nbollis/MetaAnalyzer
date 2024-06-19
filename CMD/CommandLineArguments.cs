using CMD.Util;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMD
{
    internal class CommandLineArguments
    {

        public List<CommandLineTasks> Tasks { get; set; }

        [Option('t', HelpText = "The task to run; comma-delimited", Required = true)]
        public IEnumerable<int> _taskIntegers { get; set; }

        [Option('i', HelpText = "The input Directory", Required = true)]
        public string InputDirectory { get; set; }

        [Option('o', HelpText = "The output Directory", Required = false, Default = false)]
        public bool OverrideFiles { get; set; }


        public void ValidateCommandLineSettings()
        {
            if (_taskIntegers == null)
            {
                throw new Exception("No tasks specified");
            }

            Tasks = _taskIntegers.Select(p => (CommandLineTasks)p).ToList();

            if (string.IsNullOrWhiteSpace(InputDirectory))
            {
                throw new Exception("No input directory specified");
            }
        }
    }
}
