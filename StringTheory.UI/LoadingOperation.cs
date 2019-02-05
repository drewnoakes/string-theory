using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace StringTheory.UI
{
    public sealed class LoadingOperation : IDisposable
    {
        public event Action<ITabPage> Completed;

        private readonly CancellationTokenSource _cancellationTokenSource;

        private int _isDisposed;

        public LoadingOperation(Func<CancellationToken, ITabPage> operation)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            var task = Task.Run(
                () => operation(_cancellationTokenSource.Token),
                _cancellationTokenSource.Token);

            task.ContinueWith(t =>
            {
                if (t.IsCanceled)
                {
                    return;
                }

                if (t.IsFaulted)
                {
                    // TODO update UI
                    Debug.Fail("Task faulted");
                    return;
                }

                // Complete on UI thread
                Dispatcher.CurrentDispatcher.Invoke(() => Completed?.Invoke(t.Result));
            });
        }

        public void Cancel()
        {
            if (Volatile.Read(ref _isDisposed) == 0)
                _cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
        }
    }
}