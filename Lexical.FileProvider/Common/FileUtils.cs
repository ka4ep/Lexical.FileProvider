// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           21.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;

namespace Lexical.FileProvider.Common
{
    public class FileUtils
    {
        /// <summary>
        /// Read stream fully into byte[].
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        public static byte[] ReadFully(Stream s)
        {
            if (s == null) return null;

            // Get length
            long length;
            try
            {
                length = s.Length;
            } catch (NotSupportedException)
            {
                // Cannot get length
                MemoryStream ms = new MemoryStream();
                s.CopyTo(ms);
                return ms.ToArray();
            }

            if (length > int.MaxValue) throw new IOException("File size over 2GB");

            int _len = (int)length;
            byte[] data = new byte[_len];

            // Read chunks
            int ix = 0;
            while (ix < _len)
            {
                int count = s.Read(data, ix, _len - ix);

                // "returns zero (0) if the end of the stream has been reached."
                if (count == 0) break;

                ix += count;
            }
            if (ix == _len) return data;
            throw new IOException("Failed to read stream fully");
        }

        /// <summary>
        /// Copy <paramref name="fileInfo"/> to a <paramref name="filename"/>. <paramref name="filename"/> must be exist and be size 0. 
        /// 
        /// Returns an open stream to the file at position 0 with contents copied from <paramref name="fileInfo"/>.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="filename"></param>
        /// <returns>open file stream</returns>
        /// <exception cref="IOException">if something failed</exception>
        public static FileStream CopyToFile(IFileInfo fileInfo, string filename)
        {
            using (Stream s = fileInfo.CreateReadStream())
            {
                bool OK = false;
                FileStream fs = null;
                try
                {
                    // Open file, it must exist.
                    fs = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite);

                    // Copy data
                    fs.Position = 0L;
                    s.CopyTo(fs);
                    // Rewind
                    fs.Position = 0L;

                    OK = true;
                    return fs;
                }
                finally
                {
                    // Something failed. Cleanup
                    if (!OK && fs != null) { fs.Close(); fs.Dispose(); }
                }
            }
        }


        /// <summary>
        /// Read fileinfo into memory snapshot
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns>bytes</returns>
        /// <exception cref="IOException"></exception>
        public static byte[] ReadMemorySnapshot(IFileInfo fileInfo)
        {
            using (Stream s = fileInfo.CreateReadStream())
            {
                if (s is MemoryStream ms_ && s.Position == 0L) return ms_.ToArray();

                try
                {
                    long len = fileInfo.Length;
                    if (len > Int32.MaxValue) throw new IOException($"File is over 2GB and cannot be loaded into memory snapshot.");

                    int _len = (int)len;
                    byte[] data = new byte[_len];

                    // Read chunks
                    int ix = 0;
                    while (ix < _len)
                    {
                        int count = s.Read(data, ix, _len - ix);

                        // "returns zero (0) if the end of the stream has been reached."
                        if (count == 0) break;

                        ix += count;
                    }
                    if (ix == _len) return data;
                    throw new IOException("Failed to read stream fully");
                }
                catch (NotSupportedException)
                {
                    MemoryStream ms = new MemoryStream();
                    s.CopyTo(ms);
                    ms.Position = 0L;
                    return ms.ToArray();
                }
            }
        }


    }
}
