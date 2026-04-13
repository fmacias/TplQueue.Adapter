using Fmacias.TplQueue.Contracts;
using System;

namespace Fmacias.TplQueue.Observers
{
    internal sealed class DirectObserverDispatcher : IObserverDispatcher
    {
        private DirectObserverDispatcher() { }

        public static DirectObserverDispatcher Create()
        {
            return new DirectObserverDispatcher();
        }

        public void Invoke(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            action();
        }
    }
}
