using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

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

            tabPage.CloseRequested += delegate { RemoveTabPage(tabPage); };
        }

        private void RemoveTabPage(ITabPage tabPage)
        {
            if (tabPage.CanClose)
            {
                TabPages.Remove(tabPage);

                if (tabPage is IDisposable disposable)
                    disposable.Dispose();
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

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.F4 when CanCloseCurrentTab():
                    case Key.W when CanCloseCurrentTab():
                    {
                        // Closes the active tab (if it is closeable)
                        TabPages.RemoveAt(SelectedTabIndex);
                        e.Handled = true;
                        return;
                    }
                    case Key.PageDown:
                    {
                        // Go to the next tab
                        SelectedTabIndex = (SelectedTabIndex + 1)%TabPages.Count;
                        OnPropertyChanged(nameof(SelectedTabIndex));
                        e.Handled = true;
                        return;
                    }
                    case Key.PageUp:
                    {
                        // Go to previous tab
                        SelectedTabIndex = (SelectedTabIndex - 1 + TabPages.Count)%TabPages.Count;
                        OnPropertyChanged(nameof(SelectedTabIndex));
                        e.Handled = true;
                        return;
                    }
                }
            }

            base.OnPreviewKeyDown(e);

            bool CanCloseCurrentTab() => SelectedTabIndex >= 0 &&
                                         SelectedTabIndex < TabPages.Count &&
                                         TabPages[SelectedTabIndex].CanClose;
        }

        protected override void OnClosed(EventArgs e)
        {
            foreach (var disposable in TabPages.OfType<IDisposable>())
                disposable.Dispose();

            base.OnClosed(e);
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
