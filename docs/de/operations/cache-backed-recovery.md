# Cache-Backed Recovery

`ICacheQ` ist die öffentliche Runtime-Oberfläche, die verwendet wird, wenn ein payloadfähiger Graph vor der In-Memory-Ausführung persistiert werden muss.

1. Der Payload-Graph wird in den Cache dehydriert.
2. `ResumePolling()` aktiviert die Leasing-Schleife.
3. `CacheQ` leased ausstehende Roots, sobald die umschlossene `IParallelQ` Kapazität hat.
4. Die geleaste Root wird hydriert und in die umschlossene Queue enqueueed.
