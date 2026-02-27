using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
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
    internal sealed class DataJobFactory : IDataJobFactory
    {
        private readonly IJobRootFactory _jobRootFactory;
        private readonly IJobFactory _jobFactory;
        private readonly IRetryPolicyGenericFactory _retryPolicyFactory;
        private readonly IPayloadHandlerResolver _handlerResolver;

        private DataJobFactory(
            IJobFactory jobFactory,
            IJobRootFactory jobRootFactory,
            IRetryPolicyGenericFactory retryPolicyFactory,
            IPayloadHandlerResolver handlerResolver)
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
        public static IDataJobFactory Create(
            IJobFactory jobFactory,
            IJobRootFactory jobRootFactory,
            IRetryPolicyGenericFactory retryPolicyFactory,
            IPayloadHandlerResolver handlerResolver)
        {
            return new DataJobFactory(jobFactory, jobRootFactory, retryPolicyFactory,
                handlerResolver);
        }
        public IDataJobRoot DataJobRoot(IJobNodeDto dto, IPayload payload)
        {
            return JobRoot(
                (handler, retryPolicy) 
                    => BuildJobRoot(policy: retryPolicy,jobId: dto.JobId, 
                    name: dto.Name, payload: payload, handler: handler),
                payload, 
                dto.RetryDescriptor
            );
        }

        public IDataJob DataJob(IJobNodeDto dto, IPayload payload)
        {
            return Job((handler)
                    => BuildJob(dto.JobId, dto.Name, payload, handler),
            payload);
        }

        public IDataJobRoot<T> DataJobRoot<T>(T payload, string name = "") 
            where T : IPayload
        {
            return JobRoot(
                (handler, policy) 
                    => BuildJobRoot(policy, Guid.Empty, name, payload, handler), 
                payload
            );
        }
        public IDataJobRoot<T> DataJobRoot<T>(
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

        public IDataJobRoot<T> DataJobRoot<T>(
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

        public IDataJobRoot<T> DataJobRoot<T>(
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
        public IDataJobRoot<T> DataJobRoot<T>(
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
        public IDataJobRoot<T> DataJobRoot<T>(
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

        public IDataJob<T> DataJob<T>(
            T payload,
            string name = "") where T : IPayload
        {
            return Job((handler) 
                    => BuildJob(Guid.Empty,name, payload, handler),
                payload);
        }
        public IDataJob<T> DataJob<T>(
            Guid id,
            T payload,
            string name = "") where T : IPayload
        {
            return Job((handler)
                    => BuildJob(id, name, payload, handler),
                payload);
        }

        private static IUniversaPayloadHandler ResolveHandler<T>(T payload, 
            IPayloadHandlerResolver jobHandlerResolver)
            where T : IPayload
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (jobHandlerResolver is null) throw new ArgumentNullException(nameof(jobHandlerResolver));
            if (string.IsNullOrEmpty(payload.PayloadId)) throw new ArgumentException(nameof(payload.PayloadId));
            
            IUniversaPayloadHandler handler;
            try
            {
                handler = jobHandlerResolver.Resolve(payload.HandlerId);
            }catch(KeyNotFoundException)
            {
                handler = UniversalPayloadHandler.Create((o, ct) => Task.CompletedTask);
            }
            return handler;
        }
        private IDataJobRoot<T> JobRoot<T>(
            Func<IUniversaPayloadHandler, IDataJobRoot<T>> factory, 
            T payload)
            where T : IPayload
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            return factory(ResolveHandler(payload, _handlerResolver));
        }

        private IDataJobRoot<T> JobRoot<T>(
            Func<IUniversaPayloadHandler, Func<IRetryPolicy>, IDataJobRoot<T>> factory,
            T payload, 
            IRetryPolicyDescriptor? retryPolicyDescriptor = null)
            where T : IPayload
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));

            Func<IRetryPolicy> policy = ()=> NoRetryPolicy.Create();
            
            if (retryPolicyDescriptor != null)
            {
                policy = () => _retryPolicyFactory.PolicyByDescriptor(retryPolicyDescriptor);
            }
            return factory(ResolveHandler(payload, _handlerResolver), policy);
        }

        private DataJobRoot<T> BuildJobRoot<T>(
            Func<IRetryPolicy> policy, 
            Guid jobId, string name, T payload, IUniversaPayloadHandler handler)
            where T : IPayload
        {
            var innerJob = CreateJobRoot(policy, name, handler, jobId, payload);
            return Jobs.DataJobRoot<T>.CreateRoot(
                job: innerJob, 
                payload);
        }

        private IJobRoot CreateJobRoot<T>(
            Func<IRetryPolicy> policy, 
            string name, 
            IUniversaPayloadHandler handler, 
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
            IUniversaPayloadHandler handler,
            Guid jobId, 
            T payload)
            where T : IPayload
        {
            return _jobRootFactory.JobRoot<IUniversaPayloadHandler, T>(
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
            IUniversaPayloadHandler handler, 
            T payload)
            where T : IPayload
        {
            return _jobRootFactory.JobRoot<IUniversaPayloadHandler, T>(
                body: async (cancelationToken, handlerObject, payloadObject) =>
                {
                    await handlerObject.ResolveAction(payloadObject, cancelationToken).ConfigureAwait(false);
                },
                arg1: handler,
                arg2: payload,
                retryPolicyFactory: policy,
                name: name);
        }
        
        private DataJob<T> BuildJob<T>( 
            Guid jobId, string name, T payload, IUniversaPayloadHandler handler)
            where T : IPayload
        {
            return Jobs.DataJob<T>.Create(
                job: CreateJob(name, handler, jobId, payload), 
                payload);
        }
        private IJob CreateJob<T>(
            string name, 
            IUniversaPayloadHandler handler, 
            Guid jobId, 
            T payload)
            where T : IPayload
        {
            if (jobId == Guid.Empty)
                return CreateNonIdJob(name, handler, payload);

            return CreateIdJob(name, handler, jobId, payload);
        }

        private IDataJob<T> Job<T>(
            Func<IUniversaPayloadHandler, IDataJob<T>> factory, 
            T payload)
            where T : IPayload
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            return factory(ResolveHandler(payload, _handlerResolver));
        }

        private IJob CreateIdJob<T>(
            string name, 
            IUniversaPayloadHandler handler, 
            Guid jobId, 
            T payload)
            where T : IPayload
        {
            return _jobFactory.Job<IUniversaPayloadHandler,T>(
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
            IUniversaPayloadHandler handler, 
            T payload)
            where T : IPayload
        {
            return _jobFactory.Job<IUniversaPayloadHandler, T>(
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
