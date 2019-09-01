// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           18.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lexical.FileProvider.Package
{
    /// <summary>
    /// Package loader options.
    /// </summary>
    public class PackageFileProviderOptions : IPackageFileProviderOptions, ICloneable
    {
        public const long DefaultMaxMemorySnapshotLength = 1024 * 1024 * 1024;
        public const long DefaultMaxTempSnapshotLength = long.MaxValue;
        public static readonly IPackageLoader[] DefaultPackageLoaders = new IPackageLoader[0];
        public static readonly Func<PackageEvent, bool> DefaultErrorHandler = pe => pe.LoadError is PackageException.NoSuitableLoadCapability || pe.LoadError is BadImageFormatException;
        public const bool DefaultAllowOpenFiles = true;
        public const bool DefaultReuseFailedResult = true;

        bool allowOpenFiles = DefaultAllowOpenFiles;
        bool reuseFailedResult = DefaultReuseFailedResult;
        long maxMemorySnapshotLength = DefaultMaxMemorySnapshotLength;
        long maxTempSnapshotLength = DefaultMaxTempSnapshotLength;
        IPackageLoader[] packageLoaders = DefaultPackageLoaders;
        Func<PackageEvent, bool> errorHandler = DefaultErrorHandler;

        /// <summary>
        /// Set policy whether open files is allowed or not.
        /// 
        /// If open files is allowed, then <see cref="IPackageFileProvider"/> can keep open files
        /// and keep them open for prolonged time. 
        /// 
        /// If the policy is disallowed, then the <see cref="IPackageFileProvider"/> will open files
        /// only to make snapshot copies of them.
        /// </summary>
        public bool AllowOpenFiles { get => allowOpenFiles; set => AssertWritable.allowOpenFiles = value; }

        /// <summary>
        /// Set policy whether to cache and reuse failed open package attempt.
        /// 
        /// If this policy is allowed, then <see cref="IPackageFileProvider"/> remembers what package files 
        /// could not be opened. Error result can be evicted just like other cached info.
        /// 
        /// If this policy is disallowed, then <see cref="IPackageFileProvider"/> will retry opening packages
        /// if they are requested again, even if they had failed previously.
        /// </summary>
        public bool ReuseFailedResult { get => reuseFailedResult; set => AssertWritable.reuseFailedResult = value; }
        
        /// <summary>
        /// A snapshot of package loaders. Writing takes a snapshot.
        /// </summary>
        public IEnumerable<IPackageLoader> PackageLoaders { get => packageLoaders; set => AssertWritable.packageLoaders = value.ToArray(); }

        /// <summary>
        /// Package loading error handler.
        /// </summary>
        public Func<PackageEvent, bool> ErrorHandler { get => errorHandler; set => AssertWritable.errorHandler = value; }

        /// <summary>
        /// Maximum byte[] blob to allocate.
        /// </summary>
        public long MaxMemorySnapshotLength { get => maxMemorySnapshotLength; set => AssertWritable.maxMemorySnapshotLength = value; } 

        /// <summary>
        /// Maximum temp file size to allocate.
        /// </summary>
        public long MaxTempSnapshotLength { get => maxTempSnapshotLength; set => AssertWritable.maxTempSnapshotLength = value; }

        /// <summary>
        /// Flag whether the object is in read-only state.
        /// </summary>
        bool isReadonly = false;

        /// <summary>
        /// Get and set read-only state.
        /// </summary>
        public bool IsReadonly
        {
            get => isReadonly;
            set {
                if (!value && isReadonly) throw new InvalidOperationException("Cannot change readonly object back to writable.");
                isReadonly = value;
            }
        }

        /// <summary>
        /// Asserts that the object is in writable state.
        /// </summary>
        PackageFileProviderOptions AssertWritable
            => isReadonly ? throw new InvalidOperationException(nameof(PackageFileProviderOptions) + " is set to read-only.") : this;

        /// <summary>
        /// Changes the state of the options to read-only.
        /// </summary>
        /// <returns>this</returns>
        public PackageFileProviderOptions SetReadonly()
        {
            IsReadonly = true;
            return this;
        }

        /// <summary>
        /// Create options with default values.
        /// </summary>
        public PackageFileProviderOptions() { }

        /// <summary>
        /// Create options with default values.
        /// </summary>
        /// <param name="packageLoaders"></param>
        public PackageFileProviderOptions(IEnumerable<IPackageLoader> packageLoaders)
        {
            this.packageLoaders = packageLoaders.ToArray();
        }

        /// <summary>
        /// Create a copy of <paramref name="ops"/>.
        /// </summary>
        /// <param name="ops"></param>
        /// <returns>copy</returns>
        public static PackageFileProviderOptions CopyFrom(IPackageFileProviderOptions ops)
            => (PackageFileProviderOptions)
                new PackageFileProviderOptions()
                .SetTempFileSnapshotLength(ops.MaxTempSnapshotLength)
                .SetReuseFailedResult(ops.ReuseFailedResult)
                .SetAllowOpenFiles(ops.AllowOpenFiles)
                .SetMemorySnapshotLength(ops.MaxMemorySnapshotLength)
                .SetErrorHandler(ops.ErrorHandler)
                .SetPackageLoaders(ops.PackageLoaders);

        public virtual object Clone()
            => CopyFrom(this);

        public override string ToString()
            => $"{nameof(IPackageFileProviderOptions)}({nameof(AllowOpenFiles)}={AllowOpenFiles}, {nameof(ReuseFailedResult)}={ReuseFailedResult}, {nameof(PackageLoaders)}=[{String.Join(", ", PackageLoaders)}], {nameof(MaxMemorySnapshotLength)}={MaxTempSnapshotLength}, {nameof(MaxTempSnapshotLength)}={MaxTempSnapshotLength})";

    }

    public static partial class PackageFileProviderOptionExtensions_
    {
        /// <summary>
        /// Create a clone that is read-only.
        /// </summary>
        /// <param name="options"></param>
        /// <returns>options if was already readonly, or a readonly clone</returns>
        public static IPackageFileProviderOptions AsReadonly(this IPackageFileProviderOptions options)
            => options is PackageFileProviderOptions ops && ops.IsReadonly ? ops : PackageFileProviderOptions.CopyFrom(options).SetReadonly();
    }
}
