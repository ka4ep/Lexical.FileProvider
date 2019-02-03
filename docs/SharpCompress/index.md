---
uid: Lexical.FileProvider.SharpCompress
---
# File Provider
**Lexical.FileProviders.SharpCompress** is a class library that wraps [SharpCompress](https://github.com/adamhathcock/sharpcompress) into IFileProvider.

The **new _ZipFileProvider(*string*)** constructs a file provider that reads .zip contents. 
Other classes are **RarFileProvider**, **_7zFileProvider**, **TarFileProvider** and **GZipFileProvider**.
Content can be read concurrently as file provider opens new file handles as needed.
[!code-csharp[Snippet](Examples.cs#Snippet_1)]

Another constructor argument is **new _ZipFileProvider(*Stream*)**. Note however, 
that unlike file based constructor, this stream based constructor has only one file pointer, content can be accessed only by one thread at a time. Concurrent threads have to wait.
[!code-csharp[Snippet](Examples.cs#Snippet_2)]

File provider must be disposed after use.
[!code-csharp[Snippet](Examples.cs#Snippet_3)]

Belated disposing can be added to the file provider. The disposable will be disposed once the fileprovider and all its streams are closed.
[!code-csharp[Snippet](Examples.cs#Snippet_10)]

# Package Loader
**Lexical.FileProvider.PackageLoader._Zip**, **.Rar**, **._7z**, **.Tar** and **.GZip** 
can be used as a component of *PackageFileLoader*, making it possible to drill down into archive files recursively.
[!code-csharp[Snippet](Examples.cs#Snippet_4)]

# Links
* [Lexical.FileProvider.SharpCompress](http://lexical.fi/FileProvider/docs/SharpCompress/index.html) ([NuGet](https://www.nuget.org/packages/Lexical.FileProvider.SharpCompress/), [Git](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.SharpCompress/))
* [Lexical.FileProvider.Package.Abstractions](http://lexical.fi/FileProvider/docs/Package.Abstractions/index.html) ([NuGet](https://www.nuget.org/packages/Lexical.FileProvider.Package.Abstractions/))
 * [IPackageLoader](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.Package.Abstractions/IPackageLoader.cs)
* [Microsoft.Extensions.FileProviders.Abstractions](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/file-providers) ([NuGet](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Abstractions/))
 * [IFileProvider](https://github.com/aspnet/Extensions/blob/master/src/FileProviders/Abstractions/src/IFileProvider.cs)
* [SharpCompress](https://github.com/adamhathcock/sharpcompress)
