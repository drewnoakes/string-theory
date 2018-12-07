using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace StringTheory.UI
{
    public sealed partial class StringList
    {
        /// <summary>Identifies the <see cref="StringItems"/> dependency property.</summary>
        public static readonly DependencyProperty StringItemsProperty = DependencyProperty.Register(
            nameof(StringItems),
            typeof(IEnumerable<StringItem>),
            typeof(StringList));

        public StringList()
        {
            InitializeComponent();
        }

        public IEnumerable<StringItem> StringItems
        {
            get => (IEnumerable<StringItem>) GetValue(StringItemsProperty);
            set => SetValue(StringItemsProperty, value);
        }

        private void OnCopyExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var sb = new StringBuilder();

            switch (e.Parameter)
            {
                case "Strings":
                    foreach (StringItem item in _grid.SelectedItems)
                    {
                        sb.AppendLine(item.Content);
                    }
                    break;
                case "CSV":
                    sb.AppendLine("WastedBytes,Count,Length,String");
                    foreach (StringItem item in _grid.SelectedItems)
                    {
                        sb.Append(item.WastedBytes).Append(',');
                        sb.Append(item.Count).Append(',');
                        sb.Append(item.Length).Append(',');
                        sb.Append(item.Content).AppendLine();
                    }
                    break;
                case "Markdown":
                    sb.AppendLine("| WastedBytes | Count | Length | String |");
                    sb.AppendLine("|------------:|------:|-------:|--------|");
                    foreach (StringItem item in _grid.SelectedItems)
                    {
                        sb.Append("| ");
                        sb.Append(item.WastedBytes.ToString("n0")).Append(" | ");
                        sb.Append(item.Count.ToString("n0")).Append(" | ");
                        sb.Append(item.Length.ToString("n0")).Append(" | ");
                        sb.Append(item.Content).AppendLine(" |");
                    }
                    break;
            }

            try
            {
                Clipboard.SetText(sb.ToString());

                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to set clipboard text: " + ex.Message);
            }
        }
    }
}
