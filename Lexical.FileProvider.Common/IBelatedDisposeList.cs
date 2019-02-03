﻿// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           21.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;

namespace Lexical.FileProvider.Common
{
    public interface IBelatedDisposeList : IDisposable
    {
        /// <summary>
        /// Create a handle that delays dispose of dispose list.
        /// </summary>
        /// <returns>handle</returns>
        IDisposable Belate();

        /// <summary>
        /// Add <paramref name="disposable"/> that is to be disposed along with the called object once all belated handles are disposed.
        /// 
        /// If the implementing object has already been disposed, this method immediately disposes the <paramref name="disposable"/>.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>true if was added to list, false if wasn't but was disposed immediately</returns>
        bool AddBelatedDispose(IDisposable disposable);

        /// <summary>
        /// Add <paramref name="disposables"/> that are going to be disposed along with the called object once all belated handles are disposed.
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
        bool RemovedBelatedDisposes(IEnumerable<IDisposable> disposables);
    }
}
