// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           22.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileProvider.Package;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Threading;

namespace Lexical.FileProvider.Common
{
    /// <summary>
    /// Handle and proxy of <see cref="IFileProvider"/>.
    /// </summary>
    public class FileProviderHandle : IDisposableFileProvider
    {
        /// <summary>
        /// (optional) Action to be executed when proxy is disposed.
        /// </summary>
        Action<object> disposeAction;

        /// <summary>
        /// Dispose target
        /// </summary>
        public object Target => disposeAction?.Target;

        /// <summary>
        /// Object to be used as <see cref="disposeAction"/>'s argument.
        /// </summary>
        object state;

        /// <summary>
        /// Source file provider
        /// </summary>
        public IFileProvider fileProvider;

        /// <summary>
        /// 0 - is not disposed
        /// 1 - is disposed
        /// </summary>
        long disposing = 0L;

        /// <summary>
        /// Tests whether object is disposed
        /// </summary>
        public bool IsDisposed => Interlocked.Read(ref disposing) != 0L;

        /// <summary>
        /// Create new handle to file provider.
        /// </summary>
        /// <param name="parent">(optional) parent that is notified of dispose</param>
        /// <param name="state">(optional) action's argument</param>
        /// <param name="fileProvider"></param>
        public FileProviderHandle(Action<object> disposeAction, object state, IFileProvider fileProvider)
        {
            this.disposeAction = disposeAction;
            this.state = state;
            this.fileProvider = fileProvider; //?? throw new ArgumentNullException(nameof(fileProvider));
        }

        public void Dispose()
        {
            // Only one thread can dispose, and only once
            if (Interlocked.CompareExchange(ref disposing, 1L, 0L) != 0L) return;

            // Release references
            var _disposeAction = disposeAction;
            var _state = state;
            fileProvider = null;
            disposeAction = null;
            state = null;

            // Run action
            if (_disposeAction != null) _disposeAction(_state);
        }

        /// <summary>
        /// Forwards calls to 
        /// </summary>
        /// <param name="subpath"></param>
        /// <returns></returns>
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            // Get reference
            var _fileProvider = fileProvider;

            // Check disposed
            if (IsDisposed || _fileProvider == null) throw new ObjectDisposedException(GetType().FullName);

            // Forward call to source file provider
            IDirectoryContents dir = _fileProvider.GetDirectoryContents(subpath);

            // 
            return dir;
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            // Get reference
            var _fileProvider = fileProvider;

            // Check disposed
            if (IsDisposed || _fileProvider == null) throw new ObjectDisposedException(GetType().FullName);

            // Forward call to source
            return _fileProvider.GetFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            // Get reference
            var _fileProvider = fileProvider;

            // Check disposed
            if (IsDisposed || _fileProvider == null) throw new ObjectDisposedException(GetType().FullName);

            // Forward call to source
            return _fileProvider.Watch(filter);
        }
    }
}
