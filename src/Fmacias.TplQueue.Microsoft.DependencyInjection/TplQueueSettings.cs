using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Fmacias.TplQueue.Microsoft.DependencyInjection
{
    /// <summary>
    /// Binds configuration descriptors and creates the option snapshots used during DI registration.
    /// </summary>
    internal sealed class TplQueueSettings : ITplQueueSettings
    {
        public const string SectionName = "TplQueue";

        // Bind concrete mutable descriptors, then convert them to immutable option artifacts.
        public Dictionary<string, RetryPolicySettings> RetryPolicies { get; set; } =
            new Dictionary<string, RetryPolicySettings>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, QueueSettings> Queues { get; set; } =
            new Dictionary<string, QueueSettings>(StringComparer.OrdinalIgnoreCase);

        private TplQueueSettings() { }
        public static TplQueueSettings Create()
        {
            return new TplQueueSettings();
        }

        public static TplQueueSettings Load(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var section = configuration.GetSection(SectionName);

            if (!HasContent(section))
            {
                return new TplQueueSettings();
            }

            var settings = new TplQueueSettings();
            section.Bind(settings);
            settings.Normalize();
            return settings;
        }

        public Dictionary<string, IRetryPolicyOptions> ExtractRetryPolicies()
        {
            var retryPolicies = new Dictionary<string, IRetryPolicyOptions>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in RetryPolicies)
            {
                try
                {
                    retryPolicies[entry.Key] = RetryPolicyOptions.Create(
                        entry.Value.BaseDelayMs,
                        entry.Value.MaxRetries,
                        entry.Value.Factor);
                }
                catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is ArgumentException)
                {
                    throw new InvalidOperationException(
                        $"TplQueue retry policy '{entry.Key}' is invalid.",
                        ex);
                }
            }

            return retryPolicies;
        }

        public Dictionary<string, IQOptions> ExtractQueues()
        {
            var queues = new Dictionary<string, IQOptions>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in Queues)
            {
                try
                {
                    var descriptor = entry.Value;
                    queues[entry.Key] = new QOptions(
                        descriptor.Id,
                        descriptor.MaxParallelism,
                        descriptor.RetryPolicy);
                }
                catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is ArgumentException)
                {
                    throw new InvalidOperationException(
                        $"TplQueue dispatcher '{entry.Key}' is invalid.",
                        ex);
                }
            }

            return queues;
        }

        public ITplQueueSettings Upsert(string name, IRetryPolicyOptions options)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            if (options == null) throw new ArgumentNullException(nameof(options));

            RetryPolicies[name] = RetryPolicySettings.From(options);
            return this;
        }

        public ITplQueueSettings Upsert(string name, IQOptions options)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            if (options == null) throw new ArgumentNullException(nameof(options));

            Queues[name] = QueueSettings.From(options);
            return this;
        }

        private void Normalize()
        {
            RetryPolicies = RetryPolicies ?? new Dictionary<string, RetryPolicySettings>(StringComparer.OrdinalIgnoreCase);
            Queues = Queues ?? new Dictionary<string, QueueSettings>(StringComparer.OrdinalIgnoreCase);
            foreach (var descriptor in Queues.Values)
            {
                if (descriptor.Id == Guid.Empty) descriptor.Id = Guid.NewGuid();
            }
        }

        private static bool HasContent(IConfigurationSection section)
        {
            if (section == null) return false;
            if (section.Value != null) return true;

            using (var children = section.GetChildren().GetEnumerator())
            {
                return children.MoveNext();
            }
        }

        public sealed class RetryPolicySettings: IRetryPolicyOptions
        {
            public int BaseDelayMs { get; set; }
            public int MaxRetries { get; set; }
            public double Factor { get; set; }

            public static RetryPolicySettings From(IRetryPolicyOptions options)
            {
                if (options == null) throw new ArgumentNullException(nameof(options));

                return new RetryPolicySettings
                {
                    BaseDelayMs = options.BaseDelayMs,
                    MaxRetries = options.MaxRetries,
                    Factor = options.Factor
                };
            }
        }

        public sealed class QueueSettings: IQOptions
        {
            public Guid Id { get; set; }
            public int MaxParallelism { get; set; }
            public string? RetryPolicy { get; set; } = string.Empty;

            public static QueueSettings From(IQOptions options)
            {
                if (options == null) throw new ArgumentNullException(nameof(options));

                return new QueueSettings
                {
                    Id = options.Id == Guid.Empty ? Guid.NewGuid() : options.Id,
                    MaxParallelism = options.MaxParallelism,
                    RetryPolicy = options.RetryPolicy
                };
            }
        }
    }
}
