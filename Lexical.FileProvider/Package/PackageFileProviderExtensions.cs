// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           21.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileProvider.Package
{
    public static class PackageFileProviderExtensions_
    {
        /// <summary>
        /// Configure options.
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static PackageFileProvider ConfigureOptions(this PackageFileProvider fileProvider, Action<IPackageFileProviderOptions> configure)
        {
            if (configure != null) configure(fileProvider.Options);
            return fileProvider;
        }
    }
}
