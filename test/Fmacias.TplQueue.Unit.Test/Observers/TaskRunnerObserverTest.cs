using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Observers;
using Fmacias.TplQueue.Observers.ViewModel;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Test.Observers
{
    [TestFixture()]
    public class JobObserverTests
    {
        private Mock<IJobEvent>? _eventMock;
        private Mock<IJobInfo>? _infoMock;
        private DateTime _now;

        [SetUp]
        public void SetUp()
        {
            _now = DateTime.Now;
            _infoMock = new Mock<IJobInfo>();
            _infoMock.Setup(i => i.Name).Returns("TestTask");

            _eventMock = new Mock<IJobEvent>();
            _eventMock.Setup(e => e.Status).Returns(JobEventStatus.Successed);
            _eventMock.Setup(e => e.JobDTO).Returns(_infoMock.Object);
            _eventMock.Setup(e => e.Timestamp).Returns(_now);
        }

        [Test]
        public void JobConsoleObserver_ShouldWriteToConsole()
        {
            var observer = ConsoleObserver.Create();
            Assert.DoesNotThrow(() => observer.OnNext(_eventMock!.Object));
            Assert.DoesNotThrow(() => observer.OnError(new Exception("fail")));
            Assert.DoesNotThrow(() => observer.OnCompleted());
        }

        [Test]
        public void JobProfilingObserver_ShouldLogEvents()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<IProfilingObserver>>();

            // Enable logging for the levels used by the observer
            loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            var observer = ProfilingObserver.Create(loggerMock.Object);

            observer.OnNext(_eventMock!.Object);
            observer.OnCompleted();
            observer.OnError(new Exception("boom"));

            // Assert: at least one Information log (OnNext / OnCompleted)
            loggerMock.Verify(l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v != null), // we just need the call to happen
                    It.IsAny<Exception>(),                    // null or any
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);

            // Assert: exactly one Error log with the expected EventId and message content
            loggerMock.Verify(l => l.Log(
                    LogLevel.Error,
                    It.Is<EventId>(e => e.Id == 3000), // OnError EventId
                    It.Is<It.IsAnyType>((v, _) =>
                        v.ToString()!.Contains("JobProfilingObserver", StringComparison.OrdinalIgnoreCase)), It.Is<Exception>(ex => ex.Message.Contains("boom")),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public void JobViewModelObserver_ShouldAddEventsToCollection()
        {
            var dispatcherMock = new Mock<IObserverDispatcher>();
            var observer = ViewModelObserver.Create(dispatcherMock.Object);

            dispatcherMock.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());

            observer.OnNext(_eventMock!.Object);
            observer.OnError(new Exception("problem"));
            observer.OnCompleted();

            Assert.That(observer.ProgressEvents.Count, Is.EqualTo(3));
            Assert.That(observer.ErrorEvents.Count, Is.EqualTo(1));
            Assert.That(observer.CompleteEvents.Count, Is.EqualTo(1));
       }

        [Test]
        public void JobViewModelObserver_ShouldIgnoreEventsAfterCompletion()
        {
            var dispatcherMock = new Mock<IObserverDispatcher>();
            dispatcherMock.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());

            var observer = ViewModelObserver.Create(dispatcherMock.Object);

            observer.OnCompleted();

            observer.OnNext(_eventMock!.Object);
            observer.OnError(new Exception("late"));

            Assert.That(observer.ProgressEvents.Count, Is.EqualTo(1),
                "Progress should only contain completion once terminal state reached.");
            Assert.That(observer.ErrorEvents, Is.Empty);
            Assert.That(observer.CompleteEvents.Count, Is.EqualTo(1));
        }

        [Test]
        public void JobObserverFactory_ShouldCreateObservers()
        {
            var loggerMock = new Mock<ILogger<IProfilingObserver>>();
            var dispatcherMock = new Mock<IObserverDispatcher>();

            var consoleObs = ConsoleObserver.Create();
            var profilerObs = ProfilingObserver.Create(loggerMock.Object);
            var viewModelObs = ViewModelObserver.Create(dispatcherMock.Object);

            Assert.IsInstanceOf<ConsoleObserver>(consoleObs);
            Assert.IsInstanceOf<ProfilingObserver>(profilerObs);
            Assert.IsInstanceOf<ViewModelObserver>(viewModelObs);
        }
    }
}
