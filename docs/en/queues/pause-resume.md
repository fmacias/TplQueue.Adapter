# Pause and Resume

Concrete queues are hot by default. `ResumePolling()` is best understood as "resume after `Pause()`" rather than mandatory first activation.

## Runtime expectations

- `Pause()` is soft and cooperative.
- Enqueue during pause is supported.
- Buffered items remain in the internal queue.
- `ResumePolling()` wakes the paused scheduler and drains the buffered work.
