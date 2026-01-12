using Fmaciasruano.TplQueue.Abstractions.Contracts;
using System;

namespace Fmaciasruano.TplQueue.Observers
{
    /// <summary>
    /// Observers to the console 
    /// </summary>
    internal sealed class TaskRunnerConsoleObserver : ITaskRunnerConsoleObserver
    {
        private TaskRunnerConsoleObserver() { }

        public static TaskRunnerConsoleObserver Create()
        {
            return new TaskRunnerConsoleObserver();
        }

        /// <summary>
        /// <inheritdoc cref="IObserver{TaskRunnerEvent}.OnCompleted"/>
        /// Nofication ignored
        /// </summary>
        public void OnCompleted() { }
        /// <summary>
        /// <inheritdoc cref="IObserver{TaskRunnerEvent}.OnError(Exception)"/>
        /// Notfies error to the console
        /// </summary>
        /// <param name="error"></param>
        public void OnError(Exception error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));

            Console.WriteLine($"[ERROR] {error.Message}");
        }
        /// <summary>
        /// <inheritdoc cref="IObserver{TaskRunnerEvent}.OnNext(TaskRunnerEvent)"/>
        /// Notfies status changes to the console
        /// </summary>
        /// <param name="value"></param>
        public void OnNext(ITaskRunnerEvent value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            Console.WriteLine($"[EVENT] {value.Status} - {value.RunnerDTO.Name}");
        }
    }
}
