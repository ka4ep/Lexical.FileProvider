---
uid: Lexical.FileProvider.Package.Abstractions
---
# IPackageLoader
*Lexical.FileProviders.Package.Abstractions* is a class library that contains interfaces for implementing package loaders for the *IPackageFileProvider*.
*IPackageLoader* interface is divided into multiple interfaces.

<details>
  <summary><b>IPackageLoader</b> is the interface classes that load package files into file providers. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../Lexical.FileProvider.Package.Abstractions/IPackageLoader.cs#IPackageLoader)]
</details>

<details>
  <summary><b>IPackageLoaderOpenFileCapability</b> is an interface for reading content from open files. (<u>Click here</u>)</summary>
[!code-csharp[Snippet_2](../../Lexical.FileProvider.Package.Abstractions/IPackageLoader.cs#IPackageLoaderOpenFileCapability)]
</details>

<details>
  <summary><b>IPackageLoaderLoadFileCapability</b> is an interface for loading from files. (<u>Click here</u>)</summary>
[!code-csharp[Snippet_3](../../Lexical.FileProvider.Package.Abstractions/IPackageLoader.cs#IPackageLoaderLoadFileCapability)]
</details>

<details>
  <summary><b>IPackageLoaderUseStreamCapability</b> is an interface for using stream as open content source. (<u>Click here</u>)</summary>
[!code-csharp[Snippet_4](../../Lexical.FileProvider.Package.Abstractions/IPackageLoader.cs#IPackageLoaderUseStreamCapability)]
</details>

<details>
  <summary><b>IPackageLoaderLoadFromStreamCapability</b> is an interface for loading content from stream. (<u>Click here</u>)</summary>
[!code-csharp[Snippet_5](../../Lexical.FileProvider.Package.Abstractions/IPackageLoader.cs#IPackageLoaderLoadFromStreamCapability)]
</details>

<details>
  <summary><b>IPackageLoaderUseBytesCapability</b> is an interface for loading content from stream. (<u>Click here</u>)</summary>
[!code-csharp[Snippet_5](../../Lexical.FileProvider.Package.Abstractions/IPackageLoader.cs#IPackageLoaderUseBytesCapability)]
</details>

<details>
  <summary><b>IPackageLoadInfo</b> is an interface for loading content from stream. (<u>Click here</u>)</summary>
[!code-csharp[Snippet_5](../../Lexical.FileProvider.Package.Abstractions/IPackageLoader.cs#IPackageLoadInfo)]
</details>

# ITempFileProvider
Temp file provider is attached to package file provider options.

[!code-csharp[Snippet](Examples.cs#Snippet_2)]

<details>
  <summary><b>ITempProvider</b> is an interface for creating temp files. (<u>Click here</u>)</summary>
[!code-csharp[Snippet_6](../../Lexical.FileProvider.Package.Abstractions/ITempFileProvider.cs#interfaces)]
</details>

# Links
* [Lexical.FileProvider.Package.Abstractions](http://lexical.fi/FileProvider/docs/Package.Abstractions/index.html) ([NuGet](https://www.nuget.org/packages/Lexical.FileProvider.Package.Abstractions/))
 * [IPackageLoader](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.Package.Abstractions/IPackageLoader.cs)
 * [ITempFileProvider](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.Package.Abstractions/ITempFileProvider.cs)
* [Microsoft.Extensions.FileProviders.Abstractions](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/file-providers) ([NuGet](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Abstractions/))
 * [IFileProvider](https://github.com/aspnet/Extensions/blob/master/src/FileProviders/Abstractions/src/IFileProvider.cs)

