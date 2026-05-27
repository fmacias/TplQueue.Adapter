# Development

This section groups extension and source-build guidance for the public TplQueue package line.

## Main topics

- [Extending Queues](extending-queues.md)
- [Extending Observers](extending-observers.md)
- [Extending Retry Policies](extending-retry-policies.md)
- [Serialization Modules](serialization-modules.md)
- [Dependency Injection](dependency-injection.md)
- [C# Language Version Policy](csharp-language-version.md)

## Local validation

Run the Adapter test surface:

```powershell
dotnet test .\TplQueue.Adapter.sln
```

Run repository coverage:

```powershell
.\coverage.ps1
.\coverage.ps1 -EnforceBaseline
```

The short test-project inventory is also kept in [../../test/README.md](../../test/README.md).

## Module-focused work

Most package-specific implementation details live in the module folders under `src/`.

Keep the root README as the repository entry point and use the package READMEs when you need module-level behavior.
