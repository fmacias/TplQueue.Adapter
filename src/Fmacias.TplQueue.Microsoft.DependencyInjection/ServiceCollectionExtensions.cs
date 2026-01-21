using Fmacias.TplQueue.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Fmacias.TplQueue.Microsoft.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        private const string RETRY_POLICIES = "RetryPolicies";
        private const string DISPATCHERS = "Dispatchers";

        public static IServiceCollection AddTplQueue(
            this IServiceCollection services,
            IConfiguration configurationSection,
            IApi apiImplementation)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configurationSection == null) throw new ArgumentNullException(nameof(configurationSection));
            if (apiImplementation == null) throw new ArgumentNullException(nameof(apiImplementation));

            var retryPolicies = new Dictionary<string, RetryPolicyOptions>(StringComparer.OrdinalIgnoreCase);
            var dispatchers = new Dictionary<string, IChainOptions>(StringComparer.OrdinalIgnoreCase);

            configurationSection.GetSection(RETRY_POLICIES).Bind(retryPolicies);
            configurationSection.GetSection(DISPATCHERS).Bind(dispatchers);
            services
                .AddSingleton<IReadOnlyDictionary<string, RetryPolicyOptions>>(retryPolicies)
                .AddSingleton<IReadOnlyDictionary<string, IChainOptions>>(dispatchers);

            return AddApi(services, apiImplementation);
        }

        public static IServiceCollection AddTplQueue(
            this IServiceCollection services,
            Action<TplQueueOptionsBuilder> configure,
            IApi apiImplementation)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            if (apiImplementation == null) throw new ArgumentNullException(nameof(apiImplementation));

            var builder = new TplQueueOptionsBuilder();
            configure(builder);

            services
                .AddSingleton<IReadOnlyDictionary<string, RetryPolicyOptions>>(builder.RetryPolicies)
                .AddSingleton<IReadOnlyDictionary<string, IChainOptions>>(builder.Dispatchers);

            return AddApi(services, apiImplementation);
        }

        public static IServiceCollection AddTplQueue(
            this IServiceCollection services,
            IApi apiImplementation,
            IDictionary<string, RetryPolicyOptions> retryPolicies,
            IDictionary<string, IChainOptions> dispatcherOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (retryPolicies == null) throw new ArgumentNullException(nameof(retryPolicies));
            if (dispatcherOptions == null) throw new ArgumentNullException(nameof(dispatcherOptions));

            services
                .AddSingleton(
                    retryPolicies is IReadOnlyDictionary<string, RetryPolicyOptions> rPolicies
                        ? rPolicies
                        : new Dictionary<string, RetryPolicyOptions>(retryPolicies, StringComparer.OrdinalIgnoreCase))
                .AddSingleton(
                    dispatcherOptions is IReadOnlyDictionary<string, IChainOptions> dOptions
                        ? dOptions
                        : new Dictionary<string, IChainOptions>(dispatcherOptions, StringComparer.OrdinalIgnoreCase));
    
            return AddApi(services, apiImplementation);
        }
        private static IServiceCollection AddApi(IServiceCollection services, IApi facade)
        {
            return services
                .AddSingleton(sp => facade)
                .AddSingleton(sp =>
                    sp.GetRequiredService<IApi>().GetCoreApi())
                .AddTransient(sp =>
                    sp.GetRequiredService<IApi>().GetObserverFactory())
                .AddTransient(sp =>
                    sp.GetRequiredService<IApi>().GetCacheFactory())
                .AddTransient(sp =>
                    sp.GetRequiredService<IApi>()
                        .GetRetryPolicyFactory(
                            sp.GetRequiredService<IReadOnlyDictionary<string, RetryPolicyOptions>>()
                        ))
                .AddTransient(sp =>
                    sp.GetRequiredService<IApi>().GetJobFactory())
                .AddTransient(sp =>
                    sp.GetRequiredService<IApi>().GetJobRootFactory())
                .AddTransient(sp =>
                    sp.GetRequiredService<IApi>().GetPayloadJobFactory())
                .AddTransient(sp =>
                    sp.GetRequiredService<IApi>().GetSerializableDispatcherFactory())
                .AddTransient(sp =>
                    sp.GetRequiredService<IApi>().GetTaskDispatcherFactory(
                        sp.GetRequiredService<IReadOnlyDictionary<string, IChainOptions>>(),
                        sp.GetRequiredService<IRetryPolicyFactory>()));
        }
        /// <summary>
        /// Fluent builder for code-based configuration of retry policies and dispatcher options.
        /// </summary>
        public sealed class TplQueueOptionsBuilder
        {
            internal Dictionary<string, RetryPolicyOptions> RetryPolicies { get; } =
                new Dictionary<string, RetryPolicyOptions>(StringComparer.OrdinalIgnoreCase);

            internal Dictionary<string, IChainOptions> Dispatchers { get; } =
                new Dictionary<string, IChainOptions>(StringComparer.OrdinalIgnoreCase);

            public TplQueueOptionsBuilder AddRetryPolicy(string name, RetryPolicyOptions options)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Name cannot be null or empty.", nameof(name));
                if (options is null) throw new ArgumentNullException(nameof(options));

                RetryPolicies[name] = options;
                return this;
            }

            public TplQueueOptionsBuilder AddDispatcher(string name, IChainOptions options)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Name cannot be null or empty.", nameof(name));
                if (options is null) throw new ArgumentNullException(nameof(options));

                Dispatchers[name] = options;
                return this;
            }
        }
    }
}
