// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           18.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileProvider.Package;
using Microsoft.Extensions.FileProviders;
using SharpCompress.Archives.Tar;
using System;
using System.IO;

namespace Lexical.FileProvider.PackageLoader
{
    /// <summary>
    /// Uses <see cref="TarFileProvider"/> to open .tar files.
    /// </summary>
    public class Tar : IPackageLoaderOpenFile, IPackageLoaderUseStream, IPackageLoaderUseBytes
    {
        private static readonly Tar singleton = new Tar();

        /// <summary>
        /// Static singleton instance that handles .tar extensions.
        /// </summary>
        public static Tar Singleton => singleton;

        /// <summary>
        /// Supported file extensions
        /// </summary>
        public string FileExtensionPattern { get; internal set; }

        /// <summary>
        /// Policy whether to convert '\' to '/' in the file paths that this package loader handles.
        /// </summary>
        private readonly bool convertBackslashesToSlashes;

        /// <summary>
        /// Create new package loader that loads .tar files.
        /// </summary>
        public Tar() : this(@"\.tar") { }

        /// <summary>
        /// Create new package loader that loads .tar files.
        /// </summary>
        /// <param name="fileExtensionPattern">regular expression pattern</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        public Tar(string fileExtensionPattern, bool convertBackslashesToSlashes = TarFileProvider.defaultConvertBackslashesToSlashes)
        {
            this.FileExtensionPattern = fileExtensionPattern ?? throw new ArgumentNullException(nameof(fileExtensionPattern));
            this.convertBackslashesToSlashes = convertBackslashesToSlashes;
        }

        /// <summary>
        /// Opens a .tar file with zero to multiple open file handles.
        /// Is thread-safe and thread-scalable (concurrent use is possible).
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="packageInfo">(optional) clues about the file that is being opened</param>
        /// <returns>file provider to the contents of the package</returns>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on .tar error</exception>
        public IFileProvider OpenFile(string filepath, IPackageLoadInfo packageInfo)
        {
            try
            {
                return new TarFileProvider(filepath, packageInfo?.Path, packageInfo?.LastModified, convertBackslashesToSlashes);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                throw new PackageException.LoadError(filepath, e);
            }
        }

        /// <summary>
        /// Reads .tar file from a stream. Takes ownership of the stream (closes it). 
        /// Is thread-safe, but not thread-scalable (locks threads).
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="packageInfo">(optional) clues about the file that is being opened</param>
        /// <returns>file provider to the contents of the package</returns>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on .tar error</exception>
        public IFileProvider UseStream(Stream stream, IPackageLoadInfo packageInfo)
        {
            try
            {
                return new TarFileProvider(stream, packageInfo?.Path, packageInfo?.LastModified, convertBackslashesToSlashes).AddDisposable(stream);
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
        /// <param name="packageInfo">(optional) clues about the file that is being opened</param>
        /// <returns>file provider to the contents of the package</returns>
        public IFileProvider UseBytes(byte[] data, IPackageLoadInfo packageInfo = null)
        {
            try
            {
                TarArchive opener() => TarArchive.Open(new MemoryStream(data));
                return new TarFileProvider(opener, packageInfo?.Path, packageInfo?.LastModified);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                throw new PackageException.LoadError(null, e);
            }
        }

    }
}
