# Retry Policies

TplQueue integrates retry behavior through abstractions and factory delegates.

At Core level, the main contract is `IRetryPolicy` plus `Func<IRetryPolicy>` on queues and roots. Concrete retry implementations are typically obtained through Adapter modules such as `Fmacias.TplQueue.RetryPolicies`.
