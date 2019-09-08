// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           21.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lexical.FileProvider.Package
{
    /// <summary>
    /// Extension methods for <see cref="PackageFileProvider"/>.
    /// </summary>
    public static class PackageFileProviderExtensions_CacheEvict
    {
        /// <summary>
        /// Set cache eviction mechanism.
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="expiration">How long a cache can be inactive before it will get evicted</param>
        /// <param name="checkInterval">How often to check inactive packages</param>
        /// <returns></returns>
        public static PackageFileProvider StartEvictTimer(this PackageFileProvider fileProvider, TimeSpan expiration, TimeSpan checkInterval)
        {
            EvictTimer evictMechanism = new EvictTimer(fileProvider, expiration, checkInterval);
            fileProvider.AddDisposable(evictMechanism);
            evictMechanism.Start();
            return fileProvider;
        }

        /// <summary>
        /// Evict all open package entries.
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <returns>true if all were evicted, false if atleast one still persists.</returns>
        public static bool EvictAll(this PackageFileProvider fileProvider)
        {
            bool ok = true;
            foreach (PackageInfo pi in fileProvider.GetPackageInfos())
                if (pi.State == PackageState.Error || pi.State == PackageState.NotPackage || pi.State == PackageState.Opened)
                    ok &= fileProvider.Evict(pi.FilePath);
            return ok;
        }
    }

    class EvictTimer : IDisposable
    {
        IObservablePackageFileProvider fileProvider;
        Func<TimeSpan> expirationFunc;
        Func<TimeSpan> checkIntervalFunc;
        Action<Task> checkFunc;
        Action checkFunc2;
        long disposed = 0L;
        CancellationTokenSource cancelSrc = new CancellationTokenSource();

        /// <summary>
        /// Create evict timer.
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="expiration">time tolerated of inactivity</param>
        /// <param name="checkInterval">check interval.</param>
        public EvictTimer(IObservablePackageFileProvider fileProvider, TimeSpan expiration, TimeSpan checkInterval)
            : this(fileProvider, ()=>expiration, ()=>checkInterval) { }

        public EvictTimer(IObservablePackageFileProvider fileProvider, Func<TimeSpan> expiration, Func<TimeSpan> checkInterval)
        {
            this.fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
            this.expirationFunc = expiration ?? throw new ArgumentNullException(nameof(expiration));
            this.checkIntervalFunc = checkInterval ?? throw new ArgumentNullException(nameof(checkInterval));
            checkFunc = OnTimer;
            checkFunc2 = () => OnTimer(null);
        }

        public EvictTimer Start()
        {
            Task.Factory.StartNew(checkFunc2, cancelSrc.Token);
            return this;
        }

        void OnTimer(Task t)
        {
            var _fileProvider = fileProvider;
            if (disposed != 0L || _fileProvider == null) return;

            List<Exception> errors = null;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            TimeSpan _expiration = expirationFunc();
            TimeSpan _checkInterval = checkIntervalFunc();

            // Repeat until delay is positive
            long ms = -1L;
            for (int i=0; i<10 && ms<0; i++)
            {
                foreach (PackageInfo packageInfo in _fileProvider.GetPackageInfos())
                {
                    if (packageInfo.State != PackageState.Opened) continue;
                    try
                    {
                        TimeSpan span = now - packageInfo.LastAccessTime;
                        if (span < _expiration) continue;
                        _fileProvider.Evict(packageInfo.FilePath);
                    }
                    catch (Exception e)
                    {
                        (errors ?? (errors = new List<Exception>())).Add(e);
                    }
                }

                TimeSpan delay = _checkInterval - (DateTimeOffset.UtcNow - now);
                ms = (long)delay.TotalMilliseconds;
            };

            // Queue new task
            if (ms > 0L)
            {
                Task.Delay((int)ms, cancelSrc.Token).ContinueWith(checkFunc, cancelSrc.Token);
            } else
            {
                Task.Factory.StartNew(checkFunc2);
            }
        }

        public void Dispose()
        {
            disposed = 1L;
            fileProvider = null;
            cancelSrc.Dispose();
        }
    }

}
