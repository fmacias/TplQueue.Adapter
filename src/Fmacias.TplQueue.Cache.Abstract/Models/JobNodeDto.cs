using Fmacias.TplQueue.Contracts;
using System;

namespace Fmacias.TplQueue.Cache.Abstract.Models
{
    /// <summary>
    /// Concrete DTO(Data Transfer Object) for a payload job(<see cref="IDataJob{IPayload}"/>) node.
    /// Enforces non-null <see cref="PayloadTypeName"/> and serialized payload content.
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
        /// <summary>
        /// Serialized payload content. The property name is retained for compatibility and is not limited to JSON.
        /// </summary>
        public string PayloadJson { get; private set; }
        public string PayloadHandlerKey { get; }

        private JobNodeDto(
            Guid jobId,
            Guid parentJobId,
            Type payloadType,
            string payloadJson,
            bool isRoot,
            bool isFifo,
            IRetryPolicyOptions retryPolicyDescriptor,
            string payloadHandlerKey,
            string name = "")
        {
            if (jobId == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(jobId));
            if (payloadType == null) throw new ArgumentNullException(nameof(payloadType));
            if (string.IsNullOrEmpty(payloadJson)) throw new ArgumentNullException(nameof(payloadJson));
            if (string.IsNullOrWhiteSpace(payloadHandlerKey))
                throw new ArgumentException("Payload handler key cannot be null or empty.", nameof(payloadHandlerKey));

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
            RetryPolicyOptions = retryPolicyDescriptor ?? throw new ArgumentNullException(nameof(retryPolicyDescriptor));
            PayloadHandlerKey = payloadHandlerKey;
        }

        public static JobNodeDto Create(
            IUniversalDataSerializer serializer,
            IDataJobNode dataJob,
            bool isFifo,
            IDataJobNode? parentJob)
        {
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));
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
                payloadJson: SerializePayload(dataJob, serializer),
                payloadType: payload.GetType(),
                isRoot: dataJob is IDataJobRoot,
                isFifo: isFifo,
                retryPolicyDescriptor: retryDescriptor,
                payloadHandlerKey: dataJob.PayloadHandlerKey,
                name: dataJob.Name);
        }

        public void UpdatePayloadJson(string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
                throw new ArgumentException("Serialized payload content cannot be null or whitespace.", nameof(payloadJson));

            PayloadJson = payloadJson;
        }

        private static string SerializePayload(IDataJobNode dataJob, IUniversalDataSerializer serializer)
        {
            if (dataJob is null) throw new ArgumentNullException(nameof(dataJob));
            if (serializer is null) throw new ArgumentNullException(nameof(serializer));

            var payload = dataJob.GetPayload();

            if (payload is null)
                throw new InvalidOperationException($"Payload cannot be null for job '{dataJob.Id}'.");

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
