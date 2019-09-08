// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           29.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System.IO;

namespace Lexical.FileProvider.Common
{
    /// <summary>
    /// Opens a <see cref="Stream"/> for a file entry.
    /// </summary>
    public interface IStreamProvider
    {
        /// <summary>
        /// Try to open a stream to a file.
        /// The caller take ownership of the stream and must close it.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns>stream or null.</returns>
        Stream OpenStream(string identifier);
    }
}
