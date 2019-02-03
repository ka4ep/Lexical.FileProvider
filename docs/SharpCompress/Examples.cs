using Lexical.FileProvider;
using Lexical.FileProvider.Package;
using Lexical.FileProvider.SharpCompress;
using Lexical.FileProvider.Common;
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
    class SharpCompressExamples
    {
        // Rename to "Main", or run from Main.
        public static void Run(string[] args)
        {
            {
                #region Snippet_1
                _ZipFileProvider fileProvider_zip = new _ZipFileProvider("mydata.zip");
                RarFileProvider fileProvider_rar = new RarFileProvider("mydata.rar");
                TarFileProvider fileProvider_tar = new TarFileProvider("mydata.tar");
                _7zFileProvider fileProvider_7z = new _7zFileProvider("mydata.7z");
                GZipFileProvider fileProvider_gz = new GZipFileProvider("mydata.tar.gz", "mydata.tar");
                #endregion Snippet_1
                fileProvider_zip.Dispose();
                fileProvider_rar.Dispose();
                fileProvider_tar.Dispose();
                fileProvider_7z.Dispose();
                fileProvider_gz.Dispose();
            }
            {
                #region Snippet_2
                // Open some stream
                Stream stream = new FileStream("mydata.zip", FileMode.Open);

                // Use stream as zip file.
                _ZipFileProvider fileProvider = new _ZipFileProvider(stream).AddDisposable(stream);
                #endregion Snippet_2
                fileProvider.Dispose();
            }

            {
                _ZipFileProvider fileProvider = new _ZipFileProvider("mydata.zip");
                #region Snippet_3
                fileProvider.Dispose();
                #endregion Snippet_3
            }

            {
                #region Snippet_10
                // Create file provider
                _ZipFileProvider fileProvider = new _ZipFileProvider("mydata.zip");
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
                #region Snippet_4
                // Create root file provider
                PhysicalFileProvider root = new PhysicalFileProvider(Directory.GetCurrentDirectory());

                // Create package options
                IPackageFileProviderOptions options =
                    new PackageFileProviderOptions()
                    .AddPackageLoaders(
                        Lexical.FileProvider.PackageLoader._Zip.Singleton, 
                        Lexical.FileProvider.PackageLoader.Rar.Singleton,
                        Lexical.FileProvider.PackageLoader._7z.Singleton,
                        Lexical.FileProvider.PackageLoader.Tar.Singleton,
                        Lexical.FileProvider.PackageLoader.GZip.Singleton
                    );

                // Create package file provider
                IPackageFileProvider fileProvider = new PackageFileProvider(root, options).AddDisposable(root);

                // Read compressed file
                using (Stream document = fileProvider.GetFileInfo("document.txt.gz/document.txt").CreateReadStream())
                {
                    byte[] data = FileUtils.ReadFully(document);
                    string text = Encoding.UTF8.GetString(data);
                    Console.WriteLine(text);
                }
                #endregion Snippet_4
                fileProvider.Dispose();
            }

            {
                #region Snippet_X
                #endregion Snippet_X
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

    #region Snippet_10b
    class _Disposable_ : IDisposable
    {
        public void Dispose()
            => Console.WriteLine("Disposed");
    }
    #endregion Snippet_10b

}
