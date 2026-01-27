using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Factories;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Test.Factories
{
    [TestFixture]
    public class QFactoryAdapterTests
    {
        [Test]
        public void Create_SameInput_IsNeverSingletone()
        {
            var options = new Dictionary<string, IQOptions>();
            var retryFactory = Mock.Of<IRetryPolicyFactory>();
            var coreFactory = Helper.GetQFactoryCoreMock();
            var first = QFactoryAdapter.Create(coreFactory.Object, options, retryFactory);
            var second = QFactoryAdapter.Create(coreFactory.Object, options, retryFactory);
            Assert.That(second, Is.Not.SameAs(first));
        }

        [Test]
        public void Create_DifferentInputs_NeverSingletone()
        {
            var retryFactory1 = Mock.Of<IRetryPolicyFactory>();
            var retryFactory2 = Mock.Of<IRetryPolicyFactory>();
            var coreFactory1 = Helper.GetQFactoryCoreMock();
            var coreFactory2 = Helper.GetQFactoryCoreMock();

            var first = QFactoryAdapter.Create(coreFactory1.Object, new Dictionary<string, IQOptions>(), retryFactory1);
            var second = QFactoryAdapter.Create(coreFactory2.Object, new Dictionary<string, IQOptions>(), retryFactory2);
            Assert.That(second, Is.Not.SameAs(first));
        }

        [Test]
        public void Create_ValidatesArguments()
        {
            var coreFactory = Mock.Of<IQFactoryCore>();
            var retryFactory = Mock.Of<IRetryPolicyFactory>();
            var options = new Dictionary<string, IQOptions>();
            Assert.Throws<ArgumentNullException>(() => QFactoryAdapter.Create(null!,options,retryFactory));
            Assert.Throws<ArgumentNullException>(() => QFactoryAdapter.Create(coreFactory, null!, retryFactory));
            Assert.Throws<ArgumentNullException>(() => QFactoryAdapter.Create(coreFactory,options,null!));
        }

        [Test]
        public void GetQ_CacheablePayloadQ_ThrowsInvalidCast()
        {
            var options = new Dictionary<string, IQOptions>
            {
                { "ser", new TestSerializableOptions() }
            };
            var retryFactory = Helper.GetRetryPolicyFactoryMock();
            var coreQFactory = Helper.GetQFactoryCoreMock();

            var queueFatoryAdapter = QFactoryAdapter.Create(
                coreQFactory.Object, 
                options, 
                retryFactory.Object);

            Assert.Throws<InvalidCastException>(() =>
                queueFatoryAdapter.GetCoreQ<ICacheablePayloadQ>("ser", Helper.GetLogger<ICacheablePayloadQ>()));
        }

        [Test]
        public void GetQ_ReturnsFifoQ()
        {
            var options = new Dictionary<string, IQOptions>
            {
                { "fifo", new TestParallelOptions() }
            };
            var retryFactory = Helper.GetRetryPolicyFactoryMock();
            var coreQFactory = Helper.GetQFactoryCoreMock();
            var qfactoryAdapter = QFactoryAdapter.Create(
                coreQFactory.Object,
                options,
                retryFactory.Object);

            using var queue = qfactoryAdapter
                .GetCoreQ<IFifoQ>("fifo", Helper.GetLogger<IFifoQ>());

            Assert.That(queue, Is.Not.Null);
            Assert.That(queue, Is.InstanceOf<IFifoQ>());
        }


        [Test]
        public void GetQ_ReturnsParallelQ()
        {
            var options = new Dictionary<string, IQOptions>
            {
                { "par", new TestParallelOptions() }
            };

            var retryFactory = Helper.GetRetryPolicyFactoryMock();
            var coreQFactory = Helper.GetQFactoryCoreMock();

            var qfactoryAdapter = QFactoryAdapter.Create(
                coreQFactory.Object,
                options, retryFactory.Object);

            using var queue = qfactoryAdapter.GetCoreQ<IParallelQ>("par", Helper.GetLogger<IParallelQ>());

            Assert.That(queue, Is.Not.Null);
            Assert.That(queue, Is.InstanceOf<IParallelQ>());
        }

        [Test]
        public void GetQ_InvalidCast_Throws()
        {
            var options = new Dictionary<string, IQOptions>
            {
                { "par", new TestParallelOptions() }
            };

            var retryFactory = Helper.GetRetryPolicyFactoryMock();
            var coreQFactory = Helper.GetQFactoryCoreMock();

            var qfactoryAdapter = QFactoryAdapter.Create(
                coreQFactory.Object,
                options, 
                retryFactory.Object);

            Assert.Throws<InvalidCastException>(() =>
            {
                qfactoryAdapter.GetCoreQ<XQ>("par", Helper.GetLogger<XQ>());
            });
        }

        [Test]
        public void GetQ_ValidatesArguments()
        {
            var retryFactory = Helper.GetRetryPolicyFactoryMock();
            var coreQFactory = Helper.GetQFactoryCoreMock();

            var f = QFactoryAdapter.Create(
                coreQFactory.Object,
                new Dictionary<string, IQOptions>(),
                retryFactory.Object);
            
            Assert.Throws<ArgumentException>(() =>
                f.GetCoreQ<IParallelQ>("", Helper.GetLogger<IParallelQ>()));

            Assert.Throws<KeyNotFoundException>(() =>
                f.GetCoreQ<IParallelQ>("x", Helper.GetLogger<IParallelQ>()));
        }

        [Test]
        public void CreateParallel_NullOptions_Throws()
        {
            var retryFactory = Helper.GetRetryPolicyFactoryMock();
            var coreQFactory = Helper.GetQFactoryCoreMock();

            var f = QFactoryAdapter.Create(
                coreQFactory.Object,
                new Dictionary<string, IQOptions>(),
                retryFactory.Object);

            Assert.Throws<ArgumentNullException>(() =>
                f.CreateParallel(null!, "x", Mock.Of<ILogger<IParallelQ>>()));
        }

        [Test]
        public void CreateFifo_NullOptions_Throws()
        {
            var retryFactory = Helper.GetRetryPolicyFactoryMock();
            var coreQFactory = Helper.GetQFactoryCoreMock();

            var f = QFactoryAdapter.Create(
                coreQFactory.Object,
                new Dictionary<string, IQOptions>(),
                retryFactory.Object);

            Assert.Throws<ArgumentNullException>(() =>
                f.CreateFifo(null!, "x", Mock.Of<ILogger<IFifoQ>>()));
        }

        [Test]
        public void CreateParallel_UnknownName_ThrowsKeyNotFound()
        {
            var retryFactory = Helper.GetRetryPolicyFactoryMock();
            var coreQFactory = Helper.GetQFactoryCoreMock();

            var f = QFactoryAdapter.Create(
                coreQFactory.Object,
                new Dictionary<string, IQOptions>(),
                retryFactory.Object);

            Assert.Throws<KeyNotFoundException>(() =>
                f.CreateParallel("unknown", Mock.Of<ILogger<IParallelQ>>()));
        }

        [Test]
        public void GetQOptionsByName_ValidatesOptionValues()
        {
            var badMap = new Dictionary<string, IQOptions>
            {
                {
                    "bad",
                    new TestParallelOptions { MaxParallelism = 0, RetryPolicy = "" }
                }
            };

            var retryFactory = Helper.GetRetryPolicyFactoryMock();
            var coreQFactory = Helper.GetQFactoryCoreMock();

            var f = QFactoryAdapter.Create(
                coreQFactory.Object,
                badMap,
                retryFactory.Object);

            Assert.Throws<ArgumentException>(() =>
            {
                f.CreateParallel("bad", Mock.Of<ILogger<IParallelQ>>());
            });
        }
    }
    internal class TestSerializableOptions : ICacheableQOptions
    {
        public int MaxParallelism { get; set; } = 2;
        public string RetryPolicy { get; set; } = "rp";
        public IPayloadLeaseCache PayloadLeaseCache { get; set; } = Mock.Of<IPayloadLeaseCache>();
        public IPayloadJobFactory PayloadRunnerFactory { get; set; } = Mock.Of<IPayloadJobFactory>();
    }

    internal class TestParallelOptions : IQOptions
    {
        public int MaxParallelism { get; set; } = 2;
        public string RetryPolicy { get; set; } = "rp";
    }

    public class XQ : IJobQ
    {
        public bool IsDisposed => throw new NotImplementedException();

        public Func<IJobEvent, Task> OnEventChange { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string Name => throw new NotImplementedException();

        public int MaxParallelism => throw new NotImplementedException();

        public Func<IRetryPolicy> RetryPolicyFactory => throw new NotImplementedException();

        public SemaphoreSlim Semaphore => throw new NotImplementedException();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IJobQ Enqueue(IJobRoot jobRoot, bool isFifo, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IJobQ Enqueue(IJobRoot jobRoot, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public IJobQ EnqueueFifo(IJobRoot jobRoot, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public IJobQ SetRetryPolicyFactory(Func<IRetryPolicy> retryPolicy)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(IObserver<IJobEvent> observer)
        {
            throw new NotImplementedException();
        }

        public Task WaitRunnerUntilFinishedAsync(Guid jobId)
        {
            throw new NotImplementedException();
        }
    }

}
