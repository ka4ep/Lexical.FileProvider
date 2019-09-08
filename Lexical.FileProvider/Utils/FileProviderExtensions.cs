// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           18.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;

namespace Lexical.FileProvider.Utils
{
    /// <summary>
    /// <see cref="IFileProvider"/> extension methods.
    /// </summary>
    public static class FileProviderExtensions
    {
        /// <summary>
        /// List recursively all directories and files. (Root is excluded)
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="startPath"></param>
        /// <returns>all files and directores, except root</returns>
        public static IEnumerable<IFileInfo> ListAll(this IFileProvider fileProvider, string startPath = "")
        {
            Queue<string> dirs = new Queue<string>();
            dirs.Enqueue(startPath);
            while (dirs.Count>0)
            {
                string path = dirs.Dequeue();
                IDirectoryContents contents = fileProvider.GetDirectoryContents(path);
                //if (!contents.Exists) continue;
                foreach(IFileInfo fileInfo in contents)
                {
                    string _path = string.IsNullOrEmpty(path) ? fileInfo.Name : (path.EndsWith("/", StringComparison.InvariantCulture) ? path + fileInfo.Name : path + "/" + fileInfo.Name);
                    if (fileInfo.IsDirectory) dirs.Enqueue( _path );
                    yield return fileInfo;
                }
            }
        }

        /// <summary>
        /// List recursively all directories and files. (Root is excluded)
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="startPath"></param>
        /// <returns>all paths</returns>
        public static IEnumerable<string> ListAllPaths(this IFileProvider fileProvider, string startPath = "")
        {
            Queue<string> dirs = new Queue<string>();
            dirs.Enqueue(startPath);
            while (dirs.Count > 0)
            {
                string path = dirs.Dequeue();
                IDirectoryContents contents = fileProvider.GetDirectoryContents(path);
                //if (!contents.Exists) continue;
                foreach (IFileInfo fileInfo in contents)
                {
                    string _path = string.IsNullOrEmpty(path) ? fileInfo.Name : (path.EndsWith("/", StringComparison.InvariantCulture) ? path + fileInfo.Name : path + "/" + fileInfo.Name);
                    if (fileInfo.IsDirectory) dirs.Enqueue(_path);
                    yield return _path;
                }
            }
        }

        /// <summary>
        /// List recursively all directories and files with path. (Root is excluded)
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="startPath">start path</param>
        /// <returns>Tuples with fileinfo and path</returns>
        public static IEnumerable<(IFileInfo, string)> ListAllFileInfoAndPath(this IFileProvider fileProvider, string startPath = "")
        {
            Queue<string> dirs = new Queue<string>();
            dirs.Enqueue(startPath);
            while (dirs.Count > 0)
            {
                string path = dirs.Dequeue();
                IDirectoryContents contents = fileProvider.GetDirectoryContents(path);
                //if (!contents.Exists) continue;
                foreach (IFileInfo fileInfo in contents)
                {
                    string _path = string.IsNullOrEmpty(path) ? fileInfo.Name : (path.EndsWith("/", StringComparison.InvariantCulture) ? path + fileInfo.Name : path + "/" + fileInfo.Name);
                    if (fileInfo.IsDirectory) dirs.Enqueue(_path);
                    yield return (fileInfo, _path);
                }
            }
        }
    }
}
