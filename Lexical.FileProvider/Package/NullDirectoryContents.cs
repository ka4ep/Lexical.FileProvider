// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           19.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lexical.FileProvider.Package
{
    /// <summary>
    /// File and directory info for query null.
    /// Converts fileinfos into packages.
    /// </summary>
    public class NullDirectoryContents : IFileInfo, IDirectoryContents
    {
        /// <summary>
        /// Package provider that can open and lock packages
        /// </summary>
        readonly PackageFileProvider packageProvider;

        IFileInfo rootFileInfo;
        IDirectoryContents rootDir;

        IFileInfo RootFileInfo => rootFileInfo ?? (rootFileInfo = packageProvider.FileProvider.GetFileInfo(null));
        IDirectoryContents RootDir => rootDir ?? (rootDir = packageProvider.FileProvider.GetDirectoryContents(null));

        public NullDirectoryContents(PackageFileProvider packageProvider)
        {
            this.packageProvider = packageProvider ?? throw new ArgumentNullException(nameof(packageProvider));
        }

        public bool Exists => RootFileInfo.Exists;
        public long Length => RootFileInfo.Length;
        public string PhysicalPath => rootFileInfo.PhysicalPath;
        public string Name => RootFileInfo.Name;
        public DateTimeOffset LastModified => RootFileInfo.LastModified;
        public bool IsDirectory => RootFileInfo.IsDirectory;
        public Stream CreateReadStream() => RootFileInfo.CreateReadStream();
        IFileInfo[] GetFiles()
        {
            IDirectoryContents dir = packageProvider.FileProvider.GetDirectoryContents(null);
            IFileInfo[] files = dir.ToArray();

            // Convert package entries to package file infos
            for (int i = 0; i < files.Length; i++)
            {
                // Get file reference
                string filename = files[i].Name;
                // Match to package file extensions (e.g. *.zip)
                Match match = packageProvider.Pattern.Match(filename);
                // Don't replace
                if (!match.Success) continue;
                // Convert path to structured format
                PackageFileReference fileReference = new PackageFileReference(filename, match.Success, null, filename);
                // Create file entry
                PackageFileInfo newFileInfo = new PackageFileInfo(packageProvider, fileReference);
                newFileInfo.filename = filename;
                // Set new entry
                files[i] = newFileInfo;
            }
            return files;
        }
        public IEnumerator<IFileInfo> GetEnumerator()
            => ((IEnumerable<IFileInfo>)GetFiles()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable)GetFiles()).GetEnumerator();
    }
}
