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
    /// Adapts <see cref="TempFileProviderOptionsRecord"/> to <see cref="ITempFileProviderOptions"/>.
    /// </summary>
    public class TempFileProviderOptionsMonitor : TempFileProviderOptions, IDisposable
    {
        IOptionsMonitor<TempFileProviderOptions> monitor;
        IDisposable eventSubscription;

        public TempFileProviderOptionsMonitor(IOptionsMonitor<TempFileProviderOptions> monitor)
        {
            this.monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            TempFileProviderOptionsMonitor closure = this;
            eventSubscription = this.monitor.OnChange(r=>closure.ReadFrom(r));
            this.ReadFrom(monitor.CurrentValue);
        }

        public void Dispose()
            => Interlocked.CompareExchange(ref eventSubscription, null, eventSubscription)?.Dispose();
    }

}
