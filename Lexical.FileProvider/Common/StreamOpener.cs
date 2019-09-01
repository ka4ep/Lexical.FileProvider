// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           2.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;

namespace Lexical.FileProvider.Common
{
    /// <summary>
    /// Stream provider that provides a stream for only one entry name.
    /// </summary>
    public class StreamOpener : Lexical.FileProvider.Common.StreamProvider
    {
        /// <summary>
        /// Function that opens stream
        /// </summary>
        Func<Stream> streamOpener;

        /// <summary>
        /// Entry name
        /// </summary>
        string entryName;

        /// <summary>
        /// Belate dispose source
        /// </summary>
        IBelatedDisposeList belateSource;

        public StreamOpener(Func<Stream> archiveOpener, string entryName, IBelatedDisposeList belateSource)
        {
            this.streamOpener = archiveOpener ?? throw new ArgumentNullException(nameof(archiveOpener));
            this.entryName = entryName ?? throw new ArgumentNullException(nameof(entryName));
            this.belateSource = belateSource;
        }

        public override void Dispose(ref List<Exception> disposeErrors)
        {
            streamOpener = null;
            belateSource = null;
        }

        public override Stream OpenStream(string identifier)
        {
            if (identifier != entryName) return null;

            // Opener
            Func<Stream> _archiveOpener = streamOpener ?? throw new ObjectDisposedException(GetType().FullName);

            // Open stream
            Stream stream = _archiveOpener();

            // Belate
            IDisposable belate = belateSource?.Belate();

            // Return
            return belate == null ? stream : new StreamHandle(stream, belate);
        }
    }
}
