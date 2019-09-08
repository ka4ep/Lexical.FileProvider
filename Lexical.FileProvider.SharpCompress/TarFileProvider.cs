// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           1.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;
using Lexical.FileProvider.Package;
using Lexical.FileProvider.Common;
using SharpCompress.Archives.Tar;

namespace Lexical.FileProvider
{
    /// <summary>
    /// Uses SharpCompress to open .tar files.
    /// 
    /// <see href="https://github.com/adamhathcock/sharpcompress"/>
    /// </summary>
    public class TarFileProvider : Lexical.FileProvider.SharpCompress.Internal.ArchiveFileProvider, IDisposableFileProvider
    {
        /// <summary>
        /// Whether to convert '\' to '/'.
        /// </summary>
        public const bool defaultConvertBackslashesToSlashes = false;

        /// <summary>
        /// Create tar content file provider.
        /// </summary>
        /// <param name="archive"></param>
        /// <param name="pathHint">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        public TarFileProvider(TarArchive archive, String pathHint = null, DateTimeOffset? dateTime = default, bool convertBackslashesToSlashes = defaultConvertBackslashesToSlashes) : base(archive, pathHint, dateTime, convertBackslashesToSlashes) { }

        /// <summary>
        /// Create file provider that reads tar content from a readable and seekable stream. 
        /// 
        /// Note, that one file entry stream is allowed to be open at the same time. Others will wait in lock.
        /// 
        /// Does not dispose the <paramref name="stream"/> with the file provider.
        /// To dispose stream along with its file provider, construct it like this: <code>new TarFileProvider(stream).AddDisposable(stream)</code>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="pathHint">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on tar error</exception>
        public TarFileProvider(Stream stream, String pathHint = null, DateTimeOffset? dateTime = default, bool convertBackslashesToSlashes = defaultConvertBackslashesToSlashes) : base(TarArchive.Open(stream), pathHint, dateTime, convertBackslashesToSlashes) { }

        /// <summary>
        /// Create file provider that can reopen tar archive for each concurrent thread.
        /// </summary>
        /// <param name="archiveOpener"></param>
        /// <param name="pathHint">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on tar error</exception>
        public TarFileProvider(Func<TarArchive> archiveOpener, String pathHint = null, DateTimeOffset? dateTime = default, bool convertBackslashesToSlashes = defaultConvertBackslashesToSlashes) : base(archiveOpener, pathHint, dateTime, convertBackslashesToSlashes) { }

        /// <summary>
        /// Open .tar file for reading. Opening from a file allows concurrent reading of tar entries.
        /// </summary>
        /// <param name="filename">file name</param>
        /// <param name="pathHint">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime"></param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on tar error</exception>
        public TarFileProvider(string filename, String pathHint = null, DateTimeOffset? dateTime = default, bool convertBackslashesToSlashes = defaultConvertBackslashesToSlashes) : base(()=>TarArchive.Open(filename), pathHint, dateTime??File.GetLastWriteTimeUtc(filename), convertBackslashesToSlashes) { }

        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the object.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public TarFileProvider AddDisposable(object disposable)
        {
            if (disposable is IDisposable toDispose) ((IDisposeList)this).AddDisposable(toDispose);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the file provider after all streams are closed.
        /// </summary>
        /// <param name="disposable">object to dispose</param>
        /// <returns></returns>
        public TarFileProvider AddBelatedDispose(object disposable)
        {
            if (disposable is IDisposable toDispose) belatedDisposeList.AddBelatedDispose(toDispose);
            return this;
        }
    }
}