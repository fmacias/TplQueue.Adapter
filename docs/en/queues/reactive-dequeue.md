# Reactive Dequeue Strategy

TplQueue favors reactive signaling over continuous polling.

The runtime targets `.NET Standard 2.0`, so it relies on focused async coordination primitives such as `AsyncAutoResetEvent`, `AsyncOrderedQueue`, and `SemaphoreSlim` rather than busy loops.
