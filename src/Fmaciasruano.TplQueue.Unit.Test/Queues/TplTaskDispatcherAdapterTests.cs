using System;
using System.Threading;
using System.Threading.Tasks;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Queues;
using Moq;
using NUnit.Framework;

namespace Fmaciasruano.TplQueue.Test.Queues
{
    [TestFixture]
    public class TplTaskDispatcherAdapterTests
    {
        [Test]
        public void FactoryConstructor_ShouldThrowWhenFactoryReturnsNull()
        {
            var adapter = new TplTaskDispatcherAdapter(() => null!);

            Assert.Throws<InvalidOperationException>(() => _ = adapter.Name);
        }

        [Test]
        public void FactoryConstructor_ShouldInvokeFactoryOnce()
        {
            var creationCount = 0;
            var innerDispatcher = CreateDispatcherMock().Object;

            var adapter = new TplTaskDispatcherAdapter(() =>
            {
                creationCount++;
                return innerDispatcher;
            });

            adapter.StartPolling();
            adapter.StopPolling();
            adapter.StartPolling();

            Assert.That(creationCount, Is.EqualTo(1));
        }

        private static Mock<ITaskDispatcher> CreateDispatcherMock()
        {
            var dispatcherMock = new Mock<ITaskDispatcher>();
            dispatcherMock.SetupProperty(d => d.InternalEventDelegator);
            dispatcherMock.SetupGet(d => d.Semaphore).Returns(new SemaphoreSlim(1));
            dispatcherMock.SetupGet(d => d.PulseMs).Returns(100);
            dispatcherMock.SetupGet(d => d.RetryPolicyFactory).Returns(() => Mock.Of<IRetryPolicy>());
            dispatcherMock.SetupGet(d => d.Name).Returns("inner");
            dispatcherMock.SetupGet(d => d.MaxParallelism).Returns(1);
            dispatcherMock.SetupGet(d => d.IsDisposed).Returns(false);
            dispatcherMock.Setup(d => d.StartPolling());
            dispatcherMock.Setup(d => d.StopPolling());
            dispatcherMock.Setup(d => d.Dispose());
            dispatcherMock.Setup(d => d.Subscribe(It.IsAny<IObserver<ITaskRunnerEvent>>())).Returns(Mock.Of<IDisposable>());
            dispatcherMock.Setup(d => d.Enqueue(It.IsAny<ITaskRunnerRoot>(), It.IsAny<CancellationToken>())).Returns(dispatcherMock.Object);
            dispatcherMock.Setup(d => d.EnqueueFifo(It.IsAny<ITaskRunnerRoot>(), It.IsAny<CancellationToken>())).Returns(dispatcherMock.Object);
            dispatcherMock.Setup(d => d.AddToQueue(It.IsAny<ITaskRunnerRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(dispatcherMock.Object);
            return dispatcherMock;
        }
    }
}
