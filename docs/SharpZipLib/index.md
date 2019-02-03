---
uid: Lexical.FileProvider.SharpZipLib
---
# File Provider
**Lexical.FileProviders.SharpZipLib** is a class library that wraps [SharpZipLib](https://github.com/icsharpcode/SharpZipLib) into IFileProvider.

The **new BZip2FileProvider(*string*, *string*)** constructs a file provider that reads .bzip2 content. 
Other class is **LzwFileProvider**.
Content can be read concurrently as file provider opens new file handles as needed.
[!code-csharp[Snippet](Examples.cs#Snippet_1)]

Another constructor variant is **new BZip2FileProvider(*byte[]*, *string*)**. 
[!code-csharp[Snippet](Examples.cs#Snippet_2)]

File provider must be disposed after use.
[!code-csharp[Snippet](Examples.cs#Snippet_3)]

Belated disposing can be added to the file provider. The disposable will be disposed once the fileprovider and all its streams are closed.
[!code-csharp[Snippet](Examples.cs#Snippet_10)]

# Package Loader
**Lexical.FileProvider.PackageLoader.BZip2** and **.Lzw**
can be used as a component of *PackageFileLoader*, making it possible to drill down into archive files recursively.
[!code-csharp[Snippet](Examples.cs#Snippet_4)]

# Links
* [Lexical.FileProvider.SharpZipLib](http://lexical.fi/FileProvider/docs/SharpZipLib/index.html) ([NuGet](https://www.nuget.org/packages/Lexical.FileProvider.SharpZipLib/), [Git](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.SharpZipLib/))
* [Lexical.FileProvider.Package.Abstractions](http://lexical.fi/FileProvider/docs/Package.Abstractions/index.html) ([NuGet](https://www.nuget.org/packages/Lexical.FileProvider.Package.Abstractions/))
 * [IPackageLoader](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.Package.Abstractions/IPackageLoader.cs)
* [Microsoft.Extensions.FileProviders.Abstractions](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/file-providers) ([NuGet](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Abstractions/))
 * [IFileProvider](https://github.com/aspnet/Extensions/blob/master/src/FileProviders/Abstractions/src/IFileProvider.cs)
* [SharpZipLib](https://github.com/icsharpcode/SharpZipLib)
