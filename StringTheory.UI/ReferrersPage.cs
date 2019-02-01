using System;
using System.Windows.Input;

namespace StringTheory.UI
{
    public sealed class ReferrersPage : ITabPage
    {
        public ICommand ShowStringReferencedByFieldCommand { get; }
        public ReferrerTreeViewModel ReferrerTree { get; }

        // TODO contextual tab header text
        public string HeaderText => "Referrers";
        public bool CanClose => true;

        public ReferrersPage(MainWindow mainWindow, ReferrerTreeViewModel referrerTree, HeapAnalyzer analyzer)
        {
            ReferrerTree = referrerTree ?? throw new ArgumentNullException(nameof(referrerTree));

            ShowStringReferencedByFieldCommand = new DelegateCommand<ReferrerTreeNode>(ShowStringReferencedByField);

            void ShowStringReferencedByField(ReferrerTreeNode node)
            {
                var summary = analyzer.GetTypeReferenceStringSummary(node.ReferrerType, node.FieldOffset);

                mainWindow.AddTab(new StringListPage(mainWindow, summary, analyzer));
            }
        }
    }
}