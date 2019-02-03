// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           29.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lexical.FileProvider.Package;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Lexical.FileProvider.Common
{
    /// <summary>
    /// Base implementation for archive file provider. 
    /// Is initialized at subtype constructor with read-only tree representation of directories and files.
    /// Files are opened through <see cref="IStreamProvider"/> interface, which is assigned to <see cref="streamProvider"/> field.
    /// </summary>
    public class ArchiveFileProvider : DisposeList, IDisposableFileProvider, IBelatedDisposeFileProvider, IBelatedDisposeList
    {
        /// <summary>
        /// Dispose list for belated disposables.
        /// </summary>
        protected IBelatedDisposeList belatedDisposeList = new BelatedDisposeList();

        /// <summary>
        /// Root directory info.
        /// </summary>
        protected ArchiveDirectoryEntry root;

        /// <summary>
        /// Object that opens streams for <see cref="IFileInfo"/>s.
        /// </summary>
        protected StreamProvider streamProvider;

        /// <summary>
        /// Subpath of the archive.
        /// 
        /// Not used for anything, but gives better debug messages.
        /// </summary>
        public readonly string HintPath;

        /// <summary>
        /// Create archive content file provider.
        /// </summary>
        /// <param name="hintPath">(optional) clue of the file that is being opened</param>
        /// <param name="lastModified">Date time for folder entries</param>
        public ArchiveFileProvider(string hintPath, DateTimeOffset? lastModified = default)
        {
            this.root = new ArchiveDirectoryEntry("", lastModified ?? DateTimeOffset.MinValue, null, null);
            this.HintPath = hintPath;
        }

        /// <summary>
        /// Search file from the read-only archive index.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public virtual IFileInfo GetFileInfo(string path)
        {
            if (IsDisposing) throw new ObjectDisposedException(GetType().FullName);
            if (path == null) path = "";
            string canonizedPath = CanonizePath(path);
            ArchiveFileEntry zipFile = root.GetFile(canonizedPath);
            return (IFileInfo)zipFile ?? new NotFoundFileInfo(path);
        }

        /// <summary>
        /// Search directory from the read-only directory index.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public virtual IDirectoryContents GetDirectoryContents(string path)
        {
            if (IsDisposing) throw new ObjectDisposedException(GetType().FullName);
            if (path == null) path = "";
            string canonizedPath = CanonizePath(path);
            ArchiveDirectoryEntry dir = root.GetDirectory(canonizedPath);
            return (IDirectoryContents)dir ?? NotFoundDirectoryContents.Singleton;
        }

        /// <summary>
        /// Removes trailing and preceding slashes.
        /// E.g. "/folder/subfolder/" -> "folder/subfolder"
        /// </summary>
        /// <param name="path"></param>
        /// <returns>path</returns>
        protected virtual string CanonizePath(string path)
        {
            // Follow preceding slashes
            int startIx = 0;
            while (startIx < path.Length && path[startIx] == '/') startIx++;

            // Follow trailing slashes
            int endIx = path.Length - 1;
            while (endIx >= 0 && path[endIx] == '/') endIx--;

            // Return
            return startIx == 0 && endIx == path.Length - 1 ? path : startIx < endIx ? path.Substring(startIx, endIx - startIx + 1) : String.Empty;
        }

        public virtual IChangeToken Watch(string filter)
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);
            if (filter == null) return NullChangeToken.Singleton;

            // TODO: Add here PhysicalFilesWatcher if requesting the root
            //       .. but that would require an extra nuget dependency ..
            //if (filename != null && CanonizePath(filter) == "") 

            return NullChangeToken.Singleton;
        }

        /// <summary>
        /// Forwards dispose to <see cref="streamProvider"/>. Marks this class disposed.
        /// </summary>
        /// <param name="disposeErrors"></param>
        protected override void innerDispose(ref List<Exception> disposeErrors)
        { 
            try
            {
                Interlocked.CompareExchange(ref streamProvider, null, streamProvider)?.Dispose(ref disposeErrors);
            }
            catch (Exception e)
            {
                (disposeErrors ?? (disposeErrors = new List<Exception>())).Add(e);
            }

            // Belated disposes
            try
            {
                belatedDisposeList.Dispose();
            }
            catch (Exception e)
            {
                (disposeErrors ?? (disposeErrors = new List<Exception>())).Add(e);
            }

        }

        /// <summary>
        /// Prints class name. Adds filename if it's known.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => HintPath != null ? $"{GetType().FullName}({HintPath})" : GetType().FullName;

        bool IBelatedDisposeFileProvider.AddBelatedDispose(IDisposable disposable)
            => belatedDisposeList.AddBelatedDispose(disposable);
        bool IBelatedDisposeFileProvider.AddBelatedDisposes(IEnumerable<IDisposable> disposables)
            => belatedDisposeList.AddBelatedDisposes(disposables);
        bool IBelatedDisposeFileProvider.RemoveBelatedDispose(IDisposable disposable)
            => belatedDisposeList.RemoveBelatedDispose(disposable);
        bool IBelatedDisposeFileProvider.RemoveBelatedDisposes(IEnumerable<IDisposable> disposables)
            => belatedDisposeList.RemovedBelatedDisposes(disposables);

        IDisposable IBelatedDisposeList.Belate()
            => belatedDisposeList.Belate();
        bool IBelatedDisposeList.AddBelatedDispose(IDisposable disposable)
            => belatedDisposeList.AddBelatedDispose(disposable);
        bool IBelatedDisposeList.AddBelatedDisposes(IEnumerable<IDisposable> disposables)
            => belatedDisposeList.AddBelatedDisposes(disposables);
        bool IBelatedDisposeList.RemoveBelatedDispose(IDisposable disposable)
            => belatedDisposeList.RemoveBelatedDispose(disposable);
        bool IBelatedDisposeList.RemovedBelatedDisposes(IEnumerable<IDisposable> disposables)
            => belatedDisposeList.RemovedBelatedDisposes(disposables);
    }

    /// <summary>
    /// Disposable version of <see cref="IStreamProvider"/>.
    /// </summary>
    public abstract class StreamProvider : IStreamProvider
    {
        public abstract Stream OpenStream(string identifier);
        public abstract void Dispose(ref List<Exception> disposeErrors);
    }
}