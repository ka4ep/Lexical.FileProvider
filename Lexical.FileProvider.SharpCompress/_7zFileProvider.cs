// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           1.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using Lexical.FileProvider.Package;
using Lexical.FileProvider.Common;
using SharpCompress.Archives.SevenZip;

namespace Lexical.FileProvider
{
    /// <summary>
    /// Uses SharpCompress to open .7z files.
    /// 
    /// <see href="https://github.com/adamhathcock/sharpcompress"/>
    /// </summary>
    public class _7zFileProvider : Lexical.FileProvider.SharpCompress.Internal.ArchiveFileProvider, IDisposableFileProvider
    {
        /// <summary>
        /// Whether to convert '\' to '/'.
        /// </summary>
        public const bool defaultConvertBackslashesToSlashes = false;

        /// <summary>
        /// Create 7z content file provider.
        /// </summary>
        /// <param name="archive"></param>
        /// <param name="hintPath">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        public _7zFileProvider(SevenZipArchive archive, String hintPath = null, DateTimeOffset? dateTime = default, bool convertBackslashesToSlashes = defaultConvertBackslashesToSlashes) : base(archive, hintPath, dateTime, convertBackslashesToSlashes) { }

        /// <summary>
        /// Create file provider that reads 7z content from a readable and seekable stream. 
        /// 
        /// Note, that one file entry stream is allowed to be open at the same time. Others will wait in lock.
        /// 
        /// Does not dispose the <paramref name="stream"/> with the file provider.
        /// To dispose stream along with its file provider, construct it like this: <code>new SevenZipFileProvider(stream).AddDisposable(stream)</code>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="hintPath">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on 7z error</exception>
        public _7zFileProvider(Stream stream, String hintPath = null, DateTimeOffset? dateTime = default, bool convertBackslashesToSlashes = defaultConvertBackslashesToSlashes) : base(SevenZipArchive.Open(stream), hintPath, dateTime, convertBackslashesToSlashes) { }

        /// <summary>
        /// Create file provider that can reopen 7z archive for each concurrent thread.
        /// </summary>
        /// <param name="archiveOpener"></param>
        /// <param name="hintPath">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on 7z error</exception>
        public _7zFileProvider(Func<SevenZipArchive> archiveOpener, String hintPath = null, DateTimeOffset? dateTime = default, bool convertBackslashesToSlashes = defaultConvertBackslashesToSlashes) : base(archiveOpener, hintPath, dateTime, convertBackslashesToSlashes) { }

        /// <summary>
        /// Open .7z file for reading. Opening from a file allows concurrent reading of 7z entries.
        /// </summary>
        /// <param name="filepath">file name</param>
        /// <param name="hintPath">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on 7z error</exception>
        public _7zFileProvider(string filepath, String hintPath = null, DateTimeOffset? dateTime = default, bool convertBackslashesToSlashes = defaultConvertBackslashesToSlashes) : base(()=>SevenZipArchive.Open(filepath), hintPath, dateTime??File.GetLastWriteTimeUtc(filepath), convertBackslashesToSlashes) { }

        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the object.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public _7zFileProvider AddDisposable(object disposable)
        {
            if (disposable is IDisposable toDispose) ((IDisposeList)this).AddDisposable(toDispose);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the file provider after all streams are closed.
        /// </summary>
        /// <param name="disposable">object to dispose</param>
        /// <returns></returns>
        public _7zFileProvider AddBelatedDispose(object disposable)
        {
            if (disposable is IDisposable toDispose) belatedDisposeList.AddBelatedDispose(toDispose);
            return this;
        }

    }
}