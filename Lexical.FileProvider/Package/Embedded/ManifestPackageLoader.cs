// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           18.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Lexical.FileProvider.Common;
using Microsoft.Extensions.FileProviders;

namespace Lexical.FileProvider.Package
{
    /// <summary>
    /// Loads assembly into an AppDomain, and disposes it in exit.
    /// 
    /// Cannot load assembly if its dependency is not found. There is no ReflectionOnlyLoad in .NET Standard 2.0. 
    /// 
    /// This implementation doesn't work very well and its use is discouraged. Please use DllPackageLoader instead.
    /// </summary>
    [Obsolete("DllPackageLoader works better")]
    public class ManifestEmbeddedFileProvider : IPackageLoaderLoadFile, IPackageLoaderUseBytes
    {
        private static ManifestEmbeddedFileProvider singleton = new ManifestEmbeddedFileProvider();

        /// <summary>
        /// Static singleton instance
        /// </summary>
        public static ManifestEmbeddedFileProvider Singleton => singleton;

        /// <summary>
        /// Supported file extensions
        /// </summary>
        public string FileExtensionPattern => @"\.dll|\.exe";

        /// <summary>
        /// Load manifest of a managed .dll file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="packageInfo">(optional) clues about the file that is being opened</param>
        /// <returns></returns>
        public IFileProvider LoadFile(string filename, IPackageLoadInfo packageInfo)
        {
            string path = Path.GetFullPath(filename);
            byte[] data = File.ReadAllBytes(path);

            AppDomain appDomain = AppDomain.CreateDomain(nameof(ManifestEmbeddedFileProvider) + "-" + Guid.NewGuid());
            try
            {
                Assembly asm = appDomain.Load(data);                
                Microsoft.Extensions.FileProviders.ManifestEmbeddedFileProvider embeddedFileProvider = new Microsoft.Extensions.FileProviders.ManifestEmbeddedFileProvider(asm);
                AppDomainUnloader disposable = new AppDomainUnloader(appDomain);
                IFileProvider fileProvider = new FileProviderHandle(o => (o as IDisposable).Dispose(), disposable, embeddedFileProvider);
                return fileProvider;
            } catch (Exception e)
            {
                AppDomain.Unload(appDomain);
                throw new PackageException.LoadError(filename, e);
            }            
        }

        bool reflectionLoadNotSupported = false;

        Assembly loadAssembly(string filepath)
        {
            if (!reflectionLoadNotSupported) return Assembly.LoadFile(filepath);
            try
            {
                return Assembly.ReflectionOnlyLoadFrom(filepath);
            }
            catch (System.PlatformNotSupportedException)
            {
                reflectionLoadNotSupported = true;
                return Assembly.LoadFile(filepath);
            }
        }

        Assembly loadAssembly(byte[] data)
        {
            if (!reflectionLoadNotSupported) return Assembly.Load(data);
            try
            {
                return Assembly.ReflectionOnlyLoad(data);
            }
            catch (System.PlatformNotSupportedException)
            {
                reflectionLoadNotSupported = true;
                return Assembly.Load(data);
            }
        }

        /// <summary>
        /// Load from byte[]
        /// </summary>
        /// <param name="data"></param>
        /// <param name="packageInfo">(optional) clues about the file that is being opened</param>
        /// <returns></returns>
        public IFileProvider UseBytes(byte[] data, IPackageLoadInfo packageInfo)
        {
            AppDomain appDomain = AppDomain.CreateDomain(nameof(ManifestEmbeddedFileProvider) + "-" + Guid.NewGuid());
            try
            {
                Assembly asm = appDomain.Load(data);
                Microsoft.Extensions.FileProviders.ManifestEmbeddedFileProvider embeddedFileProvider = new Microsoft.Extensions.FileProviders.ManifestEmbeddedFileProvider(asm);
                AppDomainUnloader disposable = new AppDomainUnloader(appDomain);
                IFileProvider fileProvider = new FileProviderHandle(o => (o as IDisposable).Dispose(), disposable, embeddedFileProvider);
                return fileProvider;
            }
            catch (Exception e)
            {
                AppDomain.Unload(appDomain);
                throw new PackageException.LoadError(null, e);
            }
        }
    }

    class AppDomainUnloader : IDisposable
    {
        public AppDomain appDomain;

        public AppDomainUnloader(AppDomain appDomain)
        {
            this.appDomain = appDomain ?? throw new ArgumentNullException(nameof(AppDomain));
        }

        public void Dispose()
        {
            AppDomain _appDomain = Interlocked.CompareExchange(ref appDomain, null, appDomain);
            if (_appDomain != null) AppDomain.Unload(_appDomain);
        }
    }

}
