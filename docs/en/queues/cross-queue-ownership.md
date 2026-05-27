# Cross-Queue Ownership

The first queue that enqueues a shared job instance claims execution ownership for that specific instance.

Later queues may still reference that job as a dependency, but they must not execute it again. `CrossQueueId` is observable runtime state, not user-configurable setup.
