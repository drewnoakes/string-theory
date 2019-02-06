using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using StringTheory.Analysis;

namespace StringTheory.UI
{
    public sealed class ReferrersPage : ITabPage
    {
        public static DrawingBrush IconDrawingBrush => (DrawingBrush)Application.Current.FindResource("ReferrerTreeIconBrush");

        public event Action CloseRequested;

        public ICommand ShowStringReferencedByFieldCommand { get; }
        public ReferrerTreeViewModel ReferrerTree { get; }
        public string HeaderText { get; }

        DrawingBrush ITabPage.IconDrawingBrush => IconDrawingBrush;

        public bool CanClose => true;

        public ReferrersPage(MainWindow mainWindow, ReferrerTreeViewModel referrerTree, HeapAnalyzer analyzer, string headerText)
        {
            ReferrerTree = referrerTree ?? throw new ArgumentNullException(nameof(referrerTree));
            HeaderText = headerText;

            ShowStringReferencedByFieldCommand = new DelegateCommand<ReferrerTreeNode>(ShowStringReferencedByField);

            void ShowStringReferencedByField(ReferrerTreeNode node)
            {
                var title = $"Refs of {FieldReference.DescribeFieldReferences(node.ReferrerChain)}";

                var operation = new LoadingOperation(
                    token =>
                    {
                        var summary = analyzer.GetTypeReferenceStringSummary(node.ReferrerType, node.FieldOffset);

                        var description = $"Strings referenced by field {FieldReference.DescribeFieldReferences(node.ReferrerChain)} of type {node.ReferrerType.Name}";

                        return new StringListPage(mainWindow, summary, analyzer, title, description);
                    });

                mainWindow.AddTab(new LoadingTabPage(title, StringListPage.IconDrawingBrush, operation));

            }
        }
    }
}