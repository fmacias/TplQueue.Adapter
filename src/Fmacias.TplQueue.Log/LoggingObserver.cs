using Fmacias.TplQueue.Contracts;
using Microsoft.Extensions.Logging;
using System;

namespace Fmacias.TplQueue.Log
{
    public sealed class LoggingObserver : ILoggingObserver
    {
        private readonly ILogger<ILoggingObserver> _logger;

        private LoggingObserver(ILogger<ILoggingObserver> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public static LoggingObserver Create(ILogger<ILoggingObserver> logger)
        {
            return new LoggingObserver(logger);
        }

        public void OnNext(IJobEvent e)
        {
            var name = e?.JobInfo?.Name ?? "<unknown>";
            switch (e?.Status)
            {
                case JobEventStatus.Cache:
                case JobEventStatus.Enqueueing:
                    _logEnqueueing(_logger, name, null);
                    break;
                case JobEventStatus.Enqueued:
                    _logEnqueued(_logger, name, null);
                    break;
                case JobEventStatus.Dequeued:
                    _logDequeued(_logger, name, null);
                    break;
                case JobEventStatus.Started:
                    _logStarted(_logger, name, null);
                    break;
                case JobEventStatus.Running:
                    _logRunning(_logger, name, e.RetryCount, null);
                    break;
                case JobEventStatus.Successed:
                    _logFinalized(_logger, name, null);
                    break;
                case JobEventStatus.RootSuccessed:
                    _logRootFinalized(_logger, name, null);
                    break;
                case JobEventStatus.Canceled:
                    _logCanceled(_logger, name, e.Exception);
                    break;
                case JobEventStatus.Failed:
                    _logFailed(_logger, name, e.Exception);
                    break;
                case JobEventStatus.Requeuing:
                    _logReenqueue(_logger, name, e.RetryCount, null);
                    break;
                default:
                    _logUnknown(_logger, name, null);
                    break;
            }
        }

        public void OnError(Exception error) => _logObserverError(_logger, error);

        public void OnCompleted() => _logObserverCompleted(_logger, null);

        private static readonly Action<ILogger, string, Exception?> _logEnqueueing =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1000, nameof(OnNext)), "Enqueueing '{TaskName}'");

        private static readonly Action<ILogger, string, Exception?> _logEnqueued =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(1001, nameof(OnNext)), "Enqueued '{TaskName}'");

        private static readonly Action<ILogger, string, Exception?> _logDequeued =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1002, nameof(OnNext)), "Dequeued '{TaskName}'");

        private static readonly Action<ILogger, string, Exception?> _logStarted =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1003, nameof(OnNext)), "Started '{TaskName}'");

        private static readonly Action<ILogger, string, int, Exception?> _logRunning =
            LoggerMessage.Define<string, int>(LogLevel.Trace, new EventId(1004, nameof(OnNext)), "Running '{TaskName}' (retry={Retry})");

        private static readonly Action<ILogger, string, Exception?> _logFinalized =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(1005, nameof(OnNext)), "Finalized '{TaskName}'");

        private static readonly Action<ILogger, string, Exception?> _logCanceled =
            LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1006, nameof(OnNext)), "Canceled '{TaskName}'");

        private static readonly Action<ILogger, string, Exception?> _logFailed =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(1007, nameof(OnNext)), "Failed '{TaskName}'");

        private static readonly Action<ILogger, string, int, Exception?> _logReenqueue =
            LoggerMessage.Define<string, int>(LogLevel.Warning, new EventId(1008, nameof(OnNext)), "Re-enqueue '{TaskName}' (retry={Retry})");

        private static readonly Action<ILogger, string, Exception?> _logUnknown =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1010, nameof(OnNext)), "Unknown status for '{TaskName}'");

        private static readonly Action<ILogger, Exception?> _logObserverError =
            LoggerMessage.Define(LogLevel.Error, new EventId(1100, nameof(OnError)), "Logging observer error");

        private static readonly Action<ILogger, Exception?> _logObserverCompleted =
            LoggerMessage.Define(LogLevel.Debug, new EventId(1099, nameof(OnCompleted)), "Logging observer completed");

        private static readonly Action<ILogger, string, Exception?> _logRootFinalized =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(1005, nameof(OnNext)), "Root Finalized '{TaskName}'");
    }
}
