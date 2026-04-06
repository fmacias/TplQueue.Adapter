using System;
using System.Collections.Generic;
using System.Linq;
using Fmacias.TplQueue.Cache.Abstract.Models;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Cache.Abstract.Helpers
{
    /// <summary>
    /// Default implementation of <see cref="IJobGraphDto"/> that transforms a payload runner graph
    /// into a list of <see cref="IJobNodeDto"/> instances and invokes a callback for each node.
    /// </summary>
    internal sealed class JobGraphDto : IJobGraphDto
    {
        private readonly IUniversalDataSerializer _serializer;
        private readonly IDataJobRoot _rootGraph;
        private readonly bool _isFifo;

        private JobGraphDto(
            IUniversalDataSerializer serializer,
            IDataJobRoot rootGraph,
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
            IUniversalDataSerializer serializer,
            IDataJobRoot rootGraph, bool isFifo)
        {
            return new JobGraphDto(serializer, rootGraph, isFifo);
        }

        /// <inheritdoc />
        public IReadOnlyList<IJobNodeDto> ExtractNodes(Action<IJobNodeRecord, Guid> edgedNodeCallBack)
        {
            if (edgedNodeCallBack is null) throw new ArgumentNullException(nameof(edgedNodeCallBack));
            return ExtractDtoNodesAndEdges(edgedNodeCallBack);
        }

        /// <summary>
        /// Performs a depth-first traversal of the payload runner graph, creating DTO nodes and invoking the callback.
        /// </summary>
        private IJobNodeDto[] ExtractDtoNodesAndEdges(
            Action<IJobNodeRecord, Guid> callBack)
        {
            var visited = new HashSet<Guid>();
            var nodes = new Dictionary<Guid, IJobNodeDto>();

            DfsBuild(
                dataJob: _rootGraph,
                visited: visited,
                jobNodes: nodes,
                callBack: callBack,
                rootId: _rootGraph.Id,
                parent: null);

            return nodes.Values.ToArray();
        }

        /// <summary>
        /// Depth-first traversal that builds <see cref="IJobNodeDto"/> instances lazily and calls the callback.
        /// </summary>
        private void DfsBuild(
            IDataJobNode dataJob,
            ISet<Guid> visited,
            IDictionary<Guid, IJobNodeDto> jobNodes,
            Action<IJobNodeRecord, Guid> callBack,
            Guid rootId,
            IDataJobNode? parent)
        {
            if (dataJob is null) throw new ArgumentNullException(nameof(dataJob));
            if (visited is null) throw new ArgumentNullException(nameof(visited));
            if (jobNodes is null) throw new ArgumentNullException(nameof(jobNodes));

            bool flowControl = AvoidCyclesAndDuplicateNodes(dataJob, visited, jobNodes);
            
            if (!flowControl)
            {
                return;
            }

            TraverseDependentsFirst(dataJob, visited, jobNodes, callBack, rootId);

            MaterializeDtoNode(dataJob, jobNodes, callBack, rootId, parent);
        }

        private void MaterializeDtoNode(IDataJobNode dataJob, 
            IDictionary<Guid, IJobNodeDto> jobNodes, Action<IJobNodeRecord, Guid> callBack, 
            Guid rootId, IDataJobNode? parent)
        {
            if (!jobNodes.TryGetValue(dataJob.Id, out var dto))
            {
                var isFifo = dataJob.Id == _rootGraph.Id && _isFifo;
                dto = JobNodeDto.Create(_serializer,dataJob,isFifo,parent);
                jobNodes[dataJob.Id] = dto;
                callBack(dto, rootId);
            }
        }

        private void TraverseDependentsFirst(IDataJobNode payloadJob, ISet<Guid> visited, IDictionary<Guid, IJobNodeDto> nodes, Action<IJobNodeRecord, Guid> callBack, Guid rootId)
        {
            foreach (var job in payloadJob.GetDependentDataJobs())
            {
                if (job is null) continue;
                DfsBuild(job, visited, nodes, callBack, rootId, payloadJob);
            }
        }

        private static bool AvoidCyclesAndDuplicateNodes(IDataJobNode current, ISet<Guid> visited, IDictionary<Guid, IJobNodeDto> nodes)
        {
            if (!visited.Add(current.Id) || nodes.ContainsKey(current.Id))
            {
                return false;
            }

            return true;
        }
    }
}
