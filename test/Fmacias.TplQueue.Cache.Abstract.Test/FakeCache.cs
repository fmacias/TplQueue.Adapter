using System;
using System.Collections.Generic;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Cache.Abstract.Test
{
    internal sealed class FakeCache : CacheAbstract
    {
        private readonly List<(ITaskRunnerNodeDto Node, Guid RootId)> _appendedNodes = new();
        private readonly Guid _knownRootId;
        private readonly ICacheLeaseEntry _knownEntry;
        private readonly List<ICacheLeaseEntry> _persistedEntries = new();

        public IReadOnlyList<ICacheLeaseEntry> PersistedEntries => _persistedEntries;

        public IReadOnlyList<(ITaskRunnerNodeDto Node, Guid RootId)> AppendedNodes => _appendedNodes;

        public FakeCache(
            IRetryPolicySerializable retryPolicySerializer,
            IUniversalPayloadSerializer serializer,
            Guid knownRootId,
            ICacheLeaseEntry knownEntry)
            : base(serializer)
        {
            _knownRootId = knownRootId;
            _knownEntry = knownEntry;
        }

        protected override Action<ITaskRunnerNodeDto, Guid> AppendNodeCallBack =>
            (nodeDto, rootId) =>
            {
                if (nodeDto is null) throw new ArgumentNullException(nameof(nodeDto));
                _appendedNodes.Add((nodeDto, rootId));
            };


        public override bool TryLeaseNextRoot(out IPayloadCarrierRoot payloadCarrierRoot, out ICacheLeaseEntry lease)
        {
            payloadCarrierRoot = null!;
            lease = null!;
            return false;
        }

        public override void AckNode(Guid nodeId, ISerializedPayload payloadData)
        {
            // Not needed for current tests.
            throw new NotImplementedException();
        }

        public override void FailNode(Guid nodeId, string errorMessage)
        {
            // Not needed for current tests.
            throw new NotImplementedException();
        }

        public override void CancelNode(Guid nodeId)
        {
            // Not needed for current tests.
            throw new NotImplementedException();
        }


        public override bool DeleteRootNode(Guid rootId)
        {
            // Not needed for current tests.
            throw new NotImplementedException();
        }

        public override ICacheLeaseEntry GetByTaskRunnerId(Guid id)
        {
            // Not needed for current tests.
            throw new NotImplementedException();
        }

        public override IPayloadLeaseCache CleanDeleted()
        {
            // Not needed for current tests.
            throw new NotImplementedException();
        }

        protected override void PersistEntryUpdate(ICacheLeaseEntry entry)
        {
            _persistedEntries.Add(entry);
        }

        public override IPayloadLeaseCache CleanFinalized()
        {
            throw new NotImplementedException();
        }

        public override void SuccessRootNode(Guid taskRunnerRootId)
        {
            throw new NotImplementedException();
        }

        public override void LeaseRootNode(ICacheLeaseEntry leaseEntry)
        {
            throw new NotImplementedException();
        }
    }
}
