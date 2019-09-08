// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           18.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileProvider.Common;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Lexical.FileProvider.Package
{
    /// <summary>
    /// File provider that can open files as packages.
    /// 
    /// The package separator character is always '/'.
    ///    For example: "folder/myfile.zip/folder/somelib.dll/somelib.resources"
    /// 
    /// If root provider is PhysicalFileProvider and is ran on windows, then path names can be ambiguous.
    /// For instance, a path with back-slashes "folder\myfile.zip" refers to same file as with slashes "folder/myfile.zip"
    /// 
    /// For caching purposes, the cache key must be canonized so that same entry is not opened multiple times. 
    /// In the canonized format the directory separator is '/'.
    /// 
    /// Note however that back-slashes are valid characters for filenames on linux and in packages. 
    /// For example, a linux compressed .tar.gz file can have files with back-slashes in the file name. 
    /// To be able to open these files, canonicalization is applied only to physical filenames, and on windows.
    /// </summary>
    public partial class PackageFileProvider : DisposeList, IDisposableFileProvider, IPackageFileProvider, IPackageProvider, IObservablePackageFileProvider//, IBelatedDisposeFileProvider, IBelatedDisposeList
    {
        /// <summary>
        /// The root file provider.
        /// </summary>
        public readonly IFileProvider FileProvider;

        /// <summary>
        /// Dictionary of packages.
        /// </summary>
        readonly ConcurrentDictionary<PackageFileReference, PackageEntry> packages = new ConcurrentDictionary<PackageFileReference, PackageEntry>();

        /// <summary>
        /// Pattern that captures package files names.
        /// </summary>
        internal Regex pattern;

        /// <summary>
        /// Reference that was the source of package loaders. Change in reference triggers reload. 
        /// </summary>
        internal IEnumerable<IPackageLoader> packageLoadersSource;

        /// <summary>
        /// Lazy getter of pattern.
        /// </summary>
        internal Regex Pattern => pattern!=null && packageLoadersSource==Options?.PackageLoaders ? pattern : (pattern = AssignPackageLoaders(validPackageLoaders));

        /// <summary>
        /// Options
        /// </summary>
        IPackageFileProviderOptions options;

        /// <summary>
        /// Package file provider options.
        /// Setting new options, wipes the cached pattern.
        /// </summary>
        public IPackageFileProviderOptions Options { get => options; set { pattern = null; this.options = value; } }

        /// <summary>
        /// Temp File PRovider
        /// </summary>
        ITempFileProvider tempFileProvider;

        /// <summary>
        /// Temp File PRovider
        /// </summary>
        public ITempFileProvider TempFileProvider { get => tempFileProvider; set => tempFileProvider = value; }

        /// <summary>
        /// Validated options property.
        /// </summary>
        /// <exception cref="InvalidOperationException">If Options is null</exception>
        IPackageFileProviderOptions validOptions => options ?? throw new InvalidOperationException(GetType().FullName + " must be configured with options");

        /// <summary>
        /// Validated package loaders property.
        /// </summary>
        /// <exception cref="InvalidOperationException">If Options or PackageLoaders is null</exception>
        IEnumerable<IPackageLoader> validPackageLoaders => options?.PackageLoaders ?? throw new InvalidOperationException(GetType().FullName + " must be configured with package loaders.");

        /// <summary>
        /// Set to true whether paths need canonicalization.
        /// </summary>
        bool needsCanonizalization;

        /*
        /// <summary>
        /// Dispose list for belated disposables.
        /// </summary>
        BelatedDisposeList belatedDisposeList;
        */

        /// <summary>
        /// Create new package file provider.
        /// </summary>
        /// <param name="rootFileProvider">root file provider</param>
        /// <param name="options">(optional) options, if null, default will be constructed with default values, but no package loaders</param>
        /// <param name="tempFileProvider">(optional) temp file provider. If null, then temp files are not used. Can be configured lated from property.</param>
        public PackageFileProvider(IFileProvider rootFileProvider, IPackageFileProviderOptions options = default, ITempFileProvider tempFileProvider = default)
        {
            this.FileProvider = rootFileProvider ?? throw new ArgumentException(nameof(rootFileProvider));
            this.options = options ?? new PackageFileProviderOptions();
            this.tempFileProvider = tempFileProvider;
            this.needsCanonizalization = IsPhysicalProvider(FileProvider) && Path.DirectorySeparatorChar != '/';
            this.unsubscribeObserver = observer => UnsubscribeObserver(observer);
            //this.belatedDisposeList = new BelatedDisposeList( (packageReference, a) => new PackageEntry((PackageFileReference)packageReference, a));
            //this.createEntry = packageReference => (PackageEntry)belatedDisposeList.Belate(packageReference);
            //this.updateEntry = (PackageFileReference packageReference, PackageEntry oldValue) => oldValue != null && oldValue.state != PackageState.Evicted ? oldValue : (PackageEntry)belatedDisposeList.Belate(packageReference);
        }

        /// <summary>
        /// Assign new source of package loaders. Creates new pattern.
        /// </summary>
        /// <param name="packageLoaders"></param>
        /// <returns></returns>
        Regex AssignPackageLoaders(IEnumerable<IPackageLoader> packageLoaders)
        {
            Regex rex = MakePattern(packageLoaders);
            packageLoadersSource = packageLoaders;
            return this.pattern = rex;
        }

        /// <summary>
        /// Make a pattern that separates package files from path.
        /// 
        /// For example, if input is "myfolder/packet.zip/somelib.dll/somelib.embedded.resources",
        /// then this pattern catches the following two matches "myfolder/packet.zip" and "somelib.dll".
        /// 
        /// 
        /// </summary>
        /// <param name="packageLoaders"></param>
        /// <returns></returns>
        public static Regex MakePattern(IEnumerable<IPackageLoader> packageLoaders)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(?<filename>.*?");
            sb.Append("(?<ext>");
            int i = 0;
            foreach (IPackageLoader loader in packageLoaders)
            {
                if (i > 0) sb.Append("|");
                i++;
                sb.Append("(?<");
                sb.Append(i);
                sb.Append(">");
                sb.Append(loader.FileExtensionPattern);
                sb.Append(")");
            }
            sb.Append(")");
            sb.Append(")(/|$)");
            string regexText = i == 0 ? "(?!)"/*no match*/ : sb.ToString();
            return new Regex(regexText, RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        Func<PackageFileReference, PackageEntry> createEntry = packageReference => new PackageEntry(packageReference, null);
        Func<PackageFileReference, PackageEntry, PackageEntry> updateEntry = (PackageFileReference packageReference, PackageEntry oldValue) => oldValue != null && oldValue.state != PackageState.Evicted ? oldValue : new PackageEntry(packageReference, null);
        PackageEntry GetOrCreateEntry(PackageFileReference packageReference) => packages.AddOrUpdate(packageReference, createEntry, updateEntry);

        /// <summary>
        /// Find which pattern loader can load a file by filename <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>package loader</returns>
        /// <exception cref="InvalidOperationException">if suitable package loader could not be found</exception>
        IPackageLoader GetPackageLoader(string filename)
        {
            Regex _pattern = Pattern;
            IEnumerable<IPackageLoader> _packageLoaders = packageLoadersSource;
            if (_packageLoaders == null) throw new InvalidOperationException($"No package loaders loaded.");
            Match match = _pattern.Match(filename);
            if (!match.Success) throw new InvalidOperationException($"Expected to get PackageLoader Pattern match to filename {filename}");

            // Find package loader
            IEnumerator<IPackageLoader> _packageLoaderEtor = _packageLoaders.GetEnumerator();
            GroupCollection groups = match.Groups;
            for (int i = 1; i < groups.Count; i++)
            {
                if (!_packageLoaderEtor.MoveNext()) throw new InvalidOperationException($"Expected to find more than {i - 1} package loaders");
                Group g = groups[i];
                if (g.Success) return _packageLoaderEtor.Current;
            }
            throw new InvalidOperationException($"Could not find package loader for filename {filename}");
        }

        /// <summary>
        /// Open a package <paramref name="packageReference"/>.
        /// 
        /// Returns a handle to file provider. File provider will not be evicted until the handle is disposed.
        /// 
        /// Returns null, if package failed to open because the error was expected and was suppressed by <see cref="IPackageFileProviderOptions.ErrorHandler"/>.
        /// </summary>
        /// <param name="packageReference">package refrence, if null then refers to root file provider</param>
        /// <returns>
        ///         a handle, to a fileprovider. The handle must be disposed.
        ///         null value, if package failed to open and the error was suppressed. That means that the file is not a package (wrong file format).
        /// </returns>
        /// <exception cref="ObjectDisposedException">if the opener was disposed</exception>
        /// <exception cref="Exception">any non-suppressed error that occured when the package was opened. This error can be of a previously cached open attempt. </exception>
        public IDisposableFileProvider TryOpenPackage(PackageFileReference packageReference)
        {
            // Test if object is being disposed            
            if (IsDisposing) throw new ObjectDisposedException(GetType().FullName);

            // If file reference, then get the package part of it
            packageReference = packageReference?.PackageReference;

            // Return root file provider.
            if (packageReference == null) return new FileProviderHandle(null, null, FileProvider);

            // Get-or-create entry
            PackageEntry entry = GetOrCreateEntry(packageReference);

            // See what we can do before write lock.
            Exception error;
            IFileProvider entry_fileProvider;
            PackageState state = entry.ReadState(out entry_fileProvider, out error);
            switch (state)
            {
                case PackageState.Evicted:
                    break;
                case PackageState.NotPackage:
                    // File was opened, but turned out that it wasn't a package
                    if (validOptions.ReuseFailedResult) return null;
                    // We'll retry again
                    break;
                case PackageState.Error:
                    // Opening the file produced an error
                    if (validOptions.ReuseFailedResult)
                        throw new Exception((error?.Message ?? "Failed to open package") + " " + packageReference.ToString(), error);
                    // Let's retry
                    break;
            }

            // Take reference from the entry to conserve heap objects
            packageReference = entry.packageReference;
            // Reference to parent package
            IDisposableFileProvider parentFileProvider = null;
            // Reference within parentFileProvider
            string filename = packageReference.Name;
            // Search which parent package can open the file.
            for (var _ref = packageReference; _ref != null; _ref = _ref.Parent)
            {
                parentFileProvider = TryOpenPackage(_ref.Parent);
                if (parentFileProvider != null)
                {
                    // Make name within its context (parent)
                    filename = _ref.Parent == null ? packageReference.CanonicalPath : packageReference.CanonicalPath.Substring(_ref.Parent.CanonicalPath.Length + 1);
                    break;
                }
            }            

            // One of the parent package files were not a package file
            if (parentFileProvider == null) return null;

            // Take references
            IPackageFileProviderOptions _options = validOptions;

            // Put event here
            PackageEvent packageEvent = null;

            // Reserve entry handle that prevents eviction
            FileProviderHandle entryHandle = null;
            do
            {
                // Is package file provider disposed, cancel operation
                if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

                // Try to create handle to entry
                if (!entry.IsDisposed && entry.state != PackageState.Evicted)
                    lock (entry.m_lock)
                    {
                        if (!entry.IsDisposed && entry.state != PackageState.Evicted)
                            try { entryHandle = entry.CreateHandle(); break; } catch (ObjectDisposedException) { }
                    }

                // Entry is disposed, create new
                PackageEntry newEntry = createEntry(packageReference);
                // Try to replace the old one.
                if (packages.TryUpdate(packageReference, newEntry, entry))
                {
                    // Replace entry
                    entry = newEntry;
                }
                else
                {
                    // Replace failed. Either another thread had replaced it at the same time, or it was removed.
                    // Is object being disposed
                    if (IsDisposing) throw new ObjectDisposedException(GetType().FullName);
                    // Try again
                    newEntry = GetOrCreateEntry(packageReference);
                    // Cannot replace and got same entry, shoudn't happen
                    if (entry == newEntry) throw new ObjectDisposedException(entry.GetType().FullName);
                    // Replace entry
                    entry = newEntry;
                }
            } while (entryHandle == null);

            lock (entry.m_lock)
            {
                // Update state
                state = entry.ReadState(out entry_fileProvider, out error);
                // Check state again, in lock                
                switch (entry.state)
                {
                    case PackageState.Opened:
                        // Package is open, return handle
                        if (entryHandle.fileProvider == entryHandle) return entryHandle;
                        // File provider changed, new handle
                        try
                        {
                            return entry.CreateHandle();
                        } finally
                        {
                            entryHandle.Dispose();
                        }
                    case PackageState.NotPackage:
                        // File was opened, but turned out that it wasn't a package
                        if (_options.ReuseFailedResult)
                        {
                            entryHandle.Dispose();
                            return null;
                        }
                        // We'll retry again
                        break;
                    case PackageState.Error:
                        // Opening the package produced an error in th epast
                        if (_options.ReuseFailedResult)
                        {
                            entryHandle.Dispose();
                            throw new Exception((error?.Message ?? "Failed to open package") + " " + packageReference.ToString(), error);
                        }
                        // Let's retry
                        break;
                }

                // Local field to put disposables
                List<IDisposable> disposables = null;
                try
                {
                    // Findout which package loader to use
                    IPackageLoader packageLoader = GetPackageLoader(packageReference.Name);
                    // Get file info
                    IFileInfo fileInfo = parentFileProvider.GetFileInfo(filename);
                    // Doesn't exist, or is not a file.
                    if (!fileInfo.Exists || fileInfo.IsDirectory) { entry.fp = null; entry.state = PackageState.NotPackage; return null; }
                    // Local field to put file provider
                    IFileProvider fileProvider = null;
                    // Local field to put length
                    long lengthEstimate = 0L;
                    // Try to open physical file
                    string physicalFile = fileInfo.PhysicalPath;
                    // Does policy allow opening file
                    bool allowOpenFile = _options.AllowOpenFiles;
                    // Open files consume some amount of memory too. This multiplier estimates the amount based on the file size. Very shady estimation.
                    double openFileCoefficient = 0.04;
                    // Info about package entry
                    PackageLoadInfo packageInfo = new PackageLoadInfo(packageReference.CanonicalPath, fileInfo.Length, fileInfo.LastModified);

                    // Read open physical file
                    if (fileProvider == null && allowOpenFile && physicalFile != null && File.Exists(physicalFile))
                    {
                        if (packageLoader is IPackageLoaderOpenFile fileOpener)
                        {
                            fileProvider = fileOpener.OpenFile(physicalFile, packageInfo);
                            lengthEstimate = (long)(packageInfo.Length * openFileCoefficient);
                        }
                        else if (packageLoader is IPackageLoaderUseStream streamUse_)
                        {
                            fileProvider = streamUse_.UseStream(new FileStream(physicalFile, FileMode.Open), packageInfo);
                            lengthEstimate = (long)(packageInfo.Length * openFileCoefficient);
                        }
                    }

                    // Try to read to memory snapshot
                    if (fileProvider == null && packageInfo.Length <= _options.MaxMemorySnapshotLength)
                    {
                        if (packageLoader is IPackageLoaderLoadFromStream streamLoader)
                        {
                            using (Stream s = fileInfo.CreateReadStream())
                            {
                                fileProvider = streamLoader.LoadFromStream(s, packageInfo);
                                lengthEstimate = packageInfo.Length;
                            }
                        }
                        else if (packageInfo.Length < Int32.MaxValue && packageLoader is IPackageLoaderUseBytes bytesLoader)
                        {
                            byte[] data = FileUtils.ReadMemorySnapshot(fileInfo);
                            fileProvider = bytesLoader.UseBytes(data, packageInfo);
                            lengthEstimate = data.Length;
                        }
                        else if (packageInfo.Length < Int32.MaxValue && packageLoader is IPackageLoaderUseStream streamUse__)
                        {
                            MemoryStream ms = new MemoryStream();
                            using (var s = fileInfo.CreateReadStream())
                                s.CopyTo(ms);
                            ms.Position = 0L;
                            fileProvider = streamUse__.UseStream(ms, packageInfo);
                        }
                    }

                    // Try to read from temp file snapshot
                    ITempFileProvider _tempProvider = TempFileProvider;
                    if (fileProvider == null && _tempProvider!=null && packageInfo.Length <= _options.MaxTempSnapshotLength &&
                        (packageLoader is IPackageLoaderOpenFile || packageLoader is IPackageLoaderUseStream) )
                    {
                        // Create new temp filename
                        ITempFileHandle tempFileHandle = _tempProvider.CreateTempFile();
                        // Make sure it's disposed eventually
                        (disposables ?? (disposables = new List<IDisposable>())).Add(tempFileHandle);
                        // Copy to temp file
                        FileStream fs = FileUtils.CopyToFile(fileInfo, tempFileHandle.Filename);
                        // Create file provider
                        if (packageLoader is IPackageLoaderOpenFile fileOpener)
                        {
                            // Close file
                            fs.Close();
                            fs.Dispose();
                            // Open with file provider
                            fileProvider = fileOpener.OpenFile(tempFileHandle.Filename, packageInfo);
                            lengthEstimate = (long)(fileInfo.Length * openFileCoefficient);
                        }
                        else if (packageLoader is IPackageLoaderUseStream streamUse____)
                        {
                            fileProvider = streamUse____.UseStream(fs, packageInfo);
                            if (fs is IDisposable disposable_) (disposables ?? (disposables = new List<IDisposable>())).Add(disposable_);
                            lengthEstimate = (long)(fileInfo.Length * openFileCoefficient);
                        } else
                        {
                            fs.Dispose();
                            tempFileHandle.Dispose();
                        }
                    }

                    // Try to stream, cascading streaming can be slow, so use this option as the very last choice. Also, open stream can lock the parent. 
                    if (fileProvider == null && allowOpenFile && packageLoader is IPackageLoaderUseStream __streamUse__)
                    {
                        Stream s = fileInfo.CreateReadStream();
                        if (s.CanRead && s.CanSeek)
                        {
                            // Lock parent file provider with the stream.
                            s = new StreamHandle(s, parentFileProvider);
                            fileProvider = __streamUse__.UseStream(s, packageInfo);
                            if (s is IDisposable disposable_) (disposables ?? (disposables = new List<IDisposable>())).Add(disposable_);
                            parentFileProvider = null;
                            lengthEstimate = (long)(fileInfo.Length * openFileCoefficient);
                        }
                        else
                        {
                            s.Dispose();
                        }
                    }

                    // Could not load package with any method
                    if (fileProvider == null)
                    {
                        if (disposables != null) foreach (IDisposable d in disposables) d.Dispose();
                        entry.state = PackageState.Error;
                        throw new PackageException.NoSuitableLoadCapability(packageReference.CanonicalPath, $"{packageLoader.GetType().FullName} could not load file {packageReference.CanonicalPath} with any of the package loaders.");
                    }

                    // Add fileprovider to disposables
                    if (fileProvider is IDisposable disposable) (disposables ?? (disposables = new List<IDisposable>())).Insert(0, disposable);

                    if (IsDisposing || entry.IsDisposing)
                    {
                        List<Exception> errors = new List<Exception>();
                        if (disposables != null)
                            foreach (IDisposable d in disposables)
                                try
                                {
                                    d.Dispose();
                                } catch (Exception e)
                                {
                                    // Dispose error (of temp file handle?)
                                    errors.Add(e);
                                }
                        errors.Add(new ObjectDisposedException(GetType().FullName));
                        throw errors.Count == 1 ? errors[0] : new AggregateException(errors);
                    }

                    // Mark the entry open
                    entry.lastAccessTime = entry.loadTime = DateTimeOffset.UtcNow;
                    entry.fp = fileProvider;
                    entry.length = lengthEstimate;
                    entry.state = PackageState.Opened;
                    // Add disposables to package entry or to file provider
                    if (disposables != null)
                    {
                        IBelatedDisposeFileProvider belateSource = fileProvider as IBelatedDisposeFileProvider;
                        foreach (IDisposable _disposable in disposables)
                        {
                            // Attach disposing of IFileProvider to PackageEntry
                            if (_disposable == fileProvider) entry.AddDisposable(_disposable);
                            // Attach other disposables to the fileprovider, belated
                            else if (belateSource != null) belateSource.AddBelatedDispose(_disposable);
                            // Belation is not supported, then attach disposable to PackageEntry
                            else entry.AddDisposable(_disposable);
                        }
                    }

                    // Fire event
                    if (observers != null) packageEvent = new PackageEvent { EventTime = DateTimeOffset.UtcNow, FilePath = packageReference.CanonicalPath, FileProvider = this, NewState = entry.state, OldState = state };

                    // Wrap file provider into handle and increase reference counter to prevent eviction.
                    return entry.CreateHandle();
                }
                catch (Exception e) when (HandlePackageLoadingError(entry, e, ref packageEvent, disposables))
                {
                    // Error was expected and suppressed.
                    return null;
                }
                finally
                {
                    // Exit entry lock
                    entryHandle?.Dispose();

                    // Release handle to parent object, unless lock was passed to the new file provider (see .UseStream())
                    parentFileProvider?.Dispose();

                    // Fire event
                    if (packageEvent != null) FireEvent(packageEvent);
                }
            }
        }

        /// <summary>
        /// Handle error that occurs while package is being loaded.
        /// 
        /// This method forwards the process to Options.ErrorHandler,
        /// and updates the state.
        /// 
        /// This method is called by OpenPackage and with write-lock from PackageEntry.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="e"></param>
        /// <param name="packageEvent">place event here</param>
        /// <param name="disposables"></param>
        /// <returns>true to suppress</returns>
        bool HandlePackageLoadingError(PackageEntry entry, Exception e, ref PackageEvent packageEvent, List<IDisposable> disposables)
        {
            PackageState oldState = entry.state;
            var errorHandler = options?.ErrorHandler;

            // Create event
            if (observers != null || errorHandler != null)
                packageEvent = new PackageEvent { EventTime = DateTimeOffset.UtcNow, FilePath = entry.packageReference.CanonicalPath, FileProvider = this, NewState = PackageState.Error, OldState = oldState, LoadError = e };

            // Decision whether to suppress exception or not. Suppressing implies the error is expected, but of wrong format.
            bool suppressException;
            if (errorHandler != null)
            {
                // Run delegate
                try
                {
                    suppressException = errorHandler(packageEvent);
                }
                catch (Exception)
                {
                    suppressException = false;
                }
            } else
            {
                suppressException = e is PackageException.NoSuitableLoadCapability;
            }

            // Update entry state.
            if (suppressException)
            {
                packageEvent.LoadError = null;
                entry.state = PackageState.NotPackage;
                entry.error = null;
            } else
            {
                entry.state = PackageState.Error;
                packageEvent.LoadError = e;
                entry.error = e;
            }

            // Cleanup
            if (disposables != null) foreach (IDisposable d in disposables) d.Dispose();

            // Fire event
            if (packageEvent != null && observers != null) FireEvent(packageEvent);

            return suppressException;
        }

        /// <summary>
        /// Get file info.
        /// </summary>
        /// <param name="subpath">Path to a file. Path inside packages is slash "/".</param>
        /// <returns>File info</returns>
        public IFileInfo GetFileInfo(string subpath)
        {
            // Assert it's not disposed
            if (IsDisposing) throw new ObjectDisposedException($"{GetType().FullName}: {FileProvider.ToString()}");

            // 
            if (subpath == null) return new NullDirectoryContents(this);

            // Extract every package name
            MatchCollection matches = Pattern.Matches(subpath);

            // There were no package names
            if (matches.Count == 0) return FileProvider.GetFileInfo(subpath);

            // Convert path to structured format
            PackageFileReference fileReference = PackageFileReference.Parse(subpath, matches, needsCanonizalization);

            // Create file entry
            return new PackageFileInfo(this, fileReference);
        }

        /// <summary>
        /// Get directory info
        /// </summary>
        /// <param name="subpath">Path to a directory. Path inside packages is slash "/".</param>
        /// <returns>Directory info</returns>
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            // Assert it's not disposed
            if (IsDisposing) throw new ObjectDisposedException($"{GetType().FullName}: {FileProvider.ToString()}");

            // 
            if (subpath == null) return new NullDirectoryContents(this);

            // Extract every package name from the path
            MatchCollection matches = Pattern.Matches(subpath);

            // Convert path to structured format
            PackageFileReference directoryReference = PackageFileReference.Parse(subpath, matches, needsCanonizalization);

            // Return a directory reference that opens package lazily
            return new PackageDirectoryContents(this, directoryReference);
        }

        /// <summary>
        /// Remove trailing and preceding '/' characters and '\' on windows. 
        /// For example "\Folder\Subfolder\" is canonized to "Folder\Subfolder".
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        string CanonizeSlashes(string path)
        {
            char osSeparator = Path.DirectorySeparatorChar;

            // Follow preceding slashes
            int startIx = 0;
            while (startIx < path.Length && (path[startIx] == '/' || path[startIx] == osSeparator)) startIx++;

            // Follow trailing slashes
            int endIx = path.Length - 1;
            while (endIx >= 0 && (path[endIx] == '/' || path[endIx] == osSeparator)) endIx--;

            // Return
            return startIx == 0 && endIx == path.Length - 1 ? path : startIx < endIx ? path.Substring(startIx, endIx - startIx + 1) : String.Empty;
        }

        /// <summary>
        /// Canonizes the root element of <paramref name="fileReference"/>.
        /// 
        /// For example "C:\Temp\some.zip/mylib.dll/mylib.resources" is canonized so that
        /// the new reference has elements "c:/Temp/some.zip" "mylib.dll" "mylib.resources".
        /// </summary>
        /// <param name="fileReference"></param>
        /// <returns>canonized file reference</returns>
        PackageFileReference CanonizePackageReference(PackageFileReference fileReference)
        {
            // Root file provider is not physical. No need to canonize
            if (!IsPhysicalProvider(FileProvider)) return fileReference;
            // OS has '/' as separator. No need to canonize.
            if (Path.DirectorySeparatorChar == '/') return fileReference;
            // Canonize root element's name
            PackageFileReference root = fileReference.Root;
            string rootName = root.Name;
            string newRootName = rootName.Replace(Path.DirectorySeparatorChar, '/');
            // Replace did not change anything
            if (rootName == newRootName) return fileReference;
            // Recreate the whole chain until root is canonized.
            PackageFileReference result = null;
            foreach (PackageFileReference x in fileReference.Array)
                result = x == root ? new PackageFileReference(newRootName, root.IsPackageFile, null, newRootName) : new PackageFileReference(x.Name, x.IsPackageFile, result, null);
            return result;
        }

        /// <summary>
        /// Estimate whether file provider represents physical files or not.
        /// 
        /// This implementation is shady. Other file provider implementatios could also represent physical files, but this won't detect that.
        /// The method is virtual so that the behaviour can be fixed by overloading.
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <returns></returns>
        protected virtual bool IsPhysicalProvider(IFileProvider fileProvider)
            => fileProvider.GetType().FullName != "Microsoft.Extensions.FileProviders.Physical.PhysicalFileProvider";

        /// <summary>
        /// Canonize path so that it can be used as a cache key.
        /// 
        /// Only physical file paths are canonized since physical file paths can be ambiguous (under windows).
        /// For example "Folder\somefile.ext" is same file as "Folder/somefile.ext". 
        /// 
        /// Separator character is '/' in the resulting canonized format. For example: "Folder/somefile.ext".
        /// 
        /// Canonization doesn't extend to file entries in archives. If archive was compressed in linux, it can have file entries
        /// that have backslashes in filenames. These entries shouldn't be canonized.
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileProvider"></param>
        /// <returns></returns>
        protected virtual string CanonizePath(string path, IFileProvider fileProvider)
        {
            // We are on OS where separator is '/'. No need to canonize
            if (Path.DirectorySeparatorChar == '/') return path;
            // File provider is not physical, it's a package, no need to canonize
            if (!IsPhysicalProvider(fileProvider)) return path;
            // Path is of physical file and on non '/' separator os (read: windows). Canonize the path to '/' separators
            return path.Replace(Path.DirectorySeparatorChar, '/');
        }

        /// <summary>
        /// Called by the Dispose(), which has already set the object into IsDisposing state.
        /// New package entries cannot be created after put into IsDisposing state.
        /// 
        /// This method will dispose the package entries and remove them from <see cref="packages"/> dictionary.
        /// </summary>
        /// <param name="disposeErrors"></param>
        protected override void innerDispose(ref List<Exception> disposeErrors)
        {
            // Test if there are observers
            bool hasObservers = observers != null;

            // Get pack snapshot
            PackageEntry[] packs = packages.Values.Where(pe => pe != null).ToArray();
            // Take old states, if needed
            PackageState[] oldStates = hasObservers ? packs.Select(p => p.state).ToArray() : null;

            // Dispose non-disposed
            DisposeAndCapture(packs.Where(pe => !pe.IsDisposing), ref disposeErrors);

            // Dispose belated disposables
            /*
            try
            {
                belatedDisposeList.Dispose();
            } catch (Exception e)
            {
                (disposeErrors ?? (disposeErrors = new List<Exception>())).Add(e);
            }*/

            // Remove references
            for (int i = 0; i < packs.Length; i++)
            {
                PackageEntry pe = packs[i];
                if (pe == null) continue;
                bool evicted = packages.TryUpdate(pe.packageReference, null, pe);
                if (hasObservers && evicted)
                    FireEvent(new PackageEvent { EventTime = DateTimeOffset.UtcNow, FilePath = pe.packageReference.CanonicalPath, FileProvider = this, NewState = PackageState.Evicted, OldState = oldStates[i] });
            }

            // Signal, that the observer is closed
            IObserver<PackageEvent>[] _observers;
            lock (m_observersLock) { _observers = observers; observers = null; }
            if (_observers != null) lazyObserverTaskFactory.StartNew(() =>
            {
                foreach (var observer in _observers)
                {
                    try
                    {
                        // Signal, that the observer is closed
                        observer.OnCompleted();
                    }
                    catch (Exception) { }
                }
            });
        }

        /// <summary>
        /// Add glob pattern watcher to the root <see cref="FileProvider"/>. 
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>watcher</returns>
        public IChangeToken Watch(string filter)
        {
            // There is no contract on what null means
            if (filter == null) return NullChangeToken.Singleton;

            // Check disposing
            if (IsDisposing) throw new ObjectDisposedException(GetType().FullName);

            // Extract first package name
            Match match = Pattern.Match(filter);
            Group group = match.Success ? match.Groups["ext"] : null;

            // Got no package name
            if (!match.Success || group == null || !group.Success) return FileProvider.Watch(filter);

            // Search for any dirs "**"
            int ix = filter.IndexOf("**", StringComparison.InvariantCulture);

            // Got no "**". Return watcher for path that ends at first package "folder/myfile.zip"
            if (ix < 0) return FileProvider.Watch(filter.Substring(0, group.Index + group.Length));

            // Got "**" after first package name "folder/myfile.zip/**", watch the package "folder/myfile.zip"
            if (ix > group.Index + group.Length) return FileProvider.Watch(filter.Substring(0, group.Index + group.Length));

            // Got "**" before first package name "folder/**/myfile.zip". Watch everything.
            else return FileProvider.Watch(filter.Substring(0, ix + 2));

            // Todo: If a package changes, it's not evicted, but should be. //
        }

        /// <summary>
        /// Copy on write snapshot of observers
        /// </summary>
        IObserver<PackageEvent>[] observers = null;

        /// <summary>
        /// Lock under which <see cref="observers"/> is modified.
        /// </summary>
        object m_observersLock = new object();

        /// <summary>
        /// Object that handles <see cref="PackageEvent"/>.
        /// </summary>
        protected TaskFactory observerTaskFactory;

        /// <summary>
        /// Object that handles <see cref="PackageEvent"/>.
        /// </summary>
        public TaskFactory ObserverTaskFactory { get => observerTaskFactory; set => observerTaskFactory = value; }

        /// <summary>
        /// Returns task factory for observer events.
        /// </summary>
        TaskFactory lazyObserverTaskFactory => observerTaskFactory ?? (observerTaskFactory = new TaskFactory(TaskCreationOptions.PreferFairness, TaskContinuationOptions.PreferFairness));

        /// <summary>
        /// Fire an event.
        /// Another thread will run the events in observers.
        /// If there are no observers at the time of queuing, the event is thrown away.
        /// If object is disposed, the event is thrown away.
        /// </summary>
        /// <param name="packageEvent"></param>
        protected void FireEvent(PackageEvent packageEvent)
        {
            // Assert args
            if (packageEvent == null) throw new ArgumentNullException(nameof(packageEvent));

            // Take references
            var _observers = observers;
            var _taskFactory = lazyObserverTaskFactory;

            // Don't fire event when completely disposed, but it's ok to fire event if disposing (not completed).
            if (IsDisposed || _taskFactory == null || _observers == null) return;

            // Enqueue a delegate that feeds the event to the observers. 
            // A delegate and closures are needed so that eviction events are processed correctly when the object is disposed.
            _taskFactory.StartNew(() =>
            {
                List<Exception> errors = null;
                foreach (var _observer in _observers)
                {
                    try
                    {
                        _observer.OnNext(packageEvent);
                    }
                    catch (Exception e)
                    {
                        // Error thrown by the observer..
                        (errors ?? (errors = new List<Exception>())).Add(e);
                    }
                }

                // Throw it on the face of the TaskFactory, maybe it will log it..
                if (errors != null) throw new AggregateException(errors);
            });
        }

        /// <summary>
        /// Subscribe to events related to loading and evicting packages.
        /// </summary>
        /// <param name="observer">observer receiving events</param>
        /// <returns>handle that must be disposed</returns>
        public IDisposable Subscribe(IObserver<PackageEvent> observer)
        {
            // Assert args
            if (observer == null) throw new ArgumentNullException(nameof(observer));

            // Cannot add observers if object is disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().FullName);

            // Update copy-on-write within lock
            lock (m_observersLock)
            {
                if (observers == null) observers = new[] { observer };
                else
                {
                    IObserver<PackageEvent>[] newObservers = new IObserver<PackageEvent>[observers.Length + 1];
                    Array.Copy(observers, newObservers, observers.Length);
                    newObservers[observers.Length] = observer;
                    observers = newObservers;
                }
            }

            // Return disposable handle
            return new ObserverHandle<PackageEvent>(unsubscribeObserver, observer);
        }

        Action<IObserver<PackageEvent>> unsubscribeObserver;

        /// <summary>
        /// Unsubscribe package event observer.
        /// </summary>
        /// <param name="observer"></param>
        void UnsubscribeObserver(IObserver<PackageEvent> observer)
        {
            // Assert args
            if (observer == null) throw new ArgumentNullException(nameof(observer));

            // Update copy-on-write within lock
            lock (m_observersLock)
            {
                if (observers == null) return;
                observers = observers.Where(o => o != observer).ToArray();
                if (observers.Length == 0) observers = null;
            }
        }

        /// <summary>
        /// Get information about loaded packages
        /// </summary>
        /// <returns></returns>
        public PackageInfo[] GetPackageInfos()
            => packages.Values.Where(pe => pe != null && pe.state != PackageState.Evicted).Select(pe => ConvertToPackageInfo(pe)).ToArray();

        /// <summary>
        /// Convert <see cref="PackageEntry"/> to <see cref="PackageInfo"/>.
        /// </summary>
        /// <param name="pe"></param>
        /// <returns></returns>
        static PackageInfo ConvertToPackageInfo(PackageEntry pe)
        {
            PackageInfo pi = new PackageInfo { FilePath = pe.packageReference.CanonicalPath, LastAccessTime = pe.lastAccessTime, LoadTime = pe.loadTime, LoadError = pe.error, State = pe.state };
            if (pe.state == PackageState.Opened) pi.Length = pe.length;
            return pi;
        }

        /// <summary>
        /// Get load information about package file.
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns>info</returns>
        public PackageInfo GetPackageInfo(string filepath)
        {
            // Assert arts
            if (filepath == null) throw new ArgumentNullException(nameof(filepath));

            // Refers to root file provider
            if (filepath == "") return new PackageInfo { FilePath = "", State = PackageState.Opened, LastAccessTime = DateTimeOffset.UtcNow };

            // Extract every package name
            MatchCollection matches = Pattern.Matches(filepath);

            // There were no package names
            if (matches.Count == 0) return new PackageInfo { FilePath = filepath, State = PackageState.NotPackage, LastAccessTime = DateTimeOffset.UtcNow };

            // Extract every package name from the path
            PackageFileReference fileReference = PackageFileReference.Parse(filepath, matches, needsCanonizalization);

            // Get entry
            PackageEntry pe;
            if (packages.TryGetValue(fileReference, out pe))
            {
                // Entry was found
                return ConvertToPackageInfo(pe);
            }
            else
            {
                // Entry was not found
                return new PackageInfo { FilePath = filepath, State = PackageState.NotOpened };
            }
        }

        /// <summary>
        /// Try to evict package and its cache record.
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns>true if filepath no longer exists in the cache, false if it still exists</returns>
        /// <exception cref="Exception">Any error that occurs when disposing the entry</exception>
        public bool Evict(string filepath)
        {
            // Assert args
            if (filepath == null) throw new ArgumentNullException(nameof(filepath));

            // Root filepath is not evictable
            if (filepath == "") return false;

            // Extract every package name
            MatchCollection matches = Pattern.Matches(filepath);

            // There were no package names
            if (matches.Count == 0) return false;

            // Convert to structured format
            PackageFileReference packageReference = PackageFileReference.Parse(filepath, matches, needsCanonizalization);

            // 
            PackageEntry packageEntry;

            // Search package entry. Entry was not found
            if (!packages.TryGetValue(packageReference, out packageEntry) || packageEntry == null) return true;

            // Entry is reserved, cannot evict.
            if (Interlocked.Read(ref packageEntry.handleCount) > 0L) return false;

            // State before evict.
            PackageState oldState;

            lock (packageEntry.m_lock)
            {
                // Stack slot for state, to be written in lock
               oldState = packageEntry.state;

                // Entry is reserved, cannot evict.
                if (Interlocked.Read(ref packageEntry.handleCount) > 0L) return false;

                // Dispose it. GetOrCreate won't return evicted entry.
                packageEntry.Dispose();

                // Remove entry from dictionary. 
                packages.TryUpdate(packageReference, null, packageEntry);
            }

            // Fire event
            if (observers != null) FireEvent(new PackageEvent { EventTime = DateTimeOffset.UtcNow, FilePath = packageEntry.packageReference.CanonicalPath, FileProvider = this, NewState = PackageState.Evicted, OldState = oldState });

            // Done
            return true;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the package fileprovider.
        /// 
        /// If <paramref name="disposable"/> is not <see cref="IDisposable"/>, then it's not added.
        /// </summary>
        /// <param name="disposable">object to dispose</param>
        /// <returns></returns>
        public PackageFileProvider AddDisposable(object disposable)
        {
            if (disposable is IDisposable toDispose && this is IDisposeList disposeList)
                disposeList.AddDisposable(toDispose);
            return this;
        }

        /// <summary>
        /// Acquire disposable from delegate <paramref name="func"/>.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public PackageFileProvider AddDisposable(Func<PackageFileProvider, object> func)
        {
            object o = func(this);
            if (o is IDisposable toDispose && this is IDisposeList disposeList)
                disposeList.AddDisposable(toDispose);
            return this;
        }

        /*
        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the file provider after all streams are closed.
        /// </summary>
        /// <param name="disposable">object to dispose</param>
        /// <returns></returns>
        public PackageFileProvider AddBelatedDispose(object disposable)
        {
            if (disposable is IDisposable toDispose) ((IBelatedDisposeList)belatedDisposeList).AddBelatedDispose(toDispose);
            return this;
        }
        bool IBelatedDisposeFileProvider.AddBelatedDispose(IDisposable disposable)
            => ((IBelatedDisposeList)belatedDisposeList).AddBelatedDispose(disposable);
        bool IBelatedDisposeFileProvider.AddBelatedDisposes(IEnumerable<IDisposable> disposables)
            => ((IBelatedDisposeList)belatedDisposeList).AddBelatedDisposes(disposables);
        bool IBelatedDisposeFileProvider.RemoveBelatedDispose(IDisposable disposable)
            => ((IBelatedDisposeList)belatedDisposeList).RemoveBelatedDispose(disposable);
        bool IBelatedDisposeFileProvider.RemoveBelatedDisposes(IEnumerable<IDisposable> disposables)
            => ((IBelatedDisposeList)belatedDisposeList).RemovedBelatedDisposes(disposables);

        IDisposable IBelatedDisposeList.Belate()
            => ((IBelatedDisposeList)belatedDisposeList).Belate();
        bool IBelatedDisposeList.AddBelatedDispose(IDisposable disposable)
            => ((IBelatedDisposeList)belatedDisposeList).AddBelatedDispose(disposable);
        bool IBelatedDisposeList.AddBelatedDisposes(IEnumerable<IDisposable> disposables)
            => ((IBelatedDisposeList)belatedDisposeList).AddBelatedDisposes(disposables);
        bool IBelatedDisposeList.RemoveBelatedDispose(IDisposable disposable)
            => ((IBelatedDisposeList)belatedDisposeList).RemoveBelatedDispose(disposable);
        bool IBelatedDisposeList.RemovedBelatedDisposes(IEnumerable<IDisposable> disposables)
            => ((IBelatedDisposeList)belatedDisposeList).RemovedBelatedDisposes(disposables);
            */
        /// <summary>
        /// Set temp file provider.
        /// </summary>
        /// <param name="tempFileProvider"></param>
        /// <returns>this</returns>
        public PackageFileProvider SetTempFileProvider(ITempFileProvider tempFileProvider)
        {
            this.TempFileProvider = tempFileProvider;
            return this;
        }

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => $"{GetType().Name}(Options={Options}, FileProvider={FileProvider})";
    }

    /// <summary>
    /// Information about package load status.
    /// </summary>
    class PackageLoadInfo : IPackageLoadInfo
    {
        public string Path { get; set; }
        public DateTimeOffset? LastModified { get; set; }
        public long Length { get; set; }

        public PackageLoadInfo(string path, long length, DateTimeOffset? lastModified)
        {
            Path = path;
            Length = length;
            LastModified = lastModified;
        }
    }

}
