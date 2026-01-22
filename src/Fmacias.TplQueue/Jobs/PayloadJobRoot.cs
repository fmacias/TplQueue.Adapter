using Fmacias.TplQueue.Contracts;
using System;
using System.Threading;

namespace Fmacias.TplQueue.Jobs
{
    internal sealed class PayloadJobRoot<TPayload> :
        PayloadJob<TPayload>,
        IJobAdapter,
        IPayloadJobRoot<TPayload>
        where TPayload : IPayloadCommand
    {
        private readonly IJobRoot _innerRoot;
        private PayloadJobRoot(IJobRoot innerRoot, TPayload payload,
            string jsonSerialized, IUniversalPayloadSerializer serializer)
            :base(innerRoot, payload, jsonSerialized, serializer)
        {
            _innerRoot = innerRoot;
        }
        public static PayloadJobRoot<TPayload> Load(
            ICacheLeaseEntry cacheLeaseEntry,
            IUniversalPayloadSerializer serializer,
            IJobRootFactory jobRootFactory,
            IRetryPolicyFactory retrypolicyFactory)
        {
            cacheLeaseEntry = cacheLeaseEntry ?? throw new ArgumentNullException(nameof(cacheLeaseEntry));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            if (jobRootFactory is null) throw new ArgumentNullException(nameof(jobRootFactory));
            if (retrypolicyFactory is null) throw new ArgumentNullException(nameof(retrypolicyFactory));

            var json = cacheLeaseEntry.JobNodeDto.PayloadJson;
            var payload = serializer.Deserialize<TPayload>(json);
            
            var policyFactory = () => retrypolicyFactory.Create(cacheLeaseEntry.RetryDescriptor);
            var root = jobRootFactory.Create(
                id: cacheLeaseEntry.JobId,
                body: async (ct, pl) =>
                {
                    await pl.ExecuteAsync(ct).ConfigureAwait(false);
                },
                arg: payload,
                retryPolicyFactory: policyFactory,
                name: cacheLeaseEntry.JobNodeDto.Name ?? string.Empty);

            return new PayloadJobRoot<TPayload>(root, payload, json, serializer);
        }

        public static PayloadJobRoot<TPayload> Create(IJobRoot root,
            TPayload payload, IUniversalPayloadSerializer serializer)
        {
            if (root is null) throw new ArgumentNullException(nameof(root));
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            return new PayloadJobRoot<TPayload>(root, payload, 
                serializer.Serialize(payload), serializer);
        }

        public static PayloadJobRoot<TPayload> Create(
            Guid jobId,
            TPayload payload,
            IUniversalPayloadSerializer serializer,
            IJobRootFactory jobRootFactory,
            Func<IRetryPolicy>? retryPolicyFactory = null, string name = "")
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            if (jobRootFactory is null) throw new ArgumentNullException(nameof(jobRootFactory));
            var root = jobRootFactory.Create(
                id: jobId,
                body: async (ct,pl) => await pl.ExecuteAsync(ct).ConfigureAwait(false),
                arg: payload,
                retryPolicyFactory: ResolveRetryPolicyFactory(retryPolicyFactory),
                name: name);
            var json = serializer.Serialize(payload);
            return new PayloadJobRoot<TPayload>(root, payload, json, serializer);
        }
        public static PayloadJobRoot<TPayload> Create(
            TPayload payload,
            IUniversalPayloadSerializer serializer,
            IJobRootFactory jobRootFactory,
            Func<IRetryPolicy>? retryPolicyFactory = null, string name = "")
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            if (jobRootFactory is null) throw new ArgumentNullException(nameof(jobRootFactory));
            var root = jobRootFactory.Create(
                func: async (ct, pl) => await pl.ExecuteAsync(ct).ConfigureAwait(false),
                arg: payload,
                retryPolicyFactory: ResolveRetryPolicyFactory(retryPolicyFactory),
                name: name);
            var json = serializer.Serialize(payload);
            return new PayloadJobRoot<TPayload>(root, payload, json, serializer);
        }
        public IJobQ Enqueue(IJobQ queue, CancellationToken ct) 
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

