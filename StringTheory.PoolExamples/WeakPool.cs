using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StringTheory.PoolExamples
{
    public sealed class WeakPool<T> where T : class
    {
        private readonly Stack<WeakReference<T>> _weakRefPool = new Stack<WeakReference<T>>();
        private readonly Stack<List<WeakReference<T>>> _listPool = new Stack<List<WeakReference<T>>>();

        private readonly IEqualityComparer<T> _comparer;

        // TODO review these initial size/capacity values
        private object[] _buckets = new object[2048];
        private int _capacity = 1536;

        public int Count { get; private set; }

        public WeakPool(IEqualityComparer<T> comparer = null)
        {
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        public T Intern(T value)
        {
            if (Count >= _capacity)
            {
                // TODO attempt prune before grow, with some hysteresis or flag to prevent endless pruning
                Grow();
            }

            var hash = _comparer.GetHashCode(value);
            var pos = (uint)hash % _buckets.Length;
            var bucket = _buckets[pos];

            {
                if (bucket is WeakReference<T> weakRef)
                {
                    if (weakRef.TryGetTarget(out T target))
                    {
                        if (_comparer.Equals(target, value))
                        {
                            return target;
                        }

                        Count++;
                        _buckets[pos] = new List<WeakReference<T>>(2) {weakRef, CreateWeakRef()};
                        return value;
                    }
                    else
                    {
                        weakRef.SetTarget(value);
                        return value;
                    }
                }
            }

            if (bucket is List<WeakReference<T>> list)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var weakRef = list[i];

                    if (weakRef.TryGetTarget(out T target))
                    {
                        if (_comparer.Equals(target, value))
                        {
                            return target;
                        }
                    }
                    else
                    {
                        _weakRefPool.Push(weakRef);
                        list.RemoveAt(i);
                        i--;
                        Count--;
                    }
                }

                Count++;

                list.Add(CreateWeakRef());
                return value;
            }

            if (bucket == null)
            {
                Count++;

                _buckets[pos] = CreateWeakRef();
                return value;
            }

            Debug.Fail("Bucket had prohibited type");
            return null;

            WeakReference<T> CreateWeakRef()
            {
                if (_weakRefPool.TryPop(out WeakReference<T> weakRef))
                {
                    weakRef.SetTarget(value);
                    return weakRef;
                }

                return new WeakReference<T>(value);
            }
        }

        private void Grow()
        {
            _capacity *= 2;

            var newBuckets = new object[_buckets.Length * 2];

            for (var i = 0; i < _buckets.Length; i++)
            {
                switch (_buckets[i])
                {
                    case WeakReference<T> weakRef:
                    {
                        Hash(weakRef);
                        break;
                    }

                    case List<WeakReference<T>> list:
                    {
                        for (var j = 0; j < list.Count; j++)
                        {
                            Hash(list[j]);
                        }

                        list.Clear();
                        _listPool.Push(list);
                        break;
                    }
                }
            }

            _buckets = newBuckets;

            void Hash(WeakReference<T> weakRef)
            {
                if (!weakRef.TryGetTarget(out T target))
                {
                    _weakRefPool.Push(weakRef);
                    return;
                }

                var hash = _comparer.GetHashCode(target);
                var index = Math.Abs(hash % newBuckets.Length);

                switch (newBuckets[index])
                {
                    case null:
                    {
                        newBuckets[index] = weakRef;
                        break;
                    }

                    case WeakReference<T> existingWeakRef:
                    {
                        if (!existingWeakRef.TryGetTarget(out _))
                        {
                            _weakRefPool.Push(existingWeakRef);
                            newBuckets[index] = weakRef;
                        }
                        else if (_listPool.TryPop(out var list))
                        {
                            list.Add(existingWeakRef);
                            list.Add(weakRef);
                            newBuckets[index] = list;
                        }
                        else
                        {
                            newBuckets[index] = new List<WeakReference<T>>(2) { existingWeakRef, weakRef };
                        }
                        break;
                    }

                    case List<WeakReference<T>> list:
                    {
                        list.RemoveAll(wr =>
                        {
                            var alive = wr.TryGetTarget(out _);
                            if (!alive)
                                _weakRefPool.Push(wr);
                            return !alive;
                        });

                        if (list.Count == 0)
                        {
                            _listPool.Push(list);
                            newBuckets[index] = weakRef;
                        }
                        else
                        {
                            list.Add(weakRef);
                        }
                        break;
                    }
                }
            }
        }
    }
}