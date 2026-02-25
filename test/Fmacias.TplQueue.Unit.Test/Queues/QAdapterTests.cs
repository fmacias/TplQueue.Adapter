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
            var innerQ = CreateDispatcherMock().Object;

            var adapter = new QAdapter(() =>
            {
                creationCount++;
                return innerQ;
            });

            adapter.Start();
            adapter.Pause();
            adapter.Start();

            Assert.That(creationCount, Is.EqualTo(1));
        }

        private static Mock<IQ> CreateDispatcherMock()
        {
            var jobQMock = new Mock<IQ>();
            jobQMock.SetupProperty(d => d.OnJobEventChanged);
            jobQMock.SetupGet(d => d.Semaphore).Returns(new SemaphoreSlim(1));
            jobQMock.SetupGet(d => d.RetryPolicyFactory).Returns(() => Mock.Of<IRetryPolicy>());
            jobQMock.SetupGet(d => d.Name).Returns("inner");
            jobQMock.SetupGet(d => d.MaxParallelism).Returns(1);
            jobQMock.SetupGet(d => d.IsDisposed).Returns(false);
            jobQMock.Setup(d => d.Start());
            jobQMock.Setup(d => d.Pause());
            jobQMock.Setup(d => d.Dispose());
            jobQMock.Setup(d => d.Subscribe(It.IsAny<IObserver<IJobEvent>>())).Returns(Mock.Of<IDisposable>());
            jobQMock.Setup(d => d.Enqueue(It.IsAny<IJobRoot>(), It.IsAny<CancellationToken>())).Returns(jobQMock.Object);
            jobQMock.Setup(d => d.EnqueueFifo(It.IsAny<IJobRoot>(), It.IsAny<CancellationToken>())).Returns(jobQMock.Object);
            jobQMock.Setup(d => d.Enqueue(It.IsAny<IJobRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(jobQMock.Object);
            return jobQMock;
        }
    }
}
