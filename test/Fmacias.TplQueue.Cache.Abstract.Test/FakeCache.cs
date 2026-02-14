using System;
using System.Collections.Generic;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Cache.Abstract.Test
{
    internal sealed class FakeCache : CacheAbstract
    {
        private readonly List<(IJobNodeDto Node, Guid RootId)> _appendedNodes = new();

        public IReadOnlyList<(IJobNodeDto Node, Guid RootId)> AppendedNodes => _appendedNodes;

        public FakeCache(
            IUniversalPayloadSerializer serializer,
            ICacheRepository cacheRepository,
            INodeTypeResolver typeResolver,
            IPayloadJobFactory payloadJobFactory,
            ICacheEntryFactory cacheEntryFactory)
            : base(serializer,cacheRepository,typeResolver,payloadJobFactory,cacheEntryFactory)
        {
        }

        protected override Action<IJobNodeDto, Guid> OnDehydration =>
            (nodeDto, rootId) =>
            {
                if (nodeDto is null) throw new ArgumentNullException(nameof(nodeDto));
                _appendedNodes.Add((nodeDto, rootId));
            };
    }
}
