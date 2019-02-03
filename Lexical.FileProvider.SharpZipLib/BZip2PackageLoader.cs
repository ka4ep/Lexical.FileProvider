// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           2.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileProvider.Package;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;

namespace Lexical.FileProvider.PackageLoader
{
    /// <summary>
    /// Opens .bzip2 packages using SharpZipLib.
    /// 
    /// See <see href="https://github.com/icsharpcode/SharpZipLib"/>.
    /// </summary>
    public class BZip2 : IPackageLoaderOpenFileCapability, IPackageLoaderUseBytesCapability
    {
        private static BZip2 singleton = new BZip2();

        /// <summary>
        /// Static singleton instance that handles .bzip2 extensions.
        /// </summary>
        public static BZip2 Singleton => singleton;

        /// <summary>
        /// Supported file extensions
        /// </summary>
        public string FileExtensionPattern { get; internal set; }

        /// <summary>
        /// Create new package loader that loads .bzip2 files.
        /// </summary>
        public BZip2() : this(@"\.bzip2") { }

        /// <summary>
        /// Create new package loader that loads bzip2 files.
        /// </summary>
        /// <param name="fileExtensionPattern">regular expression pattern</param>
        public BZip2(string fileExtensionPattern)
        {
            this.FileExtensionPattern = fileExtensionPattern ?? throw new ArgumentNullException(nameof(fileExtensionPattern));
        }

        /// <summary>
        /// Opens a .bzip2 file with zero to multiple open file handles.
        /// Is thread-safe and thread-scalable (concurrent use is possible).
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on bzip2 error</exception>
        public IFileProvider OpenFile(string filepath, IPackageLoadInfo packageInfo)
        {
            string entryName = ExtractName(packageInfo?.Path) ?? fallbackEntryName;

            try
            {
                return new BZip2FileProvider(filepath, entryName, packageInfo?.Path, packageInfo?.LastModified);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                throw new PackageException.LoadError(filepath, e);
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
                return new BZip2FileProvider(data, entryName, packageInfo?.Path, packageInfo?.LastModified);
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
