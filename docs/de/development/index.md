# Entwicklung

Dieser Bereich bündelt Erweiterungs- und Source-Build-Hinweise für die öffentliche TplQueue-Package-Linie.

## Hauptthemen

- [Queues erweitern](extending-queues.md)
- [Observer erweitern](extending-observers.md)
- [Retry-Policies erweitern](extending-retry-policies.md)
- [Serialisierungs-Module](serialization-modules.md)
- [Dependency Injection](dependency-injection.md)
- [C#-Language-Version-Policy](csharp-language-version.md)

## Lokale Validierung

Führen Sie die Adapter-Testoberfläche aus:

```powershell
dotnet test .\TplQueue.Adapter.sln
```

Führen Sie die Repository-Coverage aus:

```powershell
.\coverage.ps1
.\coverage.ps1 -EnforceBaseline
```

Die kurze Testprojekt-Übersicht ist ebenfalls in [../../test/README.md](../../test/README.md) hinterlegt.

## Modulfokussierte Arbeit

Die meisten package-spezifischen Implementierungsdetails befinden sich in den Modulordnern unter `src/`.

Behalten Sie die Root-README als Repository-Einstiegspunkt bei und verwenden Sie die Package-READMEs, wenn Sie modulbezogenes Verhalten benötigen.
