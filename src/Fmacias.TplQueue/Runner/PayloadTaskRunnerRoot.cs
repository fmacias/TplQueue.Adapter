using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using System;
using System.Threading;

namespace Fmaciasruano.TplQueue.Runner
{
    internal sealed class PayloadTaskRunnerRoot<TPayload> :
        PayloadTaskRunner<TPayload>,
        ITaskRunnerAdapter,
        IPayloadTaskRunnerRoot<TPayload>
        where TPayload : IPayloadCommand
    {
        private readonly ITaskRunnerRoot _innerRoot;
        private PayloadTaskRunnerRoot(ITaskRunnerRoot innerRoot, TPayload payload,
            string jsonSerialized, IUniversalPayloadSerializer serializer)
            :base(innerRoot, payload, jsonSerialized, serializer)
        {
            _innerRoot = innerRoot;
        }
        public static PayloadTaskRunnerRoot<TPayload> Load(
            ICacheLeaseEntry cacheLeaseEntry,
            IUniversalPayloadSerializer serializer,
            ITaskRunnerRootFactory taskRunnerRootFactory,
            IRetryPolicyFactory retrypolicyFactory)
        {
            cacheLeaseEntry = cacheLeaseEntry ?? throw new ArgumentNullException(nameof(cacheLeaseEntry));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            if (taskRunnerRootFactory is null) throw new ArgumentNullException(nameof(taskRunnerRootFactory));
            if (retrypolicyFactory is null) throw new ArgumentNullException(nameof(retrypolicyFactory));

            var json = cacheLeaseEntry.TaskRunnerNodeDto.PayloadJson;
            var payload = serializer.Deserialize<TPayload>(json);
            
            var policyFactory = () => retrypolicyFactory.Create(cacheLeaseEntry.RetryDescriptor);
            var root = taskRunnerRootFactory.Create(
                id: cacheLeaseEntry.TaskRunnerId,
                body: async (ct, pl) =>
                {
                    await pl.ExecuteAsync(ct).ConfigureAwait(false);
                },
                arg: payload,
                retryPolicyFactory: policyFactory,
                name: cacheLeaseEntry.TaskRunnerNodeDto.Name ?? string.Empty);

            return new PayloadTaskRunnerRoot<TPayload>(root, payload, json, serializer);
        }

        public static PayloadTaskRunnerRoot<TPayload> Create(ITaskRunnerRoot root,
            TPayload payload, IUniversalPayloadSerializer serializer)
        {
            if (root is null) throw new ArgumentNullException(nameof(root));
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            return new PayloadTaskRunnerRoot<TPayload>(root, payload, 
                serializer.Serialize(payload), serializer);
        }

        public static PayloadTaskRunnerRoot<TPayload> Create(
            Guid taskRunnerId,
            TPayload payload,
            IUniversalPayloadSerializer serializer,
            ITaskRunnerRootFactory taskRunnerRootFactory,
            Func<IRetryPolicy>? retryPolicyFactory = null, string name = "")
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            if (taskRunnerRootFactory is null) throw new ArgumentNullException(nameof(taskRunnerRootFactory));
            var root = taskRunnerRootFactory.Create(
                id: taskRunnerId,
                body: async (ct,pl) => await pl.ExecuteAsync(ct).ConfigureAwait(false),
                arg: payload,
                retryPolicyFactory: ResolveRetryPolicyFactory(retryPolicyFactory),
                name: name);
            var json = serializer.Serialize(payload);
            return new PayloadTaskRunnerRoot<TPayload>(root, payload, json, serializer);
        }
        public static PayloadTaskRunnerRoot<TPayload> Create(
            TPayload payload,
            IUniversalPayloadSerializer serializer,
            ITaskRunnerRootFactory taskRunnerRootFactory,
            Func<IRetryPolicy>? retryPolicyFactory = null, string name = "")
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            if (taskRunnerRootFactory is null) throw new ArgumentNullException(nameof(taskRunnerRootFactory));
            var root = taskRunnerRootFactory.Create(
                func: async (ct, pl) => await pl.ExecuteAsync(ct).ConfigureAwait(false),
                arg: payload,
                retryPolicyFactory: ResolveRetryPolicyFactory(retryPolicyFactory),
                name: name);
            var json = serializer.Serialize(payload);
            return new PayloadTaskRunnerRoot<TPayload>(root, payload, json, serializer);
        }
        public ITaskDispatcher Enqueue(ITaskDispatcher queue, CancellationToken ct) 
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));
            return _innerRoot.Enqueue(queue, ct);
        }

        private static Func<IRetryPolicy> ResolveRetryPolicyFactory(Func<IRetryPolicy>? retryPolicyFactory)
        {
            return retryPolicyFactory ?? (() => NoRetryPolicy.Create());
        }
    }
}

