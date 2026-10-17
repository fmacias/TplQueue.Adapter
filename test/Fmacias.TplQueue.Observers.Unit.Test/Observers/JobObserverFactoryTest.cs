using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults.Log;
using Fmacias.TplQueue.Observers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;

namespace Fmacias.TplQueue.Observers.Test.Observers
{
    [TestFixture]
    public class JobObserverFactoryTest
    {
        private Mock<IJobEvent> _eventMock = null!;
        private Mock<IJobInfo> _infoMock = null!;
        private DateTime _now;
        private IObserverFactory _observerFactory = null!;

        [SetUp]
        public void SetUp()
        {
            _now = DateTime.Now;
            _infoMock = new Mock<IJobInfo>();
            _infoMock.Setup(i => i.Name).Returns("TestTask");

            _eventMock = new Mock<IJobEvent>();
            _eventMock.Setup(e => e.Status).Returns(JobEventStatus.Successed);
            _eventMock.Setup(e => e.JobInfo).Returns(_infoMock.Object);
            _eventMock.Setup(e => e.Timestamp).Returns(_now);

            _observerFactory = ObserverFactory.Create();
        }

        [Test]
        public void JobConsoleObserver_FromFactory_ShouldWriteToConsole()
        {
            var observer = _observerFactory.CreateConsoleObserver();
            Assert.DoesNotThrow(() => observer.OnNext(_eventMock.Object));
            Assert.DoesNotThrow(() => observer.OnError(new Exception("fail")));
            Assert.DoesNotThrow(() => observer.OnCompleted());
        }

        [Test]
        public void JobProfilingObserver_FromFactory_ShouldLogEvents()
        {
            var loggerMock = new Mock<ILogger<IProfilingObserver>>();
            loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            var observer = _observerFactory.CreateProfilingObserver(loggerMock.Object);

            observer.OnNext(_eventMock.Object);
            observer.OnCompleted();
            observer.OnError(new Exception("boom"));

            loggerMock.Verify(l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v != null),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);

            loggerMock.Verify(l => l.Log(
                    LogLevel.Error,
                    It.Is<EventId>(e => e.Id == 3000),
                    It.Is<It.IsAnyType>((v, _) =>
                        (v.ToString() ?? string.Empty).Contains("JobProfilingObserver", StringComparison.OrdinalIgnoreCase)),
                    It.Is<Exception?>(ex => ex != null && ex.Message.Contains("boom")),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public void JobObserverFactory_ShouldCreateDedicatedObservers()
        {
            var profilingLoggerMock = new Mock<ILogger<IProfilingObserver>>();
            var loggingLoggerMock = new Mock<ILogger<ILoggingObserver>>();
            var fileLoggerMock = new Mock<ILogger>();

            var consoleObserver = _observerFactory.CreateConsoleObserver();
            var loggingObserver = _observerFactory.CreateLoggingObserver(loggingLoggerMock.Object);
            var fileLoggingObserver = _observerFactory.CreateFileLoggingObserver(fileLoggerMock.Object, "main");
            var profilingObserver = _observerFactory.CreateProfilingObserver(profilingLoggerMock.Object);
            var dispatcher = _observerFactory.CreateObserverDispatcher();

            Assert.That(consoleObserver, Is.InstanceOf<IConsoleObserver>());
            Assert.That(loggingObserver, Is.InstanceOf<ILoggingObserver>());
            Assert.That(fileLoggingObserver, Is.InstanceOf<IFileLoggingObserver>());
            Assert.That(profilingObserver, Is.InstanceOf<IProfilingObserver>());
            Assert.That(dispatcher, Is.InstanceOf<IObserverDispatcher>());
        }

        [Test]
        public void JobObserverFactory_WhenLoggerIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _observerFactory.CreateLoggingObserver(null!));
            Assert.Throws<ArgumentNullException>(() => _observerFactory.CreateProfilingObserver(null!));
            Assert.Throws<ArgumentNullException>(() => _observerFactory.CreateFileLoggingObserver(null!, "main"));
        }

