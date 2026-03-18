using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace StringTheory.UI;

public sealed class LoadingOperation(Func<Action<double>, CancellationToken, ITabPage> operation) : IDisposable
{
    public event Action<ITabPage> Completed;

    public event Action<double> ProgressChanged;

    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private int _isDisposed;

    public void Start()
    {
        Dispatcher.CurrentDispatcher.VerifyAccess();

        var scheduler = TaskScheduler.FromCurrentSynchronizationContext();

        var task = Task.Run(
            () => operation(SetProgress, _cancellationTokenSource.Token),
            _cancellationTokenSource.Token);

        task.ContinueWith(t =>
        {
            if (t.IsCanceled)
            {
                return;
            }

            if (t.IsFaulted)
            {
                Debug.Assert(t.Exception != null, "t.Exception != null");
                Clipboard.SetText(t.Exception.ToString());
                MessageBox.Show($"Operation failed: {t.Exception.Message}\n\nFull details copied to clipboard.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Completed?.Invoke(null);
                return;
            }

            Completed?.Invoke(t.Result);
        }, scheduler);

        void SetProgress(double d) => ProgressChanged?.Invoke(d);
    }

    public void Cancel()
    {
        if (Volatile.Read(ref _isDisposed) == 0)
            _cancellationTokenSource.Cancel();
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }
}