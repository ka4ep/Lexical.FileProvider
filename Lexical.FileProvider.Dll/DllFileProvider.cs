// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           18.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileProvider.Common;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Lexical.FileProvider
{
    /// <summary>
    /// Provides the embedded resources of a managed .dll files. 
    /// 
    /// Despite its name, the class can also provide from managed .exe files.
    /// 
    /// Uses Mono.Cecil to load resources, so it won't load the assembly into the application domain.
    /// This is useful since Reflection load doesn't work (at the time of writing) in .NET core. 
    /// 
    /// This implementation has distinct difference to EmbeddedFileProvider.
    /// EmbeddedFileProvider hides the assembly name part of the resource names. This implementation does not.
    /// Hiding parts of the filename is problematic, as they are not compatible with the resource manifest.
    /// Especially problematic when assembly has been merged from multiple assemblies.
    /// </summary>
    public class DllFileProvider : DisposeList, IDisposableFileProvider, IBelatedDisposeFileProvider, IBelatedDisposeList
    {
        /// <summary>
        /// Dispose list for belated disposables.
        /// </summary>
        IBelatedDisposeList belatedDisposeList = new BelatedDisposeList();

        /// <summary>
        /// .dll or .exe module loader.
        /// </summary>
        ModuleDefinition module;

        /// <summary>
        /// Datetime of the source file, or construction time if unknown.
        /// </summary>
        DateTimeOffset date;

        /// <summary>
        /// Cached file entries
        /// </summary>
        EmbeddedResourceEntry[] files;

        /// <summary>
        /// Property that creates file entries lazily.
        /// 
        /// If ran concurrently, one evaluation will remain, and others will be garbage collected.
        /// May occasionaly return different instances of same content to different threads, but that's 
        /// not required in the IFileProvider contract. 
        /// Evaluating same resources twice in rare occasional cases is about as good than locking it.
        /// </summary>
        EmbeddedResourceEntry[] Files => files ?? (files = CreateEntries().ToArray());

        /// <summary>
        /// Cached file map
        /// </summary>
        Dictionary<string, EmbeddedResourceEntry> fileMap;

        /// <summary>
        /// Property that creates map lazily.
        /// 
        /// If ran concurrently, one evaluation will remain, and others will be garbage collected.
        /// May occasionaly return different instances of same content to different threads, but that's 
        /// not required in the IFileProvider contract. 
        /// Evaluating same resources twice in rare occasional cases is about as good than locking it.
        /// </summary>
        Dictionary<string, EmbeddedResourceEntry> FileMap => fileMap ?? (fileMap = Files.ToDictionary(file => file.Name));

        /// <summary>
        /// Cached contents of the root
        /// </summary>
        DirectoryContents contents;

        /// <summary>
        /// Lazy evaluation of the contents.
        /// </summary>
        DirectoryContents Contents => contents ?? (contents = new DirectoryContents(Files));

        /// <summary>
        /// Lock for reading streams.
        /// </summary>
        protected internal object m_streamlock = new object();

        static ReaderParameters immediate, deferred;

        static DllFileProvider()
        {
            immediate = new ReaderParameters(ReadingMode.Immediate);
            immediate.ReadSymbols = false;
            immediate.ThrowIfSymbolsAreNotMatching = false;
            immediate.ApplyWindowsRuntimeProjections = false;
            immediate.ReadWrite = false;

            deferred = new ReaderParameters(ReadingMode.Deferred);
            deferred.ReadSymbols = false;
            deferred.ThrowIfSymbolsAreNotMatching = false;
            deferred.ApplyWindowsRuntimeProjections = false;
            deferred.ReadWrite = false;
        }

        /// <summary>
        /// Open module file, lock it for reading, and read resources as needed.
        /// File provider must be disposed on exit.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="datetime">(optional)</param>
        /// <returns>File provider that must be disposed after use to release file</returns>
        /// <exception cref="FileLoadException">on load error</exception>
        public static DllFileProvider OpenFile(string filename, DateTimeOffset? datetime = default)
        {
            // Open file
            ModuleDefinition module = ModuleDefinition.ReadModule(filename, deferred) ?? throw new FileLoadException(filename);
            // datetime: 1. from argument, 2. from file
            DateTimeOffset date = datetime != null ? (DateTimeOffset)datetime : File.GetLastWriteTime(filename);
            // Create file provider
            return new DllFileProvider(module, date).AddBelatedDispose(module);
        }

        /*
        /// <summary>
        /// Read module into memory. Release file handle after this call.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="datetime"></param>
        /// <returns>File provider that doesn't need to be disposed after use.</returns>
        public static DllFileProvider LoadFromFile(string filename, DateTimeOffset? datetime = default)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                if (fs.Length > Int32.MaxValue) throw new ArgumentException("Cannot read file over 2GB");
                MemoryStream ms = new MemoryStream((int)fs.Length);
                fs.CopyTo(ms);
                ms.Position = 0L;
                // Read module
                ModuleDefinition module = ModuleDefinition.ReadModule(ms, inMemory);
                // datetime: 1. from argument, 2. from file
                DateTimeOffset date = datetime != null ? (DateTimeOffset)datetime : File.GetLastWriteTime(filename);
                // Create file provider
                return new DllFileProvider(module, new IDisposable[] { module }, date);
            }
        }*/

        /// <summary>
        /// Open a file provider that reads modules from a stream. 
        /// File provider takes ownership of the stream and disposes it along with the file provider.
        /// File provider must be disposed by caller.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="datetime">(optional)</param>
        /// <returns>File provider that must be disposed after use to release file</returns>
        public static DllFileProvider UseStream(Stream stream, DateTimeOffset? datetime = default)
        {
            // Create deferred module
            ModuleDefinition module = ModuleDefinition.ReadModule(stream, deferred) ?? throw new FileLoadException();
            // Set datetime: 1. from argument, 2. current time
            DateTimeOffset date = datetime != null ? (DateTimeOffset)datetime : new DateTimeOffset();
            // Create provider
            return new DllFileProvider(module, date).AddBelatedDispose(module).AddBelatedDispose(stream);
        }

        /*
        /// <summary>
        /// Read module from stream. Does not take ownership of the stream. Does not dispose the stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="datetime"></param>
        /// <returns>File provider that doesn't need to be disposed after use.</returns>
        public static DllFileProvider LoadFromStream(Stream stream, DateTimeOffset? datetime = default)
        {
            // Create deferred module
            ModuleDefinition module = ModuleDefinition.ReadModule(stream, inMemory) ?? throw new FileLoadException();
            // Set datetime: 1. from argument, 2. current time
            DateTimeOffset date = datetime != null ? (DateTimeOffset)datetime : new DateTimeOffset();
            // Create provider
            return new DllFileProvider(module, new IDisposable[] { module }, date);
        }*/

        /// <summary>
        /// Construct file provider from <paramref name="module"/>.
        /// </summary>
        /// <param name="module">Instance of Mono.Cecil.ModuleDefinition</param>
        /// <param name="datetime">(optional) overriding datetime</param>
        public DllFileProvider(object module, DateTimeOffset? datetime = default)
        {
            this.module = module as ModuleDefinition ?? throw new ArgumentNullException(nameof(module));

            // Set datetime: 1. from argument, 2. from file, 3. current time
            this.date = datetime != null ? (DateTimeOffset)datetime :
                this.module.FileName != null && File.Exists(this.module.FileName) ? File.GetLastWriteTime(this.module.FileName)
                : new DateTimeOffset();
        }

        /// <summary>
        /// Create an enumerable of resources
        /// </summary>
        /// <returns></returns>
        IEnumerable<EmbeddedResourceEntry> CreateEntries()
        {
            var _module = module;
            if (IsDisposing || _module == null) throw new ObjectDisposedException(GetType().FullName);
            foreach (Resource r in _module.Resources)
            {
                if (r.ResourceType == ResourceType.Embedded && r is EmbeddedResource er)
                    yield return new EmbeddedResourceEntry(this, er, date);
            }
        }

        /// <summary>
        /// Get directory contents
        /// </summary>
        /// <param name="subpath"></param>
        /// <returns></returns>
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            if (subpath == null) return Contents;
            if (subpath == "") return Contents;
            return NotFoundDirectoryContents.Singleton;
        }

        /// <summary>
        /// Get file info
        /// </summary>
        /// <param name="subpath"></param>
        /// <returns></returns>
        public IFileInfo GetFileInfo(string subpath)
        {
            EmbeddedResourceEntry result;
            if (FileMap.TryGetValue(subpath, out result)) return result;
            return new NotFoundFileInfo(subpath);
        }

        /// <summary>
        /// Watch directory
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public IChangeToken Watch(string filter)
            => NullChangeToken.Singleton;

        /// <summary>
        /// Dispose 
        /// </summary>
        /// <param name="disposeErrors"></param>
        protected override void innerDispose(ref List<Exception> disposeErrors)
        {
            // Dispose and null module, only once
            try { 
                Interlocked.CompareExchange(ref module, null, module)?.Dispose();
            }
            catch (Exception e)
            {
                (disposeErrors ?? (disposeErrors = new List<Exception>())).Add(e);
            }

            // Belated disposes
            try
            {
                belatedDisposeList.Dispose();
            } catch (Exception e)
            {
                (disposeErrors ?? (disposeErrors = new List<Exception>())).Add(e);
            }
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the file provider.
        /// 
        /// If <paramref name="disposable"/> is not <see cref="IDisposable"/>, then it's not added.
        /// </summary>
        /// <param name="disposable">object to dispose</param>
        /// <returns></returns>
        public DllFileProvider AddDisposable(object disposable)
        {
            if (disposable is IDisposable toDispose && this is IDisposeList disposeList) disposeList.AddDisposable(toDispose);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to be disposed along with the file provider after all streams are closed.
        /// </summary>
        /// <param name="disposable">object to dispose</param>
        /// <returns></returns>
        public DllFileProvider AddBelatedDispose(object disposable)
        {
            if (disposable is IDisposable toDispose) belatedDisposeList.AddBelatedDispose(toDispose);
            return this;
        }

        bool IBelatedDisposeFileProvider.AddBelatedDispose(IDisposable disposable)
            => belatedDisposeList.AddBelatedDispose(disposable);
        bool IBelatedDisposeFileProvider.AddBelatedDisposes(IEnumerable<IDisposable> disposables)
            => belatedDisposeList.AddBelatedDisposes(disposables);
        bool IBelatedDisposeFileProvider.RemoveBelatedDispose(IDisposable disposable)
            => belatedDisposeList.RemoveBelatedDispose(disposable);
        bool IBelatedDisposeFileProvider.RemoveBelatedDisposes(IEnumerable<IDisposable> disposables)
            => belatedDisposeList.RemovedBelatedDisposes(disposables);

        IDisposable IBelatedDisposeList.Belate()
            => belatedDisposeList.Belate();
        bool IBelatedDisposeList.AddBelatedDispose(IDisposable disposable)
            => belatedDisposeList.AddBelatedDispose(disposable);
        bool IBelatedDisposeList.AddBelatedDisposes(IEnumerable<IDisposable> disposables)
            => belatedDisposeList.AddBelatedDisposes(disposables);
        bool IBelatedDisposeList.RemoveBelatedDispose(IDisposable disposable)
            => belatedDisposeList.RemoveBelatedDispose(disposable);
        bool IBelatedDisposeList.RemovedBelatedDisposes(IEnumerable<IDisposable> disposables)
            => belatedDisposeList.RemovedBelatedDisposes(disposables);
    }

    class EmbeddedResourceEntry : IFileInfo
    {
        DllFileProvider fileProvider;
        EmbeddedResource resource;
        DateTimeOffset date;
        byte[] data;
        byte[] Data => data ?? (data = resource.GetResourceData());

        public EmbeddedResourceEntry(DllFileProvider fileProvider, EmbeddedResource resource, DateTimeOffset date)
        {
            this.resource = resource;
            this.fileProvider = fileProvider;
        }

        public bool Exists => true;
        public long Length => Data.Length;
        public string PhysicalPath => null;
        public string Name => resource.Name;
        public DateTimeOffset LastModified => date;
        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            var _data = data;
            if (_data == null)
            {
                // Lock parent's stream and read data.
                lock (fileProvider.m_streamlock)
                {
                    // Read data
                    if (_data == null) _data = data = resource.GetResourceData();
                }
            }
            // Wrap into stream.
            MemoryStream ms = new MemoryStream(_data);
            IDisposable belate = ((IBelatedDisposeList)fileProvider).Belate();
            return new StreamHandle(ms, belate);
        }

        public override string ToString()
            => Name;

        public override int GetHashCode()
            => Name.GetHashCode() ^ 0x23423423;

        public override bool Equals(object obj)
        {
            if (obj is EmbeddedResourceEntry other)
            {
                return other.Name == Name;
            }
            else
            {
                return false;
            }
        }

    }

    class DirectoryContents : IDirectoryContents
    {
        IFileInfo[] files;

        public DirectoryContents(IFileInfo[] files)
        {
            this.files = files ?? throw new ArgumentNullException(nameof(files));
        }

        public bool Exists
            => true;

        public IEnumerator<IFileInfo> GetEnumerator()
            => ((IEnumerable<IFileInfo>)files).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => files.GetEnumerator();
    }
}

