# IPackageLoader
*Lexical.FileProviders.Package.Abstractions* is a class library that contains interfaces for implementing package loaders for the *IPackageFileProvider*.
*IPackageLoader* interface is divided into multiple interfaces.

<details>
  <summary><b>IPackageLoader</b> is the interface classes that load package files into file providers. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// Interace for loaders that read in <see cref="IFileProvider"/>s.
/// 
/// Must implement one or more of the following sub-interfaces:
///    <see cref="IPackageLoaderOpenFileCapability"/>
///    <see cref="IPackageLoaderLoadFileCapability"/>
///    <see cref="IPackageLoaderUseStreamCapability"/>
///    <see cref="IPackageLoaderLoadFromStreamCapability"/>
///    <see cref="IPackageLoaderUseBytesCapability"/>
/// </summary>
public interface IPackageLoader
{
    /// <summary>
    /// The file extension(s) this format can open.
    /// 
    /// The string is a regular expression. 
    /// For example "\.zip" or "\.zip|\.7z|\tar.gz"
    /// 
    /// Pattern will be used as case insensitive, so the case doesn't matter, but lower is preferred.
    /// 
    /// Do not add named groups. For example "(?&lt;name&gt;..)".
    /// 
    /// Unnamed groups are, however, allowed. For example: "\.zip(\.tmp)?"
    /// </summary>
    String FileExtensionPattern { get; }
}
```
</details>

<details>
  <summary><b>IPackageLoaderOpenFileCapability</b> is an interface for reading content from open files. (<u>Click here</u>)</summary>

```csharp
public interface IPackageLoaderOpenFileCapability : IPackageLoader
{
    /// <summary>
    /// Create a <see cref="IFileProvider"/> that opens a file and is allowed to keep it open until the file provider is disposed. 
    /// 
    /// The caller is responsible for disposing the returned file provider if it implements <see cref="IDisposable"/>.
    /// </summary>
    /// <param name="filepath">data to read from</param>
    /// <param name="packageInfo">(optional) Information about packge that is being opened</param>
    /// <returns>file provider</returns>
    /// <exception cref="Exception">If there was unexpected error, such as IOException</exception>
    /// <exception cref="InvalidOperationException">If this load method is not supported.</exception>
    /// <exception cref="IOException">Problem with io stream</exception>
    /// <exception cref="PackageException.LoadError">The when file format is erronous, package will not be opened as directory.</exception>
    IFileProvider OpenFile(string filepath, IPackageLoadInfo packageInfo = null);
}
```
</details>

<details>
  <summary><b>IPackageLoaderLoadFileCapability</b> is an interface for loading from files. (<u>Click here</u>)</summary>

```csharp
public interface IPackageLoaderLoadFileCapability : IPackageLoader
{
    /// <summary>
    /// Loads <see cref="IFileProvider"/> completely from a file. 
    /// File must be closed when the call returns.
    /// 
    /// The caller is responsible for disposing the returned file provider if it implements <see cref="IDisposable"/>.
    /// </summary>
    /// <param name="filepath">data to read from</param>
    /// <param name="packageInfo">(optional) Information about packge that is being opened</param>
    /// <returns>file provider</returns>
    /// <exception cref="Exception">If there was unexpected error, such as IOException</exception>
    /// <exception cref="InvalidOperationException">If this load method is not supported.</exception>
    /// <exception cref="IOException">Problem with io stream</exception>
    /// <exception cref="PackageException.LoadError">The when file format is erronous, package will not be opened as directory.</exception>
    IFileProvider LoadFile(string filepath, IPackageLoadInfo packageInfo = null);
}
```
</details>

<details>
  <summary><b>IPackageLoaderUseStreamCapability</b> is an interface for using stream as open content source. (<u>Click here</u>)</summary>

```csharp
public interface IPackageLoaderUseStreamCapability : IPackageLoader
{
    /// <summary>
    /// Create a <see cref="IFileProvider"/> that reads its contents from an open <paramref name="stream"/>.
    /// File provider takes ownership of the stream, and closes the stream along with the provider.
    /// 
    /// The stream must be readable and seekable, <see cref="Stream.CanSeek"/> must be true.
    /// 
    /// The caller is responsible for disposing the returned file provider if it implements <see cref="IDisposable"/>.
    /// 
    /// Note, open stream cannot be read concurrently. 
    /// </summary>
    /// <param name="stream">stream to read data from. Stream must be disposed along with the returned file provider.</param>
    /// <param name="packageInfo">(optional) Information about packge that is being opened</param>
    /// <returns>file provider that can be disposable</returns>
    /// <exception cref="Exception">If there was unexpected error, such as IOException</exception>
    /// <exception cref="InvalidOperationException">If this load method is not supported.</exception>
    /// <exception cref="IOException">Problem with io stream</exception>
    /// <exception cref="PackageException.LoadError">The when file format is erronous, package will not be opened as directory.</exception>
    IFileProvider UseStream(Stream stream, IPackageLoadInfo packageInfo = null);
}
```
</details>

<details>
  <summary><b>IPackageLoaderLoadFromStreamCapability</b> is an interface for loading content from stream. (<u>Click here</u>)</summary>

```csharp
public interface IPackageLoaderLoadFromStreamCapability : IPackageLoader
{
    /// <summary>
    /// Create a <see cref="IFileProvider"/> that is completely read from a <paramref name="stream"/>.
    /// The callee does not take ownership of the stream. 
    /// 
    /// The returned file provider can be left to be garbage collected and doesn't need to be disposed.
    /// </summary>
    /// <param name="stream">stream to read data from. Stream doesn't need to be closed by callee, but is allowed to do so.</param>
    /// <param name="packageInfo">(optional) Information about packge that is being opened</param>
    /// <returns>file provider</returns>
    /// <exception cref="Exception">If there was unexpected error, such as IOException</exception>
    /// <exception cref="InvalidOperationException">If this load method is not supported.</exception>
    /// <exception cref="IOException">Problem with io stream</exception>
    /// <exception cref="PackageException.LoadError">The when file format is erronous, package will not be opened as directory.</exception>
    IFileProvider LoadFromStream(Stream stream, IPackageLoadInfo packageInfo = null);
}
```
</details>

<details>
  <summary><b>IPackageLoaderUseBytesCapability</b> is an interface for loading content from stream. (<u>Click here</u>)</summary>

```csharp
public interface IPackageLoaderUseBytesCapability : IPackageLoader
{
    /// <summary>
    /// Load file provider from bytes.
    /// 
    /// The caller is responsible for disposing the returned file provider if it implements <see cref="IDisposable"/>.
    /// </summary>
    /// <param name="data">data to read from</param>
    /// <param name="packageInfo">(optional) Information about packge that is being opened</param>
    /// <returns>file provider</returns>
    /// <exception cref="Exception">If there was unexpected error, such as IOException</exception>
    /// <exception cref="InvalidOperationException">If this load method is not supported.</exception>
    /// <exception cref="IOException">Problem with io stream</exception>
    /// <exception cref="PackageException.LoadError">The when file format is erronous, package will not be opened as directory.</exception>
    IFileProvider UseBytes(byte[] data, IPackageLoadInfo packageInfo = null);
}
```
</details>

<details>
  <summary><b>IPackageLoadInfo</b> is an interface for loading content from stream. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// Optional hints about the package that is being loaded.
/// </summary>
public interface IPackageLoadInfo
{
    /// <summary>
    /// (optional) Path within package file provider.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// (Optional) Last modified UTC date time.
    /// </summary>
    DateTimeOffset? LastModified { get; }

    /// <summary>
    /// File length, or -1 if unknown
    /// </summary>
    long Length { get; }
}
```
</details>

