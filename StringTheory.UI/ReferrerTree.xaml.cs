using System.Windows;
using System.Windows.Controls;

namespace StringTheory.UI
{
    public sealed partial class ReferrerTree
    {
        public static readonly DependencyProperty TreeProperty = DependencyProperty.Register(nameof(Tree), typeof(ReferrerTreeViewModel), typeof(ReferrerTree));

        public ReferrerTree()
        {
            InitializeComponent();
        }

        public ReferrerTreeViewModel Tree
        {
            get => (ReferrerTreeViewModel)GetValue(TreeProperty);
            set => SetValue(TreeProperty, value);
        }

        private void OnItemExpanded(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)e.OriginalSource;

            var node = (ReferrerNode)item.DataContext;

            node.Expand();
        }
    }
}