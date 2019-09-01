// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           20.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;

namespace Lexical.FileProvider.Common
{
    // <interfaces>
    /// <summary>
    /// Temporary file provider.
    /// </summary>
    public interface ITempFileProvider : IDisposable
    {
        /// <summary>
        /// Create a new unique 0-bytes temp file that is not locked.
        /// </summary>
        /// <exception cref="IOException">if file creation failed</exception>
        /// <exception cref="ObjectDisposedException">if provider is disposed</exception>
        /// <returns>handle with a filename. Caller must dispose after use, which will delete the file if it still exists.</returns>
        ITempFileHandle CreateTempFile();
    }

    /// <summary>
    /// A handle to a temp file name. 
    /// 
    /// Dispose the handle to delete it.
    /// 
    /// If temp file is locked, Dispose() throws an <see cref="IOException"/>.
    /// 
    /// Failed deletion will still be marked as to-be-deleted.
    /// There is another delete attempt when the parent <see cref="ITempFileProvider"/> is disposed.
    /// </summary>
    public interface ITempFileHandle : IDisposable
    {
        /// <summary>
        /// Filename to 0 bytes temp file.
        /// </summary>
        String Filename { get; }
    }
    // </interfaces>

    public static class TempFileProviderExtensions
    {
        /// <summary>
        /// Alternative Dispose() that suppresses <see cref="IOException"/>.
        /// </summary>
        /// <param name="handle"></param>
        public static void DisposeSuppressIOException(this ITempFileHandle handle)
        {
            try
            {
                handle.Dispose();
            } catch (IOException)
            {
            }
        }
    }
}
