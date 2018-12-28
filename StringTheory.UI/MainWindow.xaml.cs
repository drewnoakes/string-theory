using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;

using Microsoft.Win32;

namespace StringTheory.UI
{
    public sealed partial class MainWindow : INotifyPropertyChanged
    {
        private HeapAnalyzer _analyzer;

        public MainWindow()
        {
            OpenDumpCommand      = new DelegateCommand(OpenDump);
            ShowReferrersCommand = new DelegateCommand<IList>(ShowReferrers);
            CopyStringsCommand   = new DelegateCommand<IList>(CopyStrings);
            CopyCsvCommand       = new DelegateCommand<IList>(CopyCsv);
            CopyMarkdownCommand  = new DelegateCommand<IList>(CopyMarkdown);
            DataContext = this;

            InitializeComponent();

            return;

            #region Commands

            void OpenDump()
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Dump files (*.dmp)|*.dmp|All files (*.*)|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var dumpFilePath = openFileDialog.FileName;

                    OpenDumpFile(dumpFilePath);
                }
            }

            void OpenDumpFile(string dumpFilePath)
            {
                SetCurrentValue(TitleProperty, $"String Theory - {dumpFilePath}");

                _analyzer?.Dispose();

                _analyzer = new HeapAnalyzer(dumpFilePath);

                var summary = _analyzer.GetStringSummary();

                StringItems = summary.Strings;
                OnPropertyChanged(nameof(StringItems));

                SelectedTabIndex = 1;
                OnPropertyChanged(nameof(SelectedTabIndex));
            }

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

                var graph = _analyzer.GetReferenceGraph(new HashSet<ulong>(stringItem.ValueAddresses));

                ReferrerTree = new ReferrerTreeViewModel(graph, stringItem.Content);
                OnPropertyChanged(nameof(ReferrerTree));

                SelectedTabIndex = 2;
                OnPropertyChanged(nameof(SelectedTabIndex));
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

            #endregion
        }

        public ICommand OpenDumpCommand { get; }
        public ICommand ShowReferrersCommand { get; }
        public ICommand CopyStringsCommand { get; }
        public ICommand CopyCsvCommand { get; }
        public ICommand CopyMarkdownCommand { get; }

        public IEnumerable<StringItem> StringItems { get; private set; }
        public ReferrerTreeViewModel ReferrerTree { get; private set; }

        public int SelectedTabIndex { get; set; }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
