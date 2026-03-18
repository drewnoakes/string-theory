using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using StringTheory.Analysis;

namespace StringTheory.Avalonia;

public sealed class StringListPage : ITabPage, IDisposable, INotifyPropertyChanged
{
    event Action? ITabPage.CloseRequested { add { } remove { } }

    private readonly IDisposable _analyzerLease;

    public static DrawingImage? IconDrawingImage => Application.Current?.Resources.TryGetResource("StringListIconImage", null, out var res) == true ? res as DrawingImage : null;

    public IReadOnlyList<StringItem> AllStringItems { get; }
    public IEnumerable<StringItem> StringItems { get; private set; }

    public ulong StringCount { get; }
    public ulong UniqueStringCount { get; }
    public ulong TotalStringBytes { get; }
    public ulong TotalManagedHeapBytes { get; }
    public ulong WastedBytes { get; }
    public string HeaderText { get; }
    public string Description { get; }

    public ICommand ShowReferrersCommand { get; }
    public ICommand CopyStringsCommand { get; }
    public ICommand CopyCsvCommand { get; }
    public ICommand CopyMarkdownCommand { get; }

    DrawingImage? ITabPage.IconDrawingImage => IconDrawingImage;

    public bool CanClose => true;

    public double WastedBytesPercentageOfStrings => (double)WastedBytes / TotalStringBytes;
    public double WastedBytesPercentageOfHeap => (double)WastedBytes / TotalManagedHeapBytes;

    public string FilterText
    {
        get;
        set
        {
            if (string.Equals(field, value))
                return;
            field = value;
            OnPropertyChanged();
            if (value.Length == 0)
                StringItems = AllStringItems;
            else
                StringItems = AllStringItems.Where(i => i.Content.Contains(value, StringComparison.CurrentCultureIgnoreCase)).ToList();
            OnPropertyChanged(nameof(StringItems));
        }
    } = "";

    public StringListPage(MainWindow mainWindow, StringSummary summary, HeapAnalyzer analyzer, string tabTitle, string description)
    {
        AllStringItems = summary.Strings;
        StringItems = summary.Strings;
        StringCount = summary.StringCount;
        UniqueStringCount = summary.UniqueStringCount;
        TotalStringBytes = summary.StringByteCount;
        TotalManagedHeapBytes = summary.HeapByteCount;
        WastedBytes = summary.WastedBytes;
        HeaderText = tabTitle;
        Description = description;

        _analyzerLease = analyzer.GetLease();

        ShowReferrersCommand = new DelegateCommand<IList>(ShowReferrers);
        CopyStringsCommand = new DelegateCommand<IList>(CopyStrings);
        CopyCsvCommand = new DelegateCommand<IList>(CopyCsv);
        CopyMarkdownCommand = new DelegateCommand<IList>(CopyMarkdown);

        void ShowReferrers(IList selectedItems)
        {
            var stringItems = selectedItems.Cast<StringItem>().ToList();

            if (stringItems.Count == 0)
                return;

            if (stringItems.Count != 1)
                return;

            var stringItem = stringItems.Single();

            var operation = new LoadingOperation(
                (progressCallback, token) =>
                {
                    var graph = analyzer.GetReferenceGraph(stringItem.ValueAddresses, token);

                    var referrerTree = new ReferrerTreeViewModel(graph, stringItem.Content);

                    return new ReferrersPage(mainWindow, referrerTree, analyzer, $"Referrers of {stringItem.Content}");
                });

            mainWindow.AddTab(new LoadingTabPage($"Referrers of {stringItem.Content}", ReferrersPage.IconDrawingImage, operation));
        }

        async void CopyStrings(IList selectedItems)
        {
            var sb = new StringBuilder();

            foreach (StringItem item in selectedItems)
            {
                sb.AppendLine(item.Content);
            }

            await TrySetClipboardText(sb);
        }

        async void CopyCsv(IList selectedItems)
        {
            var sb = new StringBuilder();

            sb.AppendLine("WastedBytes,Count,Length,String");
            foreach (StringItem item in selectedItems)
            {
                sb.Append(item.WastedBytes).Append(',');
                sb.Append(item.Count).Append(',');
                sb.Append(item.Length).Append(',');
                sb.Append(item.Content).AppendLine();
            }

            await TrySetClipboardText(sb);
        }

        async void CopyMarkdown(IList selectedItems)
        {
            var sb = new StringBuilder();

            sb.AppendLine("| WastedBytes | Count | Length | String |");
            sb.AppendLine("|------------:|------:|-------:|--------|");
            foreach (StringItem item in selectedItems)
            {
                sb.Append("| ");
                sb.Append(item.WastedBytes.ToString("n0")).Append(" | ");
                sb.Append(item.Count.ToString("n0")).Append(" | ");
                sb.Append(item.Length.ToString("n0")).Append(" | ");
                sb.Append(item.Content).AppendLine(" |");
            }

            await TrySetClipboardText(sb);
        }

        async System.Threading.Tasks.Task TrySetClipboardText(StringBuilder sb)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(mainWindow);
                if (topLevel?.Clipboard is { } clipboard)
                {
                    await clipboard.SetTextAsync(sb.ToString());
                }
            }
            catch
            {
                // Clipboard access can fail on some platforms
            }
        }
    }

    public void Dispose()
    {
        _analyzerLease.Dispose();
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}
