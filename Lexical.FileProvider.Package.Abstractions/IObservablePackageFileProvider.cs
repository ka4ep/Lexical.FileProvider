// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           21.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileProvider.Package
{
    /// <summary>
    /// Interface for monitoring package file provider.
    /// 
    /// This interface is used for attaching logging and cache eviction mechanism.
    /// </summary>
    public interface IObservablePackageFileProvider : IPackageFileProvider, IObservable<PackageEvent>
    {
        /// <summary>
        /// Get a snapshot cached packages infos. 
        /// This includes loaded packages, and packages whose loading has failed and failure info is still cached.
        /// </summary>
        /// <returns>a snapshot array of snapshot entries</returns>
        PackageInfo[] GetPackageInfos();

        /// <summary>
        /// Get a handle to package info.
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns>package info or null</returns>
        PackageInfo GetPackageInfo(string filepath);

        /// <summary>
        /// Try to evict a package from cache.
        /// 
        /// It won't evict if it is locked by an open handle.
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns>true if packge is no longer loaded, or didn't exist. false if package remains loaded.</returns>
        bool Evict(string filepath);
    }

    public enum PackageState : int
    {
        /// <summary>
        /// Package has not yet been opened.
        /// </summary>
        NotOpened,

        /// <summary>
        /// Package has been opened
        /// </summary>
        Opened,

        /// <summary>
        /// Opening package resulted an error. Error state is cached and rethrown.
        /// </summary>
        Error,

        /// <summary>
        /// File entry is not a package. 
        /// This state is set if file doesn't exist, is a directory, or is not a package (exception was suppressed).
        /// </summary>
        NotPackage,

        /// <summary>
        /// Package entry is evicted, closed or disposed.
        /// 
        /// This is final state from which it will not change to any other state.
        /// </summary>
        Evicted
    }

    public class PackageEvent
    {
        public string FilePath;
        public IObservablePackageFileProvider FileProvider;
        public PackageState OldState;        
        public PackageState NewState;
        public DateTimeOffset EventTime;
        public Exception LoadError;

        public override string ToString()
            => $"{GetType().FullName}(NewState={NewState}, OldState={OldState}, EventTime={EventTime}, FilePath={FilePath}, LoadError={LoadError})";
    }

    public struct PackageInfo
    {
        /// <summary>
        /// State of the package
        /// </summary>
        public PackageState State;

        /// <summary>
        /// Filepath of the package file, also its identifier.
        /// 
        /// If might be in canonilized format "c:\temp\file.zip/somedata.dll/somedata.resources" -> "c:/temp/file.zip/somedata.dll/somedata.resources"
        /// </summary>
        public string FilePath;

        /// <summary>
        /// Time when package was load was attempted.
        /// </summary>
        public DateTimeOffset LoadTime;

        /// <summary>
        /// Time when package was last accessed.
        /// </summary>
        public DateTimeOffset LastAccessTime;

        /// <summary>
        /// Error that occured when loading package
        /// </summary>
        public Exception LoadError;

        /// <summary>
        /// Estimation of memory allocation.
        /// 
        /// If package is opened or streamed from a file, the estimation is lowered.
        /// </summary>
        public long Length;
    }
}
