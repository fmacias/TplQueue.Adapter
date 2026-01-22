using System;
using System.Threading;
using System.Threading.Tasks;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Queues;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Test.Queues
{
    [TestFixture]
    public class QAdapterTests
    {
        [Test]
        public void FactoryConstructor_ShouldThrowWhenFactoryReturnsNull()
        {
            var adapter = new QAdapter(() => null!);

            Assert.Throws<InvalidOperationException>(() => _ = adapter.Name);
        }

        [Test]
        public void FactoryConstructor_ShouldInvokeFactoryOnce()
        {
            var creationCount = 0;
            var innerDispatcher = CreateDispatcherMock().Object;

            var adapter = new QAdapter(() =>
            {
                creationCount++;
                return innerDispatcher;
            });

            adapter.Start();
            adapter.Pause();
            adapter.Start();

            Assert.That(creationCount, Is.EqualTo(1));
        }

        private static Mock<IJobQ> CreateDispatcherMock()
        {
            var dispatcherMock = new Mock<IJobQ>();
            dispatcherMock.SetupProperty(d => d.OnEventChange);
            dispatcherMock.SetupGet(d => d.Semaphore).Returns(new SemaphoreSlim(1));
            dispatcherMock.SetupGet(d => d.PulseMs).Returns(100);
            dispatcherMock.SetupGet(d => d.RetryPolicyFactory).Returns(() => Mock.Of<IRetryPolicy>());
            dispatcherMock.SetupGet(d => d.Name).Returns("inner");
            dispatcherMock.SetupGet(d => d.MaxParallelism).Returns(1);
            dispatcherMock.SetupGet(d => d.IsDisposed).Returns(false);
            dispatcherMock.Setup(d => d.Start());
            dispatcherMock.Setup(d => d.Pause());
            dispatcherMock.Setup(d => d.Dispose());
            dispatcherMock.Setup(d => d.Subscribe(It.IsAny<IObserver<IJobEvent>>())).Returns(Mock.Of<IDisposable>());
            dispatcherMock.Setup(d => d.Enqueue(It.IsAny<IJobRoot>(), It.IsAny<CancellationToken>())).Returns(dispatcherMock.Object);
            dispatcherMock.Setup(d => d.EnqueueFifo(It.IsAny<IJobRoot>(), It.IsAny<CancellationToken>())).Returns(dispatcherMock.Object);
            dispatcherMock.Setup(d => d.Enqueue(It.IsAny<IJobRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(dispatcherMock.Object);
            return dispatcherMock;
        }
    }
}
