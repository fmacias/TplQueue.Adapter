# Retry Selection

Retry selection follows a simple precedence model.

1. Root-level retry policy
2. Queue-level retry policy
3. Default `NoRetryPolicy`

This lets a queue define the general operational policy while still allowing a particular graph to override it when needed.
