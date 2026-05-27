# Build and Test

## Adapter repository validation

```powershell
dotnet test .\TplQueue.Adapter.sln
.\coverage.ps1
.\coverage.ps1 -EnforceBaseline
```

## Public Core build and test commands

```powershell
dotnet restore .\core.sln --configfile .\NuGet.config
dotnet build .\core.sln
dotnet test .\core.sln
```
