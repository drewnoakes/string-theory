using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using StringTheory.Analysis;

namespace StringTheory.UI
{
    public sealed partial class StringList
    {
        public static readonly DependencyProperty StringListPageProperty = DependencyProperty.Register(nameof(StringListPage), typeof(StringListPage), typeof(StringList));

        public StringList()
        {
            InitializeComponent();
        }

        public StringListPage StringListPage
        {
            get => (StringListPage) GetValue(StringListPageProperty);
            set => SetValue(StringListPageProperty, value);
        }

        private void DataGridRow_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            StringListPage.ShowReferrersCommand.Execute(new List<StringItem>() {row.DataContext as StringItem});
        }
    }
}
