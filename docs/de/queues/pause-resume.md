# Pause und Resume

Konkrete Queues sind standardmäßig hot. `ResumePolling()` wird am besten als "resume nach `Pause()`" verstanden und nicht als verpflichtende Erstaktivierung.

## Runtime-Erwartungen

- `Pause()` ist soft und kooperativ.
- Enqueueing während einer Pause wird unterstützt.
- Gepufferte Elemente bleiben in der internen Queue.
- `ResumePolling()` weckt den pausierten Scheduler auf und arbeitet die gepufferte Arbeit ab.
