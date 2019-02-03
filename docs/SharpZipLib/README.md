# File Provider
**Lexical.FileProviders.SharpZipLib** is a class library that wraps [SharpZipLib](https://github.com/icsharpcode/SharpZipLib) into IFileProvider.

The **new BZip2FileProvider(*string*, *string*)** constructs a file provider that reads .bzip2 content. 
Other class is **LzwFileProvider**.
Content can be read concurrently as file provider opens new file handles as needed.

```csharp
BZip2FileProvider fileProvider_bzip2 = new BZip2FileProvider("mydata.tar.bzip2", "mydata.tar");
LzwFileProvider fileProvider_lzw = new LzwFileProvider("mydata.tar.Z", "mydata.tar");
```

Another constructor variant is **new BZip2FileProvider(*byte[]*, *string*)**. 

```csharp
// Read data
byte[] data;
using (Stream stream = new FileStream("mydata.tar.bzip2", FileMode.Open))
    data = FileUtils.ReadFully(stream);

// Use stream as zip file.
BZip2FileProvider fileProvider = new BZip2FileProvider(data, "mydata.tar");
```

File provider must be disposed after use.

```csharp
fileProvider.Dispose();
```

Belated disposing can be added to the file provider. The disposable will be disposed once the fileprovider and all its streams are closed.

```csharp
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
```

# Package Loader
**Lexical.FileProvider.PackageLoader.BZip2** and **.Lzw**
can be used as a component of *PackageFileLoader*, making it possible to drill down into archive files recursively.

```csharp
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
```

# Links
* [Lexical.FileProvider.SharpZipLib](http://lexical.fi/FileProvider/docs/SharpZipLib/index.html) ([NuGet](https://www.nuget.org/packages/Lexical.FileProvider.SharpZipLib/), [Git](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.SharpZipLib/))
* [Lexical.FileProvider.Package.Abstractions](http://lexical.fi/FileProvider/docs/Package.Abstractions/index.html) ([NuGet](https://www.nuget.org/packages/Lexical.FileProvider.Package.Abstractions/))
 * [IPackageLoader](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.Package.Abstractions/IPackageLoader.cs)
* [Microsoft.Extensions.FileProviders.Abstractions](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/file-providers) ([NuGet](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Abstractions/))
 * [IFileProvider](https://github.com/aspnet/Extensions/blob/master/src/FileProviders/Abstractions/src/IFileProvider.cs)
* [SharpZipLib](https://github.com/icsharpcode/SharpZipLib)
