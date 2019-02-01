using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StringTheory.UI
{
    public sealed partial class MainWindow : INotifyPropertyChanged
    {
        public ObservableCollection<ITabPage> TabPages { get; }

        public int SelectedTabIndex { get; set; }

        public MainWindow()
        {
            TabPages = new ObservableCollection<ITabPage>
            {
                new HomePage(this)
            };

            DataContext = this;

            InitializeComponent();
        }

        public void AddTab(ITabPage tabPage)
        {
            TabPages.Add(tabPage);

            SelectedTabIndex = TabPages.Count - 1;
            OnPropertyChanged(nameof(SelectedTabIndex));
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
