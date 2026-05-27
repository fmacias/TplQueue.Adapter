# Operations

This section groups operational concerns that matter when you publish, observe, sign, validate, and release the TplQueue package line.

## Main topics

- [Observability](observability.md)
- [Retry Policies](retry-policies.md)
- [Cache-Backed Recovery](cache-backed-recovery.md)
- [Build and Test](build-and-test.md)
- [Strong-Name Signing](strong-name-signing.md)
- [Versioning and Release](versioning-release.md)

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
