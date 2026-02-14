using Fmacias.TplQueue.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fmacias.TplQueue.Jobs
{
    internal class UniversalDtoHandler : IUniversaDtoHandler2
    {
        public Func<object, CancellationToken, Task> ResolveAction { get; }

        private UniversalDtoHandler(Func<object, CancellationToken, Task> resolveAction)
        {
            ResolveAction = resolveAction;
        } 
        public static IUniversaDtoHandler2 Create(Func<object, CancellationToken, Task> resolveAction)
        {
            return new UniversalDtoHandler(resolveAction);
        }
    }
}
