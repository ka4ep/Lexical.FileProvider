// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           29.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using Lexical.FileProvider.Package;
using Lexical.FileProvider.Common;

namespace Lexical.FileProvider
{
    public class ZipFileProvider : ArchiveFileProvider, IDisposableFileProvider
    {
        /// <summary>
        /// Create zip content file provider.
        /// </summary>
        /// <param name="zipArchive"></param>
        /// <param name="path">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        public ZipFileProvider(ZipArchive zipArchive, String path = null, DateTimeOffset? dateTime = null) : base(path, dateTime)
        {
            this.streamProvider = new ZipArchiveStreamProvider(zipArchive ?? throw new ArgumentNullException(nameof(zipArchive)), belatedDisposeList);
            AddArchiveEntries(this.root, zipArchive.Entries, streamProvider);
        }

        /// <summary>
        /// Create file provider that reads zip content from a readable and seekable stream. 
        /// 
        /// Note, that one file entry stream is allowed to be open at the same time. Others will wait in lock.
        /// 
        /// Does not dispose the <paramref name="stream"/> with the file provider.
        /// To dispose stream along with its file provider, construct it like this: <code>new ZipFileProvider(stream).AddDisposable(stream)</code>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding">(optional) encoding of zip file entries</param>
        /// <param name="path">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <returns></returns>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on file format error</exception>
        public ZipFileProvider(Stream stream, Encoding encoding = default, String path = null, DateTimeOffset? dateTime = null) : base(path, dateTime)
        {
            // Assert args
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead) throw new ArgumentException($"{nameof(stream)} must be readable.");
            if (!stream.CanSeek) throw new ArgumentException($"{nameof(stream)} must be seekable.");

            // Open .zip
            ZipArchive zipArchive = encoding != null ?
                new ZipArchive(stream, ZipArchiveMode.Read, false, encoding) :
                new ZipArchive(stream, ZipArchiveMode.Read, false);
            this.streamProvider = new ZipArchiveStreamProvider(zipArchive, belatedDisposeList);

            // Add zip entries
            AddArchiveEntries(this.root, zipArchive.Entries, streamProvider);
        }

        /// <summary>
        /// Create file provider that can reopen zip archive for each concurrent thread.
        /// </summary>
        /// <param name="zipArchiveOpener"></param>
        /// <param name="path">(optional) clue of the file that is being opened</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on file format error</exception>
        public ZipFileProvider(Func<ZipArchive> zipArchiveOpener, String path = null, DateTimeOffset? dateTime = null) : base(path, dateTime)
        {
            // Create stream provider
            this.streamProvider = new ZipOpenerStreamProvider(zipArchiveOpener ?? throw new ArgumentNullException(nameof(zipArchiveOpener)), belatedDisposeList);

            // Open once to read entries.
            using (var zipArchive = zipArchiveOpener())
                AddArchiveEntries(this.root, zipArchive.Entries, streamProvider);
        }

