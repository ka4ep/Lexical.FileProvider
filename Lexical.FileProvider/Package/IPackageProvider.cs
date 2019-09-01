// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           22.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileProvider.Common;

namespace Lexical.FileProvider.Package
{
    public interface IPackageProvider
    {
        /// <summary>
        /// Open a package at a <paramref name="path"/>.
        /// 
        /// Returns a handle to file provider. File provider will not be evicted until the handle is disposed.
        /// 
        /// Returns null, if package failed to open because the error was expected and was suppressed.
        /// </summary>
        /// <param name="path">package refrence, if null then refers to root file provider</param>
        /// <returns>
        ///         a handle, to a fileprovider. The handle must be disposed.
        ///         null value, if package failed to open and the error was suppressed. That means that the file is not a package (wrong file format).
        /// </returns>
        /// <exception cref="ObjectDisposedException">if the opener was disposed</exception>
        /// <exception cref="Exception">any non-suppressed error that occured when the package was opened. This error can be of a previously cached open attempt. </exception>
        IDisposableFileProvider TryOpenPackage(PackageFileReference path);
    }
}
