using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Fmacias.TplQueue.Observers.ViewModel;
using Fmacias.TplQueue.Observers;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Test.Observers
{
    [TestFixture()]
    public class JobViewModelObserverTests
    {
#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
        private class DummyEvent : IJobEvent
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
        {
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
            public IJobInfo JobInfo { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
            public JobEventStatus Status { get; set; } = JobEventStatus.Successed;

            public Exception Exception => throw new NotImplementedException();

            public TimeSpan ExecutionTime => throw new NotImplementedException();

            public DateTime ExecutionStart => throw new NotImplementedException();

            public DateTime ExecutionEnd => throw new NotImplementedException();

            public int RetryCount => throw new NotImplementedException();

            public IReadOnlyDictionary<string, object> CustomMetadata => throw new NotImplementedException();
        }

        [Test]
        public void PerformanceObserver_LogsUsingILogger()
        {
            var logs = new List<string>();
            var logger = new MockLogger<IProfilingObserver>(logs);
            var observer = ProfilingObserver.Create(logger);
    
            var mockedRunnerDto = new Mock<IJobInfo>(MockBehavior.Strict);
            mockedRunnerDto.Setup(o => o.Name).Returns("Dummy");
            var dummyEvent = new DummyEvent()
            {
                JobInfo = mockedRunnerDto.Object
            };
            observer.OnNext(dummyEvent);

            Assert.IsTrue(logs.Exists(msg => msg.Contains("Dummy")));
        }

        private class MockLogger<T> : ILogger<T>
        {
            private readonly List<string> _logs;
            public MockLogger(List<string> logs) => _logs = logs;

#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
            public IDisposable? BeginScope<TState>(TState state) => null;
#pragma warning restore CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
            public bool IsEnabled(LogLevel logLevel) => true;

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
            {
                _logs.Add(formatter(state, exception));
            }
        }
    }
}
