// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           22.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileProvider.Package;
using System;
using System.Threading;

namespace Lexical.FileProvider.Common
{
    /// <summary>
    /// A disposable handle that represents a subscription of <typeparamref name="T"/> from <see cref="IObservable{T}"/>.
    /// </summary>
    public class ObserverHandle<T> : IDisposable
    {
        /// <summary>
        /// Unsubscribe action
        /// </summary>
        Action<IObserver<T>> unsubscribeAction;

        /// <summary>
        /// subscribed observer
        /// </summary>
        IObserver<T> observer;

        /// <summary>
        /// 0 - not disposed
        /// 1 - is disposed
        /// </summary>
        long disposing = 0L;

        /// <summary>
        /// Tests if handle has been disposed.
        /// </summary>
        public bool IsDisposed => Interlocked.Read(ref disposing) != 0L;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unsubscribeAction"></param>
        /// <param name="observer"></param>
        public ObserverHandle(Action<IObserver<T>> unsubscribeAction, IObserver<T> observer)
        {
            this.unsubscribeAction = unsubscribeAction;
            this.observer = observer ?? throw new ArgumentNullException(nameof(observer));
        }

        /// <summary>
        /// Dispose handle which will unsubscribe the observer.
        /// </summary>
        public void Dispose()
        {
            // Only one thread can unsubscribe, and only once
            if (Interlocked.CompareExchange(ref disposing, 1L, 0L) != 0L) return;

            // Clear referenes
            var _unsubscribeAction = unsubscribeAction;
            var _observer = observer;
            unsubscribeAction = null;
            observer = null;

            // Run action
            _unsubscribeAction(_observer);
        }
    }
}
