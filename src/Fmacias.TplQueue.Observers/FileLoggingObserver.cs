using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults.Log;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace Fmacias.TplQueue.Observers
{
    internal sealed class FileLoggingObserver : IFileLoggingObserver
    {
        private readonly ILogger _logger;
        private readonly string _queueName;

        private FileLoggingObserver(ILogger logger, string queueName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queueName = queueName ?? string.Empty;
        }

        public static FileLoggingObserver Create(ILogger logger, string queueName)
        {
            return new FileLoggingObserver(logger, queueName);
        }

        public void OnCompleted()
        {
            LogMessages.FileObserverCompleted(_logger, _queueName, null);
        }

        public void OnError(Exception error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));
            LogMessages.FileObserverError(_logger, _queueName, error.Message, error);
        }

        public void OnNext(IJobEvent value)
        {
            if (value == null)
            {
                LogMessages.FileObserverNullEvent(_logger, _queueName, null);
                return;
            }

            if (!_logger.IsEnabled(LogLevel.Information))
            {
                return;
            }

            var sb = new StringBuilder();
            sb.Append("Status=").Append(value.Status)
              .Append(" | Runner=").Append(value.JobInfo?.Name ?? "(null)")
              .Append(" | Start=").Append(value.JobInfo?.ExecutionStart.ToString("O"))
              .Append(" | End=").Append(value.JobInfo?.ExecutionEnd.ToString("O"))
              .Append(" | Elapsed=").Append(value.JobInfo?.ExecutionTime)
              .Append(" | Retries=").Append(value.RetryCount);

            if (value.Exception != null)
            {
                sb.Append(" | Exception=").Append(value.Exception.GetType().Name).Append(": ").Append(value.Exception.Message);
            }

            LogMessages.FileObserverEventWritten(_logger, _queueName, sb.ToString(), null);
        }
    }
}
