using System;
using Fmacias.TplQueue.Contracts;
using Microsoft.Extensions.Logging;

namespace Fmacias.TplQueue.Observers
{
    /// <summary>
    /// Helpers para suscribir observadores de logging a dispatchers.
    /// No interfiere con tu IObserverFactory actual; es opt-in desde el sample.
    /// </summary>
    public static class LoggingObserverFactoryExtensions
    {
        public static IDisposable SubscribeFileLogger(
            this IJobsChain jobsChain,
            ILoggerFactory loggerFactory,
            string queueName)
        {
            if (jobsChain == null) throw new ArgumentNullException(nameof(jobsChain));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            var logger = loggerFactory.CreateLogger($"TplQueue.{queueName}");
            var obs = FileLoggingObserver.Create(logger, queueName);
            return jobsChain.Subscribe(obs);
        }
    }
}
