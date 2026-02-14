using Fmacias.TplQueue.Contracts;
using System;

namespace Fmacias.TplQueue.Cache.DomainModels
{
    /// <summary>
    /// Concrete DTO(Data Transfer Object) for a payload job(<see cref="IPayloadJob{IPayload}"/>) node.
    /// Enforces non-null <see cref="PayloadTypeName"/> and <see cref="PayloadJson"/>.
    /// </summary>
    internal sealed class JobNodeDto : IJobNodeDto, IJobNodeRecord
    {
        /// <inheritdoc/>
        public Guid JobId { get; private set; }
        public Guid ParentJobId { get; private set; }
        /// <inheritdoc/>
        public string Name { get; private set; }
        public DateTime NodeCreationUtc { get; private set; }
        public bool IsRoot { get; private set; }

        public IRetryPolicyDescriptor RetryDescriptor { get; private set; }

        public bool IsFifo { get; private set; }

        public string PayloadTypeName { get; private set; }

        public Type PayloadType { get; private set; }

        public string PayloadJson { get; private set; }

        private JobNodeDto(Guid jobId,
            Guid parentJobId, Type payloadType, string payloadJson,
            bool isRoot, bool isFifo, IRetryPolicyDescriptor retryPolicyDescriptor,
            string name = "")
        {
            if (jobId == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(jobId));
            if (payloadType == null) throw new ArgumentNullException(nameof(payloadType));
            if (string.IsNullOrEmpty(payloadJson)) throw new ArgumentNullException(nameof(payloadJson));

            JobId = jobId;
            ParentJobId = parentJobId;
            Name = name;
            PayloadType = payloadType;
            PayloadTypeName = payloadType.AssemblyQualifiedName
                           ?? payloadType.FullName
                           ?? payloadType.Name;
            PayloadJson = payloadJson;
            NodeCreationUtc = DateTime.UtcNow;
            IsRoot = isRoot;
            IsFifo = isFifo;
            RetryDescriptor = retryPolicyDescriptor;
        }

        public static JobNodeDto Create(IUniversalPayloadSerializer jsonSerializer, 
            IPayloadCarrierJob payloadJob, bool isFifo, IPayloadCarrierJob? parentJob) 
        {
            if (jsonSerializer is null) throw new ArgumentNullException(nameof(jsonSerializer));
            if (payloadJob is null) throw new ArgumentNullException(nameof(payloadJob));

            var parentId = parentJob?.Id ?? Guid.Empty;
            var isRoot = payloadJob is IPayloadJobRoot;
            var payload = payloadJob.GetPayload()
                          ?? throw new InvalidOperationException($"Payload cannot be null for job '{payloadJob.Id}'.");
            var runtimeType = payload.GetType();
            var json = SerializePayload(payloadJob, jsonSerializer);
            var policyFactory = payloadJob.GetRetryPolicyFactory();
            if (policyFactory is null)
                throw new InvalidOperationException($"Retry policy factory cannot be null for job '{payloadJob.Id}'.");

            var policy = policyFactory();
            if (policy is null)
                throw new InvalidOperationException($"Retry policy cannot be null for job '{payloadJob.Id}'.");

            var retryDescriptor = policy.ToDescriptor();
            return new JobNodeDto(
                    jobId: payloadJob.Id,
                    parentJobId: parentId,
                    payloadJson: json,
                    payloadType: runtimeType,
                    isRoot: isRoot,
                    isFifo: isFifo,
                    retryPolicyDescriptor: retryDescriptor,
                    name: payloadJob.Name);
        }

        public void UpdatePayloadJson(string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
                throw new ArgumentException("Payload json cannot be null or whitespace.", nameof(payloadJson));

            PayloadJson = payloadJson;
        }

        private static string SerializePayload(IPayloadCarrierJob payloadJob, IUniversalPayloadSerializer serializer)
        {
            if (payloadJob is null) throw new ArgumentNullException(nameof(payloadJob));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            
            var payload = payloadJob.GetPayload();
            if (payload is null)
                throw new InvalidOperationException($"Payload cannot be null for job '{payloadJob.Id}'.");
            var type = payload.GetType();

            return payloadJob.Serialize(serializer);
        }

        public object Deserialize(IUniversalPayloadSerializer serializer)
        {
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            return serializer.Deserialize(PayloadJson, PayloadType);
        }

        public T Deserialize<T>(IUniversalPayloadSerializer serializer)
        {
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            return serializer.Deserialize<T>(PayloadJson);
        }
    }
}
