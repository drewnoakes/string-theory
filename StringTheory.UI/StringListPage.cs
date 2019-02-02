using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace StringTheory.UI
{
    public sealed class StringListPage : ITabPage
    {
        public IEnumerable<StringItem> StringItems { get; }
        public ICommand ShowReferrersCommand { get; }
        public ICommand CopyStringsCommand { get; }
        public ICommand CopyCsvCommand { get; }
        public ICommand CopyMarkdownCommand { get; }

        public string HeaderText { get; }
        public bool CanClose => true;

        public StringListPage(MainWindow mainWindow, StringSummary summary, HeapAnalyzer analyzer, string tabTitle)
        {
            HeaderText = tabTitle;
            StringItems = summary.Strings;

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

                var graph = analyzer.GetReferenceGraph(new HashSet<ulong>(stringItem.ValueAddresses));

                var referrerTree = new ReferrerTreeViewModel(graph, stringItem.Content);

                mainWindow.AddTab(new ReferrersPage(mainWindow, referrerTree, analyzer, $"Referrers of {stringItem.Content}"));
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