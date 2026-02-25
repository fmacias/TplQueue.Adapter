using Fmacias.TplQueue.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fmacias.TplQueue.Jobs
{
    internal class UniversalPayloadHandler : IUniversaPayloadHandler
    {
        public Func<object, CancellationToken, Task> ResolveAction { get; }

        private UniversalPayloadHandler(Func<object, CancellationToken, Task> resolveAction)
        {
            ResolveAction = resolveAction;
        } 
        public static IUniversaPayloadHandler Create(Func<object, CancellationToken, Task> resolveAction)
        {
            return new UniversalPayloadHandler(resolveAction);
        }
    }
}
