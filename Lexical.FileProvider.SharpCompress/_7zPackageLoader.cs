// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           18.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileProvider.Package;
using Microsoft.Extensions.FileProviders;
using SharpCompress.Archives.SevenZip;
using System;
using System.IO;

namespace Lexical.FileProvider.PackageLoader
{
    /// <summary>
    /// Uses <see cref="_7zFileProvider"/> to open 7z files.
    /// </summary>
    public class _7z : IPackageLoaderOpenFileCapability, IPackageLoaderUseStreamCapability, IPackageLoaderUseBytesCapability
    {
        private static _7z singleton = new _7z();

        /// <summary>
        /// Static singleton instance that handles .7z extensions.
        /// </summary>
        public static _7z Singleton => singleton;

        /// <summary>
        /// Supported file extensions
        /// </summary>
        public string FileExtensionPattern { get; internal set; }

        /// <summary>
        /// Policy whether to convert '\' to '/' in the file paths that this package loader handles.
        /// </summary>
        bool convertBackslashesToSlashes;

        /// <summary>
        /// Create new package loader that loads 7z files.
        /// </summary>
        public _7z() : this(@"\.7z") { }

        /// <summary>
        /// Create new package loader that loads 7z files.
        /// </summary>
        /// <param name="fileExtensionPattern">regular expression pattern</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        public _7z(string fileExtensionPattern, bool convertBackslashesToSlashes = _7zFileProvider.defaultConvertBackslashesToSlashes)
        {
            this.FileExtensionPattern = fileExtensionPattern ?? throw new ArgumentNullException(nameof(fileExtensionPattern));
            this.convertBackslashesToSlashes = convertBackslashesToSlashes;
        }

        /// <summary>
        /// Opens a .7z file with zero to multiple open file handles.
        /// Is thread-safe and thread-scalable (concurrent use is possible).
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="packageInfo">(optional) clues about the file that is being opened</param>
        /// <returns>file provider to the contents of the package</returns>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on 7z error</exception>
        public IFileProvider OpenFile(string filename, IPackageLoadInfo packageInfo)
        {
            try
            {
                return new _7zFileProvider(filename, packageInfo?.Path, packageInfo?.LastModified, convertBackslashesToSlashes);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                throw new PackageException.LoadError(filename, e);
            }
        }

        /// <summary>
        /// Reads 7z file from a stream. Takes ownership of the stream (closes it). 
        /// Is thread-safe, but not thread-scalable (locks threads).
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="packageInfo">(optional) clues about the file that is being opened</param>
        /// <returns>file provider to the contents of the package</returns>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on 7z error</exception>
        public IFileProvider UseStream(Stream stream, IPackageLoadInfo packageInfo)
        {
            try
            {
                return new _7zFileProvider(stream, packageInfo?.Path, packageInfo?.LastModified, convertBackslashesToSlashes).AddDisposable(stream);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                try { stream.Dispose(); } catch (Exception) { }
                throw new PackageException.LoadError(null, e);
            }
        }

        /// <summary>
        /// Read archive from a byte[]. The caller must close the returned file provider.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="packageInfo">(optional) clues about the file that is being opened</param>
        /// <returns>file provider to the contents of the package</returns>
        public IFileProvider UseBytes(byte[] data, IPackageLoadInfo packageInfo = null)
        {
            try
            {
                Func<SevenZipArchive> opener = () => SevenZipArchive.Open(new MemoryStream(data));
                return new _7zFileProvider(opener, packageInfo?.Path, packageInfo?.LastModified);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                throw new PackageException.LoadError(null, e);
            }
        }

    }
}
