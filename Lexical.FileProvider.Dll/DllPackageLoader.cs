// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           18.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;
using Lexical.FileProvider.Package;
using Microsoft.Extensions.FileProviders;

namespace Lexical.FileProvider.PackageLoader
{
    /// <summary>
    /// This class adds to <see cref="PackageFileProvider"/> the feature to open managed .dll and .exe files.
    /// 
    /// To use this class, the caller must import NuGet library Lexical.FileProvider.Package.Abstractions.
    /// </summary>
    public class Dll : IPackageLoaderUseStreamCapability, IPackageLoaderOpenFileCapability
    {
        private static Dll singleton = new Dll();

        /// <summary>
        /// Static singleton instance that opens managed .dll files.
        /// </summary>
        public static Dll Singleton => singleton;

        /// <summary>
        /// Supported file extensions
        /// </summary>
        public string FileExtensionPattern { get; internal set; }

        /// <summary>
        /// Create new package loader that loads zip files.
        /// </summary>
        public Dll() : this(@"\.dll") { }

        /// <summary>
        /// Create new package loader that loads zip files.
        /// </summary>
        /// <param name="fileExtensionPattern">regular expression pattern</param>
        public Dll(string fileExtensionPattern)
        {
            this.FileExtensionPattern = fileExtensionPattern ?? throw new ArgumentNullException(nameof(fileExtensionPattern));
        }

        /// <summary>
        /// Open a .dll file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="packageInfo"></param>
        /// <returns>file provider</returns>
        public IFileProvider OpenFile(string filename, IPackageLoadInfo packageInfo)
        {
            try
            {
                return DllFileProvider.OpenFile(filename, packageInfo?.LastModified);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                throw new PackageException.LoadError(filename, e);
            }
        }

        /// <summary>
        /// Use <paramref name="stream"/> to access contents of a .dll file.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="packageInfo"></param>
        /// <returns>file provider</returns>
        public IFileProvider UseStream(Stream stream, IPackageLoadInfo packageInfo)
        {
            try
            {
                return DllFileProvider.UseStream(stream, packageInfo?.LastModified);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                throw new PackageException.LoadError(stream is FileStream fs ? fs.Name : null, e);
            }
        }
    }

    /// <summary>
    /// This class adds to <see cref="PackageFileProvider"/> the feature to open managed .Exe and .exe files.
    /// 
    /// To use this class, the caller must import NuGet library Lexical.FileProvider.Package.Abstractions.
    /// </summary>
    public class Exe : IPackageLoaderUseStreamCapability, IPackageLoaderOpenFileCapability
    {
        private static Exe singleton = new Exe();

        /// <summary>
        /// Static singleton instance that opens managed .Exe files.
        /// </summary>
        public static Exe Singleton => singleton;

        /// <summary>
        /// Supported file extensions
        /// </summary>
        public string FileExtensionPattern { get; internal set; }

        /// <summary>
        /// Create new package loader that loads zip files.
        /// </summary>
        public Exe() : this(@"\.exe") { }

        /// <summary>
        /// Create new package loader that loads zip files.
        /// </summary>
        /// <param name="fileExtensionPattern">regular expression pattern</param>
        public Exe(string fileExtensionPattern)
        {
            this.FileExtensionPattern = fileExtensionPattern ?? throw new ArgumentNullException(nameof(fileExtensionPattern));
        }

        /// <summary>
        /// Open a .dll file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="packageInfo"></param>
        /// <returns>file provider</returns>
        public IFileProvider OpenFile(string filename, IPackageLoadInfo packageInfo)
        {
            try
            {
                return DllFileProvider.OpenFile(filename, packageInfo?.LastModified);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                throw new PackageException.LoadError(filename, e);
            }
        }

        /// <summary>
        /// Use <paramref name="stream"/> to access contents of a .dll file.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="packageInfo"></param>
        /// <returns>file provider</returns>
        public IFileProvider UseStream(Stream stream, IPackageLoadInfo packageInfo)
        {
            try
            {
                return DllFileProvider.UseStream(stream, packageInfo?.LastModified);
            }
            catch (Exception e) when (e is InvalidDataException || e is FormatException || e is BadImageFormatException)
            {
                throw new PackageException.LoadError(stream is FileStream fs ? fs.Name : null, e);
            }
        }
    }
}
