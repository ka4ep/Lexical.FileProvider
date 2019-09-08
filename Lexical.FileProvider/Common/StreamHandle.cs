// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           27.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lexical.FileProvider.Common
{
    /// <summary>
    /// Disposable handle to a <see cref="Stream"/>.
    /// </summary>
    public class StreamHandle : Stream
    {
        /// <summary>
        /// Original stream.
        /// </summary>
        public readonly Stream sourceStream;
        IDisposable disposeHandle;
        Action disposeAction;

        /// <summary>
        /// Create dispose handle.
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <param name="disposeHandle"></param>
        /// <param name="disposeAction"></param>
        public StreamHandle(Stream sourceStream, IDisposable disposeHandle, Action disposeAction = null)
        {
            this.sourceStream = sourceStream ?? throw new ArgumentNullException(nameof(sourceStream));
            this.disposeHandle = disposeHandle;
            this.disposeAction = disposeAction;
        }

        /// <inheritdoc/>
        public override bool CanRead => sourceStream.CanRead;
        /// <inheritdoc/>
        public override bool CanSeek => sourceStream.CanSeek;
        /// <inheritdoc/>
        public override bool CanWrite => sourceStream.CanWrite;
        /// <inheritdoc/>
        public override long Length => sourceStream.Length;
        /// <inheritdoc/>
        public override long Position { get => sourceStream.Position; set => sourceStream.Position = value; }
        /// <inheritdoc/>
        public override void Flush() => sourceStream.Flush();
        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) => sourceStream.Read(buffer, offset, count);
        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => sourceStream.Seek(offset, origin);
        /// <inheritdoc/>
        public override void SetLength(long value) => sourceStream.SetLength(value);
        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => sourceStream.Write(buffer, offset, count);
        /// <inheritdoc/>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => sourceStream.BeginRead(buffer, offset, count, callback, state);
        /// <inheritdoc/>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => sourceStream.BeginWrite(buffer, offset, count, callback, state);
        /// <inheritdoc/>
        public override bool CanTimeout => sourceStream.CanTimeout;
        /// <inheritdoc/>
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => sourceStream.CopyToAsync(destination, bufferSize, cancellationToken);
        /// <inheritdoc/>
        public override int EndRead(IAsyncResult asyncResult) => sourceStream.EndRead(asyncResult);
        /// <inheritdoc/>
        public override void EndWrite(IAsyncResult asyncResult) => sourceStream.EndWrite(asyncResult);
        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken) => sourceStream.FlushAsync(cancellationToken);
        /// <inheritdoc/>
        public override object InitializeLifetimeService() => sourceStream.InitializeLifetimeService();
        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => sourceStream.ReadAsync(buffer, offset, count, cancellationToken);
        /// <inheritdoc/>
        public override int ReadByte() => sourceStream.ReadByte();
        /// <inheritdoc/>
        public override int ReadTimeout { get => sourceStream.ReadTimeout; set => sourceStream.ReadTimeout = value; }
        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => sourceStream.WriteAsync(buffer, offset, count, cancellationToken);
        /// <inheritdoc/>
        public override void WriteByte(byte value) => sourceStream.WriteByte(value);
        /// <inheritdoc/>
        public override int WriteTimeout { get => sourceStream.WriteTimeout; set => sourceStream.WriteTimeout = value; }

        /// <inheritdoc/>
        public override void Close()
        {
            IDisposable _disposeHandle = Interlocked.CompareExchange(ref disposeHandle, null, disposeHandle);
            Action _disposeAction = Interlocked.CompareExchange(ref disposeAction, null, disposeAction);
            List<Exception> errors = null;

            try
            {
                sourceStream.Close();
            }
            catch (Exception e)
            {
                (errors ?? (errors = new List<Exception>())).Add(e);
            }

            try
            {
                base.Close();
            }
            catch (Exception e)
            {
                (errors ?? (errors = new List<Exception>())).Add(e);
            }

            if (_disposeAction != null)
            {
                try
                {
                    _disposeAction();
                }
                catch (Exception e)
                {
                    (errors ?? (errors = new List<Exception>())).Add(e);
                }
            }

            if (_disposeHandle != null)
            {
                try
                {
                    _disposeHandle.Dispose();
                }
                catch (Exception e)
                {
                    (errors ?? (errors = new List<Exception>())).Add(e);
                }
            }

            if (errors != null) throw new AggregateException(errors);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => sourceStream.Equals(obj);
        /// <inheritdoc/>
        public override int GetHashCode() => sourceStream.GetHashCode();
        /// <inheritdoc/>
        public override string ToString() => $"{GetType().Name}({sourceStream})";
    }
}
