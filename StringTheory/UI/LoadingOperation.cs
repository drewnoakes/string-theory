using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace StringTheory.UI;

public sealed class LoadingOperation(Func<Action<double>, CancellationToken, ITabPage> operation) : IDisposable
{
    public event Action<ITabPage?>? Completed;

    public event Action<double>? ProgressChanged;

    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private int _isDisposed;

    public async Task Start()
    {
        Dispatcher.CurrentDispatcher.VerifyAccess();

        try
        {
            var result = await Task.Run(
                () => operation(SetProgress, _cancellationTokenSource.Token),
                _cancellationTokenSource.Token);

            Completed?.Invoke(result);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Clipboard.SetText(ex.ToString());
            MessageBox.Show($"Operation failed: {ex.Message}\n\nFull details copied to clipboard.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Completed?.Invoke(null);
        }

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