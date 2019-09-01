// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           21.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Microsoft.Extensions.FileProviders;
using System;

namespace Lexical.FileProvider.Root
{
    /// <summary>
    /// Drive letter entry.
    /// </summary>
    class DriveEntry : IDisposable
    {
        public readonly string path;
        public readonly IFileProvider fileProvider;

        public DriveEntry(string path, IFileProvider fileProvider)
        {
            this.path = path;
            this.fileProvider = fileProvider;
        }

        public void Dispose()
        {
            if (fileProvider is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
