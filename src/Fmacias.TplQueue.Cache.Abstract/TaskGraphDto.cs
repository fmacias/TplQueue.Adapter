using System;
using System.Collections.Generic;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Cache.Abstract
{
    /// <summary>
    /// Default implementation of <see cref="ITaskGraphDto"/> that transforms a payload runner graph
    /// into a list of <see cref="IJobNodeDto"/> instances and invokes a callback for each node.
    /// </summary>
    internal sealed class TaskGraphDto : ITaskGraphDto
    {
        private readonly IJsonUniversalPayloadSerializer _serializer;
        private readonly IPayloadJobRoot _rootGraph;
        private readonly bool _isFifo;

        private TaskGraphDto(
            IJsonUniversalPayloadSerializer serializer,
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
        /// Factory method to create a new <see cref="ITaskGraphDto"/> instance.
        /// </summary>
        public static ITaskGraphDto Create(
            IJsonUniversalPayloadSerializer serializer,
            IPayloadJobRoot rootGraph, bool isFifo)
        {
            return new TaskGraphDto(serializer, rootGraph, isFifo);
        }

        /// <inheritdoc />
        public IReadOnlyList<IJobNodeDto> ExtractNodes(Action<IJobNodeDto, Guid> edgedNodeCallBack)
        {
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

        private void MaterializeDtoNode(IPayloadCarrierJob current, IDictionary<Guid, IJobNodeDto> nodes, Action<IJobNodeDto, Guid> callBack, Guid rootId, IPayloadCarrierJob? parent)
        {
            if (!nodes.TryGetValue(current.Id, out var dto))
            {
                var parentId = parent?.Id ?? Guid.Empty;
                var isRoot = current is IPayloadJobRoot;

                var (typeName, json) = SerializePayload(current);
                var policyFactory = current.GetRetryPolicyFactory();
                var policy = policyFactory();
                var retryDescriptor = policy.ToDescriptor();

                dto = JobNodeDto.Create(
                    jobId: current.Id,
                    parentJobId: parentId,
                    payloadType: typeName,
                    payloadJson: json,
                    isRoot: isRoot,
                    isFifo: current.Id == _rootGraph.Id && _isFifo,
                    retryPolicyDescriptor: retryDescriptor,
                    name: current.Name);

                nodes[current.Id] = dto;
                callBack?.Invoke(dto, rootId);
            }
        }

        private void TraverseDependentsFirst(IPayloadCarrierJob current, ISet<Guid> visited, IDictionary<Guid, IJobNodeDto> nodes, Action<IJobNodeDto, Guid> callBack, Guid rootId)
        {
            foreach (var child in current.GetPayloadDependencies())
            {
                if (child is null) continue;
                DfsBuild(child, visited, nodes, callBack, rootId, current);
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

        /// <summary>
        /// Serializes the payload associated with the given carrier into JSON and returns its type name.
        /// </summary>
        private (string typeName, string json) SerializePayload(IPayloadCarrierJob carrier)
        {
            if (carrier is null) throw new ArgumentNullException(nameof(carrier));

            var payload = carrier.GetPayload();
            var type = carrier.PayloadType
                       ?? payload?.GetType()
                       ?? throw new InvalidOperationException("Payload type cannot be determined.");

            var json = _serializer.Serialize(payload, type);
            var typeName = type.AssemblyQualifiedName
                           ?? type.FullName
                           ?? type.Name;

            return (typeName, json);
        }
    }
}
