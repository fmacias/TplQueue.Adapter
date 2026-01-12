using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Abstractions.Exceptions;
using Fmaciasruano.TplQueue.Cache;
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.Core.Runners;
using Fmaciasruano.TplQueue.RetryPolicies;
using Fmaciasruano.TplQueue.Serialization.SystemTextJson;
using Fmaciasruano.TplQueue.Runner;

namespace Fmaciasruano.TplQueue.Core.Integration.Test.Cache
{
    [TestFixture]
    public class MemCacheTests
    {
        private IPayloadRunnerFactory _payloadRunnerFactory = null!;
        private IUniversalPayloadSerializer _universalPayloadSerializer = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var retryFactory = RetryPolicyFactory.Instance(new Dictionary<string, RetryPolicyOptions>
            {
                { "none", RetryPolicyOptions.Linear(0, 0) }
            });

            var dispatcherOptions = new Dictionary<string, IDispatcherOptions>();
            _payloadRunnerFactory = PayloadRunnerFactory.Instance(
                TaskRunnerFactory.Instance(),
                TaskRunnerRootFactory.Instance(),
                retryFactory);
            _universalPayloadSerializer = SystemTextJsonUniversalSerializer.Create();
        }

        private class DummyPayload : IPayloadCommand
        {
            public string HandlerId => "dummy";
            public Task ExecuteAsync(CancellationToken ct)
                => Task.CompletedTask;
        }

        [Test]
        public void Append_CreatesEntries_InLeasedState()
        {
            MemCache cache = CreateCache();
            IPayloadTaskRunnerRoot<DummyPayload> root = CreateRoot();
            IPayloadTaskRunner<DummyPayload> child = CreateChild("Child-1");
            root.After(child);
            
            var list = cache.Append(root, isFifo: true);

            Assert.That(list.Count,  Is.EqualTo(2));
            var ok = cache.TryLeaseNextRoot(out var payloadCarrierRoot, out var rootLease);
            cache.LeaseRootNode(rootLease);
            Assert.That(ok, Is.True);
            Assert.That(rootLease.Status, Is.EqualTo(EntryStatus.Leased));
            Assert.That(rootLease.IsFifo, Is.True);
            Assert.That(rootLease.TaskRunnerRootId, Is.EqualTo(root.Id));

            Assert.That(root.Id, Is.EqualTo(payloadCarrierRoot.Id));
            Assert.That(root.Name, Is.EqualTo(payloadCarrierRoot.Name));
            Assert.That(rootLease.RetryDescriptor.Kind, Is.EqualTo("linear"));
            var root1rp = root.GetRetryPolicyFactory()();
            var root2rp = payloadCarrierRoot.GetRetryPolicyFactory()();
            
            Assert.IsInstanceOf<ILinearBackoffRetryPolicy>(root2rp);

            var childEntry = cache.GetByTaskRunnerId(child.Id);
            Assert.That(childEntry.Status, Is.EqualTo(EntryStatus.Leased));
            Assert.That(childEntry.IsFifo, Is.False);
            Assert.That(childEntry.TaskRunnerRootId, Is.EqualTo(root.Id));
            Assert.That(childEntry.TaskRunnerNodeDto.Name, Is.EqualTo(child.Name));
            Assert.That(childEntry.RetryDescriptor.Kind, Is.EqualTo("linear"));

        }
        [Test]
        public void Append_ConcatenatedEntries_Test()
        {
            var cache = CreateCache();
            var root = CreateRoot();
            var child = CreateChild("child-job");
            var grandChild = CreateChild("grant-child");

            child.After(grandChild);
            root.After(child);

            var entries = cache.Append(root, isFifo: true);
            Assert.That(entries.Count, Is.EqualTo(3));

            var rootEntry = cache.GetByTaskRunnerId(root.Id);
            Assert.AreEqual(rootEntry.IsRoot, true);
            Assert.AreEqual(rootEntry.IsFifo, true);
            Assert.AreEqual(rootEntry.Status, EntryStatus.Pending);
            Assert.AreEqual(rootEntry.TaskRunnerId, root.Id);
            Assert.AreEqual(rootEntry.TaskRunnerRootId, root.Id);
            Assert.AreEqual(rootEntry.ParentTaskRunnerId, Guid.Empty);
            Assert.AreEqual(rootEntry.TaskRunnerNodeDto.TaskRunnerId, root.Id);
            Assert.AreEqual(rootEntry.TaskRunnerNodeDto.ParentTaskRunnerId, Guid.Empty);
            Assert.AreEqual(rootEntry.TaskRunnerNodeDto.Name, "root-job");

            var childEntry = cache.GetByTaskRunnerId(child.Id);
            Assert.AreEqual(childEntry.IsRoot, false);
            Assert.AreEqual(childEntry.IsFifo, false, "Only root can be fifo");
            Assert.AreEqual(childEntry.Status, EntryStatus.Pending);
            Assert.AreEqual(childEntry.TaskRunnerId, child.Id);
            Assert.AreEqual(childEntry.TaskRunnerRootId, root.Id);
            Assert.AreEqual(childEntry.ParentTaskRunnerId, root.Id);
            Assert.AreEqual(childEntry.TaskRunnerNodeDto.TaskRunnerId, child.Id);
            Assert.AreEqual(childEntry.TaskRunnerNodeDto.ParentTaskRunnerId, root.Id);
            Assert.AreEqual(childEntry.TaskRunnerNodeDto.Name, "child-job");

            var grandChildEntry = cache.GetByTaskRunnerId(grandChild.Id);
            Assert.AreEqual(grandChildEntry.IsRoot, false);
            Assert.AreEqual(grandChildEntry.IsFifo, false, "Only root can be fifo");
            Assert.AreEqual(grandChildEntry.Status, EntryStatus.Pending);
            Assert.AreEqual(grandChildEntry.TaskRunnerId, grandChild.Id);
            Assert.AreEqual(grandChildEntry.TaskRunnerRootId, root.Id);
            Assert.AreEqual(grandChildEntry.ParentTaskRunnerId, child.Id);
            Assert.AreEqual(grandChildEntry.TaskRunnerNodeDto.TaskRunnerId, grandChild.Id);
            Assert.AreEqual(grandChildEntry.TaskRunnerNodeDto.ParentTaskRunnerId, child.Id);
            Assert.AreEqual(grandChildEntry.TaskRunnerNodeDto.Name, "grant-child");

        }

