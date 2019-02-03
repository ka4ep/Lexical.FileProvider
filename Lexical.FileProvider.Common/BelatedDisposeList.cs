// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           28.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Threading;

namespace Lexical.FileProvider.Common
{
    /// <summary>
    /// A disposable that manages a list of disposable objects.
    /// 
    /// Handles can be taken to postpone calling of the disposables.
    /// BelatedDisposeList itself has notdisposed/disposing/disposed state that is separate 
    /// from the dispose of the belated disposables.
    /// 
    /// It will dispose them all when Dispose() is called and all handles are disposed.
    /// </summary>
    public class BelatedDisposeList : IBelatedDisposeList
    {
        /// <summary>
        /// Internal counter. 
        /// </summary>
        internal long counter = 1L;

        /// <summary>
        /// List of disposables that has been attached with this object.
        /// </summary>
        protected List<IDisposable> disposeList = new List<IDisposable>(2);

        /// <summary>
        /// State that is set when disposing starts and finalizes.
        /// Is changed with Interlocked. 
        ///  0 - not disposed
        ///  1 - disposing
        ///  2 - disposed
        ///  
        /// When disposing starts, new objects cannot be added to the object, instead they are disposed right at away.
        /// </summary>
        protected long disposing;

        /// <summary>
        /// Property that checks thread-synchronously whether disposing has started or completed.
        /// </summary>
        public bool IsDisposing => Interlocked.Read(ref disposing) != 0L;

        /// <summary>
        /// Property that checks thread-synchronously whether disposing has started.
        /// </summary>
        public bool IsDisposed => Interlocked.Read(ref disposing) == 2L;

        /// <summary>
        /// Action that handles call to decrement counter and dispose disposelist.
        /// </summary>
        protected Action handleDisposeAction;

        /// <summary>
        /// Factory that creates handles
        /// </summary>
        protected Func<Object, Action, IDisposable> handleFactory;

        /// <summary>
        /// Create belated list
        /// </summary>
        public BelatedDisposeList()
        {
            handleDisposeAction = () =>
            {
                // Decrement counter
                long x = Interlocked.Decrement(ref counter);
                // Dispose the dispose list
                if (x == 0L) DisposeDisposableList();
            };

            handleFactory = (o, a) => new Handle(a);
        }

        /// <summary>
        /// Create belated list
        /// </summary>
        public BelatedDisposeList(Func<Object, Action, IDisposable> handleFactory)
        {
            handleDisposeAction = () =>
            {
                // Decrement counter
                long x = Interlocked.Decrement(ref counter);
                // Dispose the dispose list
                if (x == 0L) DisposeDisposableList();
            };
            this.handleFactory = handleFactory ?? throw new ArgumentNullException(nameof(handleFactory));
        }

        /// <summary>
        /// Create a handle that delays dispose.
        /// </summary>
        /// <returns>handle</returns>
        IDisposable IBelatedDisposeList.Belate()
        {
            // Assert state
            if (IsDisposing) throw new ObjectDisposedException(GetType().FullName);

            // Create handle
            IDisposable handle = handleFactory(null, handleDisposeAction);

            // Increment counter
            Interlocked.Increment(ref counter);

            // Return handle
            return handle;
        }

        /// <summary>
        /// Create a handle that delays dispose.
        /// </summary>
        /// <param name="state"></param>
        /// <returns>handle</returns>
        public IDisposable Belate(object state)
        {
            // Assert state
            if (IsDisposing) throw new ObjectDisposedException(GetType().FullName);

            // Create handle
            IDisposable handle = handleFactory(state, handleDisposeAction);

            // Increment counter
            Interlocked.Increment(ref counter);

            // Return handle
            return handle;
        }

        class Handle : IDisposable
        {
            Action action;

            public Handle(Action action)
            {
                this.action = action;
            }

            public void Dispose()
                => Interlocked.CompareExchange(ref action, null, action)?.Invoke();
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed with the file provider.
        /// 
        /// If parent object is disposed or being disposed, the disposable will be disposed immedialy.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>true if was added to list, false if was disposed right away</returns>
        bool IBelatedDisposeList.AddBelatedDispose(IDisposable disposable)
        {
            // Argument error
            if (disposable == null) throw new ArgumentNullException(nameof(disposable));

            // Parent is disposed/ing
            if (IsDisposing) { disposable.Dispose(); return false; }

            // Add to list
            lock (disposeList) disposeList.Add(disposable);

            // Check parent again
            if (IsDisposing) { lock (disposeList) disposeList.Remove(disposable); disposable.Dispose(); return false; }

            // OK
            return true;
        }

