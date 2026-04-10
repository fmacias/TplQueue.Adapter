using Fmacias.TplQueue.Cache.Abstract.Models;
using Fmacias.TplQueue.Contracts;
using System;

namespace Fmacias.TplQueue.Cache.Abstract.Factories
{
    public class RuntimeNodeTypeResolverFactory : IRuntimeNodeTypeResolverFactory
    {
        private RuntimeNodeTypeResolverFactory() { }

        public static RuntimeNodeTypeResolverFactory Create()
        {
            return new RuntimeNodeTypeResolverFactory();
        }

        public ITypeResolver Resolver()
        {
            return RuntimeNodeTypeResolver.Create();
        }

        public IRuntimeNodeTypeResolver Resolver(AppDomain appDomain)
        {
            if (appDomain == null) throw new ArgumentNullException(nameof(appDomain));

            return RuntimeNodeTypeResolver.Create(appDomain);
        }
    }
}
