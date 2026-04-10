using Fmacias.TplQueue.Cache.Abstract.Factories;
using Fmacias.TplQueue.Contracts;
using System;
using NUnit.Framework;

namespace Fmacias.TplQueue.Cache.Abstract.Test.Factories
{
    [TestFixture]
    public sealed class RuntimeNodeTypeResolverFactoryTests
    {
        [Test]
        public void Resolver_WithoutArguments_UsesCurrentDomain()
        {
            var resolver = RuntimeNodeTypeResolverFactory.Create().Resolver();

            Assert.That(resolver, Is.InstanceOf<IRuntimeNodeTypeResolver>());
            Assert.That(((IRuntimeNodeTypeResolver)resolver).AppDomain, Is.SameAs(AppDomain.CurrentDomain));
        }

        [Test]
        public void Resolver_WithAppDomain_UsesProvidedDomain()
        {
            var resolver = RuntimeNodeTypeResolverFactory.Create().Resolver(AppDomain.CurrentDomain);

            Assert.That(resolver.AppDomain, Is.SameAs(AppDomain.CurrentDomain));
        }

        [Test]
        public void Resolver_WithNullAppDomain_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => RuntimeNodeTypeResolverFactory.Create().Resolver(null!));
        }
    }
}
