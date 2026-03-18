using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media;

namespace StringTheory.Avalonia;

public sealed class LoadingTabPage : ITabPage, INotifyPropertyChanged, IDisposable
{
    public event Action? CloseRequested;
    public event Action<ITabPage>? PageLoaded;

    private readonly LoadingOperation _operation;

    public string HeaderText { get; private set; }
    public DrawingImage? IconDrawingImage { get; private set; }
    public bool IsIndeterminate { get; private set; } = true;
    public double ProgressRatio { get; private set; }

    public ITabPage? Page { get; private set; }

    public ICommand CancelCommand { get; }

    public bool CanClose => true;

    public LoadingTabPage(string tabTitle, DrawingImage? iconDrawingImage, LoadingOperation operation)
    {
        _operation = operation;
        HeaderText = tabTitle;
        IconDrawingImage = iconDrawingImage;

        CancelCommand = new DelegateCommand(Close);

        operation.Completed += page =>
        {
            if (page == null)
            {
                Close();
                return;
            }

            Page = page;
            PageLoaded?.Invoke(page);
        };

        operation.ProgressChanged += ratio =>
        {
            if (IsIndeterminate)
            {
                IsIndeterminate = false;
                OnPropertyChanged(nameof(IsIndeterminate));
            }

            if (ProgressRatio != ratio)
            {
                ProgressRatio = ratio;
                OnPropertyChanged(nameof(ProgressRatio));
            }
        };

        _ = operation.Start();

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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}
