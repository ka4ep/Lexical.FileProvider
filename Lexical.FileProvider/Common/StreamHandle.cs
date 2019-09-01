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
    public class StreamHandle : Stream
    {
        public readonly Stream sourceStream;
        IDisposable disposeHandle;
        Action disposeAction;
        public StreamHandle(Stream sourceStream, IDisposable disposeHandle, Action disposeAction = null)
        {
            this.sourceStream = sourceStream ?? throw new ArgumentNullException(nameof(sourceStream));
            this.disposeHandle = disposeHandle;
            this.disposeAction = disposeAction;
        }

        public override bool CanRead => sourceStream.CanRead;
        public override bool CanSeek => sourceStream.CanSeek;
        public override bool CanWrite => sourceStream.CanWrite;
        public override long Length => sourceStream.Length;
        public override long Position { get => sourceStream.Position; set => sourceStream.Position = value; }
        public override void Flush() => sourceStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => sourceStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => sourceStream.Seek(offset, origin);
        public override void SetLength(long value) => sourceStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => sourceStream.Write(buffer, offset, count);
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => sourceStream.BeginRead(buffer, offset, count, callback, state);
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => sourceStream.BeginWrite(buffer, offset, count, callback, state);
        public override bool CanTimeout => sourceStream.CanTimeout;
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => sourceStream.CopyToAsync(destination, bufferSize, cancellationToken);
        public override int EndRead(IAsyncResult asyncResult) => sourceStream.EndRead(asyncResult);
        public override void EndWrite(IAsyncResult asyncResult) => sourceStream.EndWrite(asyncResult);
        public override Task FlushAsync(CancellationToken cancellationToken) => sourceStream.FlushAsync(cancellationToken);
        public override object InitializeLifetimeService() => sourceStream.InitializeLifetimeService();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => sourceStream.ReadAsync(buffer, offset, count, cancellationToken);
        public override int ReadByte() => sourceStream.ReadByte();
        public override int ReadTimeout { get => sourceStream.ReadTimeout; set => sourceStream.ReadTimeout = value; }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => sourceStream.WriteAsync(buffer, offset, count, cancellationToken);
        public override void WriteByte(byte value) => sourceStream.WriteByte(value);
        public override int WriteTimeout { get => sourceStream.WriteTimeout; set => sourceStream.WriteTimeout = value; }

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

        protected override void Dispose(bool disposing)
        {
        }

        public override bool Equals(object obj) => sourceStream.Equals(obj);
        public override int GetHashCode() => sourceStream.GetHashCode();
        public override string ToString() => $"{GetType().Name}({sourceStream})";
    }
}
