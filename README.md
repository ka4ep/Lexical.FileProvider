# Links
* Lexical.FileProvider.Package ([Web](http://lexical.fi/FileProvider/docs/Package/index.html), [NuGet](https://www.nuget.org/packages/Lexical.FileProvider/), [Git](https://github.com/tagcode/Lexical.FileProvider/tree/master/Lexical.FileProvider/Package))
* Lexical.FileProvider.Package.Abstractions ([Web](http://lexical.fi/FileProvider/docs/Package.Abstractions/index.html), [NuGet](https://www.nuget.org/packages/Lexical.FileProvider.Abstractions/), [Git](https://github.com/tagcode/Lexical.FileProvider/tree/master/Lexical.FileProvider.Abstractions/Package))
* Lexical.FileProvider.Zip ([Web](http://lexical.fi/FileProvider/docs/Zip/index.html), [NuGet](https://www.nuget.org/packages/Lexical.FileProvider/), [Git](https://github.com/tagcode/Lexical.FileProvider/tree/master/Lexical.FileProvider/Zip))
* Lexical.FileProvider.SharpCompress ([Web](http://lexical.fi/FileProvider/docs/SharpCompress/index.html), [NuGet](https://www.nuget.org/packages/Lexical.FileProvider.SharpCompress/), [Git](https://github.com/tagcode/Lexical.FileProvider/tree/master/Lexical.FileProvider.SharpCompress))
* Lexical.FileProvider.SharpZipLib ([Web](http://lexical.fi/FileProvider/docs/SharpZipLib/index.html), [NuGet](https://www.nuget.org/packages/Lexical.FileProvider.SharpZipLib/), [Git](https://github.com/tagcode/Lexical.FileProvider/tree/master/Lexical.FileProvider.SharpZipLib))
* Lexical.FileProvider.Dll ([Web](http://lexical.fi/FileProvider/docs/Dll/index.html), [NuGet](https://www.nuget.org/packages/Lexical.FileProvider.Dll/), [Git](https://github.com/tagcode/Lexical.FileProvider/tree/master/Lexical.FileProvider.Dll))
* Lexical.FileProvider.Root ([Web](http://lexical.fi/FileProvider/docs/Root/index.html), [NuGet](), [Git](https://github.com/tagcode/Lexical.FileProvider/tree/master/Lexical.FileProvider/Root))
* Lexical.FileProvider.Utils ([Web](http://lexical.fi/FileProvider/docs/Utils/index.html), [NuGet](https://www.nuget.org/packages/Lexical.FileProvider/), [Git](https://github.com/tagcode/Lexical.FileProvider/tree/master/Lexical.FileProvider/Utils))

# Introduction
**PackageFileProvider** is a file provider that can open different package file formats, such as .zip and .dll.

```csharp
// Create root file provider
RootFileProvider root = new RootFileProvider();

// Create package options
IPackageFileProviderOptions options = 
    new PackageFileProviderOptions()
    .SetAllowOpenFiles(false)
    .AddPackageLoaders(
        Lexical.FileProvider.PackageLoader.Dll.Singleton,
        Lexical.FileProvider.PackageLoader.Exe.Singleton,
        Lexical.FileProvider.PackageLoader.Zip.Singleton,
        Lexical.FileProvider.PackageLoader._Zip.Singleton,
        Lexical.FileProvider.PackageLoader.Rar.Singleton,
        Lexical.FileProvider.PackageLoader._7z.Singleton,
        Lexical.FileProvider.PackageLoader.Tar.Singleton,
        Lexical.FileProvider.PackageLoader.GZip.Singleton,
        Lexical.FileProvider.PackageLoader.BZip2.Singleton,
        Lexical.FileProvider.PackageLoader.Lzw.Singleton
    );

// Create package file provider
PackageFileProvider fileProvider = new PackageFileProvider(root, options);
```

Packages are opened as folders.

```none
mydata.zip/
mydata.zip/Folder/
mydata.zip/Folder/data.zip/
mydata.zip/Folder/data.zip/Lexical.Localization.Tests.dll/
mydata.zip/Folder/data.zip/Lexical.Localization.Tests.dll/Lexical.Localization.Tests.localization.ini
mydata.zip/Folder/data.zip/Lexical.Localization.Tests.dll/Lexical.Localization.Tests.localization.json
...
```

List package contents. Extension method in Lexical.FileProvider.Utils **.ListAllFileInfoAndPath()** visits IFileInfo and corresponding paths recursively.

```csharp
string path = Directory.GetCurrentDirectory();

foreach ((IFileInfo, String) pair in fileProvider.ListAllFileInfoAndPath(path).OrderBy(pair => pair.Item2))
    Console.WriteLine(pair.Item2 + (pair.Item1.IsDirectory ? "/" : ""));
```

# Disposing
File provider must be disposed after use. The root provider too.

```csharp
fileProvider.Dispose();
root.Dispose();
```

Root file provider can be attached to be disposed along with the *PackageFileProvider*. 

```csharp
// Create root
IFileProvider root = new RootFileProvider();
// Create package provider and attach root
PackageFileProvider fileProvider = new PackageFileProvider(root).AddDisposable(root);
// Disposes both fileProvider and its root
fileProvider.Dispose();
```

Disposable can be attached with a delegate **Func&lt;PackageFileProvider, object&gt;**.

```csharp
// Create package provider and attach root to be disposed
PackageFileProvider fileProvider = new PackageFileProvider( new RootFileProvider() ).AddDisposable( p=>p.FileProvider );
```

# Options
Reference to options is given at construction.

```csharp
PackageFileProviderOptions options = new PackageFileProviderOptions();
PackageFileProvider fileProvider = new PackageFileProvider(root, options);
```

Or, if options are not provided, then *PackageFileProvider* creates new a with default values.

```csharp
PackageFileProvider fileProvider = new PackageFileProvider(root);
IPackageFileProviderOptions options = fileProvider.Options;
```

Options must be populated with instances of **IPackageLoader**. 
These handle how different file formats are opened.
Package loader creates new **IFileProvider** instances as needed.

```csharp
PackageFileProvider fileProvider = new PackageFileProvider(root);
fileProvider.Options.AddPackageLoaders(
        Lexical.FileProvider.PackageLoader.Dll.Singleton,
        Lexical.FileProvider.PackageLoader.Zip.Singleton
);
```

If one-liner is needed, the newly created options can be configured in a lambda function **.ConfigureOptions(o=>o.*...*)**.

```csharp
PackageFileProvider fileProvider = new PackageFileProvider(root)
    .ConfigureOptions(o => o.AddPackageLoaders(Dll.Singleton, Zip.Singleton));
```

**.AsReadonly()** locks the options into an immutable read-only state.

```csharp
IPackageFileProviderOptions shared_options =
    new PackageFileProviderOptions()
    .AddPackageLoaders(Lexical.FileProvider.PackageLoader.Zip.Singleton)
    .AsReadonly();
```

# Package loading
There are four ways how packages are loaded:
1. Reading from an open file.
2. Streaming from parent file provider with an open stream.
3. Taking a snapshot copy into memory.
4. Taking a snapshot copy into temp file.

**Options.SetMemorySnapshotLength(*int*)** sets the maximum size of a memory snapshot. 
If package file is smaller or equal to this number, then the file can be loaded into a memory snapshot.
If package file is larger, then the package will not be loaded into memory. 
If this value is set to 0, then no packages are loaded into memory.

```csharp
fileProvider.Options.SetMemorySnapshotLength(1048576);
```

**.SetAllowOpenFiles(*bool*)** configures the policy of whether it is allowed to keep open file handles.
If this value is false, package files are always copied into snapshots, either to memory or to temp file.

```csharp
fileProvider.Options.SetAllowOpenFiles(true);
```

**.SetReuseFailedResult(*bool*)** configures the policy of whether it is allowed to reuse failed load result.
If the policy is true and load has failed, then the error is remembered and reused if package is accessed again.
If false, then reload is attempted on every load.
Failed load result can be evicted just as successful load results.

```csharp
fileProvider.Options.SetReuseFailedResult(true);
```

**.SetErrorHandler(*Func&lt;PackageEvent, bool&gt;*)** configures an overriding exception handler. 
This is used for considering whether an exception is expectable, such as problems with file formats.
When this handler returns true, then the exception is suppressed (not thrown). 
When false, then the exception is let to be thrown to caller.

```csharp
fileProvider.Options.SetErrorHandler( pe => pe.LoadError is PackageException.LoadError );
```

# Temp File Provider
**ITempFileProvider** must be assigned to utilize temp files. Some package loaders need them to work properly.
**TempFileProvider.Default** is a singleton instance that uses the current user's default temp folder.

```csharp
fileProvider.SetTempFileProvider(TempFileProvider.Default);
```

**TempFileProvider** is constructed with custom **TempFileOptions** for more detailed behaviour.

```csharp
// Create temp options
TempFileProviderOptions tempFileOptions = new TempFileProviderOptions {
    Directory = "%tmp%",
    Prefix = "package-",
    Suffix = ".tmp"
};
// Create temp file provider
ITempFileProvider tempProvider = new TempFileProvider(tempFileOptions);
// Assign temp file provider
fileProvider
    .SetTempFileProvider(tempProvider)
    .AddDisposable(tempProvider);
```

**Options.SetTempFileSnapshotLength(*long*)** sets the maximum size of a temp file snapshot. 
If this value is set to 0, then package file provider will not take any temp file copies.

```csharp
fileProvider.Options.SetTempFileSnapshotLength(1073741824);
```

# Observing
**.Subscribe(*IObserver&lt;PackageEvent&gt;*)** subscribes for package loading, error and evicting events.

```csharp
IDisposable handle = fileProvider.Subscribe(new MyObserver());
```
Observable receives notifications.

```csharp
class MyObserver : IObserver<PackageEvent>
{
    public void OnCompleted()
        => Console.WriteLine("End of subscription");

    public void OnError(Exception error)
        => Console.WriteLine(error);

    public void OnNext(PackageEvent value)
        => Console.WriteLine(value);
}
```
Subsription is canceled by disposing the handle.

```csharp
handle.Dispose();
```

# Logging
**.AddLogger(*ILogger*)** adds an *ILogger* as observable. 
It writes log entries about loading and eviciting of packages.

```csharp
IServiceCollection serviceCollection = new ServiceCollection().AddLogging(builder => builder.AddConsole());
ILogger logger = serviceCollection.BuildServiceProvider().GetService<ILogger<PackageFileProvider>>();
fileProvider.AddLogger(logger);
```

Looks like this.

```none
info: Lexical.FileProvider.Package.PackageFileProvider[0]
      Folder/mydata.zip/Lexical.Localization.Tests.dll was loaded.
info: Lexical.FileProvider.Package.PackageFileProvider[0]
      Folder/mydata.zip/Lexical.Localization.Tests.dll was evicted.
```

# Evicting
**.GetPackageInfos()** and **.GetPackageInfo(*string*)** gives info about cached packages.

```csharp
// Open cascading packages
fileProvider
    .GetFileInfo("mydata.zip/Folder/mydata.zip/Lexical.Localization.Tests.dll/Lexical.Localization.Tests.localization.ini")
    .CreateReadStream()
    .Dispose();

// List all open packages
foreach (PackageInfo pi in fileProvider.GetPackageInfos())
    Console.WriteLine($"{pi.FilePath} {pi.State}");

// Specific package
PackageInfo _pi = fileProvider.GetPackageInfo("mydata.zip/Folder/mydata.zip");
```

Cached packages can be evicted manually with **.Evict(*string*)**. Evict will fail if there is an open stream to a package.

```csharp
fileProvider.Evict("mydata.zip/Folder/mydata.zip/Lexical.Localization.Tests.dll");
fileProvider.Evict("mydata.zip/Folder/mydata.zip");
fileProvider.Evict("mydata.zip");
```

**.EvictAll()** releases all non-locked cached packages from memory and temp files.

```csharp
fileProvider.EvictAll();
```

**.StartEvictTimer(*TimeSpan*, *TimeSpan*)** starts a timer that checks periodically if package hasn't been accessed for a while, and evicts them.

```csharp
fileProvider.StartEvictTimer(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));
```

# Dependency Injection
There are many ways how a DI container can be put together. 
The following example demonstrates how configuration is read from *config.json*. 
Modifications are reloaded and will be forwarded to *PackageFileProvider* and *TempFileProvider* instances. 

```csharp
ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
// Add config.json
configurationBuilder
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("config.json", optional: false, reloadOnChange: true);
// Build config
IConfiguration configuration = configurationBuilder.Build();

//// Start DI Configuration
IServiceCollection serviceCollection = new ServiceCollection();
// Add the configuration that has already been loaded
serviceCollection.AddSingleton<IConfiguration>(configuration);
// Add IOptions support
serviceCollection.AddOptions();

// Add logger
serviceCollection.AddLogging(builder => builder.AddConsole());
```

*ITempFileProvider* is initialized to read configuration from "TempFiles" section into monitorable *TempFileProviderOptions* poco class.
*TempFileProviderOptionsMonitor* is a workaround that allows to not expose IOptionsMonitor to the implementing class.

```csharp
// Add instantiation of TempFileProvider
serviceCollection.AddTransient<ITempFileProvider, TempFileProvider>();
// Bind configuration section to IOptions<TempFileProviderOptions>
serviceCollection.Configure<TempFileProviderOptions>(configuration.GetSection("TempFiles"));
// Workaround, convert IOptionsMonitor<TempFileProviderOptions> to TempFileProviderOptions.
serviceCollection.AddTransient<TempFileProviderOptions, TempFileProviderOptionsMonitor>();
// Register Options validator
serviceCollection.AddSingleton(TempFileProviderOptionsValidator.Singleton);
```

*PackageFileProvider* is registered as a service of *IFileProvider*.
Configurations are read from node "PackageFileProvider" into record class *PackageFileProviderOptionsRecord*.
*PackageFileProviderOptionsMonitor* is a class that converts package loader type strings into instances of *IPackageLoader*.
*PackageFileProviderOptionsValidator* validates every field of the options record.
*RootFileProvider* is used in this example as the source file provider.

```csharp
// Bind configuration section to IOptions<PackageFileProviderOptionsRecord>
serviceCollection.Configure<PackageFileProviderOptionsRecord>(configuration.GetSection("PackageFileProvider"));
// Adapt IOptions<PackageFileProviderOptionsRecord> to IPackageFileProviderOptions
serviceCollection.AddTransient<IPackageFileProviderOptions, PackageFileProviderOptionsMonitor>();
// Register Options validator
serviceCollection.AddSingleton(PackageFileProviderOptionsValidator.Level2);
// Add root file provider at current dir
serviceCollection.AddSingleton<RootFileProvider>(new RootFileProvider());
// Add service PackageFileProvider as IFileProvider
serviceCollection.AddSingleton<IFileProvider>(s =>
   new PackageFileProvider(s.GetService<RootFileProvider>(), s.GetService<IPackageFileProviderOptions>(), s.GetService<ITempFileProvider>())
        .StartEvictTimer(s.GetService<IOptionsMonitor<PackageFileProviderOptionsRecord>>())
        .AddLogger(s.GetService<ILogger<PackageFileProvider>>())
);
```

Let's give it a go.

```csharp
using (var service = serviceCollection.BuildServiceProvider())
{
    // Get the singleton package file provider (don't dispose here)
    IFileProvider fp = service.GetService<IFileProvider>();

    // List all file paths
    foreach (string filepath in fp.ListAllPaths())
        Console.WriteLine(filepath);
}
```

The example *config.json* looks like this. 
Package loaders are assembly qualified type names. 
The second parameter is assembly name. It can be left out if the assemblies are already loaded to the AppDomain.

```json
{
  "PackageFileProvider": {
    "AllowOpenFiles": "True",
    "ReuseFailedResult": "True",
    "MaxMemorySnapshotLength": 1073741824,
    "MaxTempSnapshotLength": 1099511627776,
    "PackageLoaders": [
      "Lexical.FileProvider.PackageLoader.Dll, Lexical.FileProvider.Dll",
      "Lexical.FileProvider.PackageLoader.Exe, Lexical.FileProvider.Dll",
      "Lexical.FileProvider.PackageLoader.Zip, Lexical.FileProvider",
      "Lexical.FileProvider.PackageLoader.Rar, Lexical.FileProvider.SharpCompress",
      "Lexical.FileProvider.PackageLoader._7z, Lexical.FileProvider.SharpCompress",
      "Lexical.FileProvider.PackageLoader.Tar, Lexical.FileProvider.SharpCompress",
      "Lexical.FileProvider.PackageLoader.GZip, Lexical.FileProvider.SharpCompress",
      "Lexical.FileProvider.PackageLoader.BZip2, Lexical.FileProvider.SharpZipLib",
      "Lexical.FileProvider.PackageLoader.Lzw, Lexical.FileProvider.SharpZipLib"
    ],
    "CacheEvictTime": "15.0"
  },
  "TempFiles": {
    "Directory": "%tmp%",
    "Prefix": "package-",
    "Suffix": ".tmp"
  }
}

```

The full example.

# [Snippet](#tab/snippet)

```csharp
//// Start configuration
ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
// Add config.json
configurationBuilder.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("config.json", optional: false, reloadOnChange: true);
// Build config
IConfiguration configuration = configurationBuilder.Build();

//// Start DI Configuration
IServiceCollection serviceCollection = new ServiceCollection();
// Add the configuration that has already been loaded
serviceCollection.AddSingleton<IConfiguration>(configuration);
// Add IOptions support
serviceCollection.AddOptions();

//// Add logger
serviceCollection.AddLogging(builder => builder.AddConsole());

//// Configure Temp File Provider
// Add instantiation of TempFileProvider
serviceCollection.AddTransient<ITempFileProvider, TempFileProvider>();
// Bind configuration section to IOptions<TempFileProviderOptions>
serviceCollection.Configure<TempFileProviderOptions>(configuration.GetSection("TempFiles"));
// Workaround, convert IOptionsMonitor<TempFileProviderOptions> to TempFileProviderOptions.
serviceCollection.AddTransient<TempFileProviderOptions, TempFileProviderOptionsMonitor>();
// Register Options validator
serviceCollection.AddSingleton(TempFileProviderOptionsValidator.Singleton);

//// Configure PackageFileProvider
// Bind configuration section to IOptions<PackageFileProviderOptionsRecord>
serviceCollection.Configure<PackageFileProviderOptionsRecord>(configuration.GetSection("PackageFileProvider"));
// Adapt IOptions<PackageFileProviderOptionsRecord> to IPackageFileProviderOptions
serviceCollection.AddTransient<IPackageFileProviderOptions, PackageFileProviderOptionsMonitor>();
// Register Options validator
serviceCollection.AddSingleton(PackageFileProviderOptionsValidator.Level2);
// Add root file provider at current dir
serviceCollection.AddSingleton<RootFileProvider, RootFileProvider>();
// Add service PackageFileProvider as IFileProvider
serviceCollection.AddSingleton<IFileProvider>(s =>
   new PackageFileProvider(s.GetService<RootFileProvider>(), s.GetService<IPackageFileProviderOptions>(), s.GetService<ITempFileProvider>())
        .StartEvictTimer(s.GetService<IOptionsMonitor<PackageFileProviderOptionsRecord>>())
        .AddLogger(s.GetService<ILogger<PackageFileProvider>>())
);

//// Give it a go
using (var service = serviceCollection.BuildServiceProvider())
{
    // Get the singleton package file provider (don't dispose here)
    IFileProvider fp = service.GetService<IFileProvider>();

    // List all file paths
    foreach (string filepath in fp.ListAllPaths())
        Console.WriteLine(filepath);
}
```

