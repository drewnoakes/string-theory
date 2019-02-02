using System;
using System.Windows.Input;

namespace StringTheory.UI
{
    public sealed class ReferrersPage : ITabPage
    {
        public ICommand ShowStringReferencedByFieldCommand { get; }
        public ReferrerTreeViewModel ReferrerTree { get; }
        public string HeaderText { get; }

        public bool CanClose => true;

        public ReferrersPage(MainWindow mainWindow, ReferrerTreeViewModel referrerTree, HeapAnalyzer analyzer, string headerText)
        {
            ReferrerTree = referrerTree ?? throw new ArgumentNullException(nameof(referrerTree));
            HeaderText = headerText;

            ShowStringReferencedByFieldCommand = new DelegateCommand<ReferrerTreeNode>(ShowStringReferencedByField);

            void ShowStringReferencedByField(ReferrerTreeNode node)
            {
                var summary = analyzer.GetTypeReferenceStringSummary(node.ReferrerType, node.FieldOffset);

                var title = $"Refs of {node.ReferrerType.Name}.{FieldReference.DescribeFieldReferences(node.ReferrerChain)}";

                mainWindow.AddTab(new StringListPage(mainWindow, summary, analyzer, title));
            }
        }
    }
}