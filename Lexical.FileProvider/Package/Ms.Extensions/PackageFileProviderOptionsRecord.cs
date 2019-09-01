// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           8.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;

namespace Lexical.FileProvider.Package
{
    /// <summary>
    /// IOptions compatible POCO record for <see cref="IPackageFileProvider"/>.
    /// </summary>
    public class PackageFileProviderOptionsRecord
    {
        public const long DefaultMaxMemorySnapshotLength = 1024 * 1024 * 1024;
        public const long DefaultMaxTempSnapshotLength = long.MaxValue;
        public static readonly IPackageLoader[] DefaultPackageLoaders = new IPackageLoader[0];
        public static readonly Func<PackageEvent, bool> DefaultErrorHandler = pe => pe.LoadError is PackageException.NoSuitableLoadCapability;
        public const bool DefaultAllowOpenFiles = true;
        public const bool DefaultReuseFailedResult = true;
        public const double DefaultCacheEvictTime = 15;

        public bool AllowOpenFiles { get; set; } = DefaultAllowOpenFiles;
        public bool ReuseFailedResult { get; set; } = DefaultReuseFailedResult;
        public long MaxMemorySnapshotLength { get; set; } = DefaultMaxMemorySnapshotLength;
        public long MaxTempSnapshotLength { get; set; } = DefaultMaxTempSnapshotLength;
        public IList<String> PackageLoaders { get; } = new List<string>();

        /// <summary>
        /// Number of seconds to keep inactive package in cache. 
        /// If package hasn't been used after this time, it will be evited.
        /// 
        /// If value is 0 then evict is disabled.
        /// </summary>
        public double CacheEvictTime { get; set; } = DefaultCacheEvictTime;
    }

}
