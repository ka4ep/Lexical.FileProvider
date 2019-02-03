// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           18.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
namespace Lexical.FileProvider.Package
{
    /// <summary>
    /// File provider that can open package files recursively.
    /// </summary>
    public interface IPackageFileProvider : IDisposableFileProvider
    {
        /// <summary>
        /// File provider options. 
        /// 
        /// Options are general and can be shared with multiple file provider instances.
        /// </summary>
        IPackageFileProviderOptions Options { get; set; }

        /// <summary>
        /// Temp file provider
        /// </summary>
        ITempFileProvider TempFileProvider { get; set; }
    }

    public static class PackageFileProviderExtensions
    {
        /// <summary>
        /// Assign options and return <paramref name="fileProvider"/>.
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="options"></param>
        /// <returns>fileProvider</returns>
        public static IPackageFileProvider SetOptions(this IPackageFileProvider fileProvider, IPackageFileProviderOptions options)
        {
            fileProvider.Options = options;
            return fileProvider;
        }

        /// <summary>
        /// Assign <paramref name="tempFileProvider"/> and return <paramref name="fileProvider"/>.
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="tempFileProvider"></param>
        /// <returns>fileProvider</returns>
        public static IPackageFileProvider SetOptions(this IPackageFileProvider fileProvider, ITempFileProvider tempFileProvider)
        {
            fileProvider.TempFileProvider = tempFileProvider;
            return fileProvider;
        }

        /// <summary>
        /// Set maximum memory temp file snapshot length. If value is over 0, then temp file snapshots are allowed.
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="tempFileProvider"></param>
        /// <returns></returns>
        public static IPackageFileProvider SetTempFileProvider(this IPackageFileProvider fileProvider, ITempFileProvider tempFileProvider)
        {
            fileProvider.TempFileProvider = tempFileProvider;
            return fileProvider;
        }
    }

}
