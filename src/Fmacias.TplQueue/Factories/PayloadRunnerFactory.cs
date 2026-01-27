using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Jobs;
using System;
using System.Reflection;

namespace Fmacias.TplQueue.Factories
{
    /// <summary>
    /// <![CDATA[
    /// Factory responsible for creating payload-aware task runners and rehydrating them
    /// from cached lease entries.
    ///
    /// Normal creation paths (Create / CreateRoot) are fully generic and type-safe.
    /// Reflection is only used for Load / LoadRoot where the payload type is known
    /// only by its CLR type name stored in the cache (JobNodeDto.PayloadType).
    ///
    /// The factory also integrates with IRetryPolicyFactory so that root runners
    /// are rehydrated with the same retry policy (via IRetryPolicyDescriptor) that
    /// was used when the graph was originally created.
    /// ]]>
    /// </summary>
    internal sealed class PayloadRunnerFactory : IPayloadJobFactory
    {
        private readonly IJobRootFactory _jobRootFactory;
        private readonly IJobFactory _jobFactory;
        private readonly IRetryPolicyFactory _retryPolicyFactory;

        private PayloadRunnerFactory(
            IJobFactory jobFactory,
            IJobRootFactory jobRootFactory,
            IRetryPolicyFactory retryPolicyFactory)
        {
            _jobFactory = jobFactory ?? throw new ArgumentNullException(nameof(jobFactory));
            _jobRootFactory = jobRootFactory ?? throw new ArgumentNullException(nameof(jobRootFactory));
            _retryPolicyFactory = retryPolicyFactory ?? throw new ArgumentNullException(nameof(retryPolicyFactory));
        }

        /// <summary>
        /// Factory method that hides the concrete implementation and enforces injection
        /// of all required dependencies.
        /// </summary>
        public static IPayloadJobFactory Instance(
            IJobFactory jobFactory,
            IJobRootFactory jobRootFactory,
            IRetryPolicyFactory retryPolicyFactory)
        {
            return new PayloadRunnerFactory(jobFactory, jobRootFactory, retryPolicyFactory);
        }

        /// <inheritdoc />
        public IPayloadJob<T> Create<T>(
            T payload,
            IJsonUniversalPayloadSerializer serializer,
            string name = "")
            where T : IPayloadCommand
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));

            return PayloadJob<T>.Create(
                payload,
                serializer,
                _jobFactory,
                name);
        }

        /// <inheritdoc />
        public IPayloadJob<T> Create<T>(
            Guid jobId,
            T payload,
            IJsonUniversalPayloadSerializer serializer,
            string name = "")
            where T : IPayloadCommand
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));

            return PayloadJob<T>.Create(
                jobId,
                payload,
                serializer,
                _jobFactory,
                name);
        }

        /// <inheritdoc />
        public IPayloadJobRoot<T> CreateRoot<T>(
            Guid jobId,
            T payload,
            IJsonUniversalPayloadSerializer serializer,
            Func<IRetryPolicy>? retryPolicyFactory = null,
            string name = "")
            where T : IPayloadCommand
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));

            return PayloadJobRoot<T>.Create(
                jobId,
                payload,
                serializer,
                _jobRootFactory,
                retryPolicyFactory,
                name);
        }

        /// <inheritdoc />
        public IPayloadJobRoot<T> CreateRoot<T>(
            T payload,
            IJsonUniversalPayloadSerializer serializer,
            Func<IRetryPolicy>? retryPolicyFactory = null,
            string name = "")
            where T : IPayloadCommand
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));

            return PayloadJobRoot<T>.Create(
                payload,
                serializer,
                _jobRootFactory,
                retryPolicyFactory,
                name);
        }

        /// <inheritdoc />
        public IPayloadCarrierJob Load(
            ICacheLeaseEntry lease,
            IJsonUniversalPayloadSerializer serializer)
        {
            if (lease == null) throw new ArgumentNullException(nameof(lease));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));

            var nodeDto = lease.JobNodeDto;
            if (string.IsNullOrWhiteSpace(nodeDto.PayloadType))
                throw new InvalidOperationException(
                    "PayloadType must be provided in JobNodeDto to rehydrate a payload runner.");

            var payloadType = Type.GetType(nodeDto.PayloadType!, throwOnError: true)!;
            var runnerType = typeof(PayloadJob<>).MakeGenericType(payloadType);

            var loadMethod = runnerType.GetMethod(
                name: "Load",
                bindingAttr: BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[]
                {
                    typeof(ICacheLeaseEntry),
                    typeof(IJsonUniversalPayloadSerializer),
                    typeof(IJobFactory)
                },
                modifiers: null);

            if (loadMethod is null)
            {
                throw new InvalidOperationException(
                    "Cannot locate PayloadJob<T>.Load(ICacheLeaseEntry, IUniversalPayloadSerializer, IJobFactory).");
            }

            return (IPayloadCarrierJob)loadMethod.Invoke(
                obj: null,
                parameters: new object[]
                {
                    lease,
                    serializer,
                    _jobFactory
                })!;
        }

        /// <inheritdoc />
        public IPayloadJobRoot LoadRoot(
            ICacheLeaseEntry lease,
            IJsonUniversalPayloadSerializer serializer)
        {
            if (lease == null) throw new ArgumentNullException(nameof(lease));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));

            var nodeDto = lease.JobNodeDto;
            if (string.IsNullOrWhiteSpace(nodeDto.PayloadType))
                throw new InvalidOperationException(
                    "PayloadType must be provided in JobNodeDto to rehydrate a root payload runner.");

            var payloadType = Type.GetType(nodeDto.PayloadType!, throwOnError: true)!;
            var runnerType = typeof(PayloadJobRoot<>).MakeGenericType(payloadType);

            var loadMethod = runnerType.GetMethod(
                name: "Load",
                bindingAttr: BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[]
                {
                    typeof(ICacheLeaseEntry),
                    typeof(IJsonUniversalPayloadSerializer),
                    typeof(IJobRootFactory),
                    typeof(IRetryPolicyFactory)
                },
                modifiers: null);

            if (loadMethod is null)
            {
                throw new InvalidOperationException(
                    "Cannot locate PayloadJobRoot<T>.Load(ICacheLeaseEntry, IUniversalPayloadSerializer, IJobRootFactory, IRetryPolicyFactory).");
            }

            return (IPayloadJobRoot)loadMethod.Invoke(
                obj: null,
                parameters: new object[]
                {
                    lease,
                    serializer,
                    _jobRootFactory,
                    _retryPolicyFactory
                })!;
        }
    }
}
