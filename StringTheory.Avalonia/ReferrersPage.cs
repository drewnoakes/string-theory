using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using StringTheory.Analysis;

namespace StringTheory.Avalonia;

public sealed class ReferrersPage : ITabPage, IDisposable
{
    public static DrawingImage? IconDrawingImage => Application.Current?.Resources.TryGetResource("ReferrerTreeIconImage", null, out var res) == true ? res as DrawingImage : null;

    event Action? ITabPage.CloseRequested { add { } remove { } }

    private readonly IDisposable _analyzerLease;

    public ICommand ShowStringReferencedByFieldCommand { get; }
    public ReferrerTreeViewModel ReferrerTree { get; }
    public string HeaderText { get; }

    DrawingImage? ITabPage.IconDrawingImage => IconDrawingImage;

    public bool CanClose => true;

    public ReferrersPage(MainWindow mainWindow, ReferrerTreeViewModel referrerTree, HeapAnalyzer analyzer, string headerText)
    {
        ReferrerTree = referrerTree ?? throw new ArgumentNullException(nameof(referrerTree));
        HeaderText = headerText;

        _analyzerLease = analyzer.GetLease();

        ShowStringReferencedByFieldCommand = new DelegateCommand<ReferrerTreeNode>(ShowStringReferencedByField);

        void ShowStringReferencedByField(ReferrerTreeNode node)
        {
            var title = $"Refs of {FieldReference.DescribeFieldReferences(node.ReferrerChain ?? [])}";

            var operation = new LoadingOperation(
                (progressCallback, token) =>
                {
                    var summary = analyzer.GetTypeReferenceStringSummary(node.ReferrerType!, node.FieldOffset, token);

                    var description = $"Strings referenced by field {FieldReference.DescribeFieldReferences(node.ReferrerChain ?? [])} of type {node.ReferrerType?.Name}";

                    return new StringListPage(mainWindow, summary, analyzer, title, description);
                });

            mainWindow.AddTab(new LoadingTabPage(title, StringListPage.IconDrawingImage, operation));
        }
    }

    public void Dispose()
    {
        _analyzerLease.Dispose();
    }
}
