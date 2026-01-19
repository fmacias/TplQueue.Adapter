using System;
using System.Text;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Fmaciasruano.TplQueue.Observers
{
    /// <summary>
    /// Observer que vuelca ITaskRunnerEvent a ILogger. Diseñado para poder
    /// configurar NLog/Serilog con un archivo por cola usando el nombre del logger.
    /// </summary>
    public sealed class TaskRunnerFileLoggingObserver : IObserver<ITaskRunnerEvent>
    {
        private readonly ILogger _logger;
        private readonly string _queueName;

        private TaskRunnerFileLoggingObserver(ILogger logger, string queueName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queueName = queueName ?? string.Empty;
        }

        public static TaskRunnerFileLoggingObserver Create(ILogger logger, string queueName)
            => new TaskRunnerFileLoggingObserver(logger, queueName);

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
        public void OnNext(ITaskRunnerEvent value)
        {
            if (value is null)
            {
                _logger.LogWarning("[{Queue}] NULL event received", _queueName);
                return;
            }

            var sb = new StringBuilder();
            sb.Append("Status=").Append(value.Status)
              .Append(" | Runner=").Append(value.RunnerDTO?.Name ?? "(null)")
              .Append(" | Start=").Append(value.RunnerDTO?.ExecutionStart.ToString("O"))
              .Append(" | End=").Append(value.RunnerDTO?.ExecutionEnd.ToString("O"))
              .Append(" | Elapsed=").Append(value.RunnerDTO?.ExecutionTime)
              .Append(" | Retries=").Append(value.RetryCount);

            if (value.Exception != null)
                sb.Append(" | Exception=").Append(value.Exception.GetType().Name).Append(": ").Append(value.Exception.Message);

            _logger.LogInformation("[{Queue}] {Line}", _queueName, sb.ToString());
        }
    }
}
