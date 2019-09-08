// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           8.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileProvider.Common;
using Microsoft.Extensions.Options;
using System;
using System.Threading;

namespace Lexical.FileProvider.Package
{
    /// <summary>
    /// Adapts <see cref="IOptionsMonitor{TOptions}"/> to <see cref="TempFileProviderOptions"/>.
    /// </summary>
    public class TempFileProviderOptionsMonitor : TempFileProviderOptions, IDisposable
    {
        IOptionsMonitor<TempFileProviderOptions> monitor;
        IDisposable eventSubscription;

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="monitor"></param>
        public TempFileProviderOptionsMonitor(IOptionsMonitor<TempFileProviderOptions> monitor)
        {
            this.monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            TempFileProviderOptionsMonitor closure = this;
            eventSubscription = this.monitor.OnChange(r=>closure.ReadFrom(r));
            this.ReadFrom(monitor.CurrentValue);
        }

        /// <summary>
        /// Dispose adapter.
        /// </summary>
        public void Dispose()
            => Interlocked.CompareExchange(ref eventSubscription, null, eventSubscription)?.Dispose();
    }

}