        /// <summary>
        /// Open .zip file for reading. Opening from a file allows concurrent reading of zip entries.
        /// </summary>
        /// <param name="filename">file name</param>
        /// <param name="path">(optional) path of the file being opened within its root file provider</param>
        /// <param name="encoding">(optional) encoding of zip file entries</param>
        /// <param name="lastModified">(optional)</param>
        /// <returns></returns>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on file format error</exception>
        public ZipFileProvider(String filename, Encoding encoding = default, string path = null, DateTimeOffset? lastModified = default) : base(path, lastModified??File.GetLastWriteTimeUtc(filename))
        {
            // Make opener function
            Func<ZipArchive> opener;
            if (encoding != null) opener = () => new ZipArchive(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read), ZipArchiveMode.Read, false, encoding);
            else opener = () => new ZipArchive(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read), ZipArchiveMode.Read, false);

            // Create stream provider
            this.streamProvider = new ZipOpenerStreamProvider(opener, belatedDisposeList);

            // Open once to read entries.
            using (var zipArchive = opener())
                AddArchiveEntries(this.root, zipArchive.Entries, streamProvider);
        }

        /// <summary>
        /// Add <see cref="ZipArchiveEntry"/> into tree structure.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="zipArchiveEntries"></param>
        /// <param name="streamProvider">stream provider for files</param>
        /// <returns>this</returns>
        static ArchiveDirectoryEntry AddArchiveEntries(ArchiveDirectoryEntry root, IEnumerable<ZipArchiveEntry> zipArchiveEntries, IStreamProvider streamProvider)
        {
            foreach (ZipArchiveEntry ze in zipArchiveEntries)
            {
                // Is entry a file
                if (!ze.FullName.EndsWith("/") || ze.Length > 0L)
                {
                    // Split to filename and path
                    int slashIx = ze.FullName.LastIndexOf('/');
                    string filename = ze.FullName.Substring(slashIx + 1);
                    string dirPath = slashIx < 0 ? "" : ze.FullName.Substring(0, slashIx);

                    // Create file entry
                    ArchiveFileEntry fileEntry = new ArchiveFileEntry(streamProvider, ze.FullName, filename, ze.Length, ze.LastWriteTime);

                    // Create dir
                    ArchiveDirectoryEntry dir = root.GetOrCreateDirectory(dirPath);

                    // Add to dir
                    dir.files[filename] = fileEntry;
                }
                else
                {
                    // Create dir
                    root.GetOrCreateDirectory(ze.FullName);
                }
            }

            // Return the whole tree
            return root;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the obejct.
        /// 
        /// If <paramref name="disposable"/> is not <see cref="IDisposable"/>, then it's not added.
        /// </summary>
        /// <param name="disposable">object to dispose</param>
        /// <returns></returns>
        public ZipFileProvider AddDisposable(object disposable)
        {
            if (disposable is IDisposable toDispose) ((IDisposeList)this).AddDisposable(toDispose);
            return this;
        }


        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the file provider after all streams are closed.
        /// </summary>
        /// <param name="disposable">object to dispose</param>
        /// <returns></returns>
        public ZipFileProvider AddBelatedDispose(object disposable)
        {
            if (disposable is IDisposable toDispose) belatedDisposeList.AddBelatedDispose(toDispose);
            return this;
        }
    }

    /// <summary>
    /// <see cref="IStreamProvider"/> that reads from a <see cref="ZipArchive"/>. 
    /// Is shared with help from a lock, because there is only one stream pointer.
    /// </summary>
    class ZipArchiveStreamProvider : StreamProvider
    {
        SemaphoreSlim m_lock = new SemaphoreSlim(1, 1);
        ZipArchive archive;
        CancellationTokenSource cancelSrc = new CancellationTokenSource();
        IBelatedDisposeList belateSource;

        public ZipArchiveStreamProvider(ZipArchive archive, IBelatedDisposeList belateSource)
        {
            this.archive = archive;
            this.belateSource = belateSource;
        }

        public override Stream OpenStream(string identifier)
        {
            ZipArchive _archive = archive;
            SemaphoreSlim _lock = m_lock;
            CancellationTokenSource _cancelSrc = cancelSrc;
            if (_archive == null || _lock == null || _cancelSrc == null) throw new ObjectDisposedException(GetType().FullName);
            ZipArchiveEntry entry = _archive.GetEntry(identifier);
            if (entry == null) return null;
            _lock.Wait(_cancelSrc.Token);
            IDisposable belate = null;
            Stream s = null;
            try
            {
                s = entry.Open();

                belate = belateSource?.Belate();

                StreamHandle sh = new StreamHandle(s, belate, () => { try { m_lock.Release(); } catch (Exception) { } });
                return sh;
            }
            catch (Exception) when (ReleaseSemaphore(_lock) || CloseDisposable(s) || CloseDisposable(belate))
            {
                // Never goes here
                return null;
            }
        }

        static bool ReleaseSemaphore(SemaphoreSlim s)
        {
            s.Release();
            return false;
        }

        static bool CloseDisposable(IDisposable disposable)
        {
            disposable?.Dispose();
            return false;
        }

        public override void Dispose(ref List<Exception> disposeErrors)
        {
            // Cancel token
            try
            {
                cancelSrc.Cancel();
            }
            catch (Exception e)
            {
                (disposeErrors ?? (disposeErrors = new List<Exception>())).Add(e);
            }

            // Dispose zipArchive
            try
            {
                // Dispose and null zip archive, only once.
                Interlocked.CompareExchange(ref archive, null, archive)?.Dispose();
            }
            catch (Exception e)
            {
                (disposeErrors ?? (disposeErrors = new List<Exception>())).Add(e);
            }

            // Dispose semaphore
            try
            {
                Interlocked.CompareExchange(ref m_lock, null, m_lock)?.Dispose();
            }
            catch (Exception e)
            {
                (disposeErrors ?? (disposeErrors = new List<Exception>())).Add(e);
            }

            belateSource = null;
        }
    }

    /// <summary>
    /// <see cref="IStreamProvider"/> that re-opens <see cref="ZipArchive"/>s.
    /// </summary>
    class ZipOpenerStreamProvider : StreamProvider
    {
        Func<ZipArchive> archiveOpener;
        IBelatedDisposeList belateSource;

        public ZipOpenerStreamProvider(Func<ZipArchive> archiveOpener, IBelatedDisposeList belateSource)
        {
            this.archiveOpener = archiveOpener ?? throw new ArgumentNullException(nameof(archiveOpener));
            this.belateSource = belateSource;
        }

        public override void Dispose(ref List<Exception> disposeErrors)
        {
            archiveOpener = null;
            belateSource = null;
        }

        public override Stream OpenStream(string entryName)
        {
            Func<ZipArchive> _archiveOpener = archiveOpener ?? throw new ObjectDisposedException(GetType().FullName);
            ZipArchive archive = _archiveOpener();
            Stream s = null;
            IDisposable belate = null;
            try
            {
                // Search entry
                ZipArchiveEntry entry = archive.GetEntry(entryName);

                // Not found
                if (entry == null) { archive.Dispose(); return null; }

                // Open stream
                s = entry.Open();

                // Delay parent's disposables
                belate = belateSource?.Belate();
                Action belateCancel = belate == null ? null : (Action)(() => belate.Dispose());

                // Attach the disposing of the archive to the stream.
                return new StreamHandle(s, archive, belateCancel);
            }
            catch (Exception) when (CloseDisposable(archive) || CloseDisposable(s) || CloseDisposable(belate))
            {
                // Never goes here
                return null;
            }
        }

        static bool CloseDisposable(IDisposable disposable)
        {
            disposable?.Dispose();
            return false;
        }
    }
}