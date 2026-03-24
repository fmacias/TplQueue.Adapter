using Fmacias.TplQueue.Contracts;
using Microsoft.Extensions.Logging;
using System;

namespace Fmacias.TplQueue.Log
{
    public static class LoggingObserverFactoryExtensions
    {
        public static IDisposable SubscribeFileLogger(
            this IQ queue,
            ILoggerFactory loggerFactory,
            string queueName)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            var logger = loggerFactory.CreateLogger($"TplQueue.{queueName}");
            var observer = FileLoggingObserver.Create(logger, queueName);
            return queue.Subscribe(observer);
        }
    }
}
