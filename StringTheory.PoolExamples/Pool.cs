using System.Collections.Generic;

namespace StringTheory.PoolExamples
{
    /// <summary>
    /// A <see cref="HashSet{T}"/>-based object pool. Not thread-safe.
    /// </summary>
    /// <remarks>
    /// Objects added to this pool will never be released.
    /// </remarks>
    /// <typeparam name="T">The type of object to pool.</typeparam>
    public sealed class Pool<T> where T : class
    {
        private readonly HashSet<T> _set;

        public int Count => _set.Count;

        public Pool(IEqualityComparer<T> comparer = null)
        {
            _set = new HashSet<T>(comparer);
        }

        public T Intern(T value)
        {
            if (_set.TryGetValue(value, out T existingValue))
                return existingValue;

            _set.Add(value);
            return value;
        }
    }
}