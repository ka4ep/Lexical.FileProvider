// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           19.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileProvider.Common;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Lexical.FileProvider.Package
{
    /// <summary>
    /// This class represents the record value that is used with cache.
    /// 
    /// This class is internal to and part of <see cref="PackageFileProvider"/>.
    /// </summary>
    internal class PackageEntry : DisposeList
    {
        public PackageFileReference packageReference;
        public IFileProvider fp;
        public PackageState state = PackageState.NotOpened;
        public Exception error;
        public DateTimeOffset loadTime = DateTimeOffset.MinValue, lastAccessTime = DateTimeOffset.MinValue;
        public readonly Object m_lock = new Object();
        public Action disposeAction;

        /// <summary>
        /// File provider memory impact length estimate.
        /// </summary>
        public long length;

        public PackageEntry(PackageFileReference packageReference, Action disposeAction)
        {
            this.packageReference = packageReference ?? throw new ArgumentNullException(nameof(packageReference));
            this.disposeAction = disposeAction;
        }

        public PackageState ReadState(out IFileProvider resultFileProvider, out Exception error)
        {
            lock(m_lock)
            {
                error = this.error;
                resultFileProvider = this.fp;
                return this.state;
            }
        }

        public override void Dispose()
        {
            // Set to is disposing state here.
            Interlocked.CompareExchange(ref disposing, 1L, 0L);

            try
            {
                // Wait until file provider operation has ended.
                lock (m_lock)
                {
                    this.state = PackageState.Evicted;
                    this.fp = null;
                    this.length = 0L;

                    // Dispose the disposelist 
                    base.Dispose();
                }
            }
            finally
            {
                disposeAction?.Invoke();
            }

        }

        /// <summary>
        /// The number of open handles to this package entry
        /// </summary>
        public long handleCount = 0L;

        /// <summary>
        /// Create a handle that prevents the package entry from being evicted.
        /// If won't, however, prevent it from being disposed.
        /// </summary>
        /// <returns>fileprovider handle that must be disposed</returns>
        internal FileProviderHandle CreateHandle()
        {
            // Assert is not disposed
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            // Let's wait until operation is finished
            lock(m_lock)
            {
                // Retest disposed
                if (IsDisposed || state == PackageState.Evicted) throw new ObjectDisposedException(GetType().FullName);

                // Increment counter
                Interlocked.Increment(ref handleCount);

                // Update accessed time
                lastAccessTime = DateTimeOffset.UtcNow;

                // Return handle
                return new FileProviderHandle(handleDisposeAction, this, fp);
            }
        }

        /// <summary>
        /// Release handle action
        /// </summary>
        static Action<object> handleDisposeAction = state => Interlocked.Decrement(ref (state as PackageEntry).handleCount);
    }

    /// <summary>
    /// <see cref="PackageEntry"/> extension methods.
    /// </summary>
    public static class PackageEntryExtensions
    {
        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with <paramref name="packageEntry"/>.
        /// 
        /// If <paramref name="disposable"/> is not <see cref="IDisposable"/>, then it's not added.
        /// </summary>
        /// <param name="packageEntry"></param>
        /// <param name="disposable">object to dispose</param>
        /// <returns></returns>
        internal static PackageEntry AddDisposable(this PackageEntry packageEntry, object disposable)
        {
            if (disposable is IDisposable toDispose && packageEntry is IDisposeList disposeList)
                disposeList.AddDisposable(toDispose);
            return packageEntry;
        }

        /// <summary>
        /// Add <paramref name="disposables"/> to be disposed along with <paramref name="packageEntry"/>.
        /// 
        /// If <paramref name="disposables"/> is not <see cref="IDisposable"/>, then it's not added.
        /// </summary>
        /// <param name="packageEntry"></param>
        /// <param name="disposables">object(s) to dispose</param>
        /// <returns></returns>
        internal static PackageEntry AddDisposables(this PackageEntry packageEntry, IEnumerable disposables)
        {
            foreach(var disposable in disposables)
            {
                if (disposable is IDisposable toDispose && packageEntry is IDisposeList disposeList)
                    disposeList.AddDisposable(toDispose);
            }
            return packageEntry;
        }

    }
}
