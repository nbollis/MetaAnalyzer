namespace RadicalFragmentation;

internal class StringEventArgs : EventArgs
{
    public StringEventArgs(string message)
    {
        Message = message;
    }

    public string Message { get; }
}