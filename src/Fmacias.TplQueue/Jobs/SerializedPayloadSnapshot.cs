using Fmacias.TplQueue.Contracts;
using System;

namespace Fmacias.TplQueue.Jobs
{
    /// <summary>
    /// Serializable DTO for payloads used by payload-based jobs.
    /// Keeps handler identifier, JSON and the payload type name.
    /// </summary>
    internal class SerializedPayloadSnapshot : IPayloadSerializable
    {
        /// <inheritdoc />
        public string HandlerId { get; }

        /// <inheritdoc />
        public string PayloadType { get; }

        /// <inheritdoc />
        public string PayloadJson { get; }

        public string PayloadJsonOutput { get; private set; }

        private SerializedPayloadSnapshot(string handlerId, string payloadType, 
            string payloadJsonInput)
        {
            HandlerId = handlerId;
            PayloadType = payloadType;
            PayloadJson = payloadJsonInput;
            PayloadJsonOutput = "{}";
        }

        /// <summary>
        /// Creates a new payload with initial input data.
        /// Null values are normalized to empty strings to simplify serialization.
        /// </summary>
        public static IPayloadSerializable Create(IPayload payload, IJsonUniversalPayloadSerializer serializer)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));
            return new SerializedPayloadSnapshot(payload.HandlerId,
                GetTypeName(payload),
                serializer.Serialize(payload, payload.GetType()));
        }
        private static string GetTypeName(IPayload payload)
        {
            var type = payload.GetType();
            return type.AssemblyQualifiedName
               ?? type.FullName
               ?? type.Name;
        }
    }
}