        private IPayloadTaskRunnerRoot<DummyPayload> CreateRoot()
        {
            var payload = new DummyPayload();
            var root = _payloadRunnerFactory 
                .CreateRoot(
                    payload,
                    _universalPayloadSerializer,
                    () => new LinearBackoffRetryPolicy(),
                    name: "root-job");
            return root;
        }

        private IPayloadTaskRunner<DummyPayload> CreateChild(string name)
        {
            var payload = new DummyPayload();

            var child = _payloadRunnerFactory
                .Create(
                    payload,
                    _universalPayloadSerializer,
                    name: name);
            return child;
        }


        private MemCache CreateCache()
        {
            return MemCache.Create(_payloadRunnerFactory, _universalPayloadSerializer);
        }

        [Test]
        public void AckNode_MarksAllLeasesForRunner_AsAck()
        {
            var cache = CreateCache();
            var root = CreateRoot(); 

            cache.Append(root, isFifo: false);

            Assert.That(cache.TryLeaseNextRoot(out var payloadRoot,out var lease), Is.True);
            var runnerId = lease.TaskRunnerId;
            var serializedPayload = SerializedPayload.Create();
            cache.AckNode(runnerId, serializedPayload);
            Assert.That(root.Id, Is.EqualTo(payloadRoot.Id));

            var deleted = cache.DeleteRootNode(root.Id);
            Assert.That(deleted, Is.EqualTo(true));
            Assert.That(cache.TryLeaseNextRoot(out var a, out var b), Is.False);
        }

        [Test]
        public void FailChildNode_MarksFailed_RottNotFinalized()
        {
            var cache = CreateCache();
            var root = CreateRoot();
            var child = CreateChild("Child-Job");
            root.After(child);

            cache.Append(root, isFifo: false);

            Assert.That(cache.TryLeaseNextRoot(out var payloadRoot, out var lease), Is.True);

            cache.FailNode(child.Id, "boom");
            Assert.That(cache.DeleteRootNode(root.Id), Is.EqualTo(false));
        }
        [Test]
        public void RootChildFinalizedChildNot_Validation()
        {
            var cache = CreateCache();
            var root = CreateRoot();
            var child = CreateChild("Child-Job");
            root.After(child);

            cache.Append(root, isFifo: false);

            Assert.That(cache.TryLeaseNextRoot(out var payloadRoot, out var lease), Is.True);

            cache.AckNode(root.Id, SerializedPayload.Create());
            
            Assert.Throws<TplQueueErrorException>(() => cache.DeleteRootNode(root.Id));
        }

