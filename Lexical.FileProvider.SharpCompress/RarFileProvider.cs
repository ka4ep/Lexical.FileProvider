// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           1.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;
using Lexical.FileProvider.Package;
using Lexical.FileProvider.Common;
using SharpCompress.Archives.Rar;

namespace Lexical.FileProvider
{
    /// <summary>
    /// Uses SharpCompress library to open .rar files.
    /// 
    /// <see href="https://github.com/adamhathcock/sharpcompress"/>
    /// </summary>
    public class RarFileProvider : Lexical.FileProvider.SharpCompress.Internal.ArchiveFileProvider, IDisposableFileProvider
    {
        /// <summary>
        /// Whether to convert '\' to '/'.
        /// </summary>
        public const bool defaultConvertBackslashesToSlashes = true;

        /// <summary>
        /// Create .rar content file provider.
        /// </summary>
        /// <param name="archive"></param>
        /// <param name="hintPath">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        public RarFileProvider(RarArchive archive, String hintPath = null, DateTimeOffset? dateTime = default, bool convertBackslashesToSlashes = defaultConvertBackslashesToSlashes) : base(archive, hintPath, dateTime, convertBackslashesToSlashes) { }

        /// <summary>
        /// Create file provider that reads .rar content from a readable and seekable stream. 
        /// 
        /// Note, that one file entry stream is allowed to be open at the same time. Others will wait in lock.
        /// 
        /// Does not dispose the <paramref name="stream"/> with the file provider.
        /// To dispose stream along with its file provider, construct it like this: <code>new RarFileProvider(stream).AddDisposable(stream)</code>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="hintPath">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on .rar error</exception>
        public RarFileProvider(Stream stream, String hintPath = null, DateTimeOffset? dateTime = default, bool convertBackslashesToSlashes = defaultConvertBackslashesToSlashes) : base(RarArchive.Open(stream), hintPath, dateTime, convertBackslashesToSlashes) { }

        /// <summary>
        /// Create file provider that can reopen .rar archive for each concurrent thread.
        /// </summary>
        /// <param name="archiveOpener"></param>
        /// <param name="hintPath">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on .rar error</exception>
        public RarFileProvider(Func<RarArchive> archiveOpener, String hintPath = null, DateTimeOffset? dateTime = default, bool convertBackslashesToSlashes = defaultConvertBackslashesToSlashes) : base(archiveOpener, hintPath, dateTime, convertBackslashesToSlashes) { }

        /// <summary>
        /// Open .rar file for reading. Opening from a file allows concurrent reading of .rar entries.
        /// </summary>
        /// <param name="filepath">file name</param>
        /// <param name="hintPath">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">(optional) time for folder entries</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on .rar error</exception>
        public RarFileProvider(string filepath, String hintPath = null, DateTimeOffset? dateTime = default, bool convertBackslashesToSlashes = defaultConvertBackslashesToSlashes) : base(()=>RarArchive.Open(filepath), hintPath, dateTime??File.GetLastWriteTimeUtc(filepath), convertBackslashesToSlashes) { }

        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the objet.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public RarFileProvider AddDisposable(object disposable)
        {
            if (disposable is IDisposable toDispose) ((IDisposeList)this).AddDisposable(toDispose);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the file provider after all streams are closed.
        /// </summary>
        /// <param name="disposable">object to dispose</param>
        /// <returns></returns>
        public RarFileProvider AddBelatedDispose(object disposable)
        {
            if (disposable is IDisposable toDispose) belatedDisposeList.AddBelatedDispose(toDispose);
            return this;
        }
    }
}