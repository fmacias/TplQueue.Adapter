# Development

This section covers local source-build concerns for `TplQueue.Adapter`.

## Language-version policy

The shipped `netstandard2.0` adapter modules are pinned to `LangVersion=9.0`.

That is a source-build policy for the repository, not a consumer runtime requirement.

## Local validation

Run the repository test surface:

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
