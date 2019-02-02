using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StringTheory.UI
{
    public sealed partial class MainWindow : INotifyPropertyChanged
    {
        public ObservableCollection<ITabPage> TabPages { get; }

        public int SelectedTabIndex { get; set; }

        public ICommand CloseCommand { get; }

        public double Scale { get; private set; } = 1.0;

        public MainWindow()
        {
            TabPages = new ObservableCollection<ITabPage>
            {
                new HomePage(this)
            };

            CloseCommand = new DelegateCommand<ITabPage>(RemoveTabPage);

            DataContext = this;

            InitializeComponent();
        }

        public void AddTab(ITabPage tabPage)
        {
            TabPages.Add(tabPage);

            SelectedTabIndex = TabPages.Count - 1;
            OnPropertyChanged(nameof(SelectedTabIndex));
        }

        private void RemoveTabPage(ITabPage tabPage)
        {
            if (tabPage.CanClose)
            {
                TabPages.Remove(tabPage);
            }
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                const double min = 1;
                const double max = 4;
                const double step = 0.1;

                if (e.Delta > 0 && Scale < max)
                {
                    Scale = Math.Min(max, Scale + step);
                    OnPropertyChanged(nameof(Scale));
                }
                else if (e.Delta < 0 && Scale > min)
                {
                    Scale = Math.Max(min, Scale - step);
                    OnPropertyChanged(nameof(Scale));
                }

                e.Handled = true;
                return;
            }

            base.OnPreviewMouseWheel(e);
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
