// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           18.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileProvider.Package;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using System.IO.Compression;

namespace Lexical.FileProvider.PackageLoader
{
    /// <summary>
    /// Uses <see cref="ZipFileProvider"/> to open zip files.
    /// </summary>
    public class Zip : IPackageLoaderOpenFile, IPackageLoaderUseStream, IPackageLoaderUseBytes
    {
        private static Zip singleton = new Zip();

        /// <summary>
        /// Static singleton instance that handles .zip extensions.
        /// </summary>
        public static Zip Singleton => singleton;

        /// <summary>
        /// Supported file extensions
        /// </summary>
        public string FileExtensionPattern { get; internal set; }

        /// <summary>
        /// Create new package loader that loads zip files.
        /// </summary>
        public Zip() : this(@"\.zip") { }

        /// <summary>
        /// Create new package loader that loads zip files.
        /// </summary>
        /// <param name="fileExtensionPattern">regular expression pattern</param>
        public Zip(string fileExtensionPattern)
        {
            this.FileExtensionPattern = fileExtensionPattern ?? throw new ArgumentNullException(nameof(fileExtensionPattern));
        }

        /// <summary>
        /// Opens a .zip file with zero to multiple open file handles.
        /// Is thread-safe and thread-scalable (concurrent use is possible).
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="packageInfo">(optional) clues about the file that is being opened</param>
        /// <returns>file provider that represents the package</returns>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on file format error</exception>
        public IFileProvider OpenFile(string filename, IPackageLoadInfo packageInfo)
        {
            try
            {
                return new ZipFileProvider(filename, default, packageInfo?.Path, packageInfo?.LastModified);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                throw new PackageException.LoadError(filename, e);
            }
        }

        /// <summary>
        /// Reads zip file from a stream. Takes ownership of the stream (closes it). 
        /// Is thread-safe, but not thread-scalable (locks threads).
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="packageInfo">(optional) clues about the file that is being opened</param>
        /// <returns>file provider that represents the package</returns>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on file format error</exception>
        public IFileProvider UseStream(Stream stream, IPackageLoadInfo packageInfo)
        {
            try
            {
                return new ZipFileProvider(stream, default, packageInfo?.Path, packageInfo?.LastModified).AddDisposable(stream);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                throw new PackageException.LoadError(null, e);
            }
        }

        /// <summary>
        /// Read zip file from a byte[]. The caller must close the returned file provider.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="packageInfo"></param>
        /// <returns></returns>
        public IFileProvider UseBytes(byte[] data, IPackageLoadInfo packageInfo = null)
        {
            try
            {
                Func<ZipArchive> opener = () => new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read);
                return new ZipFileProvider(opener, packageInfo?.Path, packageInfo?.LastModified);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                throw new PackageException.LoadError(null, e);
            }
        }
    }
}
