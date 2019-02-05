using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;

namespace StringTheory.UI
{
    public sealed class LoadingTabPage : ITabPage, INotifyPropertyChanged, IDisposable
    {
        public event Action CloseRequested;

        private readonly LoadingOperation _operation;

        public string HeaderText { get; private set; }
        public DrawingBrush IconDrawingBrush { get; private set; }

        public ITabPage Page { get; private set; }

        public ICommand CancelCommand { get; }

        public bool CanClose => true;

        public LoadingTabPage(string tabTitle, DrawingBrush iconDrawingBrush, LoadingOperation operation)
        {
            _operation = operation;
            HeaderText = tabTitle;
            IconDrawingBrush = iconDrawingBrush;

            CancelCommand = new DelegateCommand(Close);

            operation.Completed += page =>
            {
                if (page == null)
                {
                    Close();
                    return;
                }

                Page = page;
                OnPropertyChanged(nameof(Page));
                HeaderText = page.HeaderText;
                OnPropertyChanged(nameof(HeaderText));
                IconDrawingBrush = page.IconDrawingBrush;
                OnPropertyChanged(nameof(IconDrawingBrush));
            };

            void Close()
            {
                operation.Cancel();
                CloseRequested?.Invoke();
            }
        }

        public void Dispose()
        {
            _operation.Dispose();
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