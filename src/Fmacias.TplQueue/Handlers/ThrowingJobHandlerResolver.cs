using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Handlers
{
    internal sealed class ThrowingJobHandlerResolver : IPayloadHandlerResolver
    {
        public IUniversaPayloadHandler Resolve(Guid handlerId)
        {
            throw new KeyNotFoundException($"No handler was registered for HandlerId '{handlerId}'.");
        }
    }
}
