// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           20.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileProvider.Root;
using Lexical.FileProvider.Common;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;

namespace Lexical.FileProvider
{
    /// <summary>
    /// Physical file provider that uses the whole filesystem of the computer. 
    /// 
    /// On windows, GetDirectoryContents(null) returns the drive letters "c:", "d:", etc.
    /// On linux, GetDirectoryContents(null) returns the root path "/". 
    /// 
    /// Unrooted queries, such as, GetDirectoryContents("") returns path that is relative to current working directory.
    /// 
    /// Supports:
    ///    C:\xxx                   Windows drive letters.
    ///    /xx                      Unix root path
    ///    \\server\share\xxx       Windows network drives
    ///    ../xxx                   Relative paths.
    /// 
    /// </summary>
    public class RootFileProvider : DisposeList, IFileProvider, IDisposable
    {
        internal protected static Regex Pattern = new Regex("(^(?<windows_driveletter>[a-zA-Z]\\:)((\\\\|\\/)(?<windows_path>.*))?$)|(^\\\\\\\\(?<share_server>[^\\\\]+)\\\\(?<share_name>[^\\\\]+)((\\\\|\\/)(?<share_path>.*))?$)|((?<unix_rooted_path>\\/.*)$)|(?<relativepath>^.*$)", RegexOptions.Compiled|RegexOptions.CultureInvariant|RegexOptions.ExplicitCapture);

        /// <summary>
        /// Constructor that initializes physical file providers.
        /// </summary>
        Func<string, IFileProvider> physicalProviderConstructor;

        /// <summary>
        /// Cached file providers.
        /// </summary>
        ConcurrentDictionary<string, DriveEntry> entries = new ConcurrentDictionary<string, DriveEntry>();

        /// <summary>
        /// Function for concurrent dictionary to create DriveEntry.
        /// </summary>
        Func<string, DriveEntry> entryConstructor;

        public RootFileProvider(Func<string, IFileProvider> physicalProviderConstructor = default)
        {
            this.physicalProviderConstructor = physicalProviderConstructor ?? (path => new PhysicalFileProvider(path));
            this.entryConstructor = createDriveEntry;
        }

        DriveEntry createDriveEntry(string path)
        {
            IFileProvider fileProvider = this.physicalProviderConstructor(path);
            DriveEntry driveEntry = new DriveEntry(path, fileProvider);
            AddDisposable(driveEntry);
            return driveEntry;
        }

        bool GetFileProviderAndPath(string path, out IFileProvider fileProvider, out string subpath)
        {
            // Assert not disposding
            if (IsDisposing) throw new ObjectDisposedException(GetType().FullName);

            // Match
            Match match = Pattern.Match(path);
            Group relativepath = match.Groups["relativepath"];

            // Fix relative path to rooted path
            if (relativepath.Success) {
                if (path == "") 
                    path = Directory.GetCurrentDirectory(); 
                else 
                    path = Path.GetFullPath(path); 
                match = Pattern.Match(path); 
            }

            // "C:\"
            Group windows_driveletter = match.Groups["windows_driveletter"], windows_path = match.Groups["windows_path"];
            if (windows_driveletter.Success)
            {
                DriveEntry driveEntry = entries.GetOrAdd(windows_driveletter.Value+"\\", entryConstructor);
                fileProvider = driveEntry.fileProvider;
                subpath = windows_path.Success ? windows_path.Value : "";
                return true;
            }

            // "/"
            Group unix_rooted_path = match.Groups["unix_rooted_path"];
            if (unix_rooted_path.Success)
            {
                DriveEntry driveEntry = entries.GetOrAdd("/", entryConstructor);
                fileProvider = driveEntry.fileProvider;
                subpath = path;
                return true;
            }

            // "\\server\share\path"
            Group share_server = match.Groups["share_server"], share_name = match.Groups["share_name"], share_path = match.Groups["share_path"];
            if (share_server.Success)
            {
                if (share_name.Success)
                {
                    string driveName = path.Substring(0, share_name.Index + share_name.Length);
                    DriveEntry driveEntry = entries.GetOrAdd(driveName, entryConstructor);
                    fileProvider = driveEntry.fileProvider;
                    subpath = share_path.Success ? share_path.Value : "";
                    return true;
                } else
                {
                    // "\\server"
                    string driveName = path.Substring(0, share_server.Index + share_server.Length);
                    DriveEntry driveEntry = entries.GetOrAdd(driveName, entryConstructor);
                    fileProvider = driveEntry.fileProvider;
                    subpath = "";
                    return true;
                }
            }

            fileProvider = null; subpath = null; return false;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            // Return all drives
            if (subpath == null) return new RootDirectoryContents();

            // Get fileprovider and new subpath
            IFileProvider fileProvider;
            if (GetFileProviderAndPath(subpath, out fileProvider, out subpath)) return fileProvider.GetDirectoryContents(subpath);

            // Failed to create drive entry
            return NotFoundDirectoryContents.Singleton;
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            // Return all drives
            if (subpath == null) return new RootDirectoryContents();

            // Get fileprovider and new subpath
            IFileProvider fileProvider;
            if (GetFileProviderAndPath(subpath, out fileProvider, out subpath)) return fileProvider.GetFileInfo(subpath);

            // Failed to create drive entry
            return new NotFoundFileInfo(subpath);
        }

        /// <summary>
        /// Get watcher.
        /// 
        /// If pattern is not rooted, it will watch from current directory.
        /// </summary>
        /// <param name="glob"></param>
        /// <returns></returns>
        public IChangeToken Watch(string glob)
        {
            // Assert arg
            if (glob == null) throw new ArgumentNullException(nameof(glob));

            // Return all drives
            if (glob == "") return NullChangeToken.Singleton;

            // Get fileprovider and new subpath
            IFileProvider fileProvider;
            if (GetFileProviderAndPath(glob, out fileProvider, out glob)) return fileProvider.Watch(glob);

            // Failed to create drive entry
            return NullChangeToken.Singleton;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the file provider.
        /// 
        /// If <paramref name="disposable"/> is not <see cref="IDisposable"/>, then it's not added.
        /// </summary>
        /// <param name="disposable">object to dispose</param>
        /// <returns></returns>
        public RootFileProvider AddDisposable(object disposable)
        {
            if (disposable is IDisposable toDispose && this is IDisposeList disposeList)
                disposeList.AddDisposable(toDispose);
            return this;
        }

    }
    
}
