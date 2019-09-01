// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           18.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using System;
using System.Threading;

namespace Lexical.FileProvider.Package
{
    public static class PackageFileProviderExtensions_Logger
    {
        /// <summary>
        /// Configure package file provider to log errors, and to suppress them.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IPackageFileProviderOptions SetToSuppressAndLogErrors(this IPackageFileProviderOptions options, ILogger logger)
            => options.SetErrorHandler( e=> {
                logger.LogError(e.LoadError, "Failed to open package file: {0}", options);
                return true;
            });

        /// <summary>
        /// Configure package file provider to log errors and then throw them.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IPackageFileProviderOptions SetToThrowAndLogErrors(this IPackageFileProviderOptions options, ILogger logger)
            => options.SetErrorHandler(e => {
                logger.LogError(e.LoadError, "Failed to open package file: {0}", options);
                return false;
            });

        /// <summary>
        /// Add logger to package file provider.
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="logger">(optional) logger</param>
        /// <returns></returns>
        public static IObservablePackageFileProvider AddLogger(this IObservablePackageFileProvider fileProvider, ILogger logger)
        {
            if (logger != null) fileProvider.Subscribe(new PackageEventLogger(logger, LogLevel.Trace));
            return fileProvider;
        }

        /// <summary>
        /// Add logger to package file provider.
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="logger">(optional) logger</param>
        /// <returns></returns>
        public static IObservablePackageFileProvider AddLogger(this IObservablePackageFileProvider fileProvider, ILogger logger, LogLevel logLevel)
        {
            if (logger != null) fileProvider.Subscribe(new PackageEventLogger(logger, logLevel));
            return fileProvider;
        }
    }

    public class PackageEventLogger : IObserver<PackageEvent>
    {
        public ILogger logger;
        public readonly LogLevel logLevel;
        static int eventId = 0;
        private static readonly Func<object, Exception, string> _messageFormatter = (s,e)=>s.ToString();

        public PackageEventLogger(ILogger logger, LogLevel logLevel)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.logLevel = logLevel;
        }
        public PackageEventLogger(ILoggerFactory loggerFactory, LogLevel logLevel)
        {
            this.logger = loggerFactory?.CreateLogger<IPackageFileProvider>() ?? throw new ArgumentNullException(nameof(logger));
            this.logLevel = logLevel;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(PackageEvent value)
        {
            int id = Interlocked.Increment(ref eventId);
            switch(value.NewState)
            {
                case PackageState.Evicted:
                    logger.Log(logLevel, id, new FormattedLogValues("{0} was evicted.", value.FilePath), null, _messageFormatter);
                    break;
                case PackageState.NotPackage:
                    logger.Log(logLevel, id, new FormattedLogValues("{0} was not a package file.", value.FilePath), null, _messageFormatter);
                    break;
                case PackageState.Opened:
                    logger.Log(logLevel, id, new FormattedLogValues("{0} was loaded.", value.FilePath), null, _messageFormatter);
                    break;
                case PackageState.Error:
                    logger.Log(logLevel, id, new FormattedLogValues("Failed to load {0}, error: {1}", value.FilePath, value.LoadError?.Message), null, _messageFormatter);
                    break;
            }
        }
    }
}
