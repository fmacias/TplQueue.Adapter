using Fmacias.TplQueue.Contracts;
using System;

namespace Fmacias.TplQueue.Cache.Abstract
{
    /// <summary>
    /// Concrete DTO for a payload task runner node.
    /// Enforces non-null <see cref="PayloadType"/> and <see cref="PayloadJson"/>.
    /// </summary>
    internal sealed class JobNodeDto : IJobNodeDto
    {
        /// <inheritdoc/>
        public Guid JobId { get; private set; }
        public Guid ParentJobId { get; private set; }

        /// <inheritdoc/>
        public string? Name { get; private set; }

        /// <inheritdoc/>
        public string PayloadType { get; private set; }

        /// <inheritdoc/>
        public string PayloadJson { get; private set; }

        public DateTime NodeCreationUtc { get; private set; }
        public bool IsRoot { get; private set; }

        public IRetryPolicyDescriptor RetryDescriptor { get; }

        public bool IsFifo { get; private set; }

        private JobNodeDto(Guid jobId, Guid parentJobId, string payloadType,
            string payloadJson, bool isRoot, bool isFifo,
            IRetryPolicyDescriptor retryPolicyDescriptor, string? name)
        {
            if (jobId == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(jobId));
            if (string.IsNullOrWhiteSpace(payloadType)) throw new ArgumentNullException(nameof(payloadType));
            if (string.IsNullOrEmpty(payloadJson)) throw new ArgumentNullException(nameof(payloadJson));

            JobId = jobId;
            ParentJobId = parentJobId;
            Name = name;
            PayloadType = payloadType;
            PayloadJson = payloadJson;
            NodeCreationUtc = DateTime.UtcNow;
            IsRoot = isRoot;
            IsFifo = isFifo;
            RetryDescriptor = retryPolicyDescriptor;
        }

        /// <summary>
        /// Factory enforcing required payload fields.
        /// </summary>
        public static JobNodeDto Create(Guid jobId, Guid parentJobId, string payloadType, string payloadJson, bool isRoot, bool isFifo, IRetryPolicyDescriptor retryPolicyDescriptor, string? name)
            => new JobNodeDto(jobId, parentJobId, payloadType, payloadJson, isRoot, isFifo, retryPolicyDescriptor, name);

        public void UpdatePayloadJson(string payloadJson)
        {
            PayloadJson = payloadJson;
        }
    }
}
