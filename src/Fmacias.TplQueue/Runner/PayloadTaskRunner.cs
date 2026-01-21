using Fmacias.TplQueue.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fmacias.TplQueue.Runner
{
    internal class PayloadTaskRunner<TPayload> :
        ITaskRunnerAdapter,
        IPayloadTaskRunner<TPayload>
        where TPayload : IPayloadCommand
    {
        private readonly ITaskRunner _inner;
        private readonly TPayload _payload;
        private readonly string _jsonSerializedInput;
        private readonly IUniversalPayloadSerializer _serializer;
        protected PayloadTaskRunner(ITaskRunner inner, TPayload payload, string jsonSerialized,
            IUniversalPayloadSerializer serializer)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _payload = payload ?? throw new ArgumentNullException(nameof(payload));
            _jsonSerializedInput = jsonSerialized ?? throw new ArgumentNullException(nameof(jsonSerialized));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            InitializePayloadData();
        }

        private ISerializedPayload InitializePayloadData()
        {
            var payloadType = typeof(TPayload).AssemblyQualifiedName
                ?? typeof(TPayload).FullName
                ?? typeof(TPayload).Name;
            var payloadData = _inner.PayloadSerializedData;

            return payloadData.SetInitialData(
                _payload.HandlerId, _jsonSerializedInput, payloadType);
        }

        public static PayloadTaskRunner<TPayload> Create(
            TPayload payload,
            IUniversalPayloadSerializer serializer,
            ITaskRunnerFactory taskRunnerFactory,
            string name = "")
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            if (taskRunnerFactory is null) throw new ArgumentNullException(nameof(taskRunnerFactory));

            var inner = taskRunnerFactory.Create(
                func: async (ct, pl) => await pl.ExecuteAsync(ct).ConfigureAwait(false),
                arg: payload,
                name: name);
            var json = serializer.Serialize(payload);
            return new PayloadTaskRunner<TPayload>(inner, payload, json, serializer);
        }

        public static PayloadTaskRunner<TPayload> Create(
            Guid taskRunnerId,
            TPayload payload,
            IUniversalPayloadSerializer serializer,
            ITaskRunnerFactory taskRunnerFactory,
            string name = "")
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            if (taskRunnerFactory is null) throw new ArgumentNullException(nameof(taskRunnerFactory));

            var innerTaskRunner = taskRunnerFactory.Create(
                id: taskRunnerId,
                body: async (ct, pl) => await pl.ExecuteAsync(ct).ConfigureAwait(false),
                arg: payload,
                name: name);
            var json = serializer.Serialize(payload);
            return new PayloadTaskRunner<TPayload>(innerTaskRunner, payload, json, serializer);
        }

        public static PayloadTaskRunner<TPayload> Load(ICacheLeaseEntry cacheLeaseEntry,
            IUniversalPayloadSerializer serializer, ITaskRunnerFactory taskRunnerFactory)
        {
            cacheLeaseEntry = cacheLeaseEntry ?? throw new ArgumentNullException(nameof(cacheLeaseEntry));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            if (taskRunnerFactory is null) throw new ArgumentNullException(nameof(taskRunnerFactory));

            var json = cacheLeaseEntry.TaskRunnerNodeDto.PayloadJson;
            var payload = serializer.Deserialize<TPayload>(json);
            var inner = taskRunnerFactory.Create(
                id: cacheLeaseEntry.TaskRunnerId,
                body: async (ct, pl) =>
                {
                    await pl.ExecuteAsync(ct).ConfigureAwait(false);
                },
                arg: payload,
                name: cacheLeaseEntry.TaskRunnerNodeDto.Name ?? string.Empty);

            return new PayloadTaskRunner<TPayload>(inner, payload, json,
                serializer);
        }

        public object GetPayload() => _payload;
        public Type PayloadType => typeof(TPayload);
        public TPayload Payload => _payload;
        public IReadOnlyList<IPayloadCarrier> GetPayloadDependencies()
            => [.. _inner.Dependencies.OfType<IPayloadCarrier>()];
        public Guid Id => _inner.Id;
        public string Name => _inner.Name;
        public bool IsCompleted => _inner.IsCompleted;
        public DateTime ExecutionStart => _inner.ExecutionStart;
        public TimeSpan ExecutionTime => _inner.ExecutionTime;
        public DateTime ExecutionEnd => _inner.ExecutionEnd;
        public TaskStatus Status => _inner.Status;
        public IReadOnlyCollection<ITaskRunnerInfo> Dependencies => _inner.Dependencies;
        public ISerializedPayload PayloadSerializedData => _inner.PayloadSerializedData;
        public ITaskRunner After(params ITaskRunner[] previousTasks) => _inner.After(previousTasks);
        public ITaskRunnerInfo[] GetInfoDependencies() => _inner.GetInfoDependencies();
        public void SetRoot(ITaskRunnerRoot taskRunnerRoot)
        {
            if (taskRunnerRoot == null) throw new ArgumentNullException(nameof(taskRunnerRoot));
            _inner.SetRoot(taskRunnerRoot);
        }
        public ITaskRunner GetInnerRunner() => _inner;
        public ITaskRunnerInfo CopyInfo()
        {
            PayloadSerializedData
                .SetOutput(_serializer.Serialize(_payload));
            return _inner.CopyInfo();
        }
        public Func<IRetryPolicy> GetRetryPolicyFactory()
        {
            return _inner.GetRetryPolicyFactory();
        }

        public ITaskRunner[] GetBatch()
        {
            return _inner.GetBatch();
        }

        public async Task WaitUntilFinishedAsync()
        {
            await _inner.WaitUntilFinishedAsync().ConfigureAwait(false);
        }
    }
}



