using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Cache;
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.Observers;
using Fmaciasruano.TplQueue.Queues;
using Fmaciasruano.TplQueue.RetryPolicies;
using Fmaciasruano.TplQueue.Serialization.SystemTextJson;
using Fmaciasruano.TplQueue.Runner;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Fmaciasruano.TplQueue.Core.Integration.Test.Queues
{
    [TestFixture()]
    public class SerializableDispatcherTest
    {
        private const int PulseMs = 250; 
        private ISerializablePayloadDispatcher _queue = null!;
        private IMemCache _memCache = null!;
        private IDisposable _loggingObserverUnsubscriber = null!;
        private ILogger<ISerializablePayloadDispatcher> _logger = null!;
        private IRetryPolicyFactory _retryPolicyFactory = null!;
        private ITaskDispatcherFactory _taskDispatcherFactory = null!;
        private ISerializableDispatcherFactory _serializableDispatcherFactory = null!;
        private IObserverFactory _observerFactory = null!;
        private ICacheFactory _cacheFactory = null!;
        private IPayloadRunnerFactory _payloadRunnerFactory = null!;
        private IUniversalPayloadSerializer _universalPayloadSerializer = null!;
        private IRetryPolicySerializable _retryPolicySeriaizer = null!;
        public class DummyPayload1000 : IPayloadCommand
        {
            public string HandlerId => "dummy";
            public bool Executed { get; private set; }
            public string? Greating { get; private set; }

            public Task ExecuteAsync(CancellationToken ct)
            {
                 Task.Delay(1000, ct).Wait();

                Greating = "Hello";
                Executed = true;
                return Task.CompletedTask;
            }
        }

        public class DummyPayload : IPayloadCommand
        {
            public string HandlerId => "dummy";
            public bool Executed { get; private set; }
            public string? Greating { get; private set; }
            public Task ExecuteAsync(CancellationToken ct)
            {
                Greating = "Hello";
                Executed = true;
                return Task.CompletedTask;
            }
        }


        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _logger = Helper.GetLogger<ISerializablePayloadDispatcher>();
            var retryPolicyOptions = new Dictionary<string, RetryPolicyOptions>();
            _retryPolicyFactory = RetryPolicyFactory.Instance(retryPolicyOptions);

            var dispatcherOptions = new Dictionary<string, IDispatcherOptions>();
            _taskDispatcherFactory = TaskDispatcherFactory.Instance(dispatcherOptions, _retryPolicyFactory);
            _serializableDispatcherFactory = SerializableDispatcherFactory.Instance();
            _cacheFactory = CacheFactory.Instance();
            _observerFactory = ObserverFactory.Instance();
            _payloadRunnerFactory = PayloadRunnerFactory.Instance(
                TaskRunnerFactory.Instance(),
                TaskRunnerRootFactory.Instance(),
                RetryPolicyFactory.Instance(new Dictionary<string, RetryPolicyOptions>{
                    { "none", RetryPolicyOptions.Linear(0, 0) }
                }));
            _universalPayloadSerializer = SystemTextJsonUniversalSerializer.Create();
        }

        [SetUp]
        public void Setup()
        {
            _memCache = _cacheFactory.CreateMemCache(_payloadRunnerFactory, _universalPayloadSerializer);
            var defaultQueue = _taskDispatcherFactory.CreateParallel(
                name: "Default test-TaskDipatcher",
                retryPolicyFactory: () => _retryPolicyFactory.CreateNoRetryPolicy(),
                maxParallelism: 8,
                logger: _logger,
                pulseMs: 250);

            var loggingObserver = _observerFactory
                .CreateLoggingObserver(Helper.GetLogger<ITaskQueueLoggingObserver>());

            _queue = _serializableDispatcherFactory.Create(
                logger: _logger,
                payloadLeaseCache: _memCache,
                defaultQueue);
            
            _loggingObserverUnsubscriber = _queue.Subscribe(loggingObserver);
        }

        [TearDown]
        public void TearDown()
        {
            _queue.Dispose();
            _loggingObserverUnsubscriber.Dispose();
            _memCache = null!;
        }

        [Test]
        public void Enqueue_AppendsToCache_NoStarted()
        {
            var rootPayload = new DummyPayload();
            var childPayload = new DummyPayload();
            var root = _payloadRunnerFactory.CreateRoot(
                rootPayload,
                _universalPayloadSerializer,
                () => _retryPolicyFactory.CreateNoRetryPolicy(),
                name: "root-job");
            
            var child = _payloadRunnerFactory.Create(
                childPayload,
                _universalPayloadSerializer,
                "Child payload job");
            
            root.After(child);
            
            _queue.Enqueue(root, CancellationToken.None);

            //Wait a moment to handle status due to callback of queue internally.
            Task.Delay(50).Wait();          
            var rootEntry = _memCache.GetByTaskRunnerId(root.Id);

            AssertCacheLeaseEntry(
                entry: rootEntry, 
                payloadCarrierRunner: root,
                isRoot: true,
                status:EntryStatus.Pending, //Only root have the state cached. Semantically a root object is enqueued, and dequeued.
                isFifo:false,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId:Guid.Empty,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",
                entryDeleted:false);

            var childEntry = _memCache.GetByTaskRunnerId(child.Id);
            AssertCacheLeaseEntry(
                entry: childEntry,
                payloadCarrierRunner: child,
                isRoot: false,
                status: EntryStatus.Pending,
                isFifo: false,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: root.Id,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",
                entryDeleted: false);
        }

        [Test]
        public async Task Enqueue_AppendsToCache_Started()
        {
            var rootPayload = new DummyPayload();
            var childPayload = new DummyPayload();
        
            var root = _payloadRunnerFactory.CreateRoot(
                rootPayload,
                _universalPayloadSerializer,
                () => _retryPolicyFactory.CreateNoRetryPolicy(),
                name: "root-job");

            var child = _payloadRunnerFactory.Create(
                childPayload,
                _universalPayloadSerializer,
                "Child payload job");
            
            root.After(child);

            _queue.Enqueue(root, CancellationToken.None);

            var rootEntry = _memCache.GetByTaskRunnerId(root.Id);
            AssertCacheLeaseEntry(
                entry: rootEntry,
                payloadCarrierRunner: root,
                isRoot: true,
                status: EntryStatus.Pending,
                isFifo: false,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: Guid.Empty,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",
                entryDeleted: false);

            var childEntry = _memCache.GetByTaskRunnerId(child.Id);

            AssertCacheLeaseEntry(
                entry: childEntry,
                payloadCarrierRunner: child,
                isRoot: false,
                status: EntryStatus.Pending,
                isFifo: false,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: root.Id,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",
                entryDeleted:false);
            
            _queue.StartPolling();
            await Task.Delay(1000);

            AssertCacheLeaseEntry(
                entry: rootEntry,
                payloadCarrierRunner: root,
                isRoot: true,
                status: EntryStatus.Acknownledged,
                isFifo: false,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: Guid.Empty,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":true,\"Greating\":\"Hello\"}",
                entryDeleted:true);


            AssertCacheLeaseEntry(
                entry: childEntry,
                payloadCarrierRunner: child,
                isRoot: false,
                status: EntryStatus.Acknownledged,
                isFifo: false,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: root.Id,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":true,\"Greating\":\"Hello\"}",
                entryDeleted:true);
        }
        [Test]
        public void EnqueueFifo_NoStarted_Fifo()
        {
            var rootPayload = new DummyPayload();
            var childPayload = new DummyPayload();

            var root = _payloadRunnerFactory.CreateRoot(
                rootPayload,
                _universalPayloadSerializer,
                () => _retryPolicyFactory.CreateNoRetryPolicy(),
                name: "root-job");

            var child = _payloadRunnerFactory.Create(
                childPayload,
                _universalPayloadSerializer,
                "Child payload job");

            root.After(child);

            _queue.EnqueueFifo(root, CancellationToken.None);
            Task.Delay(1000).Wait();

            var rootEntry = _memCache.GetByTaskRunnerId(root.Id);
            AssertCacheLeaseEntry(
                entry: rootEntry,
                payloadCarrierRunner: root,
                isRoot: true,
                status: EntryStatus.Pending, //Only root have the state cached. Semantically a root object is enqueued, and dequeued.
                isFifo: true,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: Guid.Empty,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",
                entryDeleted:false);

            var childEntry = _memCache.GetByTaskRunnerId(child.Id);
            AssertCacheLeaseEntry(
                entry: childEntry,
                payloadCarrierRunner: child,
                isRoot: false,
                status: EntryStatus.Pending,
                isFifo: false, // property is only relevant on root objects.
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: root.Id,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",
                entryDeleted:false);
        }

        [Test]
        public void EnqueueFifo_Start_Fifo()
        {
            var rootPayload = new DummyPayload();
            var childPayload = new DummyPayload();

            var root = _payloadRunnerFactory.CreateRoot(
                rootPayload,
                _universalPayloadSerializer,
                () => _retryPolicyFactory.CreateNoRetryPolicy(),
                name: "root-job");

            var child = _payloadRunnerFactory.Create(
                childPayload,
                _universalPayloadSerializer,
                "Child payload job");

            root.After(child);

            _queue.EnqueueFifo(root, CancellationToken.None);

            var rootEntry = _memCache.GetByTaskRunnerId(root.Id);

            AssertCacheLeaseEntry(
                entry: rootEntry,
                payloadCarrierRunner: root,
                isRoot: true,
                status: EntryStatus.Pending, //Only root have the state cached. Semantically a root object is enqueued, and dequeued.
                isFifo: true,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: Guid.Empty,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",
                entryDeleted:false);

            var childEntry = _memCache.GetByTaskRunnerId(child.Id);
            AssertCacheLeaseEntry(
                entry: childEntry,
                payloadCarrierRunner: child,
                isRoot: false,
                status: EntryStatus.Pending,
                isFifo: false, // property is only relevant on root objects.
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: root.Id,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",
                entryDeleted: false);

            _queue.StartPolling();
            Task.Delay(2000).Wait();

            AssertCacheLeaseEntry(
                entry: rootEntry,
                payloadCarrierRunner: root,
                isRoot: true,
                status: EntryStatus.Acknownledged,
                isFifo: true,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: Guid.Empty,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":true,\"Greating\":\"Hello\"}",
                entryDeleted:true);

            AssertCacheLeaseEntry(
                entry: childEntry,
                payloadCarrierRunner: child,
                isRoot: false,
                status: EntryStatus.Acknownledged,
                isFifo: false,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: root.Id,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":true,\"Greating\":\"Hello\"}",
                entryDeleted:true);
        }
        [Test]
        public void EnqueueFifo_Start_CancelBeforeStart_Fifo()
        {
            var rootPayload = new DummyPayload();
            var childPayload = new DummyPayload();

            var root = _payloadRunnerFactory.CreateRoot(
                rootPayload,
                _universalPayloadSerializer,
                () => _retryPolicyFactory.CreateNoRetryPolicy(),
                name: "root-job");

            var child = _payloadRunnerFactory.Create(
                childPayload,
                _universalPayloadSerializer,
                "Child payload job");

            root.After(child);

            var cancellationTockenSource = new CancellationTokenSource();
            _queue.EnqueueFifo(root, cancellationTockenSource.Token);
            var rootEntry = _memCache.GetByTaskRunnerId(root.Id);
            var childEntry = _memCache.GetByTaskRunnerId(child.Id);

            cancellationTockenSource.Cancel();
            _queue.StartPolling();
            Task.Delay(1000).Wait();

            AssertCacheLeaseEntry(
                entry: rootEntry,
                payloadCarrierRunner: root,
                isRoot: true,
                status: EntryStatus.Canceled,
                isFifo: true,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: Guid.Empty,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",
                entryDeleted:true);

            AssertCacheLeaseEntry(
                entry: childEntry,
                payloadCarrierRunner: child,
                isRoot: false,
                status: EntryStatus.Canceled,
                isFifo: false,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: root.Id,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",
                entryDeleted:true);
        }

        [Test]
        public void EnqueueFifo_Start_CancelBeforeStart_NonFifo()
        {
            var rootPayload = new DummyPayload();
            var childPayload = new DummyPayload();

            var root = _payloadRunnerFactory.CreateRoot(
                rootPayload,
                _universalPayloadSerializer,
                () => _retryPolicyFactory.CreateNoRetryPolicy(),
                name: "root-job");

            var child = _payloadRunnerFactory.Create(
                childPayload,
                _universalPayloadSerializer,
                "Child payload job");

            root.After(child);

            var cancellationTockenSource = new CancellationTokenSource();
            _queue.Enqueue(root, cancellationTockenSource.Token);
            var rootEntry = _memCache.GetByTaskRunnerId(root.Id);
            var childEntry = _memCache.GetByTaskRunnerId(child.Id);

            cancellationTockenSource.Cancel();
            _queue.StartPolling();
            Task.Delay(1000).Wait();

            AssertCacheLeaseEntry(
                entry: rootEntry,
                payloadCarrierRunner: root,
                isRoot: true,
                status: EntryStatus.Canceled,
                isFifo: false,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: Guid.Empty,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",
                entryDeleted:true);

            AssertCacheLeaseEntry(
                entry: childEntry,
                payloadCarrierRunner: child,
                isRoot: false,
                status: EntryStatus.Canceled,
                isFifo: false,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: root.Id,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",
                entryDeleted:true);
        }

        [Test]
        public async Task EnqueueFifo_Start_CancelDuringExecution_Fifo()
        {
            var rootPayload = new DummyPayload1000();
            var childPayload = new DummyPayload();

            var root = _payloadRunnerFactory.CreateRoot(
                rootPayload,
                _universalPayloadSerializer,
                () => _retryPolicyFactory.CreateNoRetryPolicy(),
                name: "root-job");

            var child = _payloadRunnerFactory.Create(
                childPayload,
                _universalPayloadSerializer,
                "Child payload job");

            root.After(child);
            var cts = new CancellationTokenSource();
            _queue.LeasingPulseMs = 100;
            _queue.EnqueueFifo(root, cts.Token);
            
            var rootEntry = _memCache.GetByTaskRunnerId(root.Id);

            AssertCacheLeaseEntry(
                entry: rootEntry,
                payloadCarrierRunner: root,
                isRoot: true,
                status: EntryStatus.Pending, //Only root have the state cached. Semantically a root object is enqueued, and dequeued.
                isFifo: true,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: Guid.Empty,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",
                entryDeleted:false);

            var childEntry = _memCache.GetByTaskRunnerId(child.Id);
            AssertCacheLeaseEntry(
                entry: childEntry,
                payloadCarrierRunner: child,
                isRoot: false,
                status: EntryStatus.Pending,
                isFifo: false, // property is only relevant on root objects.
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: root.Id,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",
                entryDeleted:false);
            
            _queue.StartPolling();
            cts.CancelAfter(500);
            await Task.Delay(1100);
            AssertCacheLeaseEntry(
                entry: rootEntry,
                payloadCarrierRunner: root,
                isRoot: true,
                status: EntryStatus.Canceled,
                isFifo: true,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: Guid.Empty,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",
                entryDeleted: true);

            AssertCacheLeaseEntry(
                entry: childEntry,
                payloadCarrierRunner: child,
                isRoot: false,
                status: EntryStatus.Acknownledged,
                isFifo: false,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: root.Id,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":true,\"Greating\":\"Hello\"}",
                entryDeleted:true);
       
            cts.Dispose();
        }
        [Test]
        public async Task EnqueueFifo_Start_CancelDuringExecution_NonFifo()
        {
            var rootPayload = new DummyPayload1000();
            var childPayload = new DummyPayload();

            var root = _payloadRunnerFactory.CreateRoot(
                rootPayload,
                _universalPayloadSerializer,
                () => _retryPolicyFactory.CreateNoRetryPolicy(),
                name: "root-job");

            var child = _payloadRunnerFactory.Create(
                childPayload,
                _universalPayloadSerializer,
                "Child payload job");

            root.After(child);
            var cts = new CancellationTokenSource();
            _queue.LeasingPulseMs = 100;
            _queue.Enqueue(root, cts.Token);

            var rootEntry = _memCache.GetByTaskRunnerId(root.Id);

            AssertCacheLeaseEntry(
                entry: rootEntry,
                payloadCarrierRunner: root,
                isRoot: true,
                status: EntryStatus.Pending, //Only root have the state cached. Semantically a root object is enqueued, and dequeued.
                isFifo: false,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: Guid.Empty,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",
                entryDeleted:false);

            var childEntry = _memCache.GetByTaskRunnerId(child.Id);
            AssertCacheLeaseEntry(
                entry: childEntry,
                payloadCarrierRunner: child,
                isRoot: false,
                status: EntryStatus.Pending,
                isFifo: false, // property is only relevant on root objects.
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: root.Id,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",                
                entryDeleted:false);

            _queue.StartPolling();
            cts.CancelAfter(500);
            await Task.Delay(1100);

            AssertCacheLeaseEntry(
                entry: rootEntry,
                payloadCarrierRunner: root,
                isRoot: true,
                status: EntryStatus.Canceled,
                isFifo: false,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: Guid.Empty,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":false,\"Greating\":null}",
                entryDeleted: true);

            AssertCacheLeaseEntry(
                entry: childEntry,
                payloadCarrierRunner: child,
                isRoot: false,
                status: EntryStatus.Acknownledged,
                isFifo: false,
                TaskRunnerRootId: root.Id,
                parentTaskRunnerId: root.Id,
                payloadJson: "{\"HandlerId\":\"dummy\",\"Executed\":true,\"Greating\":\"Hello\"}",
                entryDeleted:true);

            cts.Dispose();
        }

        private void AssertCacheLeaseEntry(ICacheLeaseEntry entry, 
            IPayloadCarrier payloadCarrierRunner, bool isRoot, 
            EntryStatus status, bool isFifo,Guid TaskRunnerRootId, 
            Guid parentTaskRunnerId,
            string payloadJson,
            bool entryDeleted)
        {
            Assert.That(entry.TaskRunnerId, Is.EqualTo(payloadCarrierRunner.Id));
            Assert.That(entry.TaskRunnerRootId, Is.EqualTo(TaskRunnerRootId));
            Assert.That(entry.IsRoot, Is.EqualTo(isRoot));
            Assert.That(entry.Status, Is.EqualTo(status));
            Assert.That(entry.IsFifo, Is.EqualTo(isFifo));
            Assert.That(entry.RetryDescriptor.Kind, Is.EqualTo("none"));
            Assert.That(entry.TaskRunnerNodeDto.IsRoot, Is.EqualTo(isRoot));
            Assert.That(entry.TaskRunnerNodeDto.ParentTaskRunnerId, Is.EqualTo(parentTaskRunnerId));
            Assert.That(entry.TaskRunnerNodeDto.TaskRunnerId, Is.EqualTo(payloadCarrierRunner.Id));
            Assert.That(entry.TaskRunnerNodeDto.Name, Is.EqualTo(payloadCarrierRunner.Name));
            Assert.That(entry.TaskRunnerNodeDto.PayloadJson, Is.EqualTo(payloadJson));
            Assert.That(entry.Deleted, Is.EqualTo(entryDeleted));
        }
    }
}

