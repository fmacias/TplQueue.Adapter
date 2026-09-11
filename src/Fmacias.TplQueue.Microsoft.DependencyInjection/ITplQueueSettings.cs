using Fmacias.TplQueue.Contracts;
using System.Collections.Generic;

namespace Fmacias.TplQueue.Microsoft.DependencyInjection
{
    /// <summary>
    /// Builds named queue and retry options for dependency-injection registration.
    /// </summary>
    /// <remarks>
    /// AddTplQueue captures independent option snapshots. Subsequent changes to these settings
    /// do not reconfigure the registered API, its factories, or existing queues.
    /// Configure on one thread before registration; concurrent mutation is not supported.
    /// </remarks>
    public interface ITplQueueSettings
    {
        /// <summary>Returns a new dictionary containing the current retry options.</summary>
        Dictionary<string, IRetryPolicyOptions> ExtractRetryPolicies();
        /// <summary>Returns a new dictionary containing the current queues and their stable identities.</summary>
        Dictionary<string, IQOptions> ExtractQueues();
        /// <summary>Adds or replaces a named retry policy in these settings.</summary>
        ITplQueueSettings Upsert(string name, IRetryPolicyOptions options);
        /// <summary>Adds or replaces a named queue; an empty ID is generated once on insertion.</summary>
        ITplQueueSettings Upsert(string name, IQOptions options);
    }
}
