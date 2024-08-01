using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaAnalyzerServer
{
    internal class CommandLineArguments
    {
    }

    public class Process
    {
        public int Id { get; set; }
        public TaskStatus Status { get; set; }
    }

    public class CmdProcess : Process
    {
        public string Prompt { get; set; }
    }

    public enum TaskStatus
    {
        Pending,
        Running,
        Completed,
        Failed
    }

    
}
