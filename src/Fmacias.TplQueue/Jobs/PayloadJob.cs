using Fmacias.TplQueue.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fmacias.TplQueue.Jobs
{
    internal class PayloadJob<TPayload> :
        IJobAdapter,
        IPayloadJob<TPayload>
        where TPayload : IPayloadCommand
    {
        private readonly IJob _inner;
        private readonly TPayload _payload;
        private readonly string _jsonSerializedInput;
        private readonly IJsonUniversalPayloadSerializer _serializer;
        protected PayloadJob(IJob inner, TPayload payload, string jsonSerialized,
            IJsonUniversalPayloadSerializer serializer)
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

        public static PayloadJob<TPayload> Create(
            TPayload payload,
            IJsonUniversalPayloadSerializer serializer,
            IJobFactory jobFactory,
            string name = "")
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            if (jobFactory is null) throw new ArgumentNullException(nameof(jobFactory));

            var inner = jobFactory.CreateJob(
                func: async (ct, pl) => await pl.ExecuteAsync(ct).ConfigureAwait(false),
                arg: payload,
                name: name);
            var json = serializer.Serialize(payload);
            return new PayloadJob<TPayload>(inner, payload, json, serializer);
        }

        public static PayloadJob<TPayload> Create(
            Guid jobId,
            TPayload payload,
            IJsonUniversalPayloadSerializer serializer,
            IJobFactory jobFactory,
            string name = "")
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            if (jobFactory is null) throw new ArgumentNullException(nameof(jobFactory));

            var innerJob = jobFactory.CreateJob(
                id: jobId,
                body: async (ct, pl) => await pl.ExecuteAsync(ct).ConfigureAwait(false),
                arg: payload,
                name: name);
            var json = serializer.Serialize(payload);
            return new PayloadJob<TPayload>(innerJob, payload, json, serializer);
        }

        public static PayloadJob<TPayload> Load(ICacheLeaseEntry cacheLeaseEntry,
            IJsonUniversalPayloadSerializer serializer, IJobFactory jobFactory)
        {
            cacheLeaseEntry = cacheLeaseEntry ?? throw new ArgumentNullException(nameof(cacheLeaseEntry));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            if (jobFactory is null) throw new ArgumentNullException(nameof(jobFactory));

            var json = cacheLeaseEntry.JobNodeDto.PayloadJson;
            var payload = serializer.Deserialize<TPayload>(json);
            var inner = jobFactory.CreateJob(
                id: cacheLeaseEntry.JobId,
                body: async (ct, pl) =>
                {
                    await pl.ExecuteAsync(ct).ConfigureAwait(false);
                },
                arg: payload,
                name: cacheLeaseEntry.JobNodeDto.Name ?? string.Empty);

            return new PayloadJob<TPayload>(inner, payload, json,
                serializer);
        }

        public object GetPayload() => _payload;
        public Type PayloadType => typeof(TPayload);
        public TPayload Payload => _payload;
        public IReadOnlyList<IPayloadCarrierJob> GetPayloadDependencies()
            => [.. _inner.Dependencies.OfType<IPayloadCarrierJob>()];
        public Guid Id => _inner.Id;
        public string Name => _inner.Name;
        public bool IsCompleted => _inner.IsCompleted;
        public DateTime ExecutionStart => _inner.ExecutionStart;
        public TimeSpan ExecutionTime => _inner.ExecutionTime;
        public DateTime ExecutionEnd => _inner.ExecutionEnd;
        public TaskStatus Status => _inner.Status;
        public IReadOnlyCollection<IJobInfo> Dependencies => _inner.Dependencies;
        public ISerializedPayload PayloadSerializedData => _inner.PayloadSerializedData;
        public IJob After(params IJob[] previousTasks) => _inner.After(previousTasks);
        public IJobInfo[] GetJobInfoDependencies() => _inner.GetJobInfoDependencies();
        public void SetRoot(IJobRoot jobRoot)
        {
            if (jobRoot == null) throw new ArgumentNullException(nameof(jobRoot));
            _inner.SetRoot(jobRoot);
        }
        public IJob GetInnerJob() => _inner;
        public IJobInfo CopyInfo()
        {
            PayloadSerializedData
                .SetOutput(_serializer.Serialize(_payload));
            return _inner.CopyInfo();
        }
        public Func<IRetryPolicy> GetRetryPolicyFactory()
        {
            return _inner.GetRetryPolicyFactory();
        }

        public IJob[] GetJobsBatch()
        {
            return _inner.GetJobsBatch();
        }

        public async Task WaitUntilFinishedAsync()
        {
            await _inner.WaitUntilFinishedAsync().ConfigureAwait(false);
        }
    }
}



