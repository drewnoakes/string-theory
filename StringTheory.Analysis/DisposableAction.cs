using System;
using System.Threading;

namespace StringTheory.Analysis;

internal sealed class DisposableAction(Action action) : IDisposable
{
    private int _isDisposed;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
        {
            action();
        }
    }
}