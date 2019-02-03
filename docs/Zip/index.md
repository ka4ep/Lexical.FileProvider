---
uid: Lexical.FileProvider.Zip
---
# File Provider
**new ZipFileProvider(*string*)** constructs a file provider that reads .zip contents. 
Content can be read concurrently as file provider opens new file handles as needed.
[!code-csharp[Snippet](Examples.cs#Snippet_1)]

Another constructor is **new ZipFileProvider(*Stream*)**. Note however, that as stream
has only one file pointer, the content can be accessed only by one thread at a time. Concurrent threads have to wait.
[!code-csharp[Snippet](Examples.cs#Snippet_2)]

File provider must be disposed after use.
[!code-csharp[Snippet](Examples.cs#Snippet_3)]

Belated dispose can be added to the file provider. They are disposed once the fileprovider is disposed and all its streams.
[!code-csharp[Snippet](Examples.cs#Snippet_10)]

# Package Loader
**Lexical.FileProvider.PackageLoader.Zip** can be used as a component of *PackageFileLoader*, making possible to drill down into .zip files.
[!code-csharp[Snippet](Examples.cs#Snippet_4)]

**Lexical.FileProvider.PackageLoader.Zip** can be constructed to open other file extensions that are of zip file format, such as .nupkg.
[!code-csharp[Snippet](Examples.cs#Snippet_5)]

# Links
* [Lexical.FileProvider.Zip](http://lexical.fi/FileProvider/docs/Zip/index.html) ([NuGet](https://www.nuget.org/packages/Lexical.FileProvider.Zip/))
 * [ZipFileProvider](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.Zip/ZipFileProvider.cs)
 * [ZipPackageLoader](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.Zip/ZipPackageLoader.cs)
* [Lexical.FileProvider.Package.Abstractions](http://lexical.fi/FileProvider/docs/Package.Abstractions/index.html) ([NuGet](https://www.nuget.org/packages/Lexical.FileProvider.Package.Abstractions/))
 * [IPackageLoader](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.Package.Abstractions/IPackageLoader.cs)
* [Microsoft.Extensions.FileProviders.Abstractions](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/file-providers) ([NuGet](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Abstractions/))
 * [IFileProvider](https://github.com/aspnet/Extensions/blob/master/src/FileProviders/Abstractions/src/IFileProvider.cs)
