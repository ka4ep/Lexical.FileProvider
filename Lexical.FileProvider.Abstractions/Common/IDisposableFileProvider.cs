// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           22.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;

namespace Lexical.FileProvider.Common
{
    /// <summary>
    /// Combination of two interfaces so that they can be returned from methods.
    /// </summary>
    public interface IDisposableFileProvider : IFileProvider, IDisposable
    {
    }

    /// <summary>
    /// Interface for fileprovider that allow delayed disposable to be attached.
    /// Delayed disposable is called once the fileprovider is disposed, and all its open streams are closed.
    /// 
    /// The dispose process goes as following: 
    /// 
    ///     Once disposed is called, the file provider goes to disposed state.
    ///     No new streams can be opened. However, there may be open streams.
    ///     Once all open streams are closed, then belated disposes are called.
    /// 
    /// </summary>
    public interface IBelatedDisposeFileProvider : IFileProvider
    {
        /// <summary>
        /// Add <paramref name="disposable"/> that is to be disposed after fileprovider is disposed and all streams are closed.
        /// 
        /// If the implementing object has already been disposed, this method immediately disposes the <paramref name="disposable"/>.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>true if was added to list, false if wasn't but was disposed immediately</returns>
        bool AddBelatedDispose(IDisposable disposable);

        /// <summary>
        /// Add <paramref name="disposables"/> that are to be disposed after fileprovider is disposed and all streams are closed
        /// 
        /// If the implementing object has already been disposed, this method immediately disposes the <paramref name="disposables"/>.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>true if were added to list, false if were disposed immediately</returns>
        bool AddBelatedDisposes(IEnumerable<IDisposable> disposables);

        /// <summary>
        /// Remove <paramref name="disposable"/> from the list. 
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>true if was removed, false if it wasn't in the list.</returns>
        bool RemoveBelatedDispose(IDisposable disposable);

        /// <summary>
        /// Remove <paramref name="disposables"/> from the list. 
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>true if was removed, false if it wasn't in the list.</returns>
        bool RemoveBelatedDisposes(IEnumerable<IDisposable> disposables);

    }
}
