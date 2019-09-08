// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           2.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using ICSharpCode.SharpZipLib.Lzw;
using Lexical.FileProvider.Common;
using System;
using System.IO;

namespace Lexical.FileProvider
{
    /// <summary>
    /// Reads .z file using SharpZipLib.
    /// 
    /// See <see href="https://github.com/icsharpcode/SharpZipLib"/>.
    /// </summary>
    public class LzwFileProvider : ArchiveFileProvider
    {
        /// <summary>
        /// Create Lzw file provider from .z file. 
        /// 
        /// Has one entry by name of <paramref name="entryName"/>. 
        /// Reads the whole stream once just to get the entry length.
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="entryName"></param>
        /// <param name="hintPath">(optional) clue of the file that is being opened</param>
        /// <param name="lastModified">Date time for folder entries</param>
        /// <exception cref="IOException"></exception>
        public LzwFileProvider(string filepath, string entryName, string hintPath = null, DateTimeOffset? lastModified = default) : base(hintPath, lastModified)
        {
            // Mutable long, and object to take closure reference to.
            long[] lengthContainer = new long[] { -1 };

            Func<Stream> opener = () =>
            {
                FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
                try
                {
                    LzwInputStream lzis = new LzwInputStream(fs);
                    lzis.IsStreamOwner = true;
                    return lengthContainer[0] < 0L ? lzis : (Stream) new LzwStreamFix(lzis, null, null, lengthContainer[0]);
                }
                catch (Exception) when (_closeStream(fs)) { throw new IOException($"Failed to read .Lzw from {filepath}"); }
            };

            // Calculate length by reading the whole thing.
            lengthContainer[0] = CalculateLength(opener);

            this.streamProvider = new StreamOpener(opener, entryName, belatedDisposeList);
            this.root.files[entryName] = new ArchiveFileEntry(this.streamProvider, entryName, entryName, lengthContainer[0], lastModified ?? DateTimeOffset.MinValue);
        }

        /// <summary>
        /// Create Lzw file provider from byte[].
        /// 
        /// Has one entry by name of <paramref name="entryName"/>. 
        /// Reads the whole stream once just to get the entry length.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="entryName"></param>
        /// <param name="hintPath">(optional) clue of the file that is being opened</param>
        /// <param name="lastModified">Date time for folder entries</param>
        /// <exception cref="IOException"></exception>
        public LzwFileProvider(byte[] data, string entryName, string hintPath = null, DateTimeOffset? lastModified = default) : base(hintPath, lastModified)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            // Mutable long, and object to take closure reference to.
            long[] lengthContainer = new long[] { -1 };

            Func<Stream> opener = () =>
            {
                MemoryStream ms = new MemoryStream(data);
                try
                {
                    LzwInputStream lzis = new LzwInputStream(ms);
                    lzis.IsStreamOwner = true;
                    return lengthContainer[0] < 0L ? lzis : (Stream)new LzwStreamFix(lzis, null, null, lengthContainer[0]);
                }
                catch (Exception) when (_closeStream(ms)) { throw new IOException($"Failed to read .Lzw from byte[]"); }
            };

            // Calculate length by reading the whole thing.
            lengthContainer[0] = CalculateLength(opener);

            this.streamProvider = new StreamOpener(opener, entryName, belatedDisposeList);
            this.root.files[entryName] = new ArchiveFileEntry(this.streamProvider, entryName, entryName, lengthContainer[0], lastModified ?? DateTimeOffset.MinValue);
        }

        static bool _closeStream(Stream s) { s?.Dispose(); return false; }

        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the obejct.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public LzwFileProvider AddDisposable(object disposable)
        {
            if (disposable is IDisposable toDispose) ((IDisposeList)this).AddDisposable(toDispose);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the file provider after all streams are closed.
        /// </summary>
        /// <param name="disposable">object to dispose</param>
        /// <returns></returns>
        public LzwFileProvider AddBelatedDispose(object disposable)
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
        static long CalculateLength(Func<Stream> opener)
        {
            long length = 0L;
            using (Stream s = opener())
            {
                byte[] buffer = new byte[0x10000];
                do
                {
                    int x = s.Read(buffer, 0, buffer.Length);
                    if (x <= 0) break;
                    length += x;
                } while (true);
            }
            return length;
        }

    }



    /// <summary>
    /// <see cref="LzwInputStream"/> reports wrong length.
    /// This class fixes the length value.
    /// </summary>
    public class LzwStreamFix : StreamHandle
    {
        readonly long newLength;
        /// <inheritdoc/>
        public override long Length => newLength;
        /// <inheritdoc/>
        public override bool CanSeek => false;
        /// <inheritdoc/>
        public override bool CanWrite => false;
        /// <inheritdoc/>
        public override bool CanTimeout => false;
        /// <summary>
        /// Create Length override.
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <param name="disposeHandle"></param>
        /// <param name="disposeAction"></param>
        /// <param name="newLength"></param>
        public LzwStreamFix(LzwInputStream sourceStream, IDisposable disposeHandle, Action disposeAction, long newLength) : base(sourceStream, disposeHandle, disposeAction)
        {
            this.newLength = newLength;
        }
    }

}
