using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Diagnostics.Runtime;
using StringTheory.Analysis;

namespace StringTheory.UI
{
    // This file projects a reference graph into a reference tree, dealing with parallel paths and cycles

    public sealed class ReferrerTreeViewModel
    {
        public IReadOnlyList<ReferrerTreeNode> Roots { get; }

        public ReferrerTreeViewModel(ReferenceGraph graph, string targetString)
        {
            var rootNode = ReferrerTreeNode.CreateStringNode(graph.TargetSet, targetString);

            rootNode.Expand();

            Roots = new[] { rootNode };
        }
    }

    public enum ReferrerTreeNodeType
    {
        TargetString,
        FieldReference,
        StaticVar,
        ThreadStaticVar,
        Pinning,
        AsyncPinning,
        LocalVar,
        StrongHandle,
        WeakHandle,
        Finalizer
    }

    public sealed class ReferrerTreeNode
    {
        private static readonly object _placeholderChild = new object();
        private static readonly Regex _typeNameRegex = new Regex(@"^(?<scope>[\w.]+\.)(?<name>[\w<>.+]+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        private readonly IReadOnlyList<ReferenceGraphNode> _backingItems;

        private readonly ReferrerTreeNode _parent;

        public ObservableCollection<object> Children { get; } = new ObservableCollection<object>();
        public int FieldOffset { get; }
        public List<FieldReference> ReferrerChain { get; }
        public ClrType ReferrerType { get; }

        public string Scope { get; }
        public string Name { get; }
        public string FieldChain { get; }
        public bool IsCycle { get; }
        public bool IsLeaf { get; }
        public ReferrerTreeNodeType Type { get; }

        public static ReferrerTreeNode CreateStringNode(IReadOnlyList<ReferenceGraphNode> backingItems, string content)
        {
            return new ReferrerTreeNode(null, backingItems, null, content, null, null, -1, null, false, false, ReferrerTreeNodeType.TargetString);
        }

        private ReferrerTreeNode CreateGCRootNode(RootGraphNode node)
        {
            return new ReferrerTreeNode(this, new[] {node}, null, node.ClrRoot.Name, null, null, -1, null, false, true, GetNodeType());

            ReferrerTreeNodeType GetNodeType()
            {
                switch (node.ClrRoot.Kind)
                {
                    case GCRootKind.StaticVar:       return ReferrerTreeNodeType.StaticVar;       // "static var StringTheory.SampleApp.Program.E"
                    case GCRootKind.ThreadStaticVar: return ReferrerTreeNodeType.ThreadStaticVar;
                    case GCRootKind.Pinning:         return ReferrerTreeNodeType.Pinning;         // "Pinned handle"
                    case GCRootKind.AsyncPinning:    return ReferrerTreeNodeType.AsyncPinning;
                    case GCRootKind.LocalVar:        return ReferrerTreeNodeType.LocalVar;
                    case GCRootKind.Strong:          return ReferrerTreeNodeType.StrongHandle;    // "Strong handle"
                    case GCRootKind.Weak:            return ReferrerTreeNodeType.WeakHandle;
                    case GCRootKind.Finalizer:       return ReferrerTreeNodeType.Finalizer;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        private ReferrerTreeNode CreateChildNode(IReadOnlyList<ReferenceGraphNode> backingItems, ClrType referrerType, int fieldOffset, List<FieldReference> referrerChain, bool isCycle)
        {
            string scope;
            string name;

            var match = _typeNameRegex.Match(referrerType.Name);

            if (match.Success)
            {
                scope = match.Groups["scope"].Value;
                name = match.Groups["name"].Value;
            }
            else
            {
                scope = null;
                name = referrerType.Name;
            }

            Debug.Assert(!string.IsNullOrEmpty(name), "Node shouldn't have empty name");

            var fieldChain = FieldReference.DescribeFieldReferences(referrerChain);

            if (fieldChain.Length != 0)
            {
                fieldChain = "." + fieldChain;
            }

            return new ReferrerTreeNode(this, backingItems, scope, name, fieldChain, referrerType, fieldOffset, referrerChain, isCycle, false, ReferrerTreeNodeType.FieldReference);
        }

        private ReferrerTreeNode(ReferrerTreeNode parent, IReadOnlyList<ReferenceGraphNode> backingItems, string scope, string name, string fieldChain, ClrType referrerType, int fieldOffset, List<FieldReference> referrerChain, bool isCycle, bool isLeaf, ReferrerTreeNodeType type)
        {
            _parent = parent;
            _backingItems = backingItems;
            Scope = scope;
            Name = name;
            FieldChain = fieldChain;
            FieldOffset = fieldOffset;
            ReferrerChain = referrerChain;
            IsCycle = isCycle;
            Type = type;
            IsLeaf = isLeaf;
            ReferrerType = referrerType;

            if (!isLeaf)
            {
                Children.Add(_placeholderChild);
            }
        }

        public bool CanShowStringsReferencedByField => _parent != null && _parent._parent == null;

        public int Count => _backingItems.Count;

        public bool IsExpanded { get; set; }

        public void Expand()
        {
            // Create child nodes by grouping backing items
            // TODO don't add placeholder child then clear it during auto expand construction (unless logic becomes ugly)

            var node = this;

            var ancestors = GetAncestors();

            // Limit the depth to which this can recur, defending against overflows here or in later arrange/layout
            var remaining = 50;

            while (remaining-- != 0)
            {
                if (node.IsLeaf || node.IsCycle)
                    return;

                ancestors.Add((node.ReferrerType, node.FieldOffset));

                node.Children.Clear();
                node.IsExpanded = true;

                var roots = node._backingItems.OfType<RootGraphNode>().ToList();

                var nonRoots = roots.Count == 0 ? node._backingItems : node._backingItems.Where(n => !(n is RootGraphNode));

                var groups = nonRoots
                    .SelectMany(i => i.Referrers)
                    .GroupBy(referrer => (referrerType: referrer.node.Object.Type, referrer.referenceChain, referrer.fieldOffset))
                    .OrderByDescending(g => g.Count());

                foreach (var group in groups)
                {
                    var backingItems = group.Select(i => i.node).ToList();

                    var isCycle = ancestors.Contains((group.Key.referrerType, group.Key.fieldOffset));

                    node.Children.Add(node.CreateChildNode(backingItems, group.Key.referrerType, group.Key.fieldOffset, group.Key.referenceChain, isCycle));
                }

                foreach (var root in roots)
                {
                    node.Children.Add(node.CreateGCRootNode(root));
                }

                if (node.Children.Count == 1)
                {
                    node = (ReferrerTreeNode)node.Children[0];
                }
                else
                {
                    break;
                }
            }

            HashSet<(ClrType ReferrerType, int fieldOffset)> GetAncestors()
            {
                var set = new HashSet<(ClrType ReferrerType, int fieldOffset)>();
                var n = _parent;

                while (n?.ReferrerType != null)
                {
                    set.Add((n.ReferrerType, n.FieldOffset));
                    n = n._parent;
                }

                return set;
            }
        }
    }
}