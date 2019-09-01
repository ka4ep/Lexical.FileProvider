// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           20.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lexical.FileProvider.Package
{
    public interface IPackageFileProviderOptions
    {
        /// <summary>
        /// Permission to allow to open package files and keep their file handles open.
        /// Open handle can be to a physical file, or to a file entry in a parent package.
        /// 
        /// Note, that when packet is opened with open stream, it might not be accessible
        /// concurrently if the parent is another open stream.
        /// </summary>
        bool AllowOpenFiles { get; set; }

        /// <summary>
        /// If package opening failed, the reason of the failure can be cached and rethrown 
        /// without new attempt on every method call.
        /// </summary>
        bool ReuseFailedResult { get; set; }

        /// <summary>
        /// Maximum length of memory snapshots.
        /// If this value is over 0, then package provider is allowed to take complete snapshots of package files into memory.
        /// </summary>
        long MaxMemorySnapshotLength { get; set; }

        /// <summary>
        /// Maximum temp file size allowed.
        /// 
        /// If this value is over 0, then package provider is allowed to take complete snapshots of package files into temp files.
        /// Note that <see cref="TempFileProvider"/> must be assigned.
        /// </summary>
        long MaxTempSnapshotLength { get; set; }

        /// <summary>
        /// Enumeration of package loaders. 
        /// Assigning a new reference updates the package loaders on the package file provider.
        /// </summary>
        IEnumerable<IPackageLoader> PackageLoaders { get; set; }

        /// <summary>
        /// This function handles package loading errors.
        /// If the function returns true, then the error is suppressed, and package is set to <see cref="PackageState.NotPackage"/> state.
        /// If function returns false or the delegate is null, then the error is thrown and package is put to Error state.
        /// Error may also be cached and be rethrown depending on <see cref="PackageFileProviderPolicy.ReuseFailedResult"/> policy.
        /// </summary>
        Func<PackageEvent, bool> ErrorHandler { get; set; }
    }

    public static partial class PackageFileProviderOptionExtensions
    {
        /// <summary>
        /// Configure options.
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IPackageFileProvider ConfigureOptions(this IPackageFileProvider fileProvider, Action<IPackageFileProviderOptions> configure)
        {
            if (configure != null) configure(fileProvider.Options);
            return fileProvider;
        }

        /// <summary>
        /// Assign package loading error handler. Logging can be added here.
        /// 
        /// Set this delegate to null to let exception be thrown to caller. 
        /// Usually when handling <see cref="IFileInfo"/> or <see cref="IDirectoryContents"/>.
        /// 
        /// If delegate returns true, then the exception is suppressed and the 
        /// package is handled as a regular file and not opened.
        /// <param name="errorHandler"></param>
        /// </summary>
        /// <param name="options"></param>
        /// <param name="errorHandler"></param>
        /// <returns></returns>
        public static IPackageFileProviderOptions SetErrorHandler(this IPackageFileProviderOptions options, Func<PackageEvent, bool> errorHandler)
        {
            options.ErrorHandler = errorHandler;
            return options;
        }

        /// <summary>
        /// Configure package file provider to suppress package loading errors.
        /// When package loading fails and error is suppressed, then the file is treated as it is normal non-package file.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IPackageFileProviderOptions SetToSuppressErrors(this IPackageFileProviderOptions options)
            => options.SetErrorHandler(suppress_errors);

        /// <summary>
        /// Configure file provider to let package loading errors be thrown.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IPackageFileProviderOptions SetToThrowErrors(this IPackageFileProviderOptions options)
            => options.SetErrorHandler(throw_errors);

        static Func<PackageEvent, bool> suppress_errors = e => true;
        static Func<PackageEvent, bool> throw_errors = e => false;

        /// <summary>
        /// Assign new set of package loaders.
        /// 
        /// <param name="options"></param>
        /// <param name="packageLoaders"></param>
        /// </summary>
        /// <returns>file provider</returns>
        public static IPackageFileProviderOptions SetPackageLoaders(this IPackageFileProviderOptions options, IEnumerable<IPackageLoader> packageLoaders)
        {
            options.PackageLoaders = packageLoaders;
            return options;
        }

        /// <summary>
        /// Add package loader.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="loader"></param>
        /// <returns></returns>
        public static IPackageFileProviderOptions AddPackageLoader(this IPackageFileProviderOptions options, IPackageLoader loader)
            => options.SetPackageLoaders(options.PackageLoaders.Concat(Enumerable.Repeat(loader, 1).ToArray()));

        /// <summary>
        /// Add package loaders.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="loaders"></param>
        /// <returns></returns>
        public static IPackageFileProviderOptions AddPackageLoaders(this IPackageFileProviderOptions options, IEnumerable<IPackageLoader> loaders)
            => options.SetPackageLoaders(options.PackageLoaders.Concat(loaders).ToArray());

        /// <summary>
        /// Add package loaders.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="loaders"></param>
        /// <returns></returns>
        public static IPackageFileProviderOptions AddPackageLoaders(this IPackageFileProviderOptions options, params IPackageLoader[] loaders)
            => options.SetPackageLoaders(options.PackageLoaders.Concat(loaders).ToArray());

        /// <summary>
        /// Set maximum memory snapshot length. If value is over 0, then memory snapshots are allowed.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="maxMemorySnapshotLength"></param>
        /// <returns></returns>
        public static IPackageFileProviderOptions SetMemorySnapshotLength(this IPackageFileProviderOptions options, long maxMemorySnapshotLength)
        {
            options.MaxMemorySnapshotLength = maxMemorySnapshotLength;
            return options;
        }

        /// <summary>
        /// Set maximum memory temp file snapshot length. If value is over 0, then temp file snapshots are allowed.
        /// 
        /// Note, that the options must be configured with TempProvider for temp files to work. 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="maxTempFileSnapshotLength"></param>
        /// <returns></returns>
        public static IPackageFileProviderOptions SetTempFileSnapshotLength(this IPackageFileProviderOptions options, long maxTempFileSnapshotLength)
        {
            options.MaxTempSnapshotLength = maxTempFileSnapshotLength;
            return options;
        }


        /// <summary>
        /// Set policy whether open files is allowed or not.
        /// 
        /// If open files is allowed, then <see cref="IPackageFileProvider"/> can keep open files
        /// and keep them open for prolonged time. 
        /// 
        /// If the policy is disallowed, then the <see cref="IPackageFileProvider"/> will open files
        /// only to make snapshot copies of them.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="allowOpenFiles"></param>
        /// <returns></returns>
        public static IPackageFileProviderOptions SetAllowOpenFiles(this IPackageFileProviderOptions options, bool allowOpenFiles)
        {
            options.AllowOpenFiles = allowOpenFiles;
            return options;
        }

        /// <summary>
        /// Set policy whether to cache and reuse failed open package attempt.
        /// 
        /// If this policy is allowed, then <see cref="IPackageFileProvider"/> remembers what package files 
        /// could not be opened. Error result can be evicted just like other cached info.
        /// 
        /// If this policy is disallowed, then <see cref="IPackageFileProvider"/> will retry opening packages
        /// if they are requested again, even if they had failed previously.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="reuseFailedResult"></param>
        /// <returns></returns>
        public static IPackageFileProviderOptions SetReuseFailedResult(this IPackageFileProviderOptions options, bool reuseFailedResult)
        {
            options.ReuseFailedResult = reuseFailedResult;
            return options;
        }

    }
}
