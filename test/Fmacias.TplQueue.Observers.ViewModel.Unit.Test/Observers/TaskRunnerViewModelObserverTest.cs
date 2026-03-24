using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Observers;
using Fmacias.TplQueue.Observers.ViewModel;
using Moq;
using NUnit.Framework;
using System;

namespace Fmacias.TplQueue.Observers.ViewModel.Test
{
    [TestFixture]
    public class JobViewModelObserverTests
    {
        [Test]
        public void JobViewModelObserver_ShouldAddEventsToCollection()
        {
            var dispatcherMock = new Mock<IObserverDispatcher>();
            dispatcherMock.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());

            var observer = ViewModelObserver.Create(dispatcherMock.Object);
            observer.OnNext(CreateEvent());
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
            observer.OnNext(CreateEvent());
            observer.OnError(new Exception("late"));

            Assert.That(observer.ProgressEvents.Count, Is.EqualTo(1));
            Assert.That(observer.ErrorEvents, Is.Empty);
            Assert.That(observer.CompleteEvents.Count, Is.EqualTo(1));
        }

        [Test]
        public void DirectObserverDispatcher_InvokesActionInline()
        {
            var executed = false;
            var dispatcher = DirectObserverDispatcher.Create();

            dispatcher.Invoke(() => executed = true);

            Assert.That(executed, Is.True);
        }

        private static IJobEvent CreateEvent()
        {
            var info = new Mock<IJobInfo>();
            info.SetupGet(x => x.Name).Returns("Dummy");

            var evt = new Mock<IJobEvent>();
            evt.SetupGet(x => x.Timestamp).Returns(DateTime.UtcNow);
            evt.SetupGet(x => x.JobInfo).Returns(info.Object);
            evt.SetupGet(x => x.Status).Returns(JobEventStatus.Successed);
            evt.SetupGet(x => x.RetryCount).Returns(0);
            return evt.Object;
        }
    }
}
