using Fmacias.TplQueue.Contracts;
using System;

namespace Fmacias.TplQueue.Observers
{
    /// <summary>
    /// Observers to the console 
    /// </summary>
    internal sealed class ConsoleObserver : IConsoleObserver
    {
        private ConsoleObserver() { }

        public static ConsoleObserver Create()
        {
            return new ConsoleObserver();
        }

        /// <summary>
        /// <inheritdoc cref="IObserver{JobEvent}.OnCompleted"/>
        /// Nofication ignored
        /// </summary>
        public void OnCompleted() { }
        /// <summary>
        /// <inheritdoc cref="IObserver{JobEvent}.OnError(Exception)"/>
        /// Notfies error to the console
        /// </summary>
        /// <param name="error"></param>
        public void OnError(Exception error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));

            Console.WriteLine($"[ERROR] {error.Message}");
        }
        /// <summary>
        /// <inheritdoc cref="IObserver{JobEvent}.OnNext(JobEvent)"/>
        /// Notfies status changes to the console
        /// </summary>
        /// <param name="value"></param>
        public void OnNext(IJobEvent value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            Console.WriteLine($"[EVENT] {value.Status} - {value.JobInfo.Name}");
        }
    }
}
