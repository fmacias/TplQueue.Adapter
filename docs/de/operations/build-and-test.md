# Build und Test

## Adapter-Repository validieren

```powershell
dotnet test .\TplQueue.Adapter.sln
.\coverage.ps1
.\coverage.ps1 -EnforceBaseline
```

## Öffentliche Core-Build- und Testkommandos

```powershell
dotnet restore .\core.sln --configfile .\NuGet.config
dotnet build .\core.sln
dotnet test .\core.sln
```
