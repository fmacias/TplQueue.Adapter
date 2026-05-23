# Operations

This section groups packaging and release concerns for `TplQueue.Adapter`.

## Local packaging

Build local preview packages with:

```powershell
.\pack-local.ps1
```

That writes the generated packages into `..\TplQueue.NugetLocal`.

## Strong-name signing

Normal source builds are unsigned.

Official signed release packages are produced only when `pack-local.ps1` receives:

- an external private `.snk` path
- the matching full public key

For coordinated public release validation and publication, use the workspace scripts instead of signing this repository in isolation.

## Release flow

The public release flow is coordinated from `WorkspaceTplQueue`:

```powershell
.\pack.ps1 -Version <version> -StrongNameKeyFile <private-key-path> -StrongNamePublicKey <public-key>
.\publish.ps1 -Version <version> -ExpectedStrongNamePublicKey <public-key>
```

The active public preview line is `0.1.0-preview.1`.

## License

`TplQueue.Adapter` is distributed under the MIT license.