        [Test]
        public void CreateJobEventSnapshot_ThroughFactoryContract_CapturesEventMetadata()
        {
            // Arrange
            var id = Guid.NewGuid();
            _infoMock.Setup(info => info.Id).Returns(id);
            _infoMock.Setup(info => info.Dependencies).Returns(Array.Empty<IJobInfo>());
            _eventMock.Setup(value => value.RetryCount).Returns(2);
            _eventMock.As<IJobExecutionEvent>().Setup(value => value.ExecutionChannel).Returns(0);
            _eventMock.Setup(value => value.Exception).Returns(new InvalidOperationException("failure"));

            // Act
            var snapshot = _observerFactory.CreateJobEventSnapshot(_eventMock.Object);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Status, Is.EqualTo(JobEventStatus.Successed));
                Assert.That(snapshot.Timestamp, Is.EqualTo(_now));
                Assert.That(snapshot.RetryCount, Is.EqualTo(2));
                Assert.That(snapshot.ExecutionChannel, Is.EqualTo(0));
                Assert.That(snapshot.JobInfo.Id, Is.EqualTo(id));
                Assert.That(snapshot.JobInfo.Name, Is.EqualTo("TestTask"));
                Assert.That(snapshot.Diagnostic.Message, Is.EqualTo("failure"));
            });
        }

        [Test]
        public void CreateJobEventSnapshot_RepeatedCalls_ReturnIndependentSnapshots()
        {
            // Arrange
            _infoMock.Setup(info => info.Dependencies).Returns(Array.Empty<IJobInfo>());

            // Act
            var first = _observerFactory.CreateJobEventSnapshot(_eventMock.Object);
            _infoMock.Setup(info => info.Name).Returns("Changed");
            var second = _observerFactory.CreateJobEventSnapshot(_eventMock.Object);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(second, Is.Not.SameAs(first));
                Assert.That(second.JobInfo, Is.Not.SameAs(first.JobInfo));
                Assert.That(first.JobInfo.Name, Is.EqualTo("TestTask"));
                Assert.That(second.JobInfo.Name, Is.EqualTo("Changed"));
            });
        }

        [Test]
        public void CreateJobEventSnapshot_NullSource_PropagatesArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => _observerFactory.CreateJobEventSnapshot(null!));

            Assert.That(exception!.ParamName, Is.EqualTo("source"));
        }

        [Test]
        public void CreateJobEventSnapshot_NullJobInfo_PropagatesArgumentException()
        {
            _eventMock.Setup(value => value.JobInfo).Returns((IJobInfo)null!);

            var exception = Assert.Throws<ArgumentException>(
                () => _observerFactory.CreateJobEventSnapshot(_eventMock.Object));

            Assert.That(exception!.ParamName, Is.EqualTo("source"));
        }

        [Test]
        public void CreateJobEventSnapshot_CyclicDependencies_PropagatesInvalidOperationException()
        {
            _infoMock.Setup(info => info.Dependencies).Returns(new[] { _infoMock.Object });

            Assert.Throws<InvalidOperationException>(
                () => _observerFactory.CreateJobEventSnapshot(_eventMock.Object));
        }

        [Test]
        public void FileLoggingObserver_WhenInformationLoggingIsDisabled_DoesNotEvaluateEventDetails()
        {
            var loggerMock = new Mock<ILogger>();
            loggerMock.Setup(logger => logger.IsEnabled(LogLevel.Information)).Returns(false);
            var eventMock = new Mock<IJobEvent>(MockBehavior.Strict);
            var observer = _observerFactory.CreateFileLoggingObserver(loggerMock.Object, "main");

            observer.OnNext(eventMock.Object);

            loggerMock.Verify(
                logger => logger.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Test]
        public void FileLoggingObserver_WhenInformationLoggingIsEnabled_UsesSharedEventMessage()
        {
            var loggerMock = new Mock<ILogger>();
            loggerMock.Setup(logger => logger.IsEnabled(LogLevel.Information)).Returns(true);
            var observer = _observerFactory.CreateFileLoggingObserver(loggerMock.Object, "main");

            observer.OnNext(_eventMock.Object);

            loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    EventCatalog.FileObserverEventWritten,
                    It.Is<It.IsAnyType>((value, _) =>
                        (value.ToString() ?? string.Empty).Contains("[main] Status=Successed", StringComparison.Ordinal)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
