using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Desktop;

namespace StringTheory.Analysis
{
    public static class ReferenceGraphBuilder
    {
        public static ReferenceGraph Build(ClrHeap heap, HashSet<ulong> targetAddresses, CancellationToken token = default)
        {
            var graph = new ReferenceGraph();

            var seen = new ObjectSet(heap);

            var stack = new HeapWalkStack();

            var nodeByAddress = new Dictionary<ulong, ReferenceGraphNode>();

            var chainCache = new Dictionary<(uint metadataToken, int fieldOffset), List<FieldReference>>();

            // For each root
            foreach (var root in Roots())
            {
                stack.Clear();

                if (root.Type == null)
                {
                    // This happens, though not sure why
                    continue;
                }

                stack.Push(new ClrObjectReference(-1, root.Object, root.Type));

                while (!stack.IsEmpty)
                {
                    token.ThrowIfCancellationRequested();

                    ref var top = ref stack.Peek();

                    if (top.Enumerator.MoveNext())
                    {
                        var o = top.Enumerator.Current;

                        if (targetAddresses.Contains(o.Address))
                        {
                            // Found a match
                            stack.Push(o);

                            // Ensure our stack's root is registered with the graph
                            ref var rootLevel = ref stack[0];
                            if (rootLevel.GraphNode == null)
                            {
                                var rootNode = new Root(root);
                                nodeByAddress[root.Object] = rootNode;
                                rootLevel.GraphNode = rootNode;
                                graph.Roots.Add(rootNode);
                            }

                            // Build path through graph for the current stack
                            // Skip the first node as it's always non-null
                            for (var i = 1; i < stack.Count; i++)
                            {
                                ref var level = ref stack[i];

                                // Ensure all levels have a graph node object
                                if (level.GraphNode == null)
                                {
                                    if (!nodeByAddress.TryGetValue(level.Reference.Address, out var node))
                                    {
                                        node = new ReferenceGraphNode(level.Reference.Object);
                                        nodeByAddress[level.Reference.Address] = node;

                                        if (i == stack.Count - 1)
                                        {
                                            graph.TargetSet.Add(node);
                                        }
                                    }

                                    level.GraphNode = node;
                                    
                                    ref var levelBefore = ref stack[i - 1];
                                    
                                    var referenceChain = GetChain(levelBefore.GraphNode.Object.Type, level.Reference);

                                    level.GraphNode.Referrers.Add((node: levelBefore.GraphNode, referenceChain, fieldOffset: level.Reference.FieldOffset));
//                                    levelBefore.GraphNode.References.Add((node: level.GraphNode, referenceChain));
                                }
                            }

                            stack.Pop();
                        }
                        
                        if (!seen.Contains(o.Address))
                        {
                            // New object; push it to the stack to start exploring it
                            stack.Push(o);
                            seen.Add(o.Address);
                        }
                    }
                    else
                    {
                        // Visited all items in this level; pop and continue the previous level
                        stack.Pop();
                    }
                }
            }

            return graph;

            IEnumerable<ClrRoot> Roots()
            {
                foreach (var strongHandle in heap.EnumerateStrongHandles(token))
                {
                    yield return GetHandleRoot(strongHandle);
                }

                foreach (var root in heap.EnumerateStackRoots(token))
                {
                    yield return root;
                }

                ClrRoot GetHandleRoot(ClrHandle handle)
                {
                    var kind = GCRootKind.Strong;

                    switch (handle.HandleType)
                    {
                        case HandleType.Pinned:
                            kind = GCRootKind.Pinning;
                            break;

                        case HandleType.AsyncPinned:
                            kind = GCRootKind.AsyncPinning;
                            break;
                    }

                    return new HandleRoot(handle.Address, handle.Object, handle.Type, handle.HandleType, kind, handle.AppDomain);
                }
            }

            List<FieldReference> GetChain(ClrType sourceType, in ClrObjectReference reference)
            {
                var key = (sourceType.MetadataToken, reference.FieldOffset);

                if (!chainCache.TryGetValue(key, out var chain))
                {
                    chain = BuildChain(in reference);
                    chainCache[key] = chain;
                }

                return chain;

                List<FieldReference> BuildChain(in ClrObjectReference r)
                {
                    var list = new List<FieldReference>(1);
                    var offset = r.FieldOffset;
                    var type = sourceType;
                    var inner = false;

                    while (type.GetFieldForOffset(offset, inner, out var childField, out var childFieldOffset))
                    {
                        list.Add(new FieldReference(childField));

                        // Only loop when digging through nested value types
                        if (!childField.Type.IsValueClass)
                            break;

                        offset = childFieldOffset;
                        type = childField.Type;
                        inner = true;
                    }

                    return list;
                }
            }
        }

