// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           18.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileProvider.Package;
using Microsoft.Extensions.FileProviders;
using SharpCompress.Archives.Zip;
using System;
using System.IO;

namespace Lexical.FileProvider.PackageLoader
{
    /// <summary>
    /// Uses <see cref="_ZipFileProvider"/> to open archive file.
    /// </summary>
    public class _Zip : IPackageLoaderOpenFile, IPackageLoaderUseStream, IPackageLoaderUseBytes
    {
        private static _Zip singleton = new _Zip();

        /// <summary>
        /// Static singleton instance that handles .zip extensions.
        /// </summary>
        public static _Zip Singleton => singleton;

        /// <summary>
        /// Supported file extensions
        /// </summary>
        public string FileExtensionPattern { get; internal set; }

        /// <summary>
        /// Policy whether to convert '\' to '/' in the file entry paths.
        /// </summary>
        bool convertBackslashesToSlashes;

        /// <summary>
        /// Create new package loader that loads rar files.
        /// </summary>
        public _Zip() : this(@"\.zip") { }

        /// <summary>
        /// Create new package loader that loads rar files.
        /// </summary>
        /// <param name="fileExtensionPattern">regular expression pattern</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        public _Zip(string fileExtensionPattern, bool convertBackslashesToSlashes = _ZipFileProvider.defaultConvertBackslashesToSlashes)
        {
            this.FileExtensionPattern = fileExtensionPattern ?? throw new ArgumentNullException(nameof(fileExtensionPattern));
            this.convertBackslashesToSlashes = convertBackslashesToSlashes;
        }

        /// <summary>
        /// Opens a .zip file with zero to multiple open file handles.
        /// Is thread-safe and thread-scalable (concurrent use is possible).
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="packageInfo">(optional) clues about the file that is being opened</param>
        /// <returns>file provider to the contents of the package</returns>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on rar error</exception>
        public IFileProvider OpenFile(string filepath, IPackageLoadInfo packageInfo)
        {
            try
            {
                return new _ZipFileProvider(filepath, packageInfo?.Path, packageInfo?.LastModified, convertBackslashesToSlashes);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                throw new PackageException.LoadError(filepath, e);
            }
        }

        /// <summary>
        /// Reads rar file from a stream. Takes ownership of the stream (closes it). 
        /// Is thread-safe, but not thread-scalable (locks threads).
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="packageInfo">(optional) clues about the file that is being opened</param>
        /// <returns>file provider to the contents of the package</returns>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on rar error</exception>
        public IFileProvider UseStream(Stream stream, IPackageLoadInfo packageInfo)
        {
            try
            {
                return new _ZipFileProvider(stream, packageInfo?.Path, packageInfo?.LastModified, convertBackslashesToSlashes).AddDisposable(stream);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                throw new PackageException.LoadError(null, e);
            }
        }

        /// <summary>
        /// Read archive from a byte[]. The caller must close the returned file provider.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="packageInfo"></param>
        /// <returns></returns>
        public IFileProvider UseBytes(byte[] data, IPackageLoadInfo packageInfo = null)
        {
            try
            {
                Func<ZipArchive> opener = () => ZipArchive.Open(new MemoryStream(data));
                return new _ZipFileProvider(opener, packageInfo?.Path, packageInfo?.LastModified);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                throw new PackageException.LoadError(null, e);
            }
        }

    }
}
