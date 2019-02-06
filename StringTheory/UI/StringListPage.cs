using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using StringTheory.Analysis;

namespace StringTheory.UI
{
    public sealed class StringListPage : ITabPage
    {
        private string _filterText;
        public event Action CloseRequested;

        public static DrawingBrush IconDrawingBrush => (DrawingBrush)Application.Current.FindResource("StringListIconBrush");

        public IEnumerable<StringItem> StringItems { get; }

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

        DrawingBrush ITabPage.IconDrawingBrush => IconDrawingBrush;

        public bool CanClose => true;

        public double WastedBytesPercentageOfStrings => (double)WastedBytes / TotalStringBytes;
        public double WastedBytesPercentageOfHeap => (double)WastedBytes / TotalManagedHeapBytes;

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (string.Equals(_filterText, value))
                    return;
                _filterText = value;
                var view = CollectionViewSource.GetDefaultView(StringItems);
                if (value == null)
                    view.Filter = null;
                else
                    view.Filter = i => ((StringItem) i).Content.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) != -1;
            }
        }

        public StringListPage(MainWindow mainWindow, StringSummary summary, HeapAnalyzer analyzer, string tabTitle, string description)
        {
            StringItems = summary.Strings;
            StringCount = summary.StringCount;
            UniqueStringCount = summary.UniqueStringCount;
            TotalStringBytes = summary.StringByteCount;
            TotalManagedHeapBytes = summary.HeapByteCount;
            WastedBytes = summary.WastedBytes;
            HeaderText = tabTitle;
            Description = description;

            ShowReferrersCommand = new DelegateCommand<IList>(ShowReferrers);
            CopyStringsCommand = new DelegateCommand<IList>(CopyStrings);
            CopyCsvCommand = new DelegateCommand<IList>(CopyCsv);
            CopyMarkdownCommand = new DelegateCommand<IList>(CopyMarkdown);

            void ShowReferrers(IList selectedItems)
            {
                var stringItems = selectedItems.Cast<StringItem>().ToList();

                if (stringItems.Count == 0)
                {
                    MessageBox.Show("Must select a string.");
                    return;
                }

                if (stringItems.Count != 1)
                {
                    // TODO support multiple
                    MessageBox.Show("Can only show referrers of a single string at a time.");
                    return;
                }

                var stringItem = stringItems.Single();

                var operation = new LoadingOperation(
                    token =>
                    {
                        var graph = analyzer.GetReferenceGraph(stringItem.ValueAddresses);

                        var referrerTree = new ReferrerTreeViewModel(graph, stringItem.Content);

                        return new ReferrersPage(mainWindow, referrerTree, analyzer, $"Referrers of {stringItem.Content}");
                    });

                mainWindow.AddTab(new LoadingTabPage($"Referrers of {stringItem.Content}", ReferrersPage.IconDrawingBrush, operation));
            }

            void CopyStrings(IList selectedItems)
            {
                var sb = new StringBuilder();

                foreach (StringItem item in selectedItems)
                {
                    sb.AppendLine(item.Content);
                }

                TrySetClipboardText(sb);
            }

            void CopyCsv(IList selectedItems)
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

                TrySetClipboardText(sb);
            }

            void CopyMarkdown(IList selectedItems)
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

                TrySetClipboardText(sb);
            }

            void TrySetClipboardText(StringBuilder sb)
            {
                try
                {
                    Clipboard.SetText(sb.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to set clipboard text: " + ex.Message);
                }
            }
        }
    }
}