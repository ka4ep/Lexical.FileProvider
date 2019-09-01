// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           21.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Microsoft.Extensions.Options;
using System;

namespace Lexical.FileProvider.Package
{
    public static class PackageFileProviderExtensions_CacheEvict_Ext
    {
        /// <summary>
        /// Set cache eviction mechanism.
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="options">Options that has CacheEvictTime property.</param>
        /// <returns></returns>
        public static PackageFileProvider StartEvictTimer(this PackageFileProvider fileProvider, IOptionsMonitor<PackageFileProviderOptionsRecord> options)
        {
            EvictTimer evictMechanism = new EvictTimer(
                fileProvider,
                expiration: () => {
                    double value = options.CurrentValue.CacheEvictTime;
                    // If value <= 0, then disable by putting expiration time to infinite
                    return value <= 0 ? TimeSpan.MaxValue : TimeSpan.FromMilliseconds(options.CurrentValue.CacheEvictTime * 1000.0);
                },
                checkInterval: () => {
                    double value = options.CurrentValue.CacheEvictTime;
                    // If value <= 0, then check next times (the options) in 15 seconds.
                    return value <= 0 ? TimeSpan.FromSeconds(15) : TimeSpan.FromMilliseconds(options.CurrentValue.CacheEvictTime * 333.0);
                });
            fileProvider.AddDisposable(evictMechanism);
            evictMechanism.Start();
            return fileProvider;
        }

    }
}
