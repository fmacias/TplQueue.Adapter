using System;
using System.Text.Json;
using Fmaciasruano.TplQueue.Abstractions.Contracts;

namespace Fmaciasruano.TplQueue.Serialization.SystemTextJson
{
    /// <summary>
    /// Universal payload serializer backed by System.Text.Json.
    /// Provides both Type-based and generic helpers for IPayloadCommand payloads.
    /// </summary>
    public sealed class SystemTextJsonUniversalSerializer : ISystemTextJsonUniversalSerializer
    {
        private readonly JsonSerializerOptions _options;

        private SystemTextJsonUniversalSerializer(JsonSerializerOptions? options = null)
        {
            _options = options ?? new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                TypeInfoResolver = null,
                IncludeFields = false
            };
        }
        public static SystemTextJsonUniversalSerializer Create(JsonSerializerOptions? options = null)
        {
            return new SystemTextJsonUniversalSerializer(options);
        }

        public string Serialize(object value, Type type)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (type is null) throw new ArgumentNullException(nameof(type));

            if (!type.IsInstanceOfType(value))
            {
                throw new ArgumentException(
                    $"The provided value of type '{value.GetType().FullName}' is not an instance of '{type.FullName}'.",
                    nameof(value));
            }

            return JsonSerializer.Serialize(value, type, _options);
        }

        public object Deserialize(string json, Type type)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentNullException(nameof(json));
            if (type is null) throw new ArgumentNullException(nameof(type));

            var result = JsonSerializer.Deserialize(json, type, _options);

            if (result is null)
            {
                throw new InvalidOperationException(
                    $"Deserialization produced null for target type '{type.FullName}'. " +
                    "Check the JSON and the target type.");
            }
            return result;
        }

        public string Serialize<T>(T value) where T : IPayloadCommand
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            return JsonSerializer.Serialize(value, _options);
        }

        public T Deserialize<T>(string json) where T : IPayloadCommand
        {
            if (json is null) throw new ArgumentNullException(nameof(json));
            var result = JsonSerializer.Deserialize<T>(json, _options);
            if (result is null)
            {
                throw new InvalidOperationException(
                    $"Deserialization produced null for target type '{typeof(T).FullName}'.");
            }
            return result;
        }

        public string Serialize(IPayloadCarrier carrier)
        {
            if (carrier is null) throw new ArgumentNullException(nameof(carrier));
            var payload = carrier.GetPayload() ?? throw new InvalidOperationException("Carrier payload is null.");
            var type = carrier.PayloadType ?? payload.GetType();
            return Serialize(payload, type);
        }
    }
}
