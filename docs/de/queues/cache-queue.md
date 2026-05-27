# Cache-Queue

`ICacheQ` erweitert die Queue-Ausführung um cachegestützte Dehydration und Leasing von Payload-Roots.

Sie wird erstellt aus:

- einer bestehenden `IParallelQ`
- einem `IDataJobCache`
- einem `ILogger<ICacheQ>`

`ICacheQ.Enqueue(...)` dehydriert den Payload-Graphen zuerst in den Cache. Nach `ResumePolling()` leased `CacheQ` ausstehende Roots, hydriert sie und enqueuet die hydrierte Root in die umschlossene Queue.
