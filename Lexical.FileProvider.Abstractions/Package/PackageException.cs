// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           28.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Runtime.Serialization;

namespace Lexical.FileProvider.Package
{
    /// <summary>
    /// Generic <see cref="IPackageFileProvider" /> related exception.
    /// </summary>
    public class PackageException : Exception
    {
        public PackageException() { }
        public PackageException(string message) : base(message) { }
        public PackageException(string message, Exception innerException) : base(message, innerException) { }
        protected PackageException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <summary>
        /// Generic file related error.
        /// </summary>
        public abstract class FileError : PackageException
        {
            /// <summary>
            /// (Optional) File path that is associated to this error.
            /// </summary>
            public readonly string FilePath;

            public FileError(string filePath) : base(filePath) { this.FilePath = filePath; }
            public FileError(string filePath, string message) : base(message) { this.FilePath = filePath; }
            public FileError(string filePath, string message, Exception innerException) : base(message, innerException) { this.FilePath = filePath; }
            protected FileError(SerializationInfo info, StreamingContext context) : base(info, context) { this.FilePath = info.GetString(nameof(FilePath)); }
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue(nameof(FilePath), FilePath);
                base.GetObjectData(info, context);
            }
        }

        /// <summary>
        /// Could not match <see cref="IPackageFileProviderOptions" /> and <see cref="IPackageLoader"/> capabilities.
        /// </summary>
        public class NoSuitableLoadCapability : FileError
        {
            public NoSuitableLoadCapability(string filePath) : base(filePath) { }
            public NoSuitableLoadCapability(string filePath, string message) : base(filePath, message) { }
            public NoSuitableLoadCapability(string filePath, string message, Exception innerException) : base(filePath, message, innerException) { }
            protected NoSuitableLoadCapability(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }

        /// <summary>
        /// Loading package failed.
        /// </summary>
        public class LoadError : FileError
        {
            public LoadError(string filePath) : base(filePath) { }
            public LoadError(string filePath, string message) : base(filePath, message) { }
            public LoadError(string filePath, string message, Exception innerException) : base(filePath, message, innerException) { }
            public LoadError(string filePath, Exception innerException) : base(filePath, filePath != null ? $"{innerException.GetType().FullName}({filePath}): {innerException.Message}" : $"{innerException.GetType().FullName}: {innerException.Message}", innerException) { }
            protected LoadError(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }
    }
}