        private struct HeapWalkStackLevel
        {
            public ClrObjectReference Reference { get; set; }
            public IEnumerator<ClrObjectReference> Enumerator { get; set; }
            public ReferenceGraphNode GraphNode { get; set; }
        }

        private sealed class HeapWalkStack
        {
            private HeapWalkStackLevel[] _levels;
            private int _last = -1;

            public HeapWalkStack(int capacity = 128)
            {
                _levels = new HeapWalkStackLevel[capacity];
            }

            public void Push(ClrObjectReference o)
            {
                _last++;

                // resize if needed
                if (_last == _levels.Length)
                {
                    var resized = new HeapWalkStackLevel[_levels.Length*2];
                    Array.Copy(_levels, resized, _levels.Length);
                    _levels = resized;
                }

                ref var level = ref _levels[_last];
                level.Reference = o;
                // TODO avoid allocation of enumerator?
                level.Enumerator = o.TargetType.EnumerateObjectReferencesWithFields(o.Address, carefully: true).GetEnumerator();
                level.GraphNode = null;
            }

            public int Count => _last + 1;

            public ref HeapWalkStackLevel this[int index] => ref _levels[index];

            public bool IsEmpty => _last == -1;

            public ref HeapWalkStackLevel Peek() => ref _levels[_last];

            public void Pop()
            {
                // TODO validate
                _last--;
            }

            public void Clear()
            {
                _last = -1;
            }
        }
    }

    public readonly struct FieldReference
    {
        public FieldReference(ClrInstanceField field)
        {
            Name = field.Name;
            Type = field.Type;
        }

        public string Name { get; }
        public ClrType Type { get; }

        #region Equality & hashing

        public bool Equals(FieldReference other) => string.Equals(Name, other.Name) && Equals(Type, other.Type);

        public override bool Equals(object obj) => obj is FieldReference other && Equals(other);

        public override int GetHashCode() => unchecked((Name.GetHashCode()*397) ^ (Type?.GetHashCode() ?? 0));

        #endregion

        public static string DescribeFieldReferences(IEnumerable<FieldReference> referenceChain)
        {
            var sb = new StringBuilder();

            foreach (var reference in referenceChain)
            {
                if (sb.Length != 0)
                {
                    sb.Append('.');
                }

                sb.Append(reference.Name);
            }

            return sb.ToString();
        }
    }

    public sealed class ReferenceGraph
    {
        public List<Root> Roots { get; } = new List<Root>();
        public List<ReferenceGraphNode> TargetSet { get; } = new List<ReferenceGraphNode>();
    }

    public sealed class Root : ReferenceGraphNode
    {
        public ClrRoot ClrRoot { get; }

        public Root(ClrRoot clrRoot) 
            : base(new ClrObject(clrRoot.Object, clrRoot.Type))
        {
            ClrRoot = clrRoot;
        }
    }

    public class ReferenceGraphNode
    {
//        public int Count { get; }
        public ClrObject Object { get; }

        public ReferenceGraphNode(ClrObject o)
        {
            Object = o;
        }

        // NOTE for referrers, the field reference list is backwards

//        public List<(ReferenceGraphNode node, List<FieldReference> referenceChain)> References { get; } = new List<(ReferenceGraphNode node, List<FieldReference> referenceChain)>(2);
        public List<(ReferenceGraphNode node, List<FieldReference> referenceChain, int fieldOffset)> Referrers { get; } = new List<(ReferenceGraphNode node, List<FieldReference> referenceChain, int fieldOffset)>(2);

        public override string ToString() => Object.ToString();
    }
}