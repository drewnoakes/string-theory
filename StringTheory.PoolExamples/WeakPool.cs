using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StringTheory.PoolExamples
{
    // NOTE these weak pools are not thread safe, though it might be possible/desirable to make them so

    public sealed class WeakStringPool : WeakPool<string>
    {
        public WeakStringPool()
            : base(StringComparer.Ordinal)
        {
        }

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
    }

    public class WeakPool<T> where T : class
    {
        protected sealed class Node
        {
            public WeakReference<T> WeakRef { get; set; }
            public Node Next { get; set; }
        }

        protected readonly Stack<WeakReference<T>> _weakRefPool = new Stack<WeakReference<T>>();
        protected readonly Stack<Node> _nodePool = new Stack<Node>();

        private readonly IEqualityComparer<T> _comparer;

        // TODO review these initial size/capacity values
        protected object[] _buckets = new object[2048];
        protected int _capacity = 1536;

        public int Count { get; protected set; }

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

            var hash = ComputeHash(value);
            var pos = (uint)hash % _buckets.Length;
            var bucket = _buckets[pos];
            
            switch (bucket)
            {
                case WeakReference<T> weakRef when weakRef.TryGetTarget(out T target):
                {
                    // existing weak ref has a target. is it the right one?
                    if (_comparer.Equals(target, value))
                    {
                        return target;
                    }

                    // create a new node, and prepend to chain
                    var node1 = CreateNode(CreateWeakRef());
                    var node2 = CreateNode(weakRef);
                    node1.Next = node2;
                    _buckets[pos] = node1;
                    Count++;
                    return value;
                }

                case WeakReference<T> weakRef:
                {
                    // existing weak reference has no target, so reuse it
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
                        if (next.WeakRef.TryGetTarget(out T target))
                        {
                            // target is alive
                            if (_comparer.Equals(target, value))
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
                    var weakRef = CreateWeakRef();
                    
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
                    _buckets[pos] = CreateWeakRef();
                    return value;
                }

                default:
                {
                    Debug.Fail("Bucket had prohibited type");
                    return null;
                }
            }

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

        protected virtual int ComputeHash(T value)
        {
            return _comparer.GetHashCode(value);
        }

        protected Node CreateNode(WeakReference<T> weakRef)
        {
            if (_nodePool.TryPop(out Node n))
            {
                n.WeakRef = weakRef;
                return n;
            }

            return new Node { WeakRef = weakRef };
        }

        protected void Grow()
        {
            _capacity *= 2;

            var newBuckets = new object[_buckets.Length * 2];

            foreach (var bucket in _buckets)
            {
                switch (bucket)
                {
                    case WeakReference<T> weakRef:
                    {
                        Hash(weakRef);
                        break;
                    }

                    case Node node:
                    {
                        var next = node;
                        do
                        {
                            Hash(next.WeakRef);

                            var tmp = next.Next;
                            next.Next = null;
                            next.WeakRef = null;
                            _nodePool.Push(next);

                            next = tmp;
                        }
                        while (next != null);
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
                            // value was collected
                            _weakRefPool.Push(existingWeakRef);
                            newBuckets[index] = weakRef;
                        }
                        else
                        {
                            // prepend
                            var node1 = CreateNode(weakRef);
                            var node2 = CreateNode(existingWeakRef);
                            node1.Next = node2;
                            newBuckets[index] = node1;
                        }
                        break;
                    }

                    case Node node:
                    {
                        node.Next = CreateNode(weakRef);
                        break;
                    }
                }
            }
        }
    }
}