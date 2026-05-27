# Architektur

Dieser Bereich erklärt, wie die öffentliche TplQueue-Dokumentation die Verantwortlichkeiten des Execution-Kernels von den adapterseitigen Integrationsverantwortlichkeiten trennt.

## Hauptthemen

- [Runtime-Modell](runtime-model.md)
- [Core-/Adapter-Grenze](core-adapter-boundary.md)
- [Payload-Handler-Integration](payload-handler-integration.md)
- [Observer-Event-Publikation](observer-event-publication.md)
- [Cache- und Persistenzgrenze](cache-persistence-boundary.md)
- [Diagramme](diagrams.md)

## Grenze zu Core

`TplQueue.Core` verantwortet:

- die Ausführung von Job-Graphen
- die Dispatch-Semantik von Queues
- die Retry-Selection-Punkte
- die Publikation von Queue-Events

`TplQueue.Adapter` verantwortet:

- die adapterseitige Komposition über `API`
- benannte Queue- und Retry-Policy-Dictionaries
- konkrete Retry-Policy-Implementierungen
- eingebaute Observer
- Cache-Implementierungen und Hydration-Helper
- Serializer-Implementierungen
- DI-Registrierungs-Helper

## Modulstruktur

Auf Repository-Ebene ist die Adapter-Linie in fokussierte Packages aufgeteilt statt in ein großes Integrations-Assembly.

Dadurch bleibt:

- die öffentliche Facade über `Fmacias.TplQueue` verfügbar
- optionale Integrationen nur dann installierbar, wenn sie benötigt werden
- die Trennung zwischen Cache-, Observer-, Serializer- und DI-Themen modular

## Payload- und Cache-Aufteilung

Ausführungsseitige Payload-Knoten gehören über `IDataJob` und `IDataJobRoot` zu Core.

Adapter ergänzt die Infrastruktur um diese Knoten herum:

- Payload-Handler-Registrierung
- Dehydration und Hydration
- Serializer-Auswahl
- Type-Resolution-Grenzen

Diese Aufteilung hält das Runtime-Modell klein und ermöglicht darüber liegende reichhaltigere Integrationsszenarien.
