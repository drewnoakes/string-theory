using System.Windows;

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
    }
}
