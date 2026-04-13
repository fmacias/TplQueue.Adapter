using Fmacias.TplQueue.Contracts;
using System;

namespace Fmacias.TplQueue.Observers
{
    public sealed class ConsoleObserver : IConsoleObserver
    {
        private ConsoleObserver() { }

        public static ConsoleObserver Create()
        {
            return new ConsoleObserver();
        }

        public void OnCompleted() { }

        public void OnError(Exception error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));
            Console.WriteLine($"[ERROR] {error.Message}");
        }

        public void OnNext(IJobEvent value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            Console.WriteLine($"[EVENT] {value.Status} - {value.JobInfo.Name}");
        }
    }
}
