# File Provider
**Lexical.FileProviders.SharpCompress** is a class library that wraps [SharpCompress](https://github.com/adamhathcock/sharpcompress) into IFileProvider.

The **new _ZipFileProvider(*string*)** constructs a file provider that reads .zip contents. 
Other classes are **RarFileProvider**, **_7zFileProvider**, **TarFileProvider** and **GZipFileProvider**.
Content can be read concurrently as file provider opens new file handles as needed.

```csharp
_ZipFileProvider fileProvider_zip = new _ZipFileProvider("mydata.zip");
RarFileProvider fileProvider_rar = new RarFileProvider("mydata.rar");
TarFileProvider fileProvider_tar = new TarFileProvider("mydata.tar");
_7zFileProvider fileProvider_7z = new _7zFileProvider("mydata.7z");
GZipFileProvider fileProvider_gz = new GZipFileProvider("mydata.tar.gz", "mydata.tar");
```

Another constructor argument is **new _ZipFileProvider(*Stream*)**. Note however, 
that unlike file based constructor, this stream based constructor has only one file pointer, content can be accessed only by one thread at a time. Concurrent threads have to wait.

```csharp
// Open some stream
Stream stream = new FileStream("mydata.zip", FileMode.Open);

// Use stream as zip file.
_ZipFileProvider fileProvider = new _ZipFileProvider(stream).AddDisposable(stream);
```

File provider must be disposed after use.

```csharp
fileProvider.Dispose();
```

Belated disposing can be added to the file provider. The disposable will be disposed once the fileprovider and all its streams are closed.

```csharp
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
```

# Package Loader
**Lexical.FileProvider.PackageLoader._Zip**, **.Rar**, **._7z**, **.Tar** and **.GZip** 
can be used as a component of *PackageFileLoader*, making it possible to drill down into archive files recursively.

```csharp
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
```

# Links
* [Lexical.FileProvider.SharpCompress](http://lexical.fi/FileProvider/docs/SharpCompress/index.html) ([NuGet](https://www.nuget.org/packages/Lexical.FileProvider.SharpCompress/), [Git](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.SharpCompress/))
* [Lexical.FileProvider.Package.Abstractions](http://lexical.fi/FileProvider/docs/Package.Abstractions/index.html) ([NuGet](https://www.nuget.org/packages/Lexical.FileProvider.Package.Abstractions/))
 * [IPackageLoader](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.Package.Abstractions/IPackageLoader.cs)
* [Microsoft.Extensions.FileProviders.Abstractions](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/file-providers) ([NuGet](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Abstractions/))
 * [IFileProvider](https://github.com/aspnet/Extensions/blob/master/src/FileProviders/Abstractions/src/IFileProvider.cs)
* [SharpCompress](https://github.com/adamhathcock/sharpcompress)
