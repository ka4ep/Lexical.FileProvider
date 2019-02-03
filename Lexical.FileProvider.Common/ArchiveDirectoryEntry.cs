// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           29.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lexical.FileProvider.Common
{
    /// <summary>
    /// Represents a directory in an archive file.
    /// </summary>
    public class ArchiveDirectoryEntry : IDirectoryContents, IFileInfo
    {
        /// <summary>
        /// No directories.
        /// </summary>
        public static readonly ArchiveDirectoryEntry[] NO_DIRS = new ArchiveDirectoryEntry[0];

        /// <summary>
        /// Directories
        /// </summary>
        public Dictionary<string, ArchiveDirectoryEntry> directories;

        /// <summary>
        /// List of files
        /// </summary>
        public Dictionary<string, ArchiveFileEntry> files;

        /// <summary>
        /// List of sub directories
        /// </summary>
        public IReadOnlyDictionary<string, ArchiveDirectoryEntry> Directories => directories;

        /// <summary>
        /// List of files
        /// </summary>
        public IReadOnlyDictionary<string, ArchiveFileEntry> Files => files;

        /// <summary>
        /// Cached combination of directories and files.
        /// </summary>
        IFileInfo[] entries;

        /// <summary>
        /// Combination of files and directories.
        /// </summary>
        public IFileInfo[] Entries => entries ?? (entries = Directories.Values.Concat<IFileInfo>(Files.Values).ToArray());

        /// <summary>
        /// Index to entries
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IFileInfo this[int index] => Entries[index];

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Create archive directory
        /// </summary>
        /// <param name="name">canonized directory path</param>
        /// <param name="date">last modified date</param>
        /// <param name="dirs">(optional) list of dirs</param>
        /// <param name="files">(optional) list of files</param>
        public ArchiveDirectoryEntry(string name, DateTimeOffset date, IEnumerable<ArchiveDirectoryEntry> dirs, IEnumerable<ArchiveFileEntry> files)
        {
            this.Name = name ?? throw new ArgumentNullException(name);
            this.directories = (dirs ?? NO_DIRS).ToDictionary(dir => dir.Name);
            this.files = (files ?? ArchiveFileEntry.NO_FILES).ToDictionary(file => file.Name);
            this.LastModified = date;
        }

        public ArchiveDirectoryEntry GetOrCreateDirectory(string subpath)
        {
            if (subpath == null) throw new ArgumentNullException(nameof(subpath));
            if (subpath == "") return this;

            // Split to local dir name and rest of the path
            int slashIx = subpath.IndexOf('/');
            string localDirectoryName = slashIx < 0 ? subpath : subpath.Substring(0, slashIx);
            string restOfThePath = slashIx < 0 ? null : subpath.Substring(slashIx + 1);

            // Get-or-create local directory
            ArchiveDirectoryEntry localDirectory = null;
            if (!directories.TryGetValue(localDirectoryName, out localDirectory))
            {
                localDirectory = new ArchiveDirectoryEntry(localDirectoryName, LastModified, null, null);
                directories[localDirectory.Name] = localDirectory;
            }

            // Return or recurse
            return restOfThePath == null ? localDirectory : localDirectory.GetOrCreateDirectory(restOfThePath);
        }

        /// <summary>
        /// Find directory
        /// </summary>
        /// <param name="subpath">canonized path</param>
        /// <returns>directory or null if was not found</returns>
        public ArchiveDirectoryEntry GetDirectory(string subpath)
        {
            if (subpath == null) throw new ArgumentNullException(nameof(subpath));
            if (subpath == "") return this;

            // Split to local dir name and rest of the path
            int slashIx = subpath.IndexOf('/');
            string localDirectoryName = slashIx < 0 ? subpath : subpath.Substring(0, slashIx);
            string restOfThePath = slashIx < 0 ? null : subpath.Substring(slashIx + 1);

            // Get local directory
            ArchiveDirectoryEntry dir = null;
            if (!directories.TryGetValue(localDirectoryName, out dir)) return null;

            // Return or recurse
            return restOfThePath == null ? dir : dir.GetDirectory(restOfThePath);
        }

        /// <summary>
        /// Find file
        /// </summary>
        /// <param name="canonizedPath">file path</param>
        /// <returns>file or null</returns>
        public ArchiveFileEntry GetFile(string canonizedPath)
        {
            if (canonizedPath == null) throw new ArgumentNullException(nameof(canonizedPath));
            if (canonizedPath == "") return null;

            // Find slash
            int slashIX = canonizedPath.IndexOf('/');

            // No slash, return direct file
            if (slashIX < 0)
            {
                ArchiveFileEntry result = null;
                files.TryGetValue(canonizedPath, out result);
                return result;
            }

            // Got slash, find local dir
            string localDirectoryName = canonizedPath.Substring(0, slashIX);
            ArchiveDirectoryEntry localDirectory = null;
            if (!directories.TryGetValue(localDirectoryName, out localDirectory)) return null;

            // Use recursion for the rest the path
            string restOfThePath = canonizedPath.Substring(slashIX + 1);
            return localDirectory.GetFile(restOfThePath);
        }

        public bool Exists => true;
        public long Length => 0L;
        public string PhysicalPath => null;
        public DateTimeOffset LastModified { get; set; }
        public bool IsDirectory => true;
        public Stream CreateReadStream() => throw new NotSupportedException();
        IEnumerator IEnumerable.GetEnumerator() => Entries.GetEnumerator();
        public IEnumerator<IFileInfo> GetEnumerator() => ((IEnumerable<IFileInfo>)Entries).GetEnumerator();
        public override string ToString() => Name;        
    }
}
