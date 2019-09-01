// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           20.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lexical.FileProvider.Root
{
    class RootDirectoryContents : IDirectoryContents, IFileInfo
    {
        static string separator = Path.DirectorySeparatorChar + "";
        public bool Exists => true;
        public long Length => 0L;
        public string PhysicalPath => null;
        public string Name => "";
        public DateTimeOffset LastModified => DateTimeOffset.MinValue;
        public bool IsDirectory => true;

        IFileInfo[] drives;
        public RootDirectoryContents()
        {
            IEnumerable<DriveInfo> driveInfos = DriveInfo.GetDrives().Where(di => di.IsReady);

            Match[] matches = driveInfos.Select(di => RootFileProvider.Pattern.Match(di.Name)).ToArray();
            int windows = matches.Where(m => m.Groups["windows_driveletter"].Success).Count();
            int unix = matches.Where(m => m.Groups["unix_rooted_path"].Success).Count();

            List<IFileInfo> list = new List<IFileInfo>(matches.Length);

            // Reduce all "/mnt/xx" into one "/" root.
            if (unix > 0) list.Add(new DriveFileInfo(new DirectoryInfo("/"), "/"));

            foreach (Match m in matches)
            {
                // Reduce all "/mnt/xx" into one "/" root.
                if (m.Groups["unix_rooted_path"].Success) continue;

                string path = m.Value;
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                if (path.EndsWith(separator)) path = path.Substring(0, path.Length - 1);
                DriveFileInfo driveInfo = new DriveFileInfo(directoryInfo, path);
                list.Add(driveInfo);
            }

            drives = list.ToArray();
        }

        public IEnumerator<IFileInfo> GetEnumerator()
            => ((IEnumerable<IFileInfo>)drives).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
            => drives.GetEnumerator();
        public Stream CreateReadStream()
            => new MemoryStream();
    }

}
