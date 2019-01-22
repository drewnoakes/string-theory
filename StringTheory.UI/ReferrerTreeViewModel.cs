using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace StringTheory.UI
{
    public sealed class ReferrerTreeViewModel
    {
        public string TargetString { get; }
        public IReadOnlyList<ReferrerTreeNode> Roots { get; }

        public ReferrerTreeViewModel(ReferenceGraph graph, string targetString)
        {
            TargetString = targetString;
            var rootNode = new ReferrerTreeNode(graph.TargetSet, targetString);

            rootNode.Expand();

            Roots = new[] { rootNode };
        }
    }

    public sealed class ReferrerTreeNode
    {
        private static readonly object _placeholderChild = new object();

        private readonly IReadOnlyList<ReferenceGraphNode> _backingItems;

        public ObservableCollection<object> Children { get; } = new ObservableCollection<object>();
        public string Title { get; }

        public ReferrerTreeNode(IReadOnlyList<ReferenceGraphNode> backingItems, string title)
        {
            Title = title;
            _backingItems = backingItems;

            if (_backingItems.Count != 0)
            {
                Children.Add(_placeholderChild);
            }
        }

        public int Count => _backingItems.Count;

        public bool IsExpanded { get; set; }

        public void Expand()
        {
            // Create child nodes by grouping backing items
            // TODO don't add placeholder child then clear it during auto expand construction (unless logic becomes ugly)

            var node = this;

            // Limit the depth to which this can recur, defending against overflows here or in later arrange/layout
            var remaining = 50;

            while (remaining-- != 0)
            {
                node.Children.Clear();
                node.IsExpanded = true;

                var groups = node._backingItems
                    .SelectMany(i => i.Referrers)
                    .GroupBy(i => (type: i.node.Object.Type, i.field))
                    .OrderByDescending(g => g.Count());

                foreach (var group in groups)
                {
                    var backingItems = group.Select(i => i.node).ToList();
                    var title = $"{group.Key.type?.Name} ({DescribeFieldReferences(group.Key.field)})";

                    node.Children.Add(new ReferrerTreeNode(backingItems, title));
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

            string DescribeFieldReferences(List<FieldReference> references)
            {
                var sb = new StringBuilder();

                foreach (var reference in references)
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
    }
}