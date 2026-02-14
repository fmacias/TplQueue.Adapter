using System;
using System.Collections.Generic;
using Fmacias.TplQueue.Cache.DomainModels;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Cache.Helpers
{
    /// <summary>
    /// Default implementation of <see cref="IJobGraphDto"/> that transforms a payload runner graph
    /// into a list of <see cref="IJobNodeDto"/> instances and invokes a callback for each node.
    /// </summary>
    internal sealed class JobGraphDto : IJobGraphDto
    {
        private readonly IUniversalPayloadSerializer _serializer;
        private readonly IPayloadJobRoot _rootGraph;
        private readonly bool _isFifo;

        private JobGraphDto(
            IUniversalPayloadSerializer serializer,
            IPayloadJobRoot rootGraph,
            bool isFifo)
        {
            _serializer = serializer
                ?? throw new ArgumentNullException(nameof(serializer));
            _rootGraph = rootGraph
                ?? throw new ArgumentNullException(nameof(rootGraph));
            _isFifo = isFifo;
        }

        /// <summary>
        /// Factory method to create a new <see cref="IJobGraphDto"/> instance.
        /// </summary>
        public static IJobGraphDto Create(
            IUniversalPayloadSerializer serializer,
            IPayloadJobRoot rootGraph, bool isFifo)
        {
            return new JobGraphDto(serializer, rootGraph, isFifo);
        }

        /// <inheritdoc />
        public IReadOnlyList<IJobNodeDto> ExtractNodes(Action<IJobNodeDto, Guid> edgedNodeCallBack)
        {
            if (edgedNodeCallBack is null) throw new ArgumentNullException(nameof(edgedNodeCallBack));
            return ExtractDtoNodesAndEdges(edgedNodeCallBack);
        }

        /// <summary>
        /// Performs a depth-first traversal of the payload runner graph, creating DTO nodes and invoking the callback.
        /// </summary>
        private IJobNodeDto[] ExtractDtoNodesAndEdges(
            Action<IJobNodeDto, Guid> callBack)
        {
            var visited = new HashSet<Guid>();
            var nodes = new Dictionary<Guid, IJobNodeDto>();

            DfsBuild(
                current: _rootGraph,
                visited: visited,
                nodes: nodes,
                callBack: callBack,
                rootId: _rootGraph.Id,
                parent: null);

            return [.. nodes.Values];
        }

        /// <summary>
        /// Depth-first traversal that builds <see cref="IJobNodeDto"/> instances lazily and calls the callback.
        /// </summary>
        private void DfsBuild(
            IPayloadCarrierJob current,
            ISet<Guid> visited,
            IDictionary<Guid, IJobNodeDto> nodes,
            Action<IJobNodeDto, Guid> callBack,
            Guid rootId,
            IPayloadCarrierJob? parent)
        {
            if (current is null) throw new ArgumentNullException(nameof(current));
            if (visited is null) throw new ArgumentNullException(nameof(visited));
            if (nodes is null) throw new ArgumentNullException(nameof(nodes));

            bool flowControl = AvoidCyclesAndDuplicateNodes(current, visited, nodes);
            
            if (!flowControl)
            {
                return;
            }

            TraverseDependentsFirst(current, visited, nodes, callBack, rootId);

            MaterializeDtoNode(current, nodes, callBack, rootId, parent);
        }

        private void MaterializeDtoNode(IPayloadCarrierJob payloadJob, 
            IDictionary<Guid, IJobNodeDto> nodes, Action<IJobNodeDto, Guid> callBack, 
            Guid rootId, IPayloadCarrierJob? parent)
        {
            if (!nodes.TryGetValue(payloadJob.Id, out var dto))
            {
                var isFifo = payloadJob.Id == _rootGraph.Id && _isFifo;
                dto = JobNodeDto.Create(_serializer,payloadJob,isFifo,parent);
                nodes[payloadJob.Id] = dto;
                callBack(dto, rootId);
            }
        }

        private void TraverseDependentsFirst(IPayloadCarrierJob payloadJob, ISet<Guid> visited, IDictionary<Guid, IJobNodeDto> nodes, Action<IJobNodeDto, Guid> callBack, Guid rootId)
        {
            foreach (var job in payloadJob.GetPayloadDependencies())
            {
                if (job is null) continue;
                DfsBuild(job, visited, nodes, callBack, rootId, payloadJob);
            }
        }

        private static bool AvoidCyclesAndDuplicateNodes(IPayloadCarrierJob current, ISet<Guid> visited, IDictionary<Guid, IJobNodeDto> nodes)
        {
            if (!visited.Add(current.Id) || nodes.ContainsKey(current.Id))
            {
                return false;
            }

            return true;
        }
    }
}
