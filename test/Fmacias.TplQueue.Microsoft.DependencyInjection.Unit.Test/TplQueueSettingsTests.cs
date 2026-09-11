using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fmacias.TplQueue.Microsoft.DependencyInjection.Unit.Test
{
    [TestFixture]
    public class TplQueueSettingsTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ConfiguredQueueIdentity_IsStableAcrossApiAndSettings(bool explicitId)
        {
            // Arrange
            var id = Guid.NewGuid();
            var values = new Dictionary<string, string>
            {
                ["TplQueue:Queues:main:MaxParallelism"] = "2",
                ["TplQueue:Queues:main:RetryPolicy"] = "none"
            };
            if (explicitId) values["TplQueue:Queues:main:Id"] = id.ToString();
            var services = new ServiceCollection();

            // Act
            services.AddTplQueue(Configuration(values), new FakeCoreApi());
            using var provider = services.BuildServiceProvider();
            var api = provider.GetRequiredService<IApi>();
            var settings = provider.GetRequiredService<ITplQueueSettings>();
            var first = settings.ExtractQueues()["MAIN"];
            var second = settings.ExtractQueues()["main"];

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(first.Id, Is.Not.EqualTo(Guid.Empty));
                Assert.That(second.Id, Is.EqualTo(first.Id));
                Assert.That(api.QueueOptions["main"].Id, Is.EqualTo(first.Id));
                Assert.That(first.MaxParallelism, Is.EqualTo(2));
                Assert.That(first.RetryPolicy, Is.EqualTo("none"));
                if (explicitId) Assert.That(first.Id, Is.EqualTo(id));
            });
        }

        [Test]
        public void FluentQueueWithEmptyIdentity_GeneratesIdentityOnlyOnce()
        {
            // Arrange
            var options = Mock.Of<IQOptions>(value => value.Id == Guid.Empty &&
                value.MaxParallelism == 1 && value.RetryPolicy == "none");
            var services = new ServiceCollection();

            // Act
            services.AddTplQueue(settings => settings.Upsert("main", options), new FakeCoreApi());
            using var provider = services.BuildServiceProvider();
            var settings = provider.GetRequiredService<ITplQueueSettings>();
            var expected = provider.GetRequiredService<IApi>().QueueOptions["main"].Id;

            // Assert
            Assert.That(settings.ExtractQueues()["main"].Id, Is.EqualTo(expected));
            Assert.That(settings.ExtractQueues()["main"].Id, Is.EqualTo(expected));
        }

        [Test]
        public void DictionaryOverridesConfiguration_AndLaterSettingsEditsDoNotChangeApiSnapshot()
        {
            // Arrange
            var configuredId = Guid.NewGuid();
            var overridingId = Guid.NewGuid();
            var services = new ServiceCollection();
            var configuration = Configuration(new Dictionary<string, string>
            {
                ["TplQueue:Queues:main:Id"] = configuredId.ToString(),
                ["TplQueue:Queues:main:MaxParallelism"] = "2",
                ["TplQueue:Queues:main:RetryPolicy"] = "none",
                ["TplQueue:Queues:other:MaxParallelism"] = "3",
                ["TplQueue:Queues:other:RetryPolicy"] = "none",
                ["TplQueue:RetryPolicies:retry:BaseDelayMs"] = "10",
                ["TplQueue:RetryPolicies:retry:MaxRetries"] = "2",
                ["TplQueue:RetryPolicies:retry:Factor"] = "2"
            });

            // Act
            services.AddTplQueue(new FakeCoreApi(),
                new Dictionary<string, IRetryPolicyOptions> { ["retry"] = RetryPolicyOptions.Create(20, 4, 2) },
                new Dictionary<string, IQOptions> { ["MAIN"] = new QOptions(overridingId, 4, "retry") }, configuration);
            using var provider = services.BuildServiceProvider();
            var api = provider.GetRequiredService<IApi>();
            provider.GetRequiredService<ITplQueueSettings>().Upsert("main", new QOptions(Guid.NewGuid(), 8, "none"));

            // Assert
            Assert.That(api.QueueOptions["main"].Id, Is.EqualTo(overridingId));
            Assert.That(api.QueueOptions["main"].MaxParallelism, Is.EqualTo(4));
            Assert.That(api.QueueOptions["other"].MaxParallelism, Is.EqualTo(3));
            Assert.That(api.RetryPolicyOptions["retry"].MaxRetries, Is.EqualTo(4));
        }

        [Test]
        public void RegisteredOptionDictionaries_CanActivateAConsumer()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddTplQueue(new FakeCoreApi());
            services.AddTransient<OptionsConsumer>();
            using var provider = services.BuildServiceProvider();

            // Act
            var consumer = provider.GetRequiredService<OptionsConsumer>();
            var api = provider.GetRequiredService<IApi>();

            // Assert
            Assert.That(consumer.Queues, Is.SameAs(api.QueueOptions));
            Assert.That(consumer.Policies, Is.SameAs(api.RetryPolicyOptions));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void ConfiguredQueue_AllowsUnspecifiedRetryPolicy(string name)
        {
            var values = new Dictionary<string, string> { ["TplQueue:Queues:main:MaxParallelism"] = "1" };
            if (name != null) values["TplQueue:Queues:main:RetryPolicy"] = name;
            var services = new ServiceCollection().AddTplQueue(Configuration(values), new FakeCoreApi());
            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IApi>().QueueOptions["main"];
            Assert.That(options.RetryPolicy, Is.EqualTo(name ?? string.Empty));
            Assert.That(options.Id, Is.Not.EqualTo(Guid.Empty));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void FluentQueue_AllowsUnspecifiedRetryPolicy(string name)
        {
            var services = new ServiceCollection().AddTplQueue(
                settings => settings.Upsert("main", new QOptions(Guid.NewGuid(), 1, name)), new FakeCoreApi());
            using var provider = services.BuildServiceProvider();
            Assert.That(provider.GetRequiredService<IApi>().QueueOptions["main"].RetryPolicy, Is.EqualTo(name));
        }

        // Exercise the real configuration binder using the public IConfiguration contract.
        // No new provider package is needed by this test project.
        private static IConfiguration Configuration(IReadOnlyDictionary<string, string> values)
        {
            IConfigurationSection Section(string path)
            {
                var section = new Mock<IConfigurationSection>();
                section.SetupGet(value => value.Path).Returns(path);
                section.SetupGet(value => value.Key).Returns(path.Split(':').Last());
                section.SetupGet(value => value.Value).Returns(values.TryGetValue(path, out var value) ? value : null);
                section.Setup(value => value.GetSection(It.IsAny<string>())).Returns<string>(key => Section(path + ":" + key));
                section.Setup(value => value.GetChildren()).Returns(() => values.Keys
                    .Where(key => key.StartsWith(path + ":", StringComparison.OrdinalIgnoreCase))
                    .Select(key => key.Substring(path.Length + 1).Split(':')[0]).Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(key => Section(path + ":" + key)));
                return section.Object;
            }
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(value => value.GetSection(It.IsAny<string>())).Returns<string>(Section);
            return configuration.Object;
        }

        public sealed class OptionsConsumer
        {
            public OptionsConsumer(IReadOnlyDictionary<string, IQOptions> queues,
                IReadOnlyDictionary<string, IRetryPolicyOptions> policies)
            {
                Queues = queues;
                Policies = policies;
            }
            public IReadOnlyDictionary<string, IQOptions> Queues { get; }
            public IReadOnlyDictionary<string, IRetryPolicyOptions> Policies { get; }
        }
    }
}
