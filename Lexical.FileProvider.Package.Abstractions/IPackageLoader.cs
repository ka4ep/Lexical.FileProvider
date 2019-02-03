// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           18.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lexical.FileProvider.Package
{
    #region IPackageLoader
    /// <summary>
    /// Interace for loaders that read in <see cref="IFileProvider"/>s.
    /// 
    /// Must implement one or more of the following sub-interfaces:
    ///    <see cref="IPackageLoaderOpenFileCapability"/>
    ///    <see cref="IPackageLoaderLoadFileCapability"/>
    ///    <see cref="IPackageLoaderUseStreamCapability"/>
    ///    <see cref="IPackageLoaderLoadFromStreamCapability"/>
    ///    <see cref="IPackageLoaderUseBytesCapability"/>
    /// </summary>
    public interface IPackageLoader
    {
        /// <summary>
        /// The file extension(s) this format can open.
        /// 
        /// The string is a regular expression. 
        /// For example "\.zip" or "\.zip|\.7z|\tar.gz"
        /// 
        /// Pattern will be used as case insensitive, so the case doesn't matter, but lower is preferred.
        /// 
        /// Do not add named groups. For example "(?&lt;name&gt;..)".
        /// 
        /// Unnamed groups are, however, allowed. For example: "\.zip(\.tmp)?"
        /// </summary>
        String FileExtensionPattern { get; }
    }
    #endregion IPackageLoader

    #region IPackageLoaderOpenFileCapability
    public interface IPackageLoaderOpenFileCapability : IPackageLoader
    {
        /// <summary>
        /// Create a <see cref="IFileProvider"/> that opens a file and is allowed to keep it open until the file provider is disposed. 
        /// 
        /// The caller is responsible for disposing the returned file provider if it implements <see cref="IDisposable"/>.
        /// </summary>
        /// <param name="filepath">data to read from</param>
        /// <param name="packageInfo">(optional) Information about packge that is being opened</param>
        /// <returns>file provider</returns>
        /// <exception cref="Exception">If there was unexpected error, such as IOException</exception>
        /// <exception cref="InvalidOperationException">If this load method is not supported.</exception>
        /// <exception cref="IOException">Problem with io stream</exception>
        /// <exception cref="PackageException.LoadError">The when file format is erronous, package will not be opened as directory.</exception>
        IFileProvider OpenFile(string filepath, IPackageLoadInfo packageInfo = null);
    }
    #endregion IPackageLoaderOpenFileCapability

    #region IPackageLoaderLoadFileCapability
    public interface IPackageLoaderLoadFileCapability : IPackageLoader
    {
        /// <summary>
        /// Loads <see cref="IFileProvider"/> completely from a file. 
        /// File must be closed when the call returns.
        /// 
        /// The caller is responsible for disposing the returned file provider if it implements <see cref="IDisposable"/>.
        /// </summary>
        /// <param name="filepath">data to read from</param>
        /// <param name="packageInfo">(optional) Information about packge that is being opened</param>
        /// <returns>file provider</returns>
        /// <exception cref="Exception">If there was unexpected error, such as IOException</exception>
        /// <exception cref="InvalidOperationException">If this load method is not supported.</exception>
        /// <exception cref="IOException">Problem with io stream</exception>
        /// <exception cref="PackageException.LoadError">The when file format is erronous, package will not be opened as directory.</exception>
        IFileProvider LoadFile(string filepath, IPackageLoadInfo packageInfo = null);
    }
    #endregion IPackageLoaderLoadFileCapability

    #region IPackageLoaderUseStreamCapability
    public interface IPackageLoaderUseStreamCapability : IPackageLoader
    {
        /// <summary>
        /// Create a <see cref="IFileProvider"/> that reads its contents from an open <paramref name="stream"/>.
        /// File provider takes ownership of the stream, and closes the stream along with the provider.
        /// 
        /// The stream must be readable and seekable, <see cref="Stream.CanSeek"/> must be true.
        /// 
        /// The caller is responsible for disposing the returned file provider if it implements <see cref="IDisposable"/>.
        /// 
        /// Note, open stream cannot be read concurrently. 
        /// </summary>
        /// <param name="stream">stream to read data from. Stream must be disposed along with the returned file provider.</param>
        /// <param name="packageInfo">(optional) Information about packge that is being opened</param>
        /// <returns>file provider that can be disposable</returns>
        /// <exception cref="Exception">If there was unexpected error, such as IOException</exception>
        /// <exception cref="InvalidOperationException">If this load method is not supported.</exception>
        /// <exception cref="IOException">Problem with io stream</exception>
        /// <exception cref="PackageException.LoadError">The when file format is erronous, package will not be opened as directory.</exception>
        IFileProvider UseStream(Stream stream, IPackageLoadInfo packageInfo = null);
    }
    #endregion IPackageLoaderUseStreamCapability

    #region IPackageLoaderLoadFromStreamCapability
    public interface IPackageLoaderLoadFromStreamCapability : IPackageLoader
    {
        /// <summary>
        /// Create a <see cref="IFileProvider"/> that is completely read from a <paramref name="stream"/>.
        /// The callee does not take ownership of the stream. 
        /// 
        /// The returned file provider can be left to be garbage collected and doesn't need to be disposed.
        /// </summary>
        /// <param name="stream">stream to read data from. Stream doesn't need to be closed by callee, but is allowed to do so.</param>
        /// <param name="packageInfo">(optional) Information about packge that is being opened</param>
        /// <returns>file provider</returns>
        /// <exception cref="Exception">If there was unexpected error, such as IOException</exception>
        /// <exception cref="InvalidOperationException">If this load method is not supported.</exception>
        /// <exception cref="IOException">Problem with io stream</exception>
        /// <exception cref="PackageException.LoadError">The when file format is erronous, package will not be opened as directory.</exception>
        IFileProvider LoadFromStream(Stream stream, IPackageLoadInfo packageInfo = null);
    }
    #endregion IPackageLoaderLoadFromStreamCapability

    #region IPackageLoaderUseBytesCapability
    public interface IPackageLoaderUseBytesCapability : IPackageLoader
    {
        /// <summary>
        /// Load file provider from bytes.
        /// 
        /// The caller is responsible for disposing the returned file provider if it implements <see cref="IDisposable"/>.
        /// </summary>
        /// <param name="data">data to read from</param>
        /// <param name="packageInfo">(optional) Information about packge that is being opened</param>
        /// <returns>file provider</returns>
        /// <exception cref="Exception">If there was unexpected error, such as IOException</exception>
        /// <exception cref="InvalidOperationException">If this load method is not supported.</exception>
        /// <exception cref="IOException">Problem with io stream</exception>
        /// <exception cref="PackageException.LoadError">The when file format is erronous, package will not be opened as directory.</exception>
        IFileProvider UseBytes(byte[] data, IPackageLoadInfo packageInfo = null);
    }
    #endregion IPackageLoaderUseBytesCapability    

    #region IPackageLoadInfo
    /// <summary>
    /// Optional hints about the package that is being loaded.
    /// </summary>
    public interface IPackageLoadInfo
    {
        /// <summary>
        /// (optional) Path within package file provider.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// (Optional) Last modified UTC date time.
        /// </summary>
        DateTimeOffset? LastModified { get; }

        /// <summary>
        /// File length, or -1 if unknown
        /// </summary>
        long Length { get; }
    }
    #endregion IPackageLoadInfo

    public static class PackageLoaderExtensions
    {
        /// <summary>
        /// Try to read supported file formats from the regular expression pattern.
        /// </summary>
        /// <param name="packageLoader"></param>
        /// <returns>for example "dll"</returns>
        public static IEnumerable<string> GetExtensions(this IPackageLoader packageLoader)
            => packageLoader.FileExtensionPattern.Split('|').Select(ext => ext.Replace(@"\", ""));

        /// <summary>
        /// Try to read supported file formats from the regular expression pattern.
        /// </summary>
        /// <param name="packageLoaders"></param>
        /// <returns>for example "dll", "zip", ... </returns>
        public static string[] GetExtensions(this IEnumerable<IPackageLoader> packageLoaders)
            => packageLoaders.SelectMany(pl => pl.FileExtensionPattern.Split('|')).Select(ext => ext.Replace(@"\", "")).ToArray();

        /// <summary>
        /// Sort packageloaders by the file extensions they support.
        /// </summary>
        /// <param name="packageLoaders"></param>
        /// <returns>map, e.g. { "dll", Dll.Singleton }</returns>
        public static IReadOnlyDictionary<string, IPackageLoader> SortByExtension(this IEnumerable<IPackageLoader> packageLoaders)
        {
            Dictionary<string, IPackageLoader> result = new Dictionary<string, IPackageLoader>();

            // Sort by extension
            foreach (IPackageLoader pl in packageLoaders)
                foreach (string extension in pl.GetExtensions())
                    result[extension] = pl;

            return result;
        }

    }

}
