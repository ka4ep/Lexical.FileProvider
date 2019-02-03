# File Provider
**new ZipFileProvider(*string*)** constructs a file provider that reads .zip contents. 
Content can be read concurrently as file provider opens new file handles as needed.

```csharp
ZipFileProvider fileProvider = new ZipFileProvider("mydata.zip");
```

Another constructor is **new ZipFileProvider(*Stream*)**. Note however, that as stream
has only one file pointer, the content can be accessed only by one thread at a time. Concurrent threads have to wait.

```csharp
// Open some stream
Stream stream = new FileStream("mydata.zip", FileMode.Open);

// Use stream as zip file.
ZipFileProvider fileProvider = new ZipFileProvider(stream).AddDisposable(stream);
```

File provider must be disposed after use.

```csharp
fileProvider.Dispose();
```

Belated dispose can be added to the file provider. They are disposed once the fileprovider is disposed and all its streams.

```csharp
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
```

# Package Loader
**Lexical.FileProvider.PackageLoader.Zip** can be used as a component of *PackageFileLoader*, making possible to drill down into .zip files.

```csharp
// Create root file provider
PhysicalFileProvider root = new PhysicalFileProvider(Directory.GetCurrentDirectory());

// Create package options
IPackageFileProviderOptions options =
    new PackageFileProviderOptions()
    .AddPackageLoaders(Lexical.FileProvider.PackageLoader.Zip.Singleton);

// Create package file provider
IPackageFileProvider fileProvider = new PackageFileProvider(root, options).AddDisposable(root);
```

**Lexical.FileProvider.PackageLoader.Zip** can be constructed to open other file extensions that are of zip file format, such as .nupkg.

```csharp
IPackageFileProviderOptions options = new PackageFileProviderOptions()
    .AddPackageLoaders( new Lexical.FileProvider.PackageLoader.Zip("\\.nupkg") );
```

# Links
* [Lexical.FileProvider.Zip](http://lexical.fi/FileProvider/docs/Zip/index.html) ([NuGet](https://www.nuget.org/packages/Lexical.FileProvider.Zip/))
 * [ZipFileProvider](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.Zip/ZipFileProvider.cs)
 * [ZipPackageLoader](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.Zip/ZipPackageLoader.cs)
* [Lexical.FileProvider.Package.Abstractions](http://lexical.fi/FileProvider/docs/Package.Abstractions/index.html) ([NuGet](https://www.nuget.org/packages/Lexical.FileProvider.Package.Abstractions/))
 * [IPackageLoader](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.Package.Abstractions/IPackageLoader.cs)
* [Microsoft.Extensions.FileProviders.Abstractions](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/file-providers) ([NuGet](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Abstractions/))
 * [IFileProvider](https://github.com/aspnet/Extensions/blob/master/src/FileProviders/Abstractions/src/IFileProvider.cs)
