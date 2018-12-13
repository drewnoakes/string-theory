using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;

using Microsoft.Win32;

namespace StringTheory.UI
{
    public sealed partial class MainWindow : INotifyPropertyChanged
    {
        public MainWindow()
        {
            OpenDumpCommand      = new DelegateCommand(OpenDump);
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
                    var file = openFileDialog.FileName;

                    var summary = HeapAnalyzer.GetStringSummary(file);

                    StringItems = summary.Strings;
                    OnPropertyChanged(nameof(StringItems));
                }
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

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
