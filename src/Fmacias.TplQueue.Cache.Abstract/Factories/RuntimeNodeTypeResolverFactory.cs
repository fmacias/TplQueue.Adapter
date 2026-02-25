using Fmacias.TplQueue.Cache.Contracts;
using Fmacias.TplQueue.Cache.DomainModels;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Cache.Abstract.Factories
{
    public class RuntimeNodeTypeResolverFactory : IRuntimeNodeTypeResolverFactory
    {
        private RuntimeNodeTypeResolverFactory() { }
        public static INodeTypeResolverFactory<INodeTypeResolver> Create()
        {
            return new RuntimeNodeTypeResolverFactory();
        }

        public INodeTypeResolver CreateResolver()
        {
            return RuntimeNodeTypeResolver.Create();
        }
    }
}
