using Fmaciasruano.TplQueue.Abstractions.Contracts;
using System;

namespace Fmaciasruano.TplQueue.Cache.Abstract
{
    /// <summary>
    /// Concrete DTO for a payload task runner node.
    /// Enforces non-null <see cref="PayloadType"/> and <see cref="PayloadJson"/>.
    /// </summary>
    internal sealed class TaskRunnerNodeDto : ITaskRunnerNodeDto
    {
        /// <inheritdoc/>
        public Guid TaskRunnerId { get; private set; }
        public Guid ParentTaskRunnerId { get; private set; }

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

        private TaskRunnerNodeDto(Guid id, Guid parentId, string payloadType,
            string payloadJson, bool isRoot, bool isFifo,
            IRetryPolicyDescriptor retryPolicyDescriptor, string? name)
        {
            if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(payloadType)) throw new ArgumentNullException(nameof(payloadType));
            if (string.IsNullOrEmpty(payloadJson)) throw new ArgumentNullException(nameof(payloadJson));

            TaskRunnerId = id;
            ParentTaskRunnerId = parentId;
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
        public static TaskRunnerNodeDto Create(Guid id, Guid parentId, string payloadType, string payloadJson, bool isRoot, bool isFifo, IRetryPolicyDescriptor retryPolicyDescriptor, string? name)
            => new TaskRunnerNodeDto(id, parentId, payloadType, payloadJson, isRoot, isFifo, retryPolicyDescriptor, name);

        public void UpdatePayloadJson(string payloadJson)
        {
            PayloadJson = payloadJson;
        }
    }
}
