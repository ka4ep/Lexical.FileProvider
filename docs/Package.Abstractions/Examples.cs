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
    class PackageAbstractionsExamples
    {
        // Rename to "Main", or run from Main.
        public static void Run(string[] args)
        {
            {
                #region Snippet_1
                IPackageFileProviderOptions options = new PackageFileProviderOptions();
                options.AddPackageLoader( Lexical.FileProvider.PackageLoader.Zip.Singleton );
                options.AddPackageLoader( Lexical.FileProvider.PackageLoader.Dll.Singleton );
                #endregion Snippet_1
            }

            {
                IFileProvider rootFileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory());
                IPackageFileProviderOptions options = new PackageFileProviderOptions();
                IPackageFileProvider fileProvider = new PackageFileProvider(rootFileProvider, options);
                #region Snippet_2
                // Create temp options
                TempFileProviderOptions tempFileOptions = new TempFileProviderOptions { Directory = "%tmp%", Prefix = "package-", Suffix = ".tmp" };

                // Create temp provider
                ITempFileProvider tempFileProvider = new TempFileProvider(tempFileOptions);

                // Try to create temp file
                using (var tempFile = tempFileProvider.CreateTempFile())
                    Console.WriteLine(tempFile.Filename);

                // Attach temp provider
                fileProvider.SetTempFileProvider(tempFileProvider).Options.SetTempFileSnapshotLength(1073741824);
                #endregion Snippet_2
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
