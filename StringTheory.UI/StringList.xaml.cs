using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace StringTheory.UI
{
    public sealed partial class StringList
    {
        public static readonly DependencyProperty StringItemsProperty          = DependencyProperty.Register(nameof(StringItems),          typeof(IEnumerable<StringItem>), typeof(StringList));
        public static readonly DependencyProperty CopyStringsCommandProperty   = DependencyProperty.Register(nameof(CopyStringsCommand),   typeof(ICommand), typeof(StringList));
        public static readonly DependencyProperty CopyCsvCommandProperty       = DependencyProperty.Register(nameof(CopyCsvCommand),       typeof(ICommand), typeof(StringList));
        public static readonly DependencyProperty CopyMarkdownCommandProperty  = DependencyProperty.Register(nameof(CopyMarkdownCommand),  typeof(ICommand), typeof(StringList));
        public static readonly DependencyProperty ShowReferrersCommandProperty = DependencyProperty.Register(nameof(ShowReferrersCommand), typeof(ICommand), typeof(StringList));

        public StringList()
        {
            InitializeComponent();
        }

        public IEnumerable<StringItem> StringItems
        {
            get => (IEnumerable<StringItem>) GetValue(StringItemsProperty);
            set => SetValue(StringItemsProperty, value);
        }

        public ICommand CopyStringsCommand
        {
            get { return (ICommand) GetValue(CopyStringsCommandProperty); }
            set { SetValue(CopyStringsCommandProperty, value); }
        }

        public ICommand CopyCsvCommand
        {
            get { return (ICommand) GetValue(CopyCsvCommandProperty); }
            set { SetValue(CopyCsvCommandProperty, value); }
        }

        public ICommand CopyMarkdownCommand
        {
            get { return (ICommand) GetValue(CopyMarkdownCommandProperty); }
            set { SetValue(CopyMarkdownCommandProperty, value); }
        }

        public ICommand ShowReferrersCommand
        {
            get { return (ICommand) GetValue(ShowReferrersCommandProperty); }
            set { SetValue(ShowReferrersCommandProperty, value); }
        }
    }
}
