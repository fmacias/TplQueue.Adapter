using System;
using System.Text;
using Fmacias.TplQueue.Contracts;
using Microsoft.Extensions.Logging;

namespace Fmacias.TplQueue.Observers
{
    /// <summary>
    /// Observer que vuelca IJobEvent a ILogger. Diseñado para poder
    /// configurar NLog/Serilog con un archivo por cola usando el nombre del logger.
    /// </summary>
    public sealed class FileLoggingObserver : IObserver<IJobEvent>
    {
        private readonly ILogger _logger;
        private readonly string _queueName;

        private FileLoggingObserver(ILogger logger, string queueName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queueName = queueName ?? string.Empty;
        }

        public static FileLoggingObserver Create(ILogger logger, string queueName)
            => new FileLoggingObserver(logger, queueName);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
        public void OnCompleted()
        {
            _logger.LogInformation("[{Queue}] OBSERVER COMPLETED", _queueName);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
        public void OnError(Exception error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));

            _logger.LogError(error, "[{Queue}] OBSERVER ERROR: {Message}", _queueName, error.Message);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
        public void OnNext(IJobEvent value)
        {
            if (value is null)
            {
                _logger.LogWarning("[{Queue}] NULL event received", _queueName);
                return;
            }

            var sb = new StringBuilder();
            sb.Append("Status=").Append(value.Status)
              .Append(" | Runner=").Append(value.JobDTO?.Name ?? "(null)")
              .Append(" | Start=").Append(value.JobDTO?.ExecutionStart.ToString("O"))
              .Append(" | End=").Append(value.JobDTO?.ExecutionEnd.ToString("O"))
              .Append(" | Elapsed=").Append(value.JobDTO?.ExecutionTime)
              .Append(" | Retries=").Append(value.RetryCount);

            if (value.Exception != null)
                sb.Append(" | Exception=").Append(value.Exception.GetType().Name).Append(": ").Append(value.Exception.Message);

            _logger.LogInformation("[{Queue}] {Line}", _queueName, sb.ToString());
        }
    }
}
