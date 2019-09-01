// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           19.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Lexical.FileProvider.Package
{
    /// <summary>
    /// Presentation of canonical path that is split at package names.
    /// 
    /// For example, for "/folder/myfile.zip/dir/mylib.dll/mylib.resources" would be split to 
    /// segments of "/folder/myfile.zip", "dir/mylib.dll" and "mylib.resources".
    /// 
    /// Directory separator is '/'. This is also separator between packages.
    /// </summary>
    public class PackageFileReference : IEquatable<PackageFileReference>
    {
        /// <summary>
        /// Parse package file names and possible non-package file with a regular-expression pattern.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern">pattern that captures package files</param>
        /// <param name="canonizeRoot">If true canonizes the first element "c:\Temp\myfile.zip" -> "c:/temp/myfile.zip"</param>
        /// <returns></returns>
        public static PackageFileReference Parse(string path, Regex pattern, bool canonizeRoot)
            => Parse(path, pattern.Matches(path), canonizeRoot);

        /// <summary>
        /// Parse package file names and possible non-package file with a regular-expression pattern.
        /// </summary>
        /// <param name="path">path</param>
        /// <param name="matches">captured matches</param>
        /// <param name="canonizeRoot">If true canonizes the first element "c:\Temp\myfile.zip" -> "c:/temp/myfile.zip"</param>
        /// <returns></returns>
        public static PackageFileReference Parse(string path, MatchCollection matches, bool canonizeRoot)
        {
            // Assert arguments are ok
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (matches == null) throw new ArgumentNullException(nameof(matches));

            // There were no package names
            if (matches.Count == 0) return new PackageFileReference(path, false, null, path);

            // Extract the last non-package filename
            // For example in "some.zip/some.zip/notpackage.txt", extract the "notpackage.txt".
            int lastMatchIx = matches[matches.Count - 1].Index + matches[matches.Count - 1].Length;
            string nonPackageFile = path.Length > lastMatchIx ? path.Substring(lastMatchIx) : null;

            // Get package file names
            PackageFileReference result = null;
            for (int matchIndex = 0; matchIndex < matches.Count; matchIndex++)
            {
                string filename = matches[matchIndex].Groups["filename"].Value;
                if (matchIndex == 0 && canonizeRoot) { filename = filename.Replace(Path.DirectorySeparatorChar, '/'); path = null; }

                bool isLast = matchIndex == matches.Count - 1;
                result = new PackageFileReference(filename, true, result, isLast && nonPackageFile == null ? path : null);
            }

            // Add the non-package file
            if (nonPackageFile != null) result = new PackageFileReference(nonPackageFile, false, result, path);

            // Return the node list
            return result;
        }

        /// <summary>
        /// Convertible to string.
        /// </summary>
        /// <param name="packageRef"></param>
        public static implicit operator string(PackageFileReference packageRef)
            => packageRef.CanonicalPath;

        /// <summary>
        /// (Optional) Parent file
        /// </summary>
        public readonly PackageFileReference Parent;

        /// <summary>
        /// File name
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// True if Name represents a package file.
        /// </summary>
        public readonly bool IsPackageFile;

        /// <summary>
        /// Cached canonical path
        /// </summary>
        private string canonicalPath;

        /// <summary>
        /// Lazy evaluation of the canonical path.
        /// </summary>
        public String CanonicalPath => canonicalPath ?? (canonicalPath = MakeCanonicalPath());

        /// <summary>
        /// Cached array from root.
        /// </summary>
        PackageFileReference[] array;

        /// <summary>
        /// Lazy evaluation of array from root.
        /// </summary>
        public PackageFileReference[] Array => array ?? (array = MakeArray());

        /// <summary>
        /// Cached hash-code
        /// </summary>
        int hashcode;

        /// <summary>
        /// Create new package reference.
        /// </summary>
        /// <param name="name">file</param>
        /// <param name="isPackageFile">true, if the file represents a package file</param>
        /// <param name="parent">(optional) reference to parent file</param>
        /// <param name="canonicalPath">(optional) canonical path. If null, it will be lazily reconstructed</param>
        public PackageFileReference(string name, bool isPackageFile, PackageFileReference parent, string canonicalPath)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.IsPackageFile = isPackageFile;
            this.Parent = parent;

            // Set canonical path if not null
            if (canonicalPath != null && canonicalPath.Length > 0)
            {
                char lastChar = canonicalPath[canonicalPath.Length - 1];
                this.canonicalPath = lastChar == '/' ? canonicalPath.Substring(0, canonicalPath.Length - 1) : canonicalPath;
            }

            // Make hashcode
            hashcode = -2128831035; // FNV-1a offset
            // Hash in nodes
            for(var node = this; node !=null; node=node.Parent)
                hashcode = (hashcode ^ node.Name.GetHashCode() ^ (node.IsPackageFile? 0x10101010: 0 )) * 16777619;
        }

        /// <summary>
        /// Construct canonical path by concatenating nodes.
        /// 
        /// For example, if nodes are "myfile.zip" and "mylib.dll", non-package node "mylib.resources"
        /// then the  canonical path is "myfile.zip/mylib.dll/mylib.resources".
        /// </summary>
        /// <returns>canonical path</returns>
        private string MakeCanonicalPath()
        {
            if (Parent == null) return Name;

            int len = -1;
            for (var node = this; node != null; node = node.Parent)
                len += 1 + node.Name.Length;

            char[] chars = new char[len];
            int i = len;
            for (var node = this; node != null; node = node.Parent)
            {
                if (node!=this) chars[--i] = '/';
                i -= node.Name.Length;
                node.Name.CopyTo(0, chars, i, node.Name.Length);
            }
            return new string(chars);
        }

        /// <summary>
        /// Make array from root to this
        /// </summary>
        /// <returns></returns>
        private PackageFileReference[] MakeArray()
        {
            int count = 0;
            for (var node = this; node != null; node = node.Parent)
                count++;

            PackageFileReference[] result = new PackageFileReference[count];
            int ix = count;
            for (var node = this; node != null; node = node.Parent)
                result[--ix] = node;

            return result;
        }

        /// <summary>
        /// Get the root most node
        /// </summary>
        public PackageFileReference Root
        {
            get
            {
                PackageFileReference result = this;
                while (result.Parent != null) result = result.Parent;
                return result;
            }
        }

        /// <summary>
        /// If this is package reference, return self. If it's not returns parent, be that null or not.
        /// </summary>
        public PackageFileReference PackageReference => IsPackageFile ? this : Parent;

        /// <summary>
        /// Count the degree if package refererences, including self. 
        /// </summary>
        public int Count
        {
            get
            {
                int count = 1;
                for (var node = Parent; node != null; node = node.Parent) count++;
                return count;
            }
        }

        /// <summary>
        /// Print string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => CanonicalPath;

        /// <summary>
        /// Get hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
            => hashcode;

        /// <summary>
        /// Compare to another object for content equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>true if contain equal content</returns>
        public override bool Equals(object obj)
        {
            if (obj is PackageFileReference other)
            {
                PackageFileReference x = this, y = other;
                for (; x!=null & y!=null; x=x.Parent, y=y.Parent)
                    if (x.Name != y.Name || x.IsPackageFile != y.IsPackageFile) return false;
                if (x != null || y != null) return false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Compare to another object for content equality
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if contain equal content</returns>
        public bool Equals(PackageFileReference other)
        {
            PackageFileReference x = this, y = other;
            for (; x != null & y != null; x = x.Parent, y = y.Parent)
                if (x.Name != y.Name || x.IsPackageFile != y.IsPackageFile) return false;
            if (x != null || y != null) return false;
            return true;
        }
    }
}
