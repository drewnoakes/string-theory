using System;
using System.Threading;

namespace StringTheory.Analysis
{
    internal sealed class DisposableAction : IDisposable
    {
        private readonly Action _action;
        private int _isDisposed;

        public DisposableAction(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
            {
                _action();
            }
        }
    }
}