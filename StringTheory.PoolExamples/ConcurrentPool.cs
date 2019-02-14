using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace StringTheory.PoolExamples
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Objects added to this pool will never be released.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public sealed class ConcurrentPool<T> where T : class
    {
        private readonly ConcurrentDictionary<T, T> _set;

        public int Count => _set.Count;

        public ConcurrentPool(IEqualityComparer<T> comparer = null)
        {
            _set = new ConcurrentDictionary<T, T>(comparer);
        }

        public T Intern(T value)
        {
            // Assuming a high hit rate, attempt retrieval first
            if (_set.TryGetValue(value, out T stored))
                return stored;
            if (_set.TryAdd(value, value))
                return value;
            if (_set.TryGetValue(value, out stored))
                return stored;
            throw new InvalidOperationException("Comparer is not behaving correctly.");
        }
    }
}
