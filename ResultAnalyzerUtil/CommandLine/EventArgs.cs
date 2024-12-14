namespace ResultAnalyzerUtil.CommandLine
{
    public class StringEventArgs : EventArgs
    {
        public StringEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }


    public class SingleFileEventArgs : EventArgs
    {
        public SingleFileEventArgs(string writtenFile)
        {
            WrittenFile = writtenFile;
        }
        public string WrittenFile { get; private set; }
    }

    public class SubProcessEventArgs : EventArgs
    {
        // TODO: Define a subprocess interface that can be used to handle subprocesses identifying information
        public SubProcessEventArgs(string subProcessIdentifier)
        {
            SubProcessIdentifier = subProcessIdentifier;
        }
        public string SubProcessIdentifier { get; }
    }

    public class ProgressBarEventArgs : EventArgs
    {
        public ProgressBarEventArgs(string progressBarName, double progress)
        {
            ProgressBarName = progressBarName;
            Progress = progress;
        }
        public string ProgressBarName { get; }
        public double Progress { get; }
    }
}
