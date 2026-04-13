using Fmacias.TplQueue.Contracts;
using Microsoft.Extensions.Logging;
using System;

namespace Fmacias.TplQueue.Observers
{
    /// <summary>
    /// Factory for the built-in observer implementations provided by this package.
    /// </summary>
    public sealed class ObserverFactory : IObserverFactory
    {
        private ObserverFactory()
        {
        }

        /// <summary>
        /// Creates a new observer factory instance.
        /// </summary>
        public static IObserverFactory Create()
        {
            return new ObserverFactory();
        }

        /// <inheritdoc />
        public IConsoleObserver CreateConsoleObserver()
        {
            return ConsoleObserver.Create();
        }

        /// <inheritdoc />
        public ILoggingObserver CreateLoggingObserver(ILogger<ILoggingObserver> logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return LoggingObserver.Create(logger);
        }

        /// <inheritdoc />
        public IObserverDispatcher CreateObserverDispatcher()
        {
            return DirectObserverDispatcher.Create();
        }

        /// <inheritdoc />
        public IProfilingObserver CreateProfilingObserver(ILogger<IProfilingObserver> logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return ProfilingObserver.Create(logger);
        }

        /// <inheritdoc />
        public IFileLoggingObserver CreateFileLoggingObserver(ILogger logger, string queueName)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return FileLoggingObserver.Create(logger, queueName);
        }
    }
}
