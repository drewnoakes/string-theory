using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace StringTheory.UI
{
    public sealed class ReferrersPage : ITabPage
    {
        public ICommand ShowStringReferencedByFieldCommand { get; }
        public ReferrerTreeViewModel ReferrerTree { get; }
        public string HeaderText { get; }
        public DrawingBrush IconDrawingBrush { get; }

        public bool CanClose => true;

        public ReferrersPage(MainWindow mainWindow, ReferrerTreeViewModel referrerTree, HeapAnalyzer analyzer, string headerText)
        {
            ReferrerTree = referrerTree ?? throw new ArgumentNullException(nameof(referrerTree));
            HeaderText = headerText;

            ShowStringReferencedByFieldCommand = new DelegateCommand<ReferrerTreeNode>(ShowStringReferencedByField);

            IconDrawingBrush = (DrawingBrush)Application.Current.FindResource("ReferrerTreeIconBrush");

            void ShowStringReferencedByField(ReferrerTreeNode node)
            {
                var summary = analyzer.GetTypeReferenceStringSummary(node.ReferrerType, node.FieldOffset);

                var title = $"Refs of {FieldReference.DescribeFieldReferences(node.ReferrerChain)}";
                var description = $"Strings referenced by field {FieldReference.DescribeFieldReferences(node.ReferrerChain)} of type {node.ReferrerType.Name}";

                mainWindow.AddTab(new StringListPage(mainWindow, summary, analyzer, title, description));
            }
        }
    }
}