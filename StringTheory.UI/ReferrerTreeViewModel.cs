using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StringTheory.UI
{
    public sealed class ReferrerTreeViewModel
    {
        public string TargetString { get; }
        public IReadOnlyList<ReferrerTreeNode> Roots { get; }

        public ReferrerTreeViewModel(ReferenceGraph graph, string targetString)
        {
            TargetString = targetString;
            Roots = new[] { new ReferrerTreeNode(graph.TargetSet, targetString) };
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

        public void Expand()
        {
            // Create child nodes by grouping backing items
            // TODO include counts in display
            // TODO display information about field ids
            // TODO insert levels for nested struct sub-fields
            // TODO auto expand where single child exists

            Children.Clear();

            var groups = _backingItems
                .SelectMany(i => i.Referrers)
                .GroupBy(i => (type: i.node.Object.Type, i.field))
                .OrderByDescending(g => g.Count());

            foreach (var group in groups)
            {
                var backingItems = group.Select(i => i.node).ToList();
                var title = $"{group.Key.type?.Name} ({group.Key.field})";

                Children.Add(new ReferrerTreeNode(backingItems, title));
            }
        }
    }
}