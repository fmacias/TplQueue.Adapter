# TplQueue.Adapter

MIT-licensed adapter layer for TplQueue. This repository now has explicit submodules for retry policies and cache providers, plus the main `Fmacias.TplQueue` facade package.

## Current module layout
- `src/Fmacias.TplQueue`: API facade and adapter composition.
- `src/Fmacias.TplQueue.RetryPolicies`: retry policy implementations/factories.
- `src/Fmacias.TplQueue.Cache.Abstract`: reusable cache orchestration abstractions.
- `src/Fmacias.TplQueue.Cache.MemCache`: in-memory cache provider.
- `src/Fmacias.TplQueue.Serialization.SystemTextJson`: serializer implementation.
- `src/Fmacias.TplQueue.Microsoft.DependencyInjection`: DI registration extensions.
- `src/Fmacias.TplQueue.Observers.ViewModel` and `src/Fmacias.TplQueue.Log`: observer/log helper modules.

## Local pipeline
Run commands from repository root (`TplQueue.Adapter`).

1. Build solution:
```powershell
dotnet build .\TplQueue.Adapter.sln
```

2. Run all tests in this repo:
```powershell
dotnet test .\TplQueue.Adapter.sln
```

3. Pack local packages in dependency order:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\pack-local.ps1
```

Expected package output:
- `..\TplQueue.NugetLocal\*.nupkg`
- `..\TplQueue.NugetLocal\*.snupkg`

## Workspace flow (optional)
When used from `WorkspaceTplQueue`, this repo can be built/packed through workspace scripts:
- `..\WorkspaceTplQueue\build.ps1 -Configuration Debug`
- `..\WorkspaceTplQueue\pack.ps1`

## Notes
- `pack-local.ps1` clears local package cache entries for `fmacias.tplqueue*` and forces restore to avoid stale package reuse.
- This repo is intended to integrate with `TplQueue.Core` (EULA) as the runtime engine.