# ITempFileProvider
Temp file provider is attached to package file provider options.


```csharp
// Create temp options
TempFileProviderOptions tempFileOptions = new TempFileProviderOptions { Directory = "%tmp%", Prefix = "package-", Suffix = ".tmp" };

// Create temp provider
ITempFileProvider tempFileProvider = new TempFileProvider(tempFileOptions);

// Try to create temp file
using (var tempFile = tempFileProvider.CreateTempFile())
    Console.WriteLine(tempFile.Filename);

// Attach temp provider
fileProvider.SetTempFileProvider(tempFileProvider).Options.SetTempFileSnapshotLength(1073741824);
```

<details>
  <summary><b>ITempProvider</b> is an interface for creating temp files. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// Temporary file provider.
/// </summary>
public interface ITempFileProvider : IDisposable
{
    /// <summary>
    /// Create a new unique 0-bytes temp file that is not locked.
    /// </summary>
    /// <exception cref="IOException">if file creation failed</exception>
    /// <exception cref="ObjectDisposedException">if provider is disposed</exception>
    /// <returns>handle with a filename. Caller must dispose after use, which will delete the file if it still exists.</returns>
    ITempFileHandle CreateTempFile();
}

/// <summary>
/// A handle to a temp file name. 
/// 
/// Dispose the handle to delete it.
/// 
/// If temp file is locked, Dispose() throws an <see cref="IOException"/>.
/// 
/// Failed deletion will still be marked as to-be-deleted.
/// There is another delete attempt when the parent <see cref="ITempFileProvider"/> is disposed.
/// </summary>
public interface ITempFileHandle : IDisposable
{
    /// <summary>
    /// Filename to 0 bytes temp file.
    /// </summary>
    String Filename { get; }
}
```
</details>

# Links
* [Lexical.FileProvider.Package.Abstractions](http://lexical.fi/FileProvider/docs/Package.Abstractions/index.html) ([NuGet](https://www.nuget.org/packages/Lexical.FileProvider.Package.Abstractions/))
 * [IPackageLoader](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.Package.Abstractions/IPackageLoader.cs)
 * [ITempFileProvider](https://github.com/tagcode/Lexical.FileProvider/blob/master/Lexical.FileProvider.Package.Abstractions/ITempFileProvider.cs)
* [Microsoft.Extensions.FileProviders.Abstractions](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/file-providers) ([NuGet](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Abstractions/))
 * [IFileProvider](https://github.com/aspnet/Extensions/blob/master/src/FileProviders/Abstractions/src/IFileProvider.cs)

