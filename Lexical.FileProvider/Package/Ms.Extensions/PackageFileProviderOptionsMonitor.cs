// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           8.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Lexical.FileProvider.Package
{
    /// <summary>
    /// Adapts <see cref="PackageFileProviderOptionsRecord"/> to <see cref="IPackageFileProviderOptions"/>.
    /// </summary>
    public class PackageFileProviderOptionsMonitor : PackageFileProviderOptions, IPackageFileProviderOptions, IDisposable
    {
        IOptionsMonitor<PackageFileProviderOptionsRecord> monitor;
        IDisposable eventSubscription;

        /// <summary>
        /// Create adapter.
        /// </summary>
        /// <param name="monitor"></param>
        public PackageFileProviderOptionsMonitor(IOptionsMonitor<PackageFileProviderOptionsRecord> monitor)
        {
            this.monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            PackageFileProviderOptionsMonitor closure = this;
            eventSubscription = this.monitor.OnChange(r=>Assign(r, closure));
            Assign(monitor.CurrentValue, this);
        }

        /// <summary>
        /// Copies configuration from poco <paramref name="src"/> to <paramref name="dst"/>.
        /// 
        /// Since packageloaders are type names in the poco, any new type will be loaded and instantiated.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        public static void Assign(PackageFileProviderOptionsRecord src, IPackageFileProviderOptions dst)
        {
            dst.AllowOpenFiles = src.AllowOpenFiles;
            dst.ReuseFailedResult = src.ReuseFailedResult;
            dst.MaxMemorySnapshotLength = src.MaxMemorySnapshotLength;
            dst.MaxTempSnapshotLength = src.MaxTempSnapshotLength;

            // Update packageloaders. 
            // Make list of new types
            List<Type> newPackageLoaders = (src.PackageLoaders ?? new string[0]).Select(typeName=>Type.GetType(typeName, true)).ToList();
            // Make list of old types
            Dictionary<Type, IPackageLoader> oldPackageLoaders = (dst.PackageLoaders ?? new IPackageLoader[0]).ToDictionary(pl=>pl.GetType());
            // Make new array
            IPackageLoader[] newArray = new IPackageLoader[newPackageLoaders.Count];
            for (int i=0; i<newArray.Length; i++)
            {
                Type type = newPackageLoaders[i];
                IPackageLoader pl;
                if (!oldPackageLoaders.TryGetValue(type, out pl)) pl = (IPackageLoader)Activator.CreateInstance(type);
                newArray[i] = pl;
            }
            // A new reference must always be created to trigger reload as per contract.
            dst.PackageLoaders = newArray;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
            => Interlocked.CompareExchange(ref eventSubscription, null, eventSubscription)?.Dispose();

    }

}
