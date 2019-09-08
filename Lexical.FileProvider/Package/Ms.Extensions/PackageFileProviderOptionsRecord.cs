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
        /// <summary>
        /// Default value for maximum length of a package file to read into RAM memory.
        /// </summary>
        public const long DefaultMaxMemorySnapshotLength = 1024 * 1024 * 1024;

        /// <summary>
        /// Default value for maximum length of a temporary file.
        /// </summary>
        public const long DefaultMaxTempSnapshotLength = long.MaxValue;

        /// <summary>
        /// Default array of package loaders.
        /// </summary>
        public static readonly IPackageLoader[] DefaultPackageLoaders = new IPackageLoader[0];

        /// <summary>
        /// Default error handler.
        /// </summary>
        public static readonly Func<PackageEvent, bool> DefaultErrorHandler = pe => pe.LoadError is PackageException.NoSuitableLoadCapability;

        /// <summary>
        /// Default policy for whether to allow to keep files open.
        /// 
        /// If false, then files are copied into memory or temp-file snapshots.
        /// </summary>
        public const bool DefaultAllowOpenFiles = true;

        /// <summary>
        /// Default policy for whether to reuse previous error results.
        /// 
        /// If true, caches exceptions. If false, retries on every new read attempt.
        /// </summary>
        public const bool DefaultReuseFailedResult = true;

        /// <summary>
        /// Default value (seconds) for how long time since package was acessed to prevent it from being evicted.
        /// </summary>
        public const double DefaultCacheEvictTime = 15;

        /// <summary>
        /// Policy for whether to allow to keep files open.
        /// 
        /// If false, then files are copied into memory or temp-file snapshots.
        /// </summary>
        public bool AllowOpenFiles { get; set; } = DefaultAllowOpenFiles;

        /// <summary>
        /// Policy for whether to reuse previous error results.
        /// 
        /// If true, caches exceptions. If false, retries on every new read attempt.
        /// </summary>
        public bool ReuseFailedResult { get; set; } = DefaultReuseFailedResult;

        /// <summary>
        /// Value for maximum length of a package file to read into RAM memory.
        /// </summary>
        public long MaxMemorySnapshotLength { get; set; } = DefaultMaxMemorySnapshotLength;

        /// <summary>
        /// Value for maximum length of a temporary file.
        /// </summary>
        public long MaxTempSnapshotLength { get; set; } = DefaultMaxTempSnapshotLength;

        /// <summary>
        /// List of package loaders to use with the package file provider. 
        /// 
        /// This represents the supported file extensions, such as: .zip, .rar, .dll, etc.
        /// </summary>
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
