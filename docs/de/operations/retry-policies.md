# Retry-Policies

TplQueue integriert Retry-Verhalten über Abstraktionen und Factory-Delegates.

Auf Core-Ebene ist der Haupt-Contract `IRetryPolicy` plus `Func<IRetryPolicy>` auf Queues und Roots. Konkrete Retry-Implementierungen werden typischerweise über Adapter-Module wie `Fmacias.TplQueue.RetryPolicies` bereitgestellt.
