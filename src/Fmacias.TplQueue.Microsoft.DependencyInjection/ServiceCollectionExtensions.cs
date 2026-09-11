using Fmacias.TplQueue.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Fmacias.TplQueue.Microsoft.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTplQueue(this IServiceCollection services, ICoreApi coreApi)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (coreApi == null) throw new ArgumentNullException(nameof(coreApi));
            return AddApi(services, coreApi, TplQueueSettings.Create());
        }

        public static IServiceCollection AddTplQueue(
            this IServiceCollection services,
            IConfiguration configuration,
            ICoreApi coreApi)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (coreApi == null) throw new ArgumentNullException(nameof(coreApi));

            return AddApi(services, coreApi, TplQueueSettings.Load(configuration));
        }

        public static IServiceCollection AddTplQueue(
            this IServiceCollection services,
            Action<ITplQueueSettings> configure,
            ICoreApi coreApi)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            if (coreApi == null) throw new ArgumentNullException(nameof(coreApi));

            var settings = TplQueueSettings.Create();
            configure(settings);
            return AddApi(services, coreApi, settings);
        }

        public static IServiceCollection AddTplQueue(
            this IServiceCollection services,
            ICoreApi coreApi,
            IDictionary<string, IRetryPolicyOptions> retryPolicies,
            IDictionary<string, IQOptions> queues, 
            IConfiguration? configuration = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (coreApi == null) throw new ArgumentNullException(nameof(coreApi));
            if (retryPolicies == null) throw new ArgumentNullException(nameof(retryPolicies));
            if (queues == null) throw new ArgumentNullException(nameof(queues));


            TplQueueSettings settings = (configuration == null) ? TplQueueSettings.Create()
                : TplQueueSettings.Load(configuration);

            foreach (var policy in retryPolicies)
            {
                settings.Upsert(policy.Key, policy.Value);
            }
            foreach (var queue in queues)
            {
                settings.Upsert(queue.Key, queue.Value);
            }
            return AddApi(services, coreApi, settings);
        }
        private static IServiceCollection AddApi(IServiceCollection services, ICoreApi coreApi,
             ITplQueueSettings tplQueueSettings)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (coreApi == null) throw new ArgumentNullException(nameof(coreApi));
            return RegisterServicesIntoContainer(services, coreApi, tplQueueSettings);
        }

        private static IServiceCollection RegisterServicesIntoContainer(IServiceCollection services, ICoreApi coreApi, ITplQueueSettings settings)
        {
            var apiFasade = API.Create(coreApi, settings.ExtractRetryPolicies(), settings.ExtractQueues());
            return services
                .AddSingleton<IApi>(apiFasade)
                .AddSingleton<IReadOnlyDictionary<string, IRetryPolicyOptions>>(apiFasade.RetryPolicyOptions)
                .AddSingleton<IReadOnlyDictionary<string, IQOptions>>(apiFasade.QueueOptions)
                .AddSingleton<IRetryPolicyAbstractFactory>(apiFasade.RetryPolicyAbstractFactory)
                .AddSingleton<IJobFactory>(apiFasade.JobFactory)
                .AddSingleton<IDataJobFactory>(apiFasade.DataJobFactory)
                .AddSingleton<IQFactoryAdapter>(apiFasade.QFactory)
                .AddSingleton<IObserverFactory>(apiFasade.ObserverFactory())
                .AddSingleton<ISystemTextJsonSerializerFactory>(apiFasade.SystemTextSerializerFactory())
                .AddSingleton<IXmlSerializerFactory>(apiFasade.XmlSerializerFactory())
                .AddSingleton<ITplQueueSettings>(settings)
                .AddTransient<ISystemTextJsonUniversalSerializer>(serviceProvider 
                    => serviceProvider
                        .GetRequiredService<ISystemTextJsonSerializerFactory>()
                        .Serializer(new JsonSerializerOptions { WriteIndented = true }))
                .AddTransient<IXmlUniversalSerializer>(serviceProvider 
                    => serviceProvider
                        .GetRequiredService<IXmlSerializerFactory>()
                        .Serializer());
        }
    }
}
