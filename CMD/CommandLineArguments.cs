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
        public List<string> ResultDirectoryPaths { get; private set; }

        [Option('s', HelpText = "Folder(s) containing search results; space-delimited")]
        public IEnumerable<string> _resultDirectory { get; set; }
    }
}
