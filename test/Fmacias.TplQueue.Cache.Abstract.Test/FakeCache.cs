using System;
using System.Collections.Generic;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Cache.Abstract.Test
{
    internal sealed class FakeCache : CacheAbstract
    {
        private readonly List<(IJobNodeRecord Node, Guid RootId)> _appendedNodes = new();

        public IReadOnlyList<(IJobNodeRecord Node, Guid RootId)> AppendedNodes => _appendedNodes;

        public FakeCache(
            IUniversalDataSerializer serializer,
            ICacheRepository cacheRepository,
            ITypeResolver typeResolver,
            IDataJobFactory payloadJobFactory,
            ICacheEntryFactory cacheEntryFactory,
            IPayloadHandlers payloadHandlerResolver,
            IRetryPolicyAbstractFactory retryPolicyAbstractFactory)
            : base(serializer, cacheRepository, payloadJobFactory, cacheEntryFactory, typeResolver, payloadHandlerResolver, retryPolicyAbstractFactory)
        {
        }

        protected override Action<IJobNodeRecord, Guid> OnDehydration =>
            (nodeDto, rootId) =>
            {
                if (nodeDto is null) throw new ArgumentNullException(nameof(nodeDto));
                _appendedNodes.Add((nodeDto, rootId));
            };
    }
}
