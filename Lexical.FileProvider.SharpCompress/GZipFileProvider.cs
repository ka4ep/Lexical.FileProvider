// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           1.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;
using System.Linq;
using Lexical.FileProvider.Package;
using Lexical.FileProvider.Common;
using SharpCompress.Archives;
using SharpCompress.Archives.GZip;

namespace Lexical.FileProvider
{
    /// <summary>
    /// Uses SharpCompress to open .gz files.
    /// 
    /// <see href="https://github.com/adamhathcock/sharpcompress"/>
    /// </summary>
    public class GZipFileProvider : Lexical.FileProvider.Common.ArchiveFileProvider, IDisposableFileProvider
    {

        /// <summary>
        /// Create file provider that reads one entry from <paramref name="archive"/>.
        /// </summary>
        /// <param name="archive"></param>
        /// <param name="entryName">Entry name of the whole package</param>
        /// <param name="hintPath">(optional) archive name "folder/document.txt.gz", entry name is extracted by removing the folder (separator '/') and last extension.</param>
        /// <param name="dateTime">(optional) Date time for folder entries</param>
        public GZipFileProvider(GZipArchive archive, string entryName, string hintPath = null, DateTimeOffset? dateTime = null) : base(hintPath, dateTime)
        {
            this.streamProvider = new Lexical.FileProvider.SharpCompress.Internal.ArchiveStreamProvider(archive ?? throw new ArgumentNullException(nameof(archive)), belatedDisposeList);
            IArchiveEntry entry = archive.Entries.First();

            long length;
            using (Stream s = entry.OpenEntryStream())
                length = CalculateLength(s);

            var fileEntry = new Lexical.FileProvider.Common.ArchiveFileEntry(streamProvider, entry.Key, entryName, length, dateTime ?? entry.LastModifiedTime ?? DateTime.MinValue);
            this.root.files[entryName] = fileEntry;
        }

        /// <summary>
        /// Create file provider that re-opens archive.
        /// </summary>
        /// <param name="archiveOpener"></param>
        /// <param name="entryName">Entry name of the whole package</param>
        /// <param name="hintPath">(optional) archive name "folder/document.txt.gz", entry name is extracted by removing the folder (separator '/') and last extension.</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on file format error</exception>
        public GZipFileProvider(Func<GZipArchive> archiveOpener, string entryName, string hintPath = null, DateTimeOffset? dateTime = default) : base(hintPath, dateTime)
        {
            if (archiveOpener == null) throw new ArgumentNullException(nameof(archiveOpener));

            // Place holder for the uncompressed length value
            long[] length = new long[1];
            // Convert archiveOpener to streamOpener
            Stream streamOpener()
            {
                IArchive archive = archiveOpener();
                try
                {
                    // Search entry
                    IArchiveEntry entry = archive.Entries.First();
                    // Not found
                    if (entry == null) { archive.Dispose(); return null; }
                    // Open stream
                    Stream s = entry.OpenEntryStream();
                    // Attach the disposing of the archive to the stream.
                    return new GZipStreamFix(s, archive, null, length[0]);
                }
                catch (Exception) when (CloseDisposable(archive))
                {
                    // Never goes here
                    return null;
                }
            }

            // Create stream provider
            this.streamProvider = new StreamOpener(streamOpener, entryName, belatedDisposeList);
            // Open once to read entries.
            using (var archive = archiveOpener())
            {
                // Read first entry
                IArchiveEntry entry = archive.Entries.First();
                // Get length
                using (var s = entry.OpenEntryStream())
                    length[0] = CalculateLength(s);
                // File entry
                var fileEntry = new Lexical.FileProvider.Common.ArchiveFileEntry(streamProvider, entryName, entryName, length[0], dateTime ?? entry.LastModifiedTime ?? DateTime.MinValue);
                // Put it in directory
                this.root.files[entryName] = fileEntry;
            }
        }

        /// <summary>
        /// Create file provider that reads .gz content from a readable and seekable stream. 
        /// 
        /// Note, that one file entry stream is allowed to be open at the same time. Others will wait in lock.
        /// 
        /// Does not dispose the <paramref name="stream"/> with the file provider.
        /// To dispose stream along with its file provider, construct it like this: <code>new GZipFileProvider(stream).AddDisposable(stream)</code>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="entryName">Entry name of the whole package</param>
        /// <param name="hintPath">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on .gz error</exception>
        public GZipFileProvider(Stream stream, String entryName, String hintPath = null, DateTimeOffset? dateTime = default)
            : this(GZipArchive.Open(stream), entryName, hintPath, dateTime) { }

        /// <summary>
        /// Open .gz file for reading. Opening from a file allows concurrent reading of .gz entries.
        /// </summary>
        /// <param name="filepath">file name</param>
        /// <param name="entryName">Entry name of the whole package</param>
        /// <param name="hintPath">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on .gz error</exception>
        public GZipFileProvider(string filepath, String entryName, String hintPath = null, DateTimeOffset? dateTime = default)
            : this(() => GZipArchive.Open(filepath), entryName, hintPath, dateTime ?? File.GetLastWriteTimeUtc(filepath)) { }

        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the object.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public GZipFileProvider AddDisposable(object disposable)
        {
            if (disposable is IDisposable toDispose) ((IDisposeList)this).AddDisposable(toDispose);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the file provider after all streams are closed.
        /// </summary>
        /// <param name="disposable">object to dispose</param>
        /// <returns></returns>
        public GZipFileProvider AddBelatedDispose(object disposable)
        {
            if (disposable is IDisposable toDispose) belatedDisposeList.AddBelatedDispose(toDispose);
            return this;
        }

        /// <summary>
        /// Calculate length by extracting the whole thing once. 
        /// It's bad for performance, but needed for maximum interoperability
        /// </summary>
        /// <param name="opener"></param>
        /// <returns></returns>
#pragma warning disable IDE0051 // Remove unused private members
        static long CalculateLength(Func<Stream> opener)
#pragma warning restore IDE0051 // Remove unused private members
        {
            using (Stream s = opener())
                return CalculateLength(s);
        }

        static long CalculateLength(Stream s)
        {
            long length = 0L;
            byte[] buffer = new byte[0x10000];
            do
            {
                int x = s.Read(buffer, 0, buffer.Length);
                if (x <= 0) break;
                length += x;
            } while (true);
            return length;
        }

        static bool CloseDisposable(IDisposable disposable)
        {
            disposable.Dispose();
            return false;
        }

    }

    /// <summary>
    /// Work-around to <see cref="Stream"/> that replaces <see cref="Length"/> value.
    /// </summary>
    public class GZipStreamFix : StreamHandle
    {
        /// <summary>
        /// New length value.
        /// </summary>
        readonly long newLength;

        /// <summary>
        /// Overridden length
        /// </summary>
        public override long Length => newLength;

        /// <summary>
        /// 
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// 
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// 
        /// </summary>
        public override bool CanTimeout => false;

        /// <summary>
        /// Create stream with overriding Length value.
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <param name="disposeHandle"></param>
        /// <param name="disposeAction"></param>
        /// <param name="newLength"></param>
        public GZipStreamFix(Stream sourceStream, IDisposable disposeHandle, Action disposeAction, long newLength) : base(sourceStream, disposeHandle, disposeAction)
        {
            this.newLength = newLength;
        }
    }
}