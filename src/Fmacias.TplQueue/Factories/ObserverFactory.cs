using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Observers;
using Microsoft.Extensions.Logging;

namespace Fmacias.TplQueue.Factories
{
    internal sealed class ObserverFactory : IObserverFactory
    {
        private ObserverFactory(){}
        public static IObserverFactory Instance()
        {
            return new ObserverFactory();
        }
        public IConsoleObserver CreateConsoleObserver()
        {
            return ConsoleObserver.Create();
        }

        public ILoggingObserver CreateLoggingObserver(ILogger<ILoggingObserver> logger)
        {
            return LoggingObserver.Create(logger);
        }

        public IObserverDispatcher CreateObserverDispatcher()
        {
            return DirectObserverDispatcher.Create();
        }

        public IProfilingObserver CreateProfilingObserver(ILogger<IProfilingObserver> logger)
        {
            return ProfilingObserver.Create(logger);
        }
    }
}
