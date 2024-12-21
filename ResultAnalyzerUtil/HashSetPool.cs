using Microsoft.Extensions.ObjectPool;

namespace ResultAnalyzerUtil;


    // Example Usage:
    // var pool = new HashSetPool<int>();
    // var hashSet = pool.Get();
    // hashSet.Add(1);
    // Do Work
    // pool.Return(hashSet);

    // Used to pool HashSet instances to reduce memory allocations
    public class HashSetPool<T>
{
    private readonly ObjectPool<HashSet<T>> _pool;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashSetPool{T}"/> class.
    /// </summary>
    /// <param name="initialCapacity">Initial capacity for the pooled HashSet instances.</param>
    public HashSetPool(int initialCapacity = 16)
    {
        var policy = new HashSetPooledObjectPolicy<T>(initialCapacity);
        var provider = new DefaultObjectPoolProvider { MaximumRetained = Environment.ProcessorCount * 2 };
        _pool = provider.Create(policy);
    }

    /// <summary>
    /// Retrieves a HashSet instance from the pool.
    /// </summary>
    /// <returns>A HashSet instance.</returns>
    public HashSet<T> Get() => _pool.Get();

    /// <summary>
    /// Returns a HashSet instance back to the pool.
    /// </summary>
    /// <param name="hashSet">The HashSet instance to return.</param>
    public void Return(HashSet<T> hashSet)
    {
        if (hashSet == null) throw new ArgumentNullException(nameof(hashSet));
        hashSet.Clear(); // Ensure the HashSet is clean before returning it to the pool
        _pool.Return(hashSet);
    }

    private class HashSetPooledObjectPolicy<TItem> : PooledObjectPolicy<HashSet<TItem>>
    {
        private int InitialCapacity { get; }
        public HashSetPooledObjectPolicy(int initialCapacity)
        {
            InitialCapacity = initialCapacity;
        }

        public override HashSet<TItem> Create()
        {
            return new HashSet<TItem>(capacity: InitialCapacity);
        }

        public override bool Return(HashSet<TItem> obj)
        {
            // Ensure the HashSet can be safely reused
            obj.Clear();
            return true;
        }
    }
}