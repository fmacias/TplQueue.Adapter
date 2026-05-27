# Cancellation

Per-enqueue cancellation is tracked both before dispatch and during execution.

- If a token is already canceled when the scheduler is ready to dispatch the job, the job is marked as `Canceled`, it is not executed, and the dispatcher slot is released immediately.
- If cancellation happens after execution has started, the running job observes the token, the queue publishes `Canceled`, and the slot is released during finalization.
