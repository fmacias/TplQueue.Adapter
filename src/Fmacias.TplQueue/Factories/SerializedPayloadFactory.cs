using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Jobs;

namespace Fmacias.TplQueue.Factories
{
    internal class SerializedPayloadFactory : ISerializedPayloadFactory
    {
        public ISerializedPayload Create(IPayload payload, IJsonUniversalPayloadSerializer serializer)
        {
            return SerializedPayloadSnapshot.Create(payload, serializer);
        }
    }
}
