// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           29.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;

namespace Lexical.FileProvider.Common
{
    /// <summary>
    /// Represents a file entry in an archive.
    /// </summary>
    public class ArchiveFileEntry : IFileInfo
    {
        /// <summary>
        /// No files.
        /// </summary>
        public static readonly ArchiveFileEntry[] NO_FILES = new ArchiveFileEntry[0];

        /// <summary>
        /// Reference to object that opens streams.
        /// </summary>
        IStreamProvider streamProvider;

        /// <inheritdoc/>
        public bool Exists => true;
        /// <inheritdoc/>
        public long Length { get; protected set; }
        /// <inheritdoc/>
        public string Name { get; protected set; }
        /// <inheritdoc/>
        public string PhysicalPath => null;
        /// <inheritdoc/>
        public bool IsDirectory => false;
        /// <inheritdoc/>
        public DateTimeOffset LastModified { get; protected set; }

        /// <summary>
        /// Entry name as it is in the archive file.
        /// The identifier that is given to the <see cref="streamProvider"/>.
        /// </summary>
        public readonly string Identifier;

        /// <summary>
        /// Create new file entry.
        /// </summary>
        /// <param name="streamProvider">object that opens streams</param>
        /// <param name="identifier">identifier that is given to <paramref name="streamProvider"/></param>
        /// <param name="name">name of the file within its parent folder context</param>
        /// <param name="length">length of file</param>
        /// <param name="lastModified">last accessed date, utc</param>
        public ArchiveFileEntry(IStreamProvider streamProvider, string identifier, string name, long length, DateTimeOffset lastModified)
        {
            this.streamProvider = streamProvider ?? throw new ArgumentNullException(nameof(streamProvider));
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Identifier = identifier;
            this.Length = length;
            this.LastModified = lastModified;
        }

        /// <summary>
        /// Open stream. The caller must dispose the stream
        /// </summary>
        /// <returns>stream</returns>
        /// <exception cref="FileNotFoundException"></exception>
        public Stream CreateReadStream()
            => streamProvider.OpenStream(Identifier) ?? throw new FileNotFoundException(Identifier);

        /// <summary>
        /// Print info.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => Name;
    }
}
