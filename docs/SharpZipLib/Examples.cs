using Lexical.FileProvider.Package;
using Lexical.FileProvider.SharpCompress;
using Lexical.FileProvider;
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
    class SharpZipLibExamples
    {
        // Rename to "Main", or run from Main.
        public static void Run(string[] args)
        {
            {
                #region Snippet_1
                BZip2FileProvider fileProvider_bzip2 = new BZip2FileProvider("mydata.tar.bzip2", "mydata.tar");
                LzwFileProvider fileProvider_lzw = new LzwFileProvider("mydata.tar.Z", "mydata.tar");
                #endregion Snippet_1
                fileProvider_lzw.Dispose();
                fileProvider_bzip2.Dispose();
            }
            {
                #region Snippet_2
                // Read data
                byte[] data;
                using (Stream stream = new FileStream("mydata.tar.bzip2", FileMode.Open))
                    data = FileUtils.ReadFully(stream);

                // Use stream as zip file.
                BZip2FileProvider fileProvider = new BZip2FileProvider(data, "mydata.tar");
                #endregion Snippet_2
                fileProvider.Dispose();
            }

            {
                BZip2FileProvider fileProvider = new BZip2FileProvider("mydata.tar.bzip2", "mydata.tar");
                #region Snippet_3
                fileProvider.Dispose();
                #endregion Snippet_3
            }

            {
                #region Snippet_10
                // Create file provider
                BZip2FileProvider fileProvider = new BZip2FileProvider("mydata.tar.bzip2", "mydata.tar");
                // Add disposable for belated dispose
                fileProvider.AddBelatedDispose(new _Disposable_());
                // Open stream
                Stream s = fileProvider
                        .GetFileInfo("mydata.tar")
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
                        Lexical.FileProvider.PackageLoader.Tar.Singleton,
                        Lexical.FileProvider.PackageLoader.BZip2.Singleton,
                        Lexical.FileProvider.PackageLoader.Lzw.Singleton
                    );

                // Create package file provider
                IPackageFileProvider fileProvider = new PackageFileProvider(root, options).AddDisposable(root);

                // Read compressed file
                using (Stream document = fileProvider.GetFileInfo("document.txt.Z/document.txt").CreateReadStream())
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
}
