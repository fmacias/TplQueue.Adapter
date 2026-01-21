using Fmacias.TplQueue.Contracts;
using System;

namespace Fmacias.TplQueue.Observers
{
    /// <summary>
    /// Testing and Console specific implementation of <see cref="IObserverDispatcher"/>
    /// </summary>
    internal sealed class DirectObserverDispatcher : IObserverDispatcher
    {
        private DirectObserverDispatcher() { }
        public static DirectObserverDispatcher Create()
        {
            return new DirectObserverDispatcher();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        public void Invoke(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            action();
        }
    }
}
