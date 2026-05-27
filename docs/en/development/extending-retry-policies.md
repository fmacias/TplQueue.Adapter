# Extending Retry Policies

Concrete retry policies belong on the Adapter side.

Keep `IRetryPolicy` implementations deterministic and keep factory delegates side-effect free.
