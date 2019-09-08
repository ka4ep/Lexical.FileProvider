// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           19.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lexical.FileProvider.Package
{
    /// <summary>
    /// A reference to a directory in a package. 
    /// 
    /// This class can be used for loading directories even if they have been evited from parent's cache.
    /// </summary>
    public class PackageDirectoryContents : IDirectoryContents
    {
        /// <summary>
        /// Package provider that can open and lock packages
        /// </summary>
        readonly PackageFileProvider packageProvider;

        /// <summary>
        /// File reference.
        /// </summary>
        readonly PackageFileReference directoryReference;

        /// <summary>
        /// Create contents reader.
        /// </summary>
        /// <param name="packageProvider"></param>
        /// <param name="directoryReference"></param>
        public PackageDirectoryContents(PackageFileProvider packageProvider, PackageFileReference directoryReference)
        {
            this.packageProvider = packageProvider ?? throw new ArgumentNullException(nameof(packageProvider));
            this.directoryReference = directoryReference ?? throw new ArgumentNullException(nameof(directoryReference));
        }

        IDirectoryContents GetDirectoryContents()
        {
            for (var _ref = directoryReference; _ref != null; _ref = _ref.Parent)
            {
                // Open package file
                using (var fp = packageProvider.TryOpenPackage(_ref.Parent))
                {
                    // Wasn't package, try parent
                    if (fp == null) continue;
                    // Make name within its context (parent)
                    string name = _ref.Parent == null ? directoryReference.CanonicalPath : directoryReference.CanonicalPath.Substring(_ref.Parent.CanonicalPath.Length + 1);
                    // Get directory info
                    IDirectoryContents dir = fp.GetDirectoryContents(name) ?? NotFoundDirectoryContents.Singleton;
                    // 
                    return dir;
                }
            }
            return NotFoundDirectoryContents.Singleton;
        }

        /// <inheritdoc/>
        public bool Exists
            => directoryReference.IsPackageFile || GetDirectoryContents().Exists;

        static IFileInfo[] no_files = new IFileInfo[0];

        IFileInfo[] GetFiles()
        {
            IFileInfo[] files = null;
            string subdir = null;

            // Open as package
            PackageFileReference package_to_open = directoryReference.IsPackageFile ? directoryReference : directoryReference.Parent;
            using (var fp = packageProvider.TryOpenPackage(package_to_open))
            {
                if (fp != null)
                {
                    subdir = directoryReference.IsPackageFile ? "" : directoryReference.Name;
                    files = fp.GetDirectoryContents(subdir).ToArray();
                }
            }

            // Open as directory
            if (files == null)
            {
                using (var fp = packageProvider.TryOpenPackage(directoryReference.Parent))
                {
                    if (fp != null)
                    {
                        subdir = directoryReference.Name;
                        IDirectoryContents dir = fp.GetDirectoryContents(subdir);
                        files = fp.GetDirectoryContents(subdir).ToArray();
                    }
                }
            }

            // Convert package entries to package file infos
            if (files != null)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    // Get file reference
                    string filename = files[i].Name;
                    // Match to package file extensions (e.g. *.zip)
                    Match match = packageProvider.Pattern.Match(filename);
                    // don't replace
                    //if (!match.Success) continue;
                    // Convert path to structured format
                    PackageFileReference fileReference = new PackageFileReference(subdir == "" || subdir == "/" ? filename : subdir + "/" + filename, match.Success, package_to_open, null);
                    // Create file entry
                    PackageFileInfo newFileInfo = new PackageFileInfo(packageProvider, fileReference);
                    newFileInfo.filename = filename;
                    // Set new entry
                    files[i] = newFileInfo;
                }
                return files;
            }

            return no_files;
        }

        /// <inheritdoc/>
        public IEnumerator<IFileInfo> GetEnumerator()
            => ((IEnumerable<IFileInfo>)GetFiles()).GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
            => GetFiles().GetEnumerator();

        /// <inheritdoc/>
        public override string ToString()
            => directoryReference.CanonicalPath;
    }
}
