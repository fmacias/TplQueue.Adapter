# Local NuGet packaging for TplQueue

This solution is configured to use a local NuGet feed at `./_local-packages` via `NuGet.config`.

## Pack to the local feed

Run the following commands from the solution root:

```powershell
dotnet pack Fmaciasruano.TplQueue/src/Fmaciasruano.TplQueue.csproj -c Release -o ./_local-packages
dotnet pack Fmaciasruano.TplQueue.Cache.Abstract/Fmaciasruano.TplQueue.Cache.Abstract.csproj -c Release -o ./_local-packages
dotnet pack Fmaciasruano.TplQueue.Observers.ViewModel/src/Fmaciasruano.TplQueue.Observers.ViewModel.csproj -c Release -o ./_local-packages
dotnet pack Fmaciasruano.TplQueue.RetryPolicies/src/Fmaciasruano.TplQueue.RetryPolicies.csproj -c Release -o ./_local-packages
dotnet pack Fmaciasruano.TplQueue.Serialization.SystemTextJson/src/Fmaciasruano.TplQueue.Serialization.SystemTextJson.csproj -c Release -o ./_local-packages
dotnet pack Fmaciasruano.TplQueue.Microsoft.DependencyInjection/src/Fmaciasruano.TplQueue.Microsoft.DependencyInjection.csproj -c Release -o ./_local-packages
dotnet pack Fmaciasruano.TplQueue.Log/Fmaciasruano.TplQueue.Log.csproj -c Release -o ./_local-packages
```

These commands generate `.nupkg` files in `./_local-packages`. `dotnet restore` and Visual Studio will then use the `LocalPackages` source in `NuGet.config` to resolve these packages.

## Workflow to test local changes

1. Modify the library project(s).
2. Re-run the `dotnet pack` command(s) above to regenerate the package(s) into `./_local-packages`.
3. Run `dotnet restore` and `dotnet build` on the solution.

## Clear local caches (if needed)

```powershell
dotnet nuget locals all --clear
```

## Publishing later

When publishing to a public NuGet feed, you can push the same `PackageId`/`Version`. At that point, you may remove `LocalPackages` from `NuGet.config` or place it below the public feed priority.
