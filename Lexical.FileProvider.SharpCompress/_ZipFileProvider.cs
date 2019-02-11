// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           1.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;
using Lexical.FileProvider.Package;
using Lexical.FileProvider.Common;
using SharpCompress.Archives.Zip;

namespace Lexical.FileProvider
{
    /// <summary>
    /// Uses SharpCompress library to open .zip files.
    /// 
    /// <see href="https://github.com/adamhathcock/sharpcompress"/>
    /// </summary>
    public class _ZipFileProvider : Lexical.FileProvider.SharpCompress.Internal.ArchiveFileProvider, IDisposableFileProvider
    {
        /// <summary>
        /// Whether to convert '\' to '/'.
        /// </summary>
        public const bool defaultConvertBackslashesToSlashes = false;

        /// <summary>
        /// Create zip content file provider.
        /// </summary>
        /// <param name="archive"></param>
        /// <param name="pathHint">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        public _ZipFileProvider(ZipArchive archive, String pathHint = null, DateTimeOffset? dateTime = default, bool convertBackslashesToSlashes = defaultConvertBackslashesToSlashes) : base(archive, pathHint, dateTime, convertBackslashesToSlashes) { }

        /// <summary>
        /// Create file provider that reads zip content from a readable and seekable stream. 
        /// 
        /// Note, that one file entry stream is allowed to be open at the same time. Others will wait in lock.
        /// 
        /// Does not dispose the <paramref name="stream"/> with the file provider.
        /// To dispose stream along with its file provider, construct it like this: <code>new ZipFileProvider(stream).AddDisposable(stream)</code>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="pathHint">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on zip error</exception>
        public _ZipFileProvider(Stream stream, String pathHint = null, DateTimeOffset? dateTime = default, bool convertBackslashesToSlashes = defaultConvertBackslashesToSlashes) : base(ZipArchive.Open(stream), pathHint, dateTime, convertBackslashesToSlashes) { }

        /// <summary>
        /// Create file provider that can reopen zip archive for each concurrent thread.
        /// </summary>
        /// <param name="archiveOpener"></param>
        /// <param name="pathHint">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on zip error</exception>
        public _ZipFileProvider(Func<ZipArchive> archiveOpener, String pathHint = null, DateTimeOffset? dateTime = default, bool convertBackslashesToSlashes = defaultConvertBackslashesToSlashes) : base(archiveOpener, pathHint, dateTime, convertBackslashesToSlashes) { }

        /// <summary>
        /// Open .zip file for reading. Opening from a file allows concurrent reading of zip entries.
        /// </summary>
        /// <param name="filename">file name</param>
        /// <param name="pathHint">(optional) clue of the file that is being opened</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on zip error</exception>
        public _ZipFileProvider(string filename, String pathHint = null, DateTimeOffset? dateTime = default, bool convertBackslashesToSlashes = defaultConvertBackslashesToSlashes) : base(()=>ZipArchive.Open(filename), pathHint, dateTime??File.GetLastWriteTimeUtc(filename), convertBackslashesToSlashes) { }
        
        public _ZipFileProvider AddDisposable(object disposable)
        {
            if (disposable is IDisposable toDispose) ((IDisposeList)this).AddDisposable(toDispose);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the file provider after all streams are closed.
        /// </summary>
        /// <param name="disposable">object to dispose</param>
        /// <returns></returns>
        public _ZipFileProvider AddBelatedDispose(object disposable)
        {
            if (disposable is IDisposable toDispose) belatedDisposeList.AddBelatedDispose(toDispose);
            return this;
        }

    }
}