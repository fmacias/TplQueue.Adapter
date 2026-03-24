using Fmacias.TplQueue.Contracts;
using System;

namespace Fmacias.TplQueue.Cache.Abstract.Models
{
    /// <summary>
    /// Concrete DTO(Data Transfer Object) for a payload job(<see cref="IDataJob{IPayload}"/>) node.
    /// Enforces non-null <see cref="PayloadTypeName"/> and <see cref="PayloadJson"/>.
    /// </summary>
    internal sealed class JobNodeDto : IJobNodeDto
    {
        /// <inheritdoc/>
        public Guid JobId { get; }
        public Guid ParentJobId { get; }
        /// <inheritdoc/>
        public string Name { get; }
        public DateTime NodeCreationUtc { get; }
        public bool IsRoot { get; }

        public IRetryPolicyOptions RetryPolicyOptions { get; }

        public bool IsFifo { get; }

        public string PayloadTypeName { get; }

        public Type PayloadType { get; }

        public string PayloadJson { get; private set; }

        public Guid PayloadHandlerId { get; }

        private JobNodeDto(Guid jobId,
            Guid parentJobId, Type payloadType, string payloadJson,
            bool isRoot, bool isFifo, IRetryPolicyOptions retryPolicyDescriptor,
            Guid payloadHandlerId, string name = "")
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
            RetryPolicyOptions = retryPolicyDescriptor;
            PayloadHandlerId = payloadHandlerId;
        }

        public static JobNodeDto Create(IUniversalDataSerializer jsonSerializer, 
            IDataJob dataJob, bool isFifo, IDataJob? parentJob) 
        {
            if (jsonSerializer is null) throw new ArgumentNullException(nameof(jsonSerializer));
            if (dataJob is null) throw new ArgumentNullException(nameof(dataJob));
            
            var payload = dataJob.GetPayload()
                          ?? throw new InvalidOperationException($"Payload cannot be null for job '{dataJob.Id}'.");

            var policyFactory = dataJob.GetRetryPolicyFactory();
            
            if (policyFactory is null)
                throw new InvalidOperationException($"Retry policy factory cannot be null for job '{dataJob.Id}'.");

            var policy = policyFactory();
            
            if (policy is null)
                throw new InvalidOperationException($"Retry policy cannot be null for job '{dataJob.Id}'.");

            var retryDescriptor = policy.ToDescriptor();
            return new JobNodeDto(
                    jobId: dataJob.Id,
                    parentJobId: parentJob?.Id ?? Guid.Empty,
                    payloadJson: SerializePayload(dataJob, jsonSerializer),
                    payloadType: payload.GetType(),
                    isRoot: dataJob is IDataJobRoot,
                    isFifo: isFifo,
                    retryPolicyDescriptor: retryDescriptor,
                    payloadHandlerId: dataJob.PayloadHandlerId, 
                    name: dataJob.Name);
        }

        public void UpdatePayloadJson(string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
                throw new ArgumentException("Payload json cannot be null or whitespace.", nameof(payloadJson));

            PayloadJson = payloadJson;
        }

        private static string SerializePayload(IDataJob dataJob, IUniversalDataSerializer serializer)
        {
            if (dataJob is null) throw new ArgumentNullException(nameof(dataJob));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            
            var payload = dataJob.GetPayload();
            
            if (payload is null)
                throw new InvalidOperationException($"Payload cannot be null for job '{dataJob.Id}'.");
            
            var type = payload.GetType();

            return dataJob.Serialize(serializer);
        }

        public object Deserialize(IUniversalDataSerializer serializer)
        {
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            return serializer.Deserialize(PayloadJson, PayloadType);
        }

        public T Deserialize<T>(IUniversalDataSerializer serializer)
        {
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
            return serializer.Deserialize<T>(PayloadJson);
        }
    }
}
