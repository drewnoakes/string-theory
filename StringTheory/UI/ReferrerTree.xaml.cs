using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StringTheory.UI
{
    public sealed partial class ReferrerTree
    {
        public static readonly DependencyProperty TreeProperty = DependencyProperty.Register(nameof(Tree), typeof(ReferrerTreeViewModel), typeof(ReferrerTree));
        public static readonly DependencyProperty ShowStringReferencedByFieldCommandProperty = DependencyProperty.Register(nameof(ShowStringReferencedByFieldCommand), typeof(ICommand), typeof(ReferrerTree));

        public ReferrerTree()
        {
            InitializeComponent();
        }

        public ReferrerTreeViewModel Tree
        {
            get => (ReferrerTreeViewModel)GetValue(TreeProperty);
            set => SetValue(TreeProperty, value);
        }

        public ICommand ShowStringReferencedByFieldCommand
        {
            get => (ICommand)GetValue(ShowStringReferencedByFieldCommandProperty);
            set => SetValue(ShowStringReferencedByFieldCommandProperty, value);
        }

        private void OnItemExpanded(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)e.OriginalSource;

            var node = (ReferrerTreeNode)item.DataContext;

            node.Expand();
        }
    }
}