using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Diagnostics.Runtime;

namespace StringTheory.Analysis;

public static class ReferenceGraphBuilder
{
    public static ReferenceGraph Build(ClrHeap heap, HashSet<ulong> targetAddresses, CancellationToken token = default)
    {
        var graph = new ReferenceGraph();

        var seen = new ObjectSet(heap);

        var stack = new HeapWalkStack();

        var nodeByAddress = new Dictionary<ulong, ReferenceGraphNode>();

        var chainCache = new Dictionary<(int metadataToken, int fieldOffset), List<FieldReference>>();

        // For each root
        foreach (var root in heap.EnumerateRoots())
        {
            stack.Clear();

            if (root.Object.Type == null)
            {
                // This happens, though not sure why
                continue;
            }

            stack.Push(root.Object);

            while (!stack.IsEmpty)
            {
                token.ThrowIfCancellationRequested();

                ref var top = ref stack.Peek();

                if (top.Enumerator.MoveNext())
                {
                    var o = top.Enumerator.Current;

                    if (targetAddresses.Contains(o.Object.Address))
                    {
                        // Found a match
                        stack.Push(o.Object, o);

                        // Ensure our stack's root is registered with the graph
                        ref var rootLevel = ref stack[0];
                        if (rootLevel.GraphNode == null)
                        {
                            var rootNode = new RootGraphNode(root);
                            nodeByAddress[root.Object.Address] = rootNode;
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
                                if (!nodeByAddress.TryGetValue(level.Object.Address, out var node))
                                {
                                    node = new ReferenceGraphNode(level.Object);
                                    nodeByAddress[level.Object.Address] = node;

                                    if (i == stack.Count - 1)
                                    {
                                        graph.TargetSet.Add(node);
                                    }
                                }

                                level.GraphNode = node;
                                
                                ref var levelBefore = ref stack[i - 1];
                                
                                var referenceChain = level.Reference.HasValue ? GetChain(level.Reference.Value) : [];

                                level.GraphNode.Referrers.Add((node: levelBefore.GraphNode, referenceChain, fieldOffset: level.Reference?.Offset ?? -1));
//                                levelBefore.GraphNode.References.Add((node: level.GraphNode, referenceChain));
                            }
                        }

                        stack.Pop();
                    }
                    
                    if (!seen.Contains(o.Object.Address))
                    {
                        // New object; push it to the stack to start exploring it
                        stack.Push(o.Object, o);
                        seen.Add(o.Object.Address);
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

        List<FieldReference> GetChain(ClrReference reference)
        {
            var containingType = reference.Field?.ContainingType;
            var key = (containingType?.MetadataToken ?? 0, reference.Offset);

            if (!chainCache.TryGetValue(key, out var chain))
            {
                chain = BuildChain(reference);
                chainCache[key] = chain;
            }

            return chain;

            List<FieldReference> BuildChain(ClrReference r)
            {
                var list = new List<FieldReference>(1);

                if (r.Field != null)
                    list.Add(new FieldReference(r.Field));

                var inner = r.InnerField;
                while (inner.HasValue)
                {
                    if (inner.Value.Field != null)
                        list.Add(new FieldReference(inner.Value.Field));
                    inner = inner.Value.InnerField;
                }

                return list;
            }
        }
    }

    private struct HeapWalkStackLevel
    {
        public ClrObject Object { get; set; }
        public ClrReference? Reference { get; set; }
        public IEnumerator<ClrReference> Enumerator { get; set; }
        public ReferenceGraphNode GraphNode { get; set; }
    }

    private sealed class HeapWalkStack(int capacity = 128)
    {
        private HeapWalkStackLevel[] _levels = new HeapWalkStackLevel[capacity];
        private int _last = -1;

        public void Push(ClrObject obj, ClrReference? reference = null)
        {
            if (obj.Type == null)
                return;

            _last++;

            // resize if needed
            if (_last == _levels.Length)
            {
                var resized = new HeapWalkStackLevel[_levels.Length*2];
                Array.Copy(_levels, resized, _levels.Length);
                _levels = resized;
            }

            ref var level = ref _levels[_last];
            level.Object = obj;
            level.Reference = reference;
            // TODO avoid allocation of enumerator?
            level.Enumerator = obj.EnumerateReferencesWithFields(carefully: true, considerDependantHandles: false).GetEnumerator();
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

public readonly struct FieldReference(ClrInstanceField field)
{
    public string Name { get; } = field.Name;
    public ClrType Type { get; } = field.Type;

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
    public List<RootGraphNode> Roots { get; } = [];
    public List<ReferenceGraphNode> TargetSet { get; } = [];
}

public sealed class RootGraphNode(ClrRoot clrRoot) : ReferenceGraphNode(clrRoot.Object)
{
    public ClrRoot ClrRoot { get; } = clrRoot;
}

public class ReferenceGraphNode(ClrObject o)
{
    //        public int Count { get; }
    public ClrObject Object { get; } = o;

    // NOTE for referrers, the field reference list is backwards

    //        public List<(ReferenceGraphNode node, List<FieldReference> referenceChain)> References { get; } = new List<(ReferenceGraphNode node, List<FieldReference> referenceChain)>(2);
    public List<(ReferenceGraphNode node, List<FieldReference> referenceChain, int fieldOffset)> Referrers { get; } = new List<(ReferenceGraphNode node, List<FieldReference> referenceChain, int fieldOffset)>(2);

    public override string ToString() => Object.ToString();
}