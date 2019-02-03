using Lexical.FileProvider;
using Lexical.FileProvider.Package;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace docs
{
    class ZipExamples
    {
        // Rename to "Main", or run from Main.
        public static void Run(string[] args)
        {
            {
                #region Snippet_1
                ZipFileProvider fileProvider = new ZipFileProvider("mydata.zip");
                #endregion Snippet_1
                fileProvider.Dispose();
            }
            {
                #region Snippet_2
                // Open some stream
                Stream stream = new FileStream("mydata.zip", FileMode.Open);

                // Use stream as zip file.
                ZipFileProvider fileProvider = new ZipFileProvider(stream).AddDisposable(stream);
                #endregion Snippet_2
                fileProvider.Dispose();
            }

            {
                ZipFileProvider fileProvider = new ZipFileProvider("mydata.zip");
                #region Snippet_3
                fileProvider.Dispose();
                #endregion Snippet_3
            }

            {
                #region Snippet_4
                // Create root file provider
                PhysicalFileProvider root = new PhysicalFileProvider(Directory.GetCurrentDirectory());

                // Create package options
                IPackageFileProviderOptions options =
                    new PackageFileProviderOptions()
                    .AddPackageLoaders(Lexical.FileProvider.PackageLoader.Zip.Singleton);

                // Create package file provider
                IPackageFileProvider fileProvider = new PackageFileProvider(root, options).AddDisposable(root);
                #endregion Snippet_4
                fileProvider.Dispose();
            }

            {
                // Create root file provider
                PhysicalFileProvider root = new PhysicalFileProvider(Directory.GetCurrentDirectory());

                // Create package options
                #region Snippet_5
                IPackageFileProviderOptions options = new PackageFileProviderOptions()
                    .AddPackageLoaders( new Lexical.FileProvider.PackageLoader.Zip("\\.nupkg") );
                #endregion Snippet_5
                // Create package file provider
                IPackageFileProvider fileProvider = new PackageFileProvider(root, options).AddDisposable(root);
                fileProvider.Dispose();
            }

            {
                #region Snippet_10
                // Create file provider
                ZipFileProvider fileProvider = new ZipFileProvider("mydata.zip");
                // Add disposable for belated dispose
                fileProvider.AddBelatedDispose(new _Disposable_());
                // Open stream
                Stream s = fileProvider
                        .GetFileInfo("Lexical.Localization.Tests.dll")
                        .CreateReadStream();
                // Dispose file provider
                fileProvider.Dispose();
                // Dispose the open stream  --  _Disposable_ is disposed here.
                s.Dispose();
                #endregion Snippet_10
            }

            {
                #region Snippet_X
                #endregion Snippet_X
            }

            {
                #region Snippet_X
                #endregion Snippet_X
            }
        }
    }
}
