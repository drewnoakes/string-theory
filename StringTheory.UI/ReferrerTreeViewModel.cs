using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Diagnostics.Runtime;

namespace StringTheory.UI
{
    public sealed class ReferrerTreeViewModel
    {
        public string TargetString { get; }
        public IReadOnlyList<ReferrerTreeNode> Roots { get; }

        public ReferrerTreeViewModel(ReferenceGraph graph, string targetString)
        {
            TargetString = targetString;
            
            var rootNode = ReferrerTreeNode.CreateRoot(graph.TargetSet, targetString);

            rootNode.Expand();

            Roots = new[] { rootNode };
        }
    }

    public sealed class ReferrerTreeNode
    {
        private static readonly object _placeholderChild = new object();

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

        public static ReferrerTreeNode CreateRoot(IReadOnlyList<ReferenceGraphNode> backingItems, string content)
        {
            return new ReferrerTreeNode(null, backingItems, null, content, null, null, -1, null, false);
        }

        public ReferrerTreeNode CreateChild(IReadOnlyList<ReferenceGraphNode> backingItems, ClrType referrerType, int fieldOffset, List<FieldReference> referrerChain, bool isCycle)
        {
            string scope;
            string name;
            string fieldChain;

            if (backingItems.Count == 1 && backingItems[0] is Root root)
            {
                scope = root.ClrRoot.Name;
                name = null;
                fieldChain = null;
            }
            else
            {
                var index = referrerType.Name.LastIndexOf('.');
                if (index != -1)
                {
                    scope = referrerType.Name.Substring(0, index + 1);
                    name = referrerType.Name.Substring(index + 1);
                }
                else
                {
                    scope = null;
                    name = referrerType.Name;
                }

                fieldChain = FieldReference.DescribeFieldReferences(referrerChain);

                if (fieldChain.Length != 0)
                {
                    fieldChain = "." + fieldChain;
                }
            }

            return new ReferrerTreeNode(this, backingItems, scope, name, fieldChain, referrerType, fieldOffset, referrerChain, isCycle);
        }

        private ReferrerTreeNode(ReferrerTreeNode parent, IReadOnlyList<ReferenceGraphNode> backingItems, string scope, string name, string fieldChain, ClrType referrerType, int fieldOffset, List<FieldReference> referrerChain, bool isCycle)
        {
            _parent = parent;
            Scope = scope;
            Name = name;
            FieldChain = fieldChain;
            FieldOffset = fieldOffset;
            ReferrerChain = referrerChain;
            IsCycle = isCycle;
            ReferrerType = referrerType;
            _backingItems = backingItems;

            if (_backingItems.Count != 0)
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
                node.Children.Clear();
                node.IsExpanded = true;

                var groups = node._backingItems
                    .SelectMany(i => i.Referrers)
                    .GroupBy(referrer => (referrerType: referrer.node.Object.Type, referrer.referenceChain, referrer.fieldOffset))
                    .OrderByDescending(g => g.Count());

                foreach (var group in groups)
                {
                    var backingItems = group.Select(i => i.node).ToList();

                    var isCycle = ancestors.Contains((group.Key.referrerType, group.Key.fieldOffset));

                    node.Children.Add(node.CreateChild(backingItems, group.Key.referrerType, group.Key.fieldOffset, group.Key.referenceChain, isCycle));
                }

                if (node.Children.Count == 1)
                {
                    node = (ReferrerTreeNode)node.Children[0];
                    if (node.IsCycle)
                        break;
                    ancestors.Add((node.ReferrerType, node.FieldOffset));
                }
                else
                {
                    break;
                }
            }

            HashSet<(ClrType ReferrerType, int fieldOffset)> GetAncestors()
            {
                var set = new HashSet<(ClrType ReferrerType, int fieldOffset)>();
                var n = this;

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