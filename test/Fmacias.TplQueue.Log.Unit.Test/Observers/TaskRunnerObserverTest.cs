using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Observers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;

namespace Fmacias.TplQueue.Log.Test.Observers
{
    [TestFixture]
    public class JobObserverTests
    {
        private Mock<IJobEvent> _eventMock;
        private Mock<IJobInfo> _infoMock;
        private DateTime _now;

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
        }

        [Test]
        public void JobConsoleObserver_ShouldWriteToConsole()
        {
            var observer = ConsoleObserver.Create();
            Assert.DoesNotThrow(() => observer.OnNext(_eventMock.Object));
            Assert.DoesNotThrow(() => observer.OnError(new Exception("fail")));
            Assert.DoesNotThrow(() => observer.OnCompleted());
        }

        [Test]
        public void JobProfilingObserver_ShouldLogEvents()
        {
            var loggerMock = new Mock<ILogger<IProfilingObserver>>();
            loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            var observer = ProfilingObserver.Create(loggerMock.Object);

            observer.OnNext(_eventMock.Object);
            observer.OnCompleted();
            observer.OnError(new Exception("boom"));

            loggerMock.Verify(l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v != null),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);

            loggerMock.Verify(l => l.Log(
                    LogLevel.Error,
                    It.Is<EventId>(e => e.Id == 3000),
                    It.Is<It.IsAnyType>((v, _) =>
                        v.ToString().Contains("JobProfilingObserver", StringComparison.OrdinalIgnoreCase)),
                    It.Is<Exception>(ex => ex.Message.Contains("boom")),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public void JobObserverFactory_ShouldCreateObservers()
        {
            var loggerMock = new Mock<ILogger<IProfilingObserver>>();
            var consoleObs = ConsoleObserver.Create();
            var profilerObs = ProfilingObserver.Create(loggerMock.Object);

            Assert.IsInstanceOf<ConsoleObserver>(consoleObs);
            Assert.IsInstanceOf<ProfilingObserver>(profilerObs);
        }
    }
}
