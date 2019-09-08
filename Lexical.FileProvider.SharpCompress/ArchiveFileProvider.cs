// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           1.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Lexical.FileProvider.Package;
using Lexical.FileProvider.Common;
using SharpCompress.Archives;

namespace Lexical.FileProvider.SharpCompress.Internal
{
    /// <summary>
    /// Recycle archive file provider code from Lexical.FileProvider.Zip. Adapted to use <see cref="IArchive"/>.
    /// 
    /// <see href="https://github.com/adamhathcock/sharpcompress"/>
    /// </summary>
    public class ArchiveFileProvider : Lexical.FileProvider.Common.ArchiveFileProvider, IDisposableFileProvider
    {
        /// <summary>
        /// Create file provider that reads <paramref name="archive"/>.
        /// </summary>
        /// <param name="archive"></param>
        /// <param name="hintPath">(optional) clue to path of the package file</param>
        /// <param name="dateTime">(optional) Date time for folder entries</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        public ArchiveFileProvider(IArchive archive, string hintPath = null, DateTimeOffset? dateTime = null, bool convertBackslashesToSlashes = false) : base(hintPath, dateTime)
        {
            this.streamProvider = new ArchiveStreamProvider(archive ?? throw new ArgumentNullException(nameof(archive)), belatedDisposeList);
            AddArchiveEntries(this.root, archive.Entries, streamProvider, convertBackslashesToSlashes);
        }

        /// <summary>
        /// Create file provider that re-opens archive.
        /// </summary>
        /// <param name="archiveOpener"></param>
        /// <param name="hintPath">(optional) clue to path of the package file</param>
        /// <param name="dateTime">Date time for folder entries</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        /// <exception cref="IOException">On I/O error</exception>
        /// <exception cref="PackageException.LoadError">on file format error</exception>
        public ArchiveFileProvider(Func<IArchive> archiveOpener, string hintPath = null, DateTimeOffset? dateTime = default, bool convertBackslashesToSlashes = false) : base(hintPath, dateTime)
        {
            // Create stream provider
            this.streamProvider = new ArchiveOpenerStreamProvider(archiveOpener ?? throw new ArgumentNullException(nameof(archiveOpener)), belatedDisposeList);

            // Open once to read entries.
            using (var archive = archiveOpener())
                AddArchiveEntries(this.root, archive.Entries, streamProvider, convertBackslashesToSlashes);
        }

        /// <summary>
        /// Add <paramref name="archiveEntries"/> into tree structure.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="archiveEntries"></param>
        /// <param name="streamProvider">stream provider for files</param>
        /// <param name="convertBackslashesToSlashes">if true converts '\' to '/'</param>
        /// <returns>this</returns>
        protected virtual Lexical.FileProvider.Common.ArchiveDirectoryEntry AddArchiveEntries(Lexical.FileProvider.Common.ArchiveDirectoryEntry root, IEnumerable<IArchiveEntry> archiveEntries, Lexical.FileProvider.Common.IStreamProvider streamProvider, bool convertBackslashesToSlashes)
        {
            foreach (IArchiveEntry entry in archiveEntries)
            {
                string path = convertBackslashesToSlashes ? entry.Key.Replace('\\', '/') : entry.Key;

                // Is entry a file
                if (!entry.IsDirectory)
                {
                    // Split to filename and path
                    int slashIx = path.LastIndexOf('/');
                    string filename = path.Substring(slashIx + 1);
                    string dirPath = slashIx < 0 ? "" : path.Substring(0, slashIx);

                    // Create file entry
                    Lexical.FileProvider.Common.ArchiveFileEntry fileEntry = new Lexical.FileProvider.Common.ArchiveFileEntry(streamProvider, entry.Key, filename, entry.Size, entry.LastModifiedTime ?? DateTime.MinValue);

                    // Create dir
                    Lexical.FileProvider.Common.ArchiveDirectoryEntry dir = root.GetOrCreateDirectory(dirPath);

                    // Add to dir
                    dir.files[filename] = fileEntry;
                }
                else
                {                    
                    // Create dir
                    var dir = root.GetOrCreateDirectory(path);
                    if (entry.LastModifiedTime!=null) dir.LastModified = (DateTime)entry.LastModifiedTime;
                }
            }

            // Return the whole tree
            return root;
        }
    }

    class ArchiveStreamProvider : Lexical.FileProvider.Common.StreamProvider
    {
        SemaphoreSlim m_lock = new SemaphoreSlim(1, 1);
        IArchive archive;
        IBelatedDisposeList belateSource;
        CancellationTokenSource cancelSrc = new CancellationTokenSource();

        public ArchiveStreamProvider(IArchive archive, IBelatedDisposeList belateSource)
        {
            this.archive = archive;
            this.belateSource = belateSource;
        }

        public override Stream OpenStream(string identifier)
        {
            IArchive _archive = archive;
            SemaphoreSlim _lock = m_lock;
            CancellationTokenSource _cancelSrc = cancelSrc;
            if (_archive == null || _lock == null || _cancelSrc == null) throw new ObjectDisposedException(GetType().FullName);
            IArchiveEntry entry = _archive.Entries.Where(e => e.Key == identifier).FirstOrDefault();
            if (entry == null) return null;
            _lock.Wait(_cancelSrc.Token);
            Stream s = null;
            IDisposable belate = null;
            try
            {
                s = entry.OpenEntryStream();
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

            // Clean references
            belateSource = null;
        }

        static bool CloseDisposable(IDisposable disposable)
        {
            disposable?.Dispose();
            return false;
        }
    }

    /// <summary>
    /// <see cref="IStreamProvider"/> that re-opens <see cref="IArchive"/>s.
    /// </summary>
    class ArchiveOpenerStreamProvider : Lexical.FileProvider.Common.StreamProvider
    {
        Func<IArchive> archiveOpener;
        IBelatedDisposeList belateSource;

        public ArchiveOpenerStreamProvider(Func<IArchive> archiveOpener, IBelatedDisposeList belateSource)
        {
            this.archiveOpener = archiveOpener ?? throw new ArgumentNullException(nameof(archiveOpener));
            this.belateSource = belateSource;
        }

        public override void Dispose(ref List<Exception> disposeErrors)
        {
            archiveOpener = null;
            belateSource = null;
        }

        public override Stream OpenStream(string identifier)
        {
            Func<IArchive> _archiveOpener = archiveOpener ?? throw new ObjectDisposedException(GetType().FullName);
            IArchive archive = _archiveOpener();
            Stream s = null;
            IDisposable belate = null;
            try
            {
                // Search entry
                IArchiveEntry entry = archive.Entries.Where(e=>e.Key == identifier).FirstOrDefault();

                // Not found
                if (entry == null) { archive.Dispose(); return null; }

                // Open stream
                s = entry.OpenEntryStream();

                // Belate dispose of source
                belate = belateSource?.Belate();
                Action belateCancel = belate == null ? null : (Action) ( () => belate.Dispose() );

                // Attach the disposing of the archive to the stream.
                return new StreamHandle(s, archive, belateCancel);
            }
            catch (Exception) when (CloseDisposable(archive) || CloseDisposable(s) || CloseDisposable(belateSource))
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