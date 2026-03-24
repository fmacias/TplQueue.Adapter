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

            var retryPolicies = new Dictionary<string, IRetryPolicyOptions>(StringComparer.OrdinalIgnoreCase);
            var dispatchers = new Dictionary<string, IQOptions>(StringComparer.OrdinalIgnoreCase);

            configurationSection.GetSection(RETRY_POLICIES).Bind(retryPolicies);
            configurationSection.GetSection(DISPATCHERS).Bind(dispatchers);
            services
                .AddSingleton<IReadOnlyDictionary<string, IRetryPolicyOptions>>(retryPolicies)
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
                .AddSingleton<IReadOnlyDictionary<string, IRetryPolicyOptions>>(builder.RetryPolicies)
                .AddSingleton<IReadOnlyDictionary<string, IQOptions>>(builder.Dispatchers);

            return AddApi(services, apiImplementation);
        }

        public static IServiceCollection AddTplQueue(
            this IServiceCollection services,
            IApi apiImplementation,
            IDictionary<string, IRetryPolicyOptions> retryPolicies,
            IDictionary<string, IQOptions> dispatcherOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (apiImplementation == null) throw new ArgumentNullException(nameof(apiImplementation));
            if (retryPolicies == null) throw new ArgumentNullException(nameof(retryPolicies));
            if (dispatcherOptions == null) throw new ArgumentNullException(nameof(dispatcherOptions));

            services
                .AddSingleton<IReadOnlyDictionary<string, IRetryPolicyOptions>>(
                    retryPolicies is IReadOnlyDictionary<string, IRetryPolicyOptions> rPolicies
                        ? rPolicies
                        : new Dictionary<string, IRetryPolicyOptions>(retryPolicies, StringComparer.OrdinalIgnoreCase))
                .AddSingleton<IReadOnlyDictionary<string, IQOptions>>(
                    dispatcherOptions is IReadOnlyDictionary<string, IQOptions> dOptions
                        ? dOptions
                        : new Dictionary<string, IQOptions>(dispatcherOptions, StringComparer.OrdinalIgnoreCase));
    
            return AddApi(services, apiImplementation);
        }
        private static IServiceCollection AddApi(IServiceCollection services, IApi facade)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (facade == null) throw new ArgumentNullException(nameof(facade));

            return services
                .AddSingleton(facade)
                .AddSingleton(facade.RetryPolicyAbstractFactory)
                .AddSingleton(facade.JobFactory)
                .AddSingleton(facade.DataJobFactory)
                .AddSingleton(facade.QFactory)
                .AddSingleton(facade.ObserverFactory())
                .AddSingleton(facade.SystemTexSerializerFactory());
        }
        /// <summary>
        /// Fluent builder for code-based configuration of retry policies and dispatcher options.
        /// </summary>
        public sealed class TplQueueOptionsBuilder
        {
            internal Dictionary<string, IRetryPolicyOptions> RetryPolicies { get; } =
                new Dictionary<string, IRetryPolicyOptions>(StringComparer.OrdinalIgnoreCase);

            internal Dictionary<string, IQOptions> Dispatchers { get; } =
                new Dictionary<string, IQOptions>(StringComparer.OrdinalIgnoreCase);

            public TplQueueOptionsBuilder AddRetryPolicy(string name, IRetryPolicyOptions options)
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