        [Test]
        public void FailRootNode_MarksFailed_AndPreventsValidation()
        {
            var cache = CreateCache();
            var root = CreateRoot();
            Assert.IsInstanceOf<ITaskRunnerRoot>(root);
            var child = CreateChild("Child-job");
            Assert.IsInstanceOf<ITaskRunner>(child);
            root.After(child);

            cache.Append(root, isFifo: false);

            Assert.That(cache.TryLeaseNextRoot(out var payloadRoot, out var lease), Is.True);
            cache.AckNode(child.Id, SerializedPayload.Create());
            cache.FailNode(root.Id, "");
            
            Assert.That(
                cache.GetByTaskRunnerId(child.Id).Status,
                Is.EqualTo(EntryStatus.Acknownledged));
            Assert.That(
                cache.GetByTaskRunnerId(root.Id).Status,
                Is.EqualTo(EntryStatus.Failed));
            Assert.That(cache.DeleteRootNode(root.Id), Is.EqualTo(true));
        }
        [Test]
        public void CancelNode_MarksCanceled_AndAllowsTerminal()
        {
            var cache = CreateCache();
            var root = CreateRoot();

            cache.Append(root, isFifo: false);

            Assert.That(cache.TryLeaseNextRoot(out var payloadRoot,out var lease), Is.True);

            cache.CancelNode(lease.TaskRunnerId);
            Assert.That(cache.DeleteRootNode(root.Id), Is.EqualTo(true));

        }

        [Test]
        public void TryExtractPayloadCarrierRoot_RehydratesGraph()
        {
            var cache = CreateCache();
            var root = CreateRoot();
            cache.Append(root, isFifo: false);

            Assert.That(cache.TryLeaseNextRoot(out var payloadRoot, out var lease), Is.True);
            Assert.That(payloadRoot, Is.Not.Null);
            Assert.That(payloadRoot.Id, Is.EqualTo(root.Id));
        }

        [Test]
        public void TryLeaseNextRoot_RehydratesDependenciesGraph()
        {
            var cache = CreateCache();
            var root = CreateRoot();
            var child = CreateChild("child-job");
            var grandChild = CreateChild("grant-child");

            child.After(grandChild);
            root.After(child);

            var entries = cache.Append(root, isFifo: false);
            
            Assert.That(cache.TryLeaseNextRoot(out var payloadRoot, out _), Is.True);
            var dependencies = payloadRoot.GetPayloadDependencies().ToArray();

            Assert.That(dependencies.Length, Is.EqualTo(1));
            Assert.That(dependencies[0].Id, Is.EqualTo(child.Id));
            Assert.That(
                dependencies[0].GetPayloadDependencies().Select(p => p.Id),
                Does.Contain(grandChild.Id));
        }

        [Test]
        public void CleanFinalizedTest()
        {
            var cache = CreateCache();
            var root = CreateRoot();
            var child = CreateChild("child-job");

            root.After(child);
            cache.Append(root, isFifo: false);

            cache.FailNode(child.Id, "boom");

            cache.CleanFinalized();
            Assert.That(cache.GetByTaskRunnerId(child.Id), Is.EqualTo(null));
        }
        [Test]
        public void RootFinalizedTest()
        {
            var cache = CreateCache();
            var root = CreateRoot();
            var child = CreateChild("child-job");

            root.After(child);
            cache.Append(root, isFifo: false);

            cache.FailNode(child.Id, "boom");
            cache.FailNode(root.Id,"root also failes");
            Assert.That(cache.DeleteRootNode(root.Id), Is.EqualTo(true));
            cache.SuccessRootNode(root.Id);
            Assert.AreEqual(true, cache.GetByTaskRunnerId(child.Id).Deleted);
            Assert.AreEqual(true, cache.GetByTaskRunnerId(root.Id).Deleted);
        }

    }
}
