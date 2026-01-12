using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Observers;
using Fmaciasruano.TplQueue.Observers.ViewModel;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Fmaciasruano.TplQueue.Test.Observers
{
    [TestFixture()]
    public class TaskRunnerObserverTests
    {
        private Mock<ITaskRunnerEvent>? _eventMock;
        private Mock<ITaskRunnerInfo>? _infoMock;
        private DateTime _now;

        [SetUp]
        public void SetUp()
        {
            _now = DateTime.Now;
            _infoMock = new Mock<ITaskRunnerInfo>();
            _infoMock.Setup(i => i.Name).Returns("TestTask");

            _eventMock = new Mock<ITaskRunnerEvent>();
            _eventMock.Setup(e => e.Status).Returns(TaskRunnerEventStatus.Successed);
            _eventMock.Setup(e => e.RunnerDTO).Returns(_infoMock.Object);
            _eventMock.Setup(e => e.Timestamp).Returns(_now);
        }

        [Test]
        public void TaskRunnerConsoleObserver_ShouldWriteToConsole()
        {
            var observer = TaskRunnerConsoleObserver.Create();
            Assert.DoesNotThrow(() => observer.OnNext(_eventMock!.Object));
            Assert.DoesNotThrow(() => observer.OnError(new Exception("fail")));
            Assert.DoesNotThrow(() => observer.OnCompleted());
        }

        [Test]
        public void TaskRunnerProfilingObserver_ShouldLogEvents()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<IProfilingObserver>>();

            // Enable logging for the levels used by the observer
            loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            var observer = TaskRunnerProfilingObserver.Create(loggerMock.Object);

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
                        v.ToString()!.Contains("TaskRunnerProfilingObserver", StringComparison.OrdinalIgnoreCase)), It.Is<Exception>(ex => ex.Message.Contains("boom")),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public void TaskRunnerViewModelObserver_ShouldAddEventsToCollection()
        {
            var dispatcherMock = new Mock<IObserverDispatcher>();
            var observer = TaskRunnerViewModelObserver.Create(dispatcherMock.Object);

            dispatcherMock.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());

            observer.OnNext(_eventMock!.Object);
            observer.OnError(new Exception("problem"));
            observer.OnCompleted();

            Assert.That(observer.ProgressEvents.Count, Is.EqualTo(3));
            Assert.That(observer.ErrorEvents.Count, Is.EqualTo(1));
            Assert.That(observer.CompleteEvents.Count, Is.EqualTo(1));
       }

        [Test]
        public void TaskRunnerViewModelObserver_ShouldIgnoreEventsAfterCompletion()
        {
            var dispatcherMock = new Mock<IObserverDispatcher>();
            dispatcherMock.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());

            var observer = TaskRunnerViewModelObserver.Create(dispatcherMock.Object);

            observer.OnCompleted();

            observer.OnNext(_eventMock!.Object);
            observer.OnError(new Exception("late"));

            Assert.That(observer.ProgressEvents.Count, Is.EqualTo(1),
                "Progress should only contain completion once terminal state reached.");
            Assert.That(observer.ErrorEvents, Is.Empty);
            Assert.That(observer.CompleteEvents.Count, Is.EqualTo(1));
        }

        [Test]
        public void TaskRunnerObserverFactory_ShouldCreateObservers()
        {
            var loggerMock = new Mock<ILogger<IProfilingObserver>>();
            var dispatcherMock = new Mock<IObserverDispatcher>();

            var consoleObs = TaskRunnerConsoleObserver.Create();
            var profilerObs = TaskRunnerProfilingObserver.Create(loggerMock.Object);
            var viewModelObs = TaskRunnerViewModelObserver.Create(dispatcherMock.Object);

            Assert.IsInstanceOf<TaskRunnerConsoleObserver>(consoleObs);
            Assert.IsInstanceOf<TaskRunnerProfilingObserver>(profilerObs);
            Assert.IsInstanceOf<TaskRunnerViewModelObserver>(viewModelObs);
        }
    }
}
