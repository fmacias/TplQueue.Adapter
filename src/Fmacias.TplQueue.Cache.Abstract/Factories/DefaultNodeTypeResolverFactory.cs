using Fmacias.TplQueue.Cache.DomainModels;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Cache.Factories
{
    /// <summary>
    /// Creates the default implementation of <see cref="INodeTypeResolver"/>.
    /// </summary>
    public sealed class DefaultNodeTypeResolverFactory : INodeTypeResolverFactory
    {
        private DefaultNodeTypeResolverFactory() { }

        /// <summary>
        /// Creates a new factory instance.
        /// </summary>
        public static DefaultNodeTypeResolverFactory Create() 
        {
            return new DefaultNodeTypeResolverFactory();
        }

        /// <inheritdoc />
        public INodeTypeResolver CreateResolver()
        {
            return DefaultNodeTypeResolver.Create();
        }
    }
}
