﻿// --------------------------------------------------------
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
    /// Opens .z packages using SharpZipLib.
    /// 
    /// See <see href="https://github.com/icsharpcode/SharpZipLib"/>.
    /// </summary>
    public class Lzw : IPackageLoaderOpenFile, IPackageLoaderUseBytes
    {
        private static readonly Lzw singleton = new Lzw();

        /// <summary>
        /// Static singleton instance that handles .Lzw extensions.
        /// </summary>
        public static Lzw Singleton => singleton;

        /// <summary>
        /// Supported file extensions
        /// </summary>
        public string FileExtensionPattern { get; internal set; }

        /// <summary>
        /// Create new package loader that loads z files.
        /// </summary>
        public Lzw() : this(@"\.z") { }

        /// <summary>
        /// Create new package loader that loads z files.
        /// </summary>
        /// <param name="fileExtensionPattern">regular expression pattern</param>
        public Lzw(string fileExtensionPattern)
        {
            this.FileExtensionPattern = fileExtensionPattern ?? throw new ArgumentNullException(nameof(fileExtensionPattern));
        }

        /// <summary>
        /// Opens a .Lzw file with zero to multiple open file handles.
        /// Is thread-safe and thread-scalable (concurrent use is possible).
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="packageInfo">(optional) clues about the file that is being opened</param>
        /// <returns></returns>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on z error</exception>
        public IFileProvider OpenFile(string filepath, IPackageLoadInfo packageInfo)
        {
            string entryName = ExtractName(packageInfo?.Path) ?? fallbackEntryName;

            try
            {
                return new LzwFileProvider(filepath, entryName, packageInfo?.Path, packageInfo?.LastModified);
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
                return new LzwFileProvider(data, entryName, packageInfo?.Path, packageInfo?.LastModified);
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
