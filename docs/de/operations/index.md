# Betrieb

Dieser Bereich bündelt betriebliche Themen, die relevant sind, wenn Sie die TplQueue-Package-Linie veröffentlichen, beobachten, signieren, validieren und releasen.

## Hauptthemen

- [Observability](observability.md)
- [Retry-Policies](retry-policies.md)
- [Cache-Backed Recovery](cache-backed-recovery.md)
- [Build und Test](build-and-test.md)
- [Strong-Name-Signing](strong-name-signing.md)
- [Versionierung und Release](versioning-release.md)

## Lokales Packaging

Erstellen Sie lokale Preview-Packages mit:

```powershell
.\pack-local.ps1
```

Dadurch werden die erzeugten Packages in `..\TplQueue.NugetLocal` geschrieben.

## Strong-Name-Signing

Normale Source-Builds sind unsigniert.

Offizielle signierte Release-Packages werden nur erzeugt, wenn `pack-local.ps1` Folgendes erhält:

- einen externen privaten `.snk`-Pfad
- den dazugehörigen vollständigen Public Key

Für koordinierte öffentliche Release-Validierung und Veröffentlichung verwenden Sie die Workspace-Skripte, statt dieses Repository isoliert zu signieren.

## Release-Flow

Der öffentliche Release-Flow wird von `WorkspaceTplQueue` aus koordiniert:

```powershell
.\pack.ps1 -Version <version> -StrongNameKeyFile <private-key-path> -StrongNamePublicKey <public-key>
.\publish.ps1 -Version <version> -ExpectedStrongNamePublicKey <public-key>
```

Die aktive öffentliche Preview-Linie ist `0.1.0-preview.1`.

## Lizenz

`TplQueue.Adapter` wird unter der MIT-Lizenz verteilt.
