# Fmacias.TplQueue.Serialization.Xml

XML serializer module for TplQueue payload and cache scenarios.

See also:

- [TplQueue.Adapter root README](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md)
- [TplQueue.Adapter serialization section](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md#serialization)
- [TplQueue.Core cache section](https://github.com/fmacias/TplQueue.Core/blob/main/README.md#cache-and-persistence)
- [Fmacias.TplQueue README](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/README.md)

Repository-wide packaging and strong-name signing rules are documented in the [TplQueue.Adapter root README](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md#strong-name-signing).

## Local pipeline

Run from `TplQueue.Adapter` root:

```powershell
dotnet build .\src\Fmacias.TplQueue.Serialization.Xml\Fmacias.TplQueue.Serialization.Xml.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\pack-local.ps1
```
