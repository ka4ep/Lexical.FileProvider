// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           19.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileProvider.Common;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;

namespace Lexical.FileProvider.Package
{
    /// <summary>
    /// A reference to a file in a package. 
    /// 
    /// This class can be used for loading files even if they have been evited from parent's cache.
    /// </summary>
    public class PackageFileInfo : IFileInfo
    {
        /// <summary>
        /// Package provider that can open and lock packages
        /// </summary>
        readonly IPackageProvider packageProvider;

        /// <summary>
        /// File reference.
        /// </summary>
        readonly PackageFileReference fileReference;

        /// <summary>
        /// Create package info
        /// </summary>
        /// <param name="packageProvider"></param>
        /// <param name="fileReference"></param>
        public PackageFileInfo(IPackageProvider packageProvider, PackageFileReference fileReference)
        {
            this.packageProvider = packageProvider ?? throw new ArgumentNullException(nameof(packageProvider));
            this.fileReference = fileReference ?? throw new ArgumentNullException(nameof(fileReference));
        }

        IFileInfo GetFileInfo()
        {
            for (var _ref = fileReference; _ref != null; _ref = _ref.Parent)
            {
                // Open package file
                using (var fp = packageProvider.TryOpenPackage(_ref.Parent))
                {
                    // Wasn't package, try parent
                    if (fp == null)
                        continue;
                    // Make name within its context (parent)
                    string _filename = _ref.Parent == null ? fileReference.CanonicalPath : fileReference.CanonicalPath.Substring(_ref.Parent.CanonicalPath.Length + 1);
                    // Get file info
                    IFileInfo fi = fp.GetFileInfo(_filename) ?? new NotFoundFileInfo(fileReference.CanonicalPath);
                    // 
                    return fi;
                }
            }
            return new NotFoundFileInfo(fileReference.CanonicalPath);
        }

        IDirectoryContents GetDirectoryInfo()
        {
            using (var fp = packageProvider.TryOpenPackage(fileReference.Parent))
                return fp?.GetDirectoryContents(fileReference.Name) ?? NotFoundDirectoryContents.Singleton;
        }

        /// <summary>
        /// Test if file exists
        /// </summary>
        public bool Exists
            => GetFileInfo().Exists;

        /// <summary>
        /// Get file length
        /// </summary>
        public long Length
            => GetFileInfo().Length;

        /// <summary>
        /// Get path
        /// </summary>
        public string PhysicalPath
            => GetFileInfo().PhysicalPath;

        /// <summary>
        /// Cached filename without directory.
        /// </summary>
        internal string filename = null;

        /// <summary>
        /// Filename without directory.
        /// </summary>
        public string Name => filename ?? (filename = MakeFilename());

        /// <summary>
        /// Separate directory from file path.
        /// </summary>
        /// <returns></returns>
        string MakeFilename()
        {
            int slashIx = fileReference.Name.LastIndexOf('/');
            if (slashIx >= 0) return fileReference.Name.Substring(slashIx + 1);
            return fileReference.Name;
        }

        /// <summary>
        /// Last modified date
        /// </summary>
        public DateTimeOffset LastModified
            => GetFileInfo().LastModified;

        /// <summary>
        /// Test if is directory
        /// </summary>
        public bool IsDirectory
            => fileReference.IsPackageFile || (fileReference.Parent == null && fileReference.Name == "") || GetDirectoryInfo().Exists;

        /// <summary>
        /// Open stream
        /// </summary>
        /// <returns></returns>
        public Stream CreateReadStream()
        {
            for (var _ref = fileReference; _ref != null; _ref = _ref.Parent)
            {
                // Open package file
                IDisposableFileProvider fp = packageProvider.TryOpenPackage(_ref.Parent);
                // Wasn't package, try parent
                if (fp == null) continue;
                // Make name within its context (parent)
                string _filename = _ref.Parent == null ? fileReference.CanonicalPath : fileReference.CanonicalPath.Substring(_ref.Parent.CanonicalPath.Length + 1);
                // Get entry
                IFileInfo fi = fp.GetFileInfo(_filename);
                // Open a file inside the package
                Stream s = fi.CreateReadStream();
                // Got memory stream, release package
                if (s is MemoryStream) { fp.Dispose(); return s; }
                // Got open stream, lock parent open as long as stream is open
                return new StreamHandle(s, fp);
            }
            // The file wasn't a package
            throw new FileNotFoundException($"The package {fileReference?.Parent?.Name} does not exist.");
        }

        /// <summary>
        /// Print info.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => fileReference.CanonicalPath;
    }

}
