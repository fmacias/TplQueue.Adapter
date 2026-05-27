# TplQueue

**Preview-Status: Die aktuelle TplQueue-Release-Linie ist ein Preview-Prototyp. Sie ist noch nicht produktionsreif und sollte derzeit nur in explorativen, Proof-of-Concept- oder nicht kritischen Umgebungen evaluiert werden.**

Dieser Dokumentationsbaum ist der öffentliche Einstiegspunkt für das gesamte TplQueue-Ökosystem.

Er führt zusammen:

- die `TplQueue.Core`-Runtime-Konzepte, die Consumer zum Aufbau von Queues und Job-Graphen benötigen
- die `TplQueue.Adapter`-Integrationshinweise für Packages, Dependency Injection, Caching, Observer, Serialisierung und die Erstellung benannter Queues
- die `TplQueue.Usage`-Sample-Referenzen für lauffähige Package-Consumption-Szenarien

Die Core-Usability-Dokumentation ist direkt in die folgenden Bereiche integriert. Lizenz- und Rechtsgrenzen bleiben unter [Lizenz](license/index.md).

## Hier starten

- [Einstieg](getting-started/index.md)
- [Core-Konzepte](core-concepts/index.md)
- [Queues](queues/index.md)
- [Architektur](architecture/index.md)
- [Betrieb](operations/index.md)
- [Entwicklung](development/index.md)
- [Referenz](reference/index.md)
- [Lizenz](license/index.md)

## Was TplQueue bereitstellt

- `IJob`- und `IJobRoot`-Graph-Komposition
- `IDataJob`- und `IDataJobRoot`-Ausführung mit Payload
- `IParallelQ`-, `IFifoQ`- und `ICacheQ`-Dispatch-Modelle
- Retry-Policy-Auswahl auf Queue- und Root-Ebene
- observerbasierte Runtime-Sichtbarkeit
- Adapter-seitige Unterstützung für Dependency Injection, Serialisierung, Cache-Module und benannte Queue-Konfiguration

## Öffentliche Package-Oberflächen

- `Fmacias.TplQueue.Core`
- `Fmacias.TplQueue`
- `Fmacias.TplQueue.Microsoft.DependencyInjection`
- `Fmacias.TplQueue.Cache.Abstract`
- `Fmacias.TplQueue.Cache.MemCache`
- `Fmacias.TplQueue.RetryPolicies`
- `Fmacias.TplQueue.Serialization.SystemTextJson`
- `Fmacias.TplQueue.Observers`

## Kompatibilitätshinweis

Ältere öffentliche Dokumentation nutzte `usage/` und `reference.md` als wichtigste Einstiegspunkte. Diese Pfade bleiben als Kompatibilitätsseiten verfügbar, die primäre Source of Truth ist jetzt jedoch die oben gezeigte Bereichsstruktur.