        bool IBelatedDisposeList.AddBelatedDisposes(IEnumerable<IDisposable> disposables)
        {
            // Argument error
            if (disposables == null) throw new ArgumentNullException(nameof(disposables));

            // Parent is disposed/ing
            if (IsDisposing)
            {
                // Captured errors
                List<Exception> disposeErrors = null;
                // Dispose now
                DisposeAndCapture(disposables, ref disposeErrors);
                // Throw captured errors
                if (disposeErrors != null) throw new AggregateException(disposeErrors);
                return false;
            }

            // Add to list
            lock (disposeList) disposeList.AddRange(disposables);

            // Check parent again
            if (IsDisposing)
            {
                // Captured errors
                List<Exception> disposeErrors = null;
                // Dispose now
                DisposeAndCapture(disposables, ref disposeErrors);
                // Remove
                lock (disposeList) foreach (IDisposable d in disposables) disposeList.Remove(d);
                // Throw captured errors
                if (disposeErrors != null) throw new AggregateException(disposeErrors);
                return false;
            }

            // OK
            return true;
        }

        /// <summary>
        /// Remove <paramref name="disposable"/> from list of attached disposables.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>true if an item of <paramref name="disposable"/> was removed, false if it wasn't there</returns>
        bool IBelatedDisposeList.RemoveBelatedDispose(IDisposable disposable)
        {
            // Argument error
            if (disposable == null) throw new ArgumentNullException(nameof(disposable));

            lock (this)
            {
                if (disposable == null) return false;
                return disposeList.Remove(disposable);
            }
        }

        /// <summary>
        /// Remove <paramref name="disposables"/> from the list. 
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>true if was removed, false if it wasn't in the list.</returns>
        bool IBelatedDisposeList.RemovedBelatedDisposes(IEnumerable<IDisposable> disposables)
        {
            // Argument error
            if (disposables == null) throw new ArgumentNullException(nameof(disposables));

            bool ok = true;
            lock (this)
            {
                if (disposables == null) return false;
                foreach (IDisposable d in disposables)
                    ok &= disposeList.Remove(d);
                return ok;
            }
        }

        /// <summary>
        /// Dispose enumerable and capture errors
        /// </summary>
        /// <param name="disposables">list of disposables</param>
        /// <param name="disposeErrors">list to be created if errors occur</param>
        public static void DisposeAndCapture(IEnumerable<IDisposable> disposables, ref List<Exception> disposeErrors)
        {
            if (disposables == null) return;

            // Dispose disposables
            foreach (IDisposable disposable in disposables)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (AggregateException ae)
                {
                    (disposeErrors ?? (disposeErrors = new List<Exception>())).AddRange(ae.InnerExceptions);
                }
                catch (Exception e)
                {
                    // Capture error
                    (disposeErrors ?? (disposeErrors = new List<Exception>())).Add(e);
                }
            }
        }

        public virtual void Dispose()
        {
            // Is disposing
            long previousState = Interlocked.CompareExchange(ref disposing, 1L, 0L);

            // First dispose call decrements counter.
            bool disposeDisposables = false;
            if (previousState == 0L) disposeDisposables = Interlocked.Decrement(ref counter) == 0L;

            // Captured errors
            List<Exception> disposeErrors = null;

            // Call innerDispose(). Capture errors to compose it with others.
            try
            {
                innerDispose(ref disposeErrors);
            }
            catch (Exception e)
            {
                // Capture error
                (disposeErrors ?? (disposeErrors = new List<Exception>())).Add(e);
            }

            // Dispose dispose list
            if (disposeDisposables)
            {
                try
                {
                    // Extract snapshot, clear array
                    IDisposable[] toDispose = null;
                    lock (this)
                    {
                        if (disposeList != null && disposeList.Count>0) toDispose = disposeList.ToArray();
                        disposeList.Clear();
                    }

                    // Dispose disposables
                    if (toDispose != null) DisposeAndCapture(toDispose, ref disposeErrors);
                }
                catch (Exception e)
                {
                    // Capture error
                    (disposeErrors ?? (disposeErrors = new List<Exception>())).Add(e);
                }
            }

            // Is disposed
            Interlocked.CompareExchange(ref disposing, 2L, 1L);

            // Throw captured errors
            if (disposeErrors != null) throw new AggregateException(disposeErrors);
        }

        /// <summary>
        /// Dispose dispose lists.
        /// </summary>
        /// <exception cref="AggregateException">thrown if disposing threw errors</exception>
        protected virtual void DisposeDisposableList()
        {
            // Extract snapshot, clear array
            IDisposable[] toDispose = null;
            lock (this) {
                if (disposeList != null && disposeList.Count>0) toDispose = disposeList.ToArray();
                disposeList.Clear();
            }

            // Captured errors
            List<Exception> disposeErrors = null;

            // Dispose disposables
            DisposeAndCapture(toDispose, ref disposeErrors);

            // Throw captured errors
            if (disposeErrors != null) throw new AggregateException(disposeErrors);
        }

        /// <summary>
        /// Override this to add more dispose mechanism in subtypes.
        /// 
        /// Inner dispose is not called when the disposable list is disposed.
        /// It is called when the BelatedDisposeList is disposed.
        /// </summary>
        /// <param name="disposeErrors">list that can be instantiated and where errors can be added</param>
        /// <exception cref="Exception">any exception is captured and aggregated with other errors</exception>
        protected virtual void innerDispose(ref List<Exception> disposeErrors)
        {
        }

    }
}
