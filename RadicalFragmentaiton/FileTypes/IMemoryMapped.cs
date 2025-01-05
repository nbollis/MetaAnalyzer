using Readers;

namespace RadicalFragmentation;
public interface IMemoryMapped<out T> : IResultFile, IDisposable
{
    public long Count { get; }
    public IEnumerable<T> ReadChunks(long offset, int chunkSize);
}

