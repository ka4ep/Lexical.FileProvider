// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           21.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;

namespace Lexical.FileProvider.Root
{
    /// <summary>
    /// Represents a file on a physical filesystem
    /// </summary>
    class DriveFileInfo : IFileInfo
    {
        DirectoryInfo info;
        public bool Exists => info.Exists;
        public long Length => 0L;
        public string PhysicalPath => info.FullName;
        public string Name { get; internal set; }
        public DateTimeOffset LastModified => DateTimeOffset.MinValue;
        public bool IsDirectory => true;
        public DriveFileInfo(DirectoryInfo info, string name)
        {
            this.info = info;
            this.Name = name ?? info.Name;
        }
        public Stream CreateReadStream()
            => new MemoryStream();
    }
}
