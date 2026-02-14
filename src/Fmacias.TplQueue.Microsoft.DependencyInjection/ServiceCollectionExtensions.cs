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
            var dispatchers = new Dictionary<string, IQOptions>(StringComparer.OrdinalIgnoreCase);

            configurationSection.GetSection(RETRY_POLICIES).Bind(retryPolicies);
            configurationSection.GetSection(DISPATCHERS).Bind(dispatchers);
            services
                .AddSingleton<IReadOnlyDictionary<string, RetryPolicyOptions>>(retryPolicies)
                .AddSingleton<IReadOnlyDictionary<string, IQOptions>>(dispatchers);

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
                .AddSingleton<IReadOnlyDictionary<string, IQOptions>>(builder.Dispatchers);

            return AddApi(services, apiImplementation);
        }

        public static IServiceCollection AddTplQueue(
            this IServiceCollection services,
            IApi apiImplementation,
            IDictionary<string, RetryPolicyOptions> retryPolicies,
            IDictionary<string, IQOptions> dispatcherOptions)
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
                    dispatcherOptions is IReadOnlyDictionary<string, IQOptions> dOptions
                        ? dOptions
                        : new Dictionary<string, IQOptions>(dispatcherOptions, StringComparer.OrdinalIgnoreCase));
    
            return AddApi(services, apiImplementation);
        }
        private static IServiceCollection AddApi(IServiceCollection services, IApi facade)
        {
            return services
                .AddSingleton(sp => facade)
                .AddSingleton(sp =>
                    sp.GetRequiredService<IApi>().GetCoreApi())
                .AddTransient(sp =>
                    sp.GetRequiredService<IApi>().ObserverFactory())
                .AddTransient(sp =>
                    sp.GetRequiredService<IApi>().CacheFactory())
                .AddTransient(sp =>
                    sp.GetRequiredService<IApi>()
                        .RetryPolicyFactory(
                            sp.GetRequiredService<IReadOnlyDictionary<string, RetryPolicyOptions>>()
                        ))
                .AddTransient(sp =>
                    sp.GetRequiredService<IApi>().GetJobFactoryCore())
                .AddTransient(sp =>
                    sp.GetRequiredService<IApi>().GetJobRootFactoryCore())
                .AddTransient(sp =>
                    sp.GetRequiredService<IApi>()
                        .PayloadJobFactory(
                            sp.GetRequiredService<IReadOnlyDictionary<string, RetryPolicyOptions>>()
                        ))
                .AddTransient(sp =>
                    sp.GetRequiredService<IApi>().CacheableQFactory())
                .AddTransient(sp =>
                    sp.GetRequiredService<IApi>().QFactory(
                        sp.GetRequiredService<IReadOnlyDictionary<string, IQOptions>>(), sp.GetRequiredService<IReadOnlyDictionary<string, RetryPolicyOptions>>()));
        }
        /// <summary>
        /// Fluent builder for code-based configuration of retry policies and dispatcher options.
        /// </summary>
        public sealed class TplQueueOptionsBuilder
        {
            internal Dictionary<string, RetryPolicyOptions> RetryPolicies { get; } =
                new Dictionary<string, RetryPolicyOptions>(StringComparer.OrdinalIgnoreCase);

            internal Dictionary<string, IQOptions> Dispatchers { get; } =
                new Dictionary<string, IQOptions>(StringComparer.OrdinalIgnoreCase);

            public TplQueueOptionsBuilder AddRetryPolicy(string name, RetryPolicyOptions options)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Name cannot be null or empty.", nameof(name));
                if (options is null) throw new ArgumentNullException(nameof(options));

                RetryPolicies[name] = options;
                return this;
            }

            public TplQueueOptionsBuilder AddDispatcher(string name, IQOptions options)
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
