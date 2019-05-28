using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StringTheory.PoolExamples
{
    public sealed class WeakPool<T> where T : class
    {
        private sealed class Node
        {
            public WeakReference<T> WeakRef { get; set; }
            public Node Next { get; set; }
        }

        private readonly Stack<WeakReference<T>> _weakRefPool = new Stack<WeakReference<T>>();
        private readonly Stack<Node> _nodePool = new Stack<Node>();

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

        private Node CreateNode(WeakReference<T> weakRef)
        {
            if (_nodePool.TryPop(out Node n))
            {
                n.WeakRef = weakRef;
                return n;
            }

            return new Node { WeakRef = weakRef };
        }

        private void Grow()
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