using Fmacias.TplQueue.Contracts;
using System;
using System.Reflection;

namespace Fmacias.TplQueue.Runner
{
    /// <summary>
    /// <![CDATA[
    /// Factory responsible for creating payload-aware task runners and rehydrating them
    /// from cached lease entries.
    ///
    /// Normal creation paths (Create / CreateRoot) are fully generic and type-safe.
    /// Reflection is only used for Load / LoadRoot where the payload type is known
    /// only by its CLR type name stored in the cache (TaskRunnerNodeDto.PayloadType).
    ///
    /// The factory also integrates with IRetryPolicyFactory so that root runners
    /// are rehydrated with the same retry policy (via IRetryPolicyDescriptor) that
    /// was used when the graph was originally created.
    /// ]]>
    /// </summary>
    internal sealed class PayloadRunnerFactory : IPayloadRunnerFactory
    {
        private readonly ITaskRunnerRootFactory _taskRunnerRootFactory;
        private readonly ITaskRunnerFactory _taskRunnerFactory;
        private readonly IRetryPolicyFactory _retryPolicyFactory;

        private PayloadRunnerFactory(
            ITaskRunnerFactory taskRunnerFactory,
            ITaskRunnerRootFactory taskRunnerRootFactory,
            IRetryPolicyFactory retryPolicyFactory)
        {
            _taskRunnerFactory = taskRunnerFactory ?? throw new ArgumentNullException(nameof(taskRunnerFactory));
            _taskRunnerRootFactory = taskRunnerRootFactory ?? throw new ArgumentNullException(nameof(taskRunnerRootFactory));
            _retryPolicyFactory = retryPolicyFactory ?? throw new ArgumentNullException(nameof(retryPolicyFactory));
        }

        /// <summary>
        /// Factory method that hides the concrete implementation and enforces injection
        /// of all required dependencies.
        /// </summary>
        public static IPayloadRunnerFactory Instance(
            ITaskRunnerFactory taskRunnerFactory,
            ITaskRunnerRootFactory taskRunnerRootFactory,
            IRetryPolicyFactory retryPolicyFactory)
        {
            return new PayloadRunnerFactory(taskRunnerFactory, taskRunnerRootFactory, retryPolicyFactory);
        }

        /// <inheritdoc />
        public IPayloadTaskRunner<T> Create<T>(
            T payload,
            IUniversalPayloadSerializer serializer,
            string name = "")
            where T : IPayloadCommand
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));

            return PayloadTaskRunner<T>.Create(
                payload,
                serializer,
                _taskRunnerFactory,
                name);
        }

        /// <inheritdoc />
        public IPayloadTaskRunner<T> Create<T>(
            Guid taskRunnerId,
            T payload,
            IUniversalPayloadSerializer serializer,
            string name = "")
            where T : IPayloadCommand
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));

            return PayloadTaskRunner<T>.Create(
                taskRunnerId,
                payload,
                serializer,
                _taskRunnerFactory,
                name);
        }

        /// <inheritdoc />
        public IPayloadTaskRunnerRoot<T> CreateRoot<T>(
            Guid taskRunnerId,
            T payload,
            IUniversalPayloadSerializer serializer,
            Func<IRetryPolicy>? retryPolicyFactory = null,
            string name = "")
            where T : IPayloadCommand
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));

            return PayloadTaskRunnerRoot<T>.Create(
                taskRunnerId,
                payload,
                serializer,
                _taskRunnerRootFactory,
                retryPolicyFactory,
                name);
        }

        /// <inheritdoc />
        public IPayloadTaskRunnerRoot<T> CreateRoot<T>(
            T payload,
            IUniversalPayloadSerializer serializer,
            Func<IRetryPolicy>? retryPolicyFactory = null,
            string name = "")
            where T : IPayloadCommand
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));

            return PayloadTaskRunnerRoot<T>.Create(
                payload,
                serializer,
                _taskRunnerRootFactory,
                retryPolicyFactory,
                name);
        }

        /// <inheritdoc />
        public IPayloadCarrier Load(
            ICacheLeaseEntry lease,
            IUniversalPayloadSerializer serializer)
        {
            if (lease == null) throw new ArgumentNullException(nameof(lease));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));

            var nodeDto = lease.TaskRunnerNodeDto;
            if (string.IsNullOrWhiteSpace(nodeDto.PayloadType))
                throw new InvalidOperationException(
                    "PayloadType must be provided in TaskRunnerNodeDto to rehydrate a payload runner.");

            var payloadType = Type.GetType(nodeDto.PayloadType!, throwOnError: true)!;
            var runnerType = typeof(PayloadTaskRunner<>).MakeGenericType(payloadType);

            var loadMethod = runnerType.GetMethod(
                name: "Load",
                bindingAttr: BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[]
                {
                    typeof(ICacheLeaseEntry),
                    typeof(IUniversalPayloadSerializer),
                    typeof(ITaskRunnerFactory)
                },
                modifiers: null);

            if (loadMethod is null)
            {
                throw new InvalidOperationException(
                    "Cannot locate PayloadTaskRunner<T>.Load(ICacheLeaseEntry, IUniversalPayloadSerializer, ITaskRunnerFactory).");
            }

            return (IPayloadCarrier)loadMethod.Invoke(
                obj: null,
                parameters: new object[]
                {
                    lease,
                    serializer,
                    _taskRunnerFactory
                })!;
        }

        /// <inheritdoc />
        public IPayloadCarrierRoot LoadRoot(
            ICacheLeaseEntry lease,
            IUniversalPayloadSerializer serializer)
        {
            if (lease == null) throw new ArgumentNullException(nameof(lease));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));

            var nodeDto = lease.TaskRunnerNodeDto;
            if (string.IsNullOrWhiteSpace(nodeDto.PayloadType))
                throw new InvalidOperationException(
                    "PayloadType must be provided in TaskRunnerNodeDto to rehydrate a root payload runner.");

            var payloadType = Type.GetType(nodeDto.PayloadType!, throwOnError: true)!;
            var runnerType = typeof(PayloadTaskRunnerRoot<>).MakeGenericType(payloadType);

            var loadMethod = runnerType.GetMethod(
                name: "Load",
                bindingAttr: BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[]
                {
                    typeof(ICacheLeaseEntry),
                    typeof(IUniversalPayloadSerializer),
                    typeof(ITaskRunnerRootFactory),
                    typeof(IRetryPolicyFactory)
                },
                modifiers: null);

            if (loadMethod is null)
            {
                throw new InvalidOperationException(
                    "Cannot locate PayloadTaskRunnerRoot<T>.Load(ICacheLeaseEntry, IUniversalPayloadSerializer, ITaskRunnerRootFactory, IRetryPolicyFactory).");
            }

            return (IPayloadCarrierRoot)loadMethod.Invoke(
                obj: null,
                parameters: new object[]
                {
                    lease,
                    serializer,
                    _taskRunnerRootFactory,
                    _retryPolicyFactory
                })!;
        }
    }
}
