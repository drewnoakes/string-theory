using System;
using System.Diagnostics;

namespace StringTheory.Pools
{
    public sealed class WeakStringPool : WeakPool<string>
    {
        public WeakStringPool()
            : base(StringComparer.Ordinal)
        {
        }

#if NETSTANDARD2_1
        public string InternSpan(ReadOnlySpan<char> span)
        {
            int hash = HashSpan(span);

            if (Count >= _capacity)
            {
                // TODO attempt prune before grow, with some hysteresis or flag to prevent endless pruning
                Grow();
            }

            var pos = (uint)hash % _buckets.Length;
            var bucket = _buckets[pos];
            
            switch (bucket)
            {
                case WeakReference<string> weakRef when weakRef.TryGetTarget(out string target):
                {
                    // existing weak ref has a target. is it the right one?
                    if (SpanEquals(target, span))
                    {
                        return target;
                    }

                    // create a new node, and prepend to chain
                    var value = new string(span);
                    var node1 = CreateNode(CreateWeakRef(value));
                    var node2 = CreateNode(weakRef);
                    node1.Next = node2;
                    _buckets[pos] = node1;
                    Count++;
                    return value;
                }

                case WeakReference<string> weakRef:
                {
                    // existing weak reference has no target, so reuse it
                    var value = new string(span);
                    weakRef.SetTarget(value);
                    return value;
                }

                case Node node:
                {
                    // walk existing chain, checking for a match
                    var first = (Node)null;
                    var prev = (Node)null;
                    var next = node;
                    do
                    {
                        if (next.WeakRef.TryGetTarget(out string target))
                        {
                            // target is alive
                            if (SpanEquals(target, span))
                            {
                                // match found
                                return target;
                            }

                            // remember the first live node so we can prepend to it if necessary
                            if (first == null)
                                first = next;

                            // move to next item
                            prev = next;
                            next = next.Next;
                        }
                        else
                        {
                            // target was collected
                            if (prev != null)
                            {
                                prev.Next = next.Next;
                            }

                            _weakRefPool.Push(next.WeakRef);
                            _nodePool.Push(next);
                            Count--;
                            next = next.Next;
                        }
                    }
                    while (next != null);

                    // not found
                    var value = new string(span);
                    var weakRef = CreateWeakRef(value);
                    
                    if (prev == null)
                    {
                        // all nodes were removed
                        _buckets[pos] = weakRef;
                    }
                    else if (first != null)
                    {
                        // prepend to chain
                        var n = CreateNode(weakRef);
                        n.Next = first;
                        _buckets[pos] = n;
                    }
                    else
                    {
                        // all nodes in the existing chain were removed
                        _buckets[pos] = weakRef;
                    }
                    Count++;
                    return value;
                }

                case null:
                {
                    Count++;
                    var value = new string(span);
                    _buckets[pos] = CreateWeakRef(value);
                    return value;
                }

                default:
                {
                    Debug.Fail("Bucket had prohibited type");
                    return null;
                }
            }

            WeakReference<string> CreateWeakRef(string value)
            {
                if (_weakRefPool.TryPop(out WeakReference<string> weakRef))
                {
                    weakRef.SetTarget(value);
                    return weakRef;
                }

                return new WeakReference<string>(value);
            }
        }

        protected override int ComputeHash(string value)
        {
            return HashSpan(value);
        }

        private static int HashSpan(in ReadOnlySpan<char> span)
        {
            int hash = (5381 << 16) + 5381;

            for (int i = 0; i < span.Length; i++)
            {
                int val = span[i];
                hash = ((hash << 5) + hash + (hash >> 27)) ^ val;
            }

            return hash;
        }

        private static bool SpanEquals(string s, in ReadOnlySpan<char> span)
        {
            if (s.Length != span.Length)
            {
                return false;
            }

            if (s.Length == 0)
            {
                return true;
            }

            unsafe
            {
                fixed (char* pstr = s)
                fixed (char* pspan = span)
                {
                    char* a = pstr;
                    char* b = pspan;

                    var length = s.Length;

                    while (length-- != 0)
                    {
                        if (*a++ != *b++)
                            return false;
                    }

                    return true;
                }
            }
        }
#endif
    }
}