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
            var retryPolicyOptions = new Dictionary<string, IRetryPolicyOptions>();
            var retryFactory = Mock.Of<IRetryPolicyAbstractFactory>();
            var coreFactory = Helper.GetQFactoryCoreMock();
            var first = QFactoryAdapter.Create(coreFactory.Object, retryFactory, options, retryPolicyOptions);
            var second = QFactoryAdapter.Create(coreFactory.Object, retryFactory, options, retryPolicyOptions);
            Assert.That(second, Is.Not.SameAs(first));
        }

        [Test]
        public void Create_DifferentInputs_NeverSingletone()
        {
            var coreFactory1 = Helper.GetQFactoryCoreMock();
            var coreFactory2 = Helper.GetQFactoryCoreMock();
            var retryPolicygenericFactory = Mock.Of<IRetryPolicyAbstractFactory>();

            var first = QFactoryAdapter.Create(
                coreFactory1.Object, 
                retryPolicygenericFactory, 
                new Dictionary<string, IQOptions>(), 
                new Dictionary<string, IRetryPolicyOptions>());

            var second = QFactoryAdapter.Create(
                coreFactory2.Object,
                retryPolicygenericFactory, 
                new Dictionary<string, IQOptions>(),
                new Dictionary<string, IRetryPolicyOptions>());

            Assert.That(second, Is.Not.SameAs(first));
        }

        [Test]
        public void Create_ValidatesArguments()
        {
            var coreFactory = Mock.Of<IQFactory>();
            var retryFactory = Mock.Of<IRetryPolicyAbstractFactory>();
            var options = new Dictionary<string, IQOptions>();
            var retryOptions = new Dictionary<string, IRetryPolicyOptions>();

            Assert.Throws<ArgumentNullException>(() => QFactoryAdapter.Create(null!, retryFactory, options, retryOptions));
            Assert.Throws<ArgumentNullException>(() => QFactoryAdapter.Create(coreFactory, retryFactory, null!, retryOptions));
            Assert.Throws<ArgumentNullException>(() => QFactoryAdapter.Create(coreFactory, null!, options, retryOptions));
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
            var retryOptions = new Dictionary<string, IRetryPolicyOptions>();

            var queueFatoryAdapter = QFactoryAdapter.Create(
                coreQFactory.Object,
                retryFactory.Object,
                options,
                retryOptions);

            Assert.Throws<InvalidCastException>(() =>
                queueFatoryAdapter.GetCoreQ<ICacheQ>("ser", Helper.GetLogger<ICacheQ>()));
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
            var retryOptions = new Dictionary<string, IRetryPolicyOptions>();

            var qfactoryAdapter = QFactoryAdapter.Create(
                coreQFactory.Object,
                retryFactory.Object, options, retryOptions);

            using var queue = qfactoryAdapter.GetCoreQ<IFifoQ>("fifo", Helper.GetLogger<IFifoQ>());

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
            var retryOptions = new Dictionary<string, IRetryPolicyOptions>();

            var qfactoryAdapter = QFactoryAdapter.Create(
                coreQFactory.Object,
                retryFactory.Object, options, retryOptions);

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
            var retryOptions = new Dictionary<string, IRetryPolicyOptions>();

            var qfactoryAdapter = QFactoryAdapter.Create(
                coreQFactory.Object,
                retryFactory.Object,
                options, retryOptions);

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
            var retryOptions = new Dictionary<string, IRetryPolicyOptions>();

            var f = QFactoryAdapter.Create(
                coreQFactory.Object,
                retryFactory.Object,
                new Dictionary<string, IQOptions>(), retryOptions);
            
            Assert.Throws<ArgumentException>(() =>
                f.GetCoreQ<IParallelQ>("", Helper.GetLogger<IParallelQ>()));

            //x not exists in configuration
            var defaultParallel = f.GetCoreQ<IParallelQ>("x", Helper.GetLogger<IParallelQ>());

            Assert.That(defaultParallel.MaxParallelism, Is.EqualTo(Environment.ProcessorCount));
        }

        [Test]
        public void CreateParallel_NullOptions_Throws()
        {
            var retryFactory = Helper.GetRetryPolicyFactoryMock();
            var coreQFactory = Helper.GetQFactoryCoreMock();
            var retryOptions = new Dictionary<string, IRetryPolicyOptions>();

            var f = QFactoryAdapter.Create(
                coreQFactory.Object,
                retryFactory.Object,
                new Dictionary<string, IQOptions>(), 
                retryOptions);

            Assert.Throws<ArgumentNullException>(() =>
                f.Parallel(null!, "x", Mock.Of<ILogger<IParallelQ>>()));
        }

        [Test]
        public void CreateFifo_NullOptions_Throws()
        {
            var retryFactory = Helper.GetRetryPolicyFactoryMock();
            var coreQFactory = Helper.GetQFactoryCoreMock();
            var retryOptions = new Dictionary<string, IRetryPolicyOptions>();

            var f = QFactoryAdapter.Create(
                coreQFactory.Object,
                retryFactory.Object,
                new Dictionary<string, IQOptions>(), 
                retryOptions);

            Assert.Throws<ArgumentNullException>(() =>
                f.Fifo(null!, "x", Mock.Of<ILogger<IFifoQ>>()));
        }

        [Test]
        public void CreateParallel_UnknownName_LoadsDefaultParallel()
        {
            var retryFactory = Helper.GetRetryPolicyFactoryMock();
            var coreQFactory = Helper.GetQFactoryCoreMock();
            var retryOptions = new Dictionary<string, IRetryPolicyOptions>();

            var f = QFactoryAdapter.Create(
                coreQFactory.Object,
                retryFactory.Object,
                new Dictionary<string, IQOptions>(), 
                retryOptions);

            var queue = f.Parallel("unknown", Mock.Of<ILogger<IParallelQ>>());

            Assert.That(queue.MaxParallelism, Is.EqualTo(Environment.ProcessorCount));
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
            var retryOptions = new Dictionary<string, IRetryPolicyOptions>();

            var f = QFactoryAdapter.Create(
                coreQFactory.Object,
                retryFactory.Object,
                badMap, retryOptions);

            Assert.Throws<ArgumentException>(() =>
            {
                f.Parallel("bad", Mock.Of<ILogger<IParallelQ>>());
            });
        }
    }
    internal class TestSerializableOptions : ICacheableQOptions
    {
        public int MaxParallelism { get; set; } = 2;
        public string RetryPolicy { get; set; } = "rp";
        public IDataJobCache PayloadLeaseCache { get; set; } = Mock.Of<IDataJobCache>();
        public IDataJobFactory PayloadRunnerFactory { get; set; } = Mock.Of<IDataJobFactory>();

        public Guid Id { get; set; } = Guid.NewGuid();
    }

    internal class TestParallelOptions : IQOptions
    {
        public int MaxParallelism { get; set; } = 2;
        public string RetryPolicy { get; set; } = "rp";

        public Guid Id => Guid.NewGuid();
    }

    public class XQ : IQ
    {
        public bool IsDisposed => throw new NotImplementedException();

        public Func<IJobEvent, Task> OnJobEventChanged { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string Name => throw new NotImplementedException();

        public int MaxParallelism => throw new NotImplementedException();

        public Func<IRetryPolicy> RetryPolicyFactory => throw new NotImplementedException();

        public SemaphoreSlim Semaphore => throw new NotImplementedException();

        public Guid QueueId => throw new NotImplementedException();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IQ Enqueue(IJobRoot jobRoot, bool isFifo, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IQ Enqueue(IJobRoot jobRoot, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public IQ EnqueueFifo(IJobRoot jobRoot, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public IQ SetRetryPolicyFactory(Func<IRetryPolicy> retryPolicy)
        {
            throw new NotImplementedException();
        }

        public void ResumePolling()
        {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(IObserver<IJobEvent> observer)
        {
            throw new NotImplementedException();
        }

        public Task Wait()
        {
            throw new NotImplementedException();
        }
    }

}
