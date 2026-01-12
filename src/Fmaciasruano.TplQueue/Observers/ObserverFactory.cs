using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Observers.ViewModel;
using Microsoft.Extensions.Logging;

namespace Fmaciasruano.TplQueue.Observers
{
    internal sealed class ObserverFactory : IObserverFactory
    {
        private ObserverFactory(){}
        public static IObserverFactory Instance()
        {
            return new ObserverFactory();
        }
        public ITaskRunnerConsoleObserver CreateConsoleObserver()
        {
            return TaskRunnerConsoleObserver.Create();
        }

        public ITaskQueueLoggingObserver CreateLoggingObserver(ILogger<ITaskQueueLoggingObserver> logger)
        {
            return TaskQueueLoggingObserver.Create(logger);
        }

        public IObserverDispatcher CreateObserverDispatcher()
        {
            return DirectObserverDispatcher.Create();
        }

        public IProfilingObserver CreateProfilingObserver(ILogger<IProfilingObserver> logger)
        {
            return TaskRunnerProfilingObserver.Create(logger);
        }
        public ITaskRunnerViewModelObserver CreateViewModeObserver(IObserverDispatcher observerDispatcher)
        {
            return TaskRunnerViewModelObserver.Create(observerDispatcher);
        }
    }
}
