using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Jobs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fmacias.TplQueue.Factories
{
    /// <summary>
    /// <![CDATA[
    /// Factory responsible for creating payload-aware jobs and rehydrating them
    /// from cached lease entries.
    ///
    /// Normal creation paths are fully generic and type-safe.
    /// Reflection is only used for Load / LoadRoot where the payload type is known
    /// only by its CLR(Common Language Runtime) type name stored in the cache 
    /// (JobNodeDto.PayloadType).
    ///
    /// The factory also integrates with IRetryPolicyFactory so that root jobs
    /// are rehydrated with the same retry policy (via IRetryPolicyDescriptor) that
    /// was used when the graph of jobs was originally created.
    /// ]]>
    /// </summary>
    internal sealed class PayloadJobFactory : IPayloadJobFactory
    {
        private readonly IJobRootFactory _jobRootFactory;
        private readonly IJobFactory _jobFactory;
        private readonly IRetryPolicyFactory _retryPolicyFactory;
        private readonly IJobHandlerResolver2 _handlerResolver;

        private PayloadJobFactory(
            IJobFactory jobFactory,
            IJobRootFactory jobRootFactory,
            IRetryPolicyFactory retryPolicyFactory,
            IJobHandlerResolver2 handlerResolver)
        {
            _jobFactory = jobFactory ?? throw new ArgumentNullException(nameof(jobFactory));
            _jobRootFactory = jobRootFactory 
                ?? throw new ArgumentNullException(nameof(jobRootFactory));
            _retryPolicyFactory = retryPolicyFactory 
                ?? throw new ArgumentNullException(nameof(retryPolicyFactory));
            _handlerResolver = handlerResolver 
                ?? throw new ArgumentNullException(nameof(handlerResolver));
        }
        /// <summary>
        /// Factory method that hides the concrete implementation and enforces injection
        /// of all required dependencies.
        /// </summary>
        public static IPayloadJobFactory Create(
            IJobFactory jobFactory,
            IJobRootFactory jobRootFactory,
            IRetryPolicyFactory retryPolicyFactory,
            IJobHandlerResolver2 handlerResolver)
        {
            return new PayloadJobFactory(jobFactory, jobRootFactory, retryPolicyFactory,
                handlerResolver);
        }
        public IPayloadJobRoot CreateJobRoot(IJobNodeDto dto, IPayload payload)
        {
            return JobRoot(
                (handler, policy) 
                    => BuildJobRoot(policy, dto.JobId, dto.Name, payload, handler),
                payload, 
                dto.RetryDescriptor
            );
        }
        public IPayloadJobRoot<T> CreateJobRoot<T>(T payload, string name = "") 
            where T : IPayload
        {
            return JobRoot(
                (handler, policy) 
                    => BuildJobRoot(policy, Guid.Empty, name, payload, handler), 
                payload
            );
        }
        public IPayloadJobRoot<T> CreateJobRoot<T>(
            Guid id,
            T payload,
            string name = "") where T : IPayload
        {
            return JobRoot(
                (handler, policy) 
                    => BuildJobRoot(policy, id, name, payload, handler), 
                payload
            );
        }

        public IPayloadJobRoot<T> CreateJobRoot<T>(
            T payload,
            IRetryPolicyDescriptor retryPolicyDescriptor,
            string name = "") where T : IPayload
        {
            return JobRoot(
                (handler, policy)
                    => BuildJobRoot(policy, Guid.Empty, name, payload, handler),
                payload, 
                retryPolicyDescriptor
            );
        }

        public IPayloadJobRoot<T> CreateJobRoot<T>(
            Guid id,
            T payload,
            IRetryPolicyDescriptor retryPolicyDescriptor,
            string name = "") where T : IPayload
        {
            return JobRoot(
                (handler, policy)
                    => BuildJobRoot(policy, id, name, payload, handler),
                payload, 
                retryPolicyDescriptor
            );
        }
        public IPayloadJobRoot<T> CreateJobRoot<T>(
            T payload,
            Func<IRetryPolicy> policy,
            string name = "") where T : IPayload
        {
            return JobRoot(
                (handler)
                    => BuildJobRoot(policy, Guid.Empty, name, payload, handler),
                payload
            );
        }
        public IPayloadJobRoot<T> CreateJobRoot<T>(
            Guid jobId,
            T payload,
            Func<IRetryPolicy> policy,
            string name = "") where T : IPayload
        {
            return JobRoot(
                (handler)
                    => BuildJobRoot(policy, jobId, name, payload, handler),
                payload
            );
        }

        public IPayloadCarrierJob CreateJob(IJobNodeDto dto, IPayload payload)
        {
            return Job((handler) 
                    => BuildJob(dto.JobId, dto.Name, payload, handler),
            payload);
        }

        public IPayloadJob<T> CreateJob<T>(
            T payload,
            string name = "") where T : IPayload
        {
            return Job((handler) 
                    => BuildJob(Guid.Empty,name, payload, handler),
                payload);
        }
        public IPayloadJob<T> CreateJob<T>(
            Guid id,
            T payload,
            string name = "") where T : IPayload
        {
            return Job((handler)
                    => BuildJob(id, name, payload, handler),
                payload);
        }

        private static IUniversaDtoHandler2 ResolveHandler<T>(T payload, 
            IJobHandlerResolver2 jobHandlerResolver)
            where T : IPayload
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (jobHandlerResolver is null) throw new ArgumentNullException(nameof(jobHandlerResolver));
            if (string.IsNullOrEmpty(payload.PayloadId)) throw new ArgumentException(nameof(payload.PayloadId));
            
            IUniversaDtoHandler2 handler;
            try
            {
                handler = jobHandlerResolver.Resolve(payload.HandlerId);
            }catch(KeyNotFoundException)
            {
                handler = UniversalDtoHandler.Create((o, ct) => Task.CompletedTask);
            }
            return handler;
        }
        private IPayloadJobRoot<T> JobRoot<T>(
            Func<IUniversaDtoHandler2, IPayloadJobRoot<T>> factory, 
            T payload)
            where T : IPayload
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            return factory(ResolveHandler(payload, _handlerResolver));
        }

        private IPayloadJobRoot<T> JobRoot<T>(
            Func<IUniversaDtoHandler2, Func<IRetryPolicy>, IPayloadJobRoot<T>> factory,
            T payload, 
            IRetryPolicyDescriptor? retryPolicyDescriptor = null)
            where T : IPayload
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));

            Func<IRetryPolicy> policy = ()=> NoRetryPolicy.Create();
            
            if (retryPolicyDescriptor != null)
            {
                policy = () => _retryPolicyFactory.Create(retryPolicyDescriptor);
            }
            return factory(ResolveHandler(payload, _handlerResolver), policy);
        }

        private PayloadJobRoot<T> BuildJobRoot<T>(
            Func<IRetryPolicy> policy, 
            Guid jobId, string name, T payload, IUniversaDtoHandler2 handler)
            where T : IPayload
        {
            var innerJob = CreateJobRoot(policy, name, handler, jobId, payload);
            return PayloadJobRoot<T>.CreateRoot(
                job: innerJob, 
                payload);
        }

        private IJobRoot CreateJobRoot<T>(
            Func<IRetryPolicy> policy, 
            string name, 
            IUniversaDtoHandler2 handler, 
            Guid jobId, 
            T payload)
            where T : IPayload
        {
            if (jobId == Guid.Empty)
                return RootWithoutId(policy, name, handler, payload);

            return RootWithId(policy, name, handler, jobId, payload);
        }

        private IJobRoot RootWithId<T>(
            Func<IRetryPolicy> policy, 
            string name, 
            IUniversaDtoHandler2 handler,
            Guid jobId, 
            T payload)
            where T : IPayload
        {
            return _jobRootFactory.CreateJob<IUniversaDtoHandler2, T>(
                id: jobId,
                body: async (cancelationToken, handlerObject, payloadObject) =>
                {
                    await handlerObject.ResolveAction(payloadObject, cancelationToken).ConfigureAwait(false);
                },
                arg1: handler,
                arg2: payload,
                retryPolicyFactory: policy,
                name: name);
        }
        private IJobRoot RootWithoutId<T>(
            Func<IRetryPolicy> policy, 
            string name, 
            IUniversaDtoHandler2 handler, 
            T payload)
            where T : IPayload
        {
            return _jobRootFactory.CreateJob<IUniversaDtoHandler2, T>(
                body: async (cancelationToken, handlerObject, payloadObject) =>
                {
                    await handlerObject.ResolveAction(payloadObject, cancelationToken).ConfigureAwait(false);
                },
                arg1: handler,
                arg2: payload,
                retryPolicyFactory: policy,
                name: name);
        }
        
        private PayloadJob<T> BuildJob<T>( 
            Guid jobId, string name, T payload, IUniversaDtoHandler2 handler)
            where T : IPayload
        {
            return PayloadJob<T>.Create(
                job: CreateJob(name, handler, jobId, payload), 
                payload);
        }
        private IJob CreateJob<T>(
            string name, 
            IUniversaDtoHandler2 handler, 
            Guid jobId, 
            T payload)
            where T : IPayload
        {
            if (jobId == Guid.Empty)
                return CreateNonIdJob(name, handler, payload);

            return CreateIdJob(name, handler, jobId, payload);
        }

        private IPayloadJob<T> Job<T>(
            Func<IUniversaDtoHandler2, IPayloadJob<T>> factory, 
            T payload)
            where T : IPayload
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            return factory(ResolveHandler(payload, _handlerResolver));
        }

        private IJob CreateIdJob<T>(
            string name, 
            IUniversaDtoHandler2 handler, 
            Guid jobId, 
            T payload)
            where T : IPayload
        {
            return _jobFactory.CreateJob<IUniversaDtoHandler2,T>(
                id: jobId,
                body: async (cancelationToken, handlerObject, payloadObject) =>
                {
                    await handlerObject.ResolveAction(payloadObject,cancelationToken).ConfigureAwait(false);
                },
                arg1: handler,
                arg2: payload,
                name: name);
        }
        private IJob CreateNonIdJob<T>(
            string name, 
            IUniversaDtoHandler2 handler, 
            T payload)
            where T : IPayload
        {
            return _jobFactory.CreateJob<IUniversaDtoHandler2, T>(
                body: async (cancelationToken, handlerObject, payloadObject) =>
                {
                    await handlerObject.ResolveAction(payloadObject,cancelationToken).ConfigureAwait(false);
                },
                arg1: handler,
                arg2: payload,
                name: name);
        }
    }
}
