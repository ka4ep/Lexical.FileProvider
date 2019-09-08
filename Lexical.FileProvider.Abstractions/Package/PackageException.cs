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
        /// <summary>
        /// Create exception.
        /// </summary>
        public PackageException() { }

        /// <summary>
        /// Create exception.
        /// </summary>
        /// <param name="message"></param>
        public PackageException(string message) : base(message) { }

        /// <summary>
        /// Create exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public PackageException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Create exception.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
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

            /// <summary>
            /// Create exception.
            /// </summary>
            /// <param name="filePath"></param>
            public FileError(string filePath) : base(filePath) { this.FilePath = filePath; }

            /// <summary>
            /// Create exception.
            /// </summary>
            /// <param name="filePath"></param>
            /// <param name="message"></param>
            public FileError(string filePath, string message) : base(message) { this.FilePath = filePath; }

            /// <summary>
            /// Create exception.
            /// </summary>
            /// <param name="filePath"></param>
            /// <param name="message"></param>
            /// <param name="innerException"></param>
            public FileError(string filePath, string message, Exception innerException) : base(message, innerException) { this.FilePath = filePath; }

            /// <summary>
            /// Derialize exception from <paramref name="context"/>.
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected FileError(SerializationInfo info, StreamingContext context) : base(info, context) { this.FilePath = info.GetString(nameof(FilePath)); }

            /// <summary>
            /// Serialize object data to <paramref name="context"/>.
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
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
            /// <summary>
            /// Create exception.
            /// </summary>
            /// <param name="filePath"></param>
            public NoSuitableLoadCapability(string filePath) : base(filePath) { }

            /// <summary>
            /// Create exception.
            /// </summary>
            /// <param name="filePath"></param>
            /// <param name="message"></param>
            public NoSuitableLoadCapability(string filePath, string message) : base(filePath, message) { }

            /// <summary>
            /// Create exception.
            /// </summary>
            /// <param name="filePath"></param>
            /// <param name="message"></param>
            /// <param name="innerException"></param>
            public NoSuitableLoadCapability(string filePath, string message, Exception innerException) : base(filePath, message, innerException) { }

            /// <summary>
            /// Derialize exception from <paramref name="context"/>.
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected NoSuitableLoadCapability(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }

        /// <summary>
        /// Loading package failed.
        /// </summary>
        public class LoadError : FileError
        {
            /// <summary>
            /// Create exception.
            /// </summary>
            /// <param name="filePath"></param>
            public LoadError(string filePath) : base(filePath) { }

            /// <summary>
            /// Create exception.
            /// </summary>
            /// <param name="filePath"></param>
            /// <param name="message"></param>
            public LoadError(string filePath, string message) : base(filePath, message) { }

            /// <summary>
            /// Create exception.
            /// </summary>
            /// <param name="filePath"></param>
            /// <param name="message"></param>
            /// <param name="innerException"></param>
            public LoadError(string filePath, string message, Exception innerException) : base(filePath, message, innerException) { }

            /// <summary>
            /// Create exception.
            /// </summary>
            /// <param name="filePath"></param>
            /// <param name="innerException"></param>
            public LoadError(string filePath, Exception innerException) : base(filePath, filePath != null ? $"{innerException.GetType().FullName}({filePath}): {innerException.Message}" : $"{innerException.GetType().FullName}: {innerException.Message}", innerException) { }

            /// <summary>
            /// Derialize exception from <paramref name="context"/>.
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected LoadError(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }
    }
}
