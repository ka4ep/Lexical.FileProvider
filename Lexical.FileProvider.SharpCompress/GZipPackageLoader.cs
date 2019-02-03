// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           18.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileProvider.Package;
using Microsoft.Extensions.FileProviders;
using SharpCompress.Archives.GZip;
using System;
using System.IO;

namespace Lexical.FileProvider.PackageLoader
{
    /// <summary>
    /// Uses <see cref="GZipFileProvider"/> to open .gz files.
    /// </summary>
    public class GZip : IPackageLoaderOpenFileCapability, IPackageLoaderUseStreamCapability, IPackageLoaderUseBytesCapability
    {
        private static GZip singleton = new GZip();

        /// <summary>
        /// Static singleton instance that handles .gz extensions.
        /// </summary>
        public static GZip Singleton => singleton;

        /// <summary>
        /// Supported file extensions
        /// </summary>
        public string FileExtensionPattern { get; internal set; }

        /// <summary>
        /// Create new package loader that loads .gz files.
        /// </summary>
        public GZip() : this(@"\.gz|\.gzip") { }

        /// <summary>
        /// Create new package loader that loads .gz files.
        /// </summary>
        /// <param name="fileExtensionPattern">regular expression pattern</param>
        public GZip(string fileExtensionPattern)
        {
            this.FileExtensionPattern = fileExtensionPattern ?? throw new ArgumentNullException(nameof(fileExtensionPattern));
        }

        /// <summary>
        /// Opens a .gz file with zero to multiple open file handles.
        /// Is thread-safe and thread-scalable (concurrent use is possible).
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on .gz error</exception>
        public IFileProvider OpenFile(string filepath, IPackageLoadInfo packageInfo)
        {
            string entryName = ExtractName(packageInfo?.Path) ?? fallbackEntryName;
            try
            {
                return new GZipFileProvider(filepath, entryName, packageInfo?.Path, packageInfo?.LastModified);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                throw new PackageException.LoadError(filepath, e);
            }
        }

        /// <summary>
        /// Reads .gz file from a stream. Takes ownership of the stream (closes it). 
        /// Is thread-safe, but not thread-scalable (locks threads).
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="filename">(optional) clue of the file that is being opened</param>
        /// <returns></returns>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on .gz error</exception>
        public IFileProvider UseStream(Stream stream, IPackageLoadInfo packageInfo)
        {
            string entryName = ExtractName(packageInfo?.Path) ?? fallbackEntryName;
            try
            {
                return new GZipFileProvider(stream, entryName, packageInfo?.Path, packageInfo?.LastModified).AddDisposable(stream);
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
        /// <param name="packageInfo"></param>
        /// <returns></returns>
        public IFileProvider UseBytes(byte[] data, IPackageLoadInfo packageInfo = null)
        {
            string entryName = ExtractName(packageInfo?.Path) ?? fallbackEntryName;
            try
            {
                Func<GZipArchive> opener = () => GZipArchive.Open(new MemoryStream(data));
                return new GZipFileProvider(opener, entryName, packageInfo?.Path, packageInfo?.LastModified);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                throw new PackageException.LoadError(null, e);
            }
        }

        /// <summary>
        /// Name to use if entry name is not available.
        /// </summary>
        public const string fallbackEntryName = "file";

        /// <summary>
        /// Extracts filename for the content entry.
        /// For example "mypath/document.txt.gz" -> "document.txt"
        /// 
        /// If path is not available returns "file"
        /// </summary>
        /// <param name="path">(optional)</param>
        /// <returns></returns>
        protected virtual string ExtractName(string path)
        {
            if (path == null || path == "") return null;
            int startIx = path.LastIndexOf('/') + 1, endIx = path.LastIndexOf('.');
            if (startIx < 0) startIx = 0;
            if (endIx < 0) endIx = path.Length - 1;
            if (endIx <= startIx) return null;
            string result = path.Substring(startIx, endIx - startIx);
            return result;
        }

    }
}
