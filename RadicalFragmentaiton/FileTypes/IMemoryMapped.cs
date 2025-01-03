using Readers;

namespace RadicalFragmentation;
public interface IMemoryMapped<T> : IResultFile, IDisposable
{
    public int Count { get; }
    public IEnumerable<T> ReadChunks(int chunkSize);
}

