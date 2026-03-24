using Fmacias.TplQueue.Cache.Abstract.Models;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Cache.Abstract.Factories
{
    public class RuntimeNodeTypeResolverFactory : IRuntimeNodeTypeResolverFactory
    {
        private RuntimeNodeTypeResolverFactory() { }
        public static INodeTypeResolverFactory<ITypeResolver> Create()
        {
            return new RuntimeNodeTypeResolverFactory();
        }

        public ITypeResolver Resolver()
        {
            return RuntimeNodeTypeResolver.Create();
        }
    }
}
