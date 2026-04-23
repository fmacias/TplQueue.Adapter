param(
  [string]$Version,
  [string]$StrongNameKeyFile,
  [string]$StrongNamePublicKey
)

$ErrorActionPreference = 'Stop'

# Run dotnet with a known argument list and stop on non-zero exit.
# Example: Invoke-Dotnet -DotnetArgs @('pack','My.sln','-c','Release')
function Invoke-Dotnet {
  param([string[]]$DotnetArgs)

  & dotnet @DotnetArgs
  if ($LASTEXITCODE -ne 0) {
    throw "dotnet $($DotnetArgs -join ' ') failed with exit code $LASTEXITCODE."
  }
}

function Get-PackProperties {
  param(
    [string]$Version,
    [string]$StrongNameKeyFile,
    [string]$StrongNamePublicKey
  )

  if (-not [string]::IsNullOrWhiteSpace($StrongNameKeyFile) -and [string]::IsNullOrWhiteSpace($StrongNamePublicKey)) {
    throw 'StrongNamePublicKey is required when StrongNameKeyFile is provided.'
  }

  $properties = @()
  if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $properties += @(
      "-p:Version=$Version",
      "-p:PackageVersion=$Version",
      "-p:TplQueuePackageVersion=$Version"
    )
  }

  if (-not [string]::IsNullOrWhiteSpace($StrongNameKeyFile)) {
    $properties += @(
      '-p:TplQueueOfficialSign=true',
      "-p:TplQueueStrongNameKeyFile=$StrongNameKeyFile"
    )
  }

  if (-not [string]::IsNullOrWhiteSpace($StrongNamePublicKey)) {
    $properties += "-p:TplQueueStrongNamePublicKey=$StrongNamePublicKey"
  }

  return $properties
}

function Invoke-PackScript {
  param(
    [string]$ScriptPath,
    [string]$Version,
    [string]$StrongNameKeyFile,
    [string]$StrongNamePublicKey
  )

  $scriptArgs = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $ScriptPath)
  if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $scriptArgs += @('-Version', $Version)
  }
  if (-not [string]::IsNullOrWhiteSpace($StrongNameKeyFile)) {
    $scriptArgs += @('-StrongNameKeyFile', $StrongNameKeyFile)
  }
  if (-not [string]::IsNullOrWhiteSpace($StrongNamePublicKey)) {
    $scriptArgs += @('-StrongNamePublicKey', $StrongNamePublicKey)
  }

  & powershell @scriptArgs
  if ($LASTEXITCODE -ne 0) {
    throw "Dependency pack-local failed: $ScriptPath"
  }
}

# Return the folder that contains this script.
# Example: if this script is in C:\repo\TplQueue.Adapter\pack-local.ps1, returns C:\repo\TplQueue.Adapter.
function Get-RepoRoot {
  if ($PSScriptRoot) {
    return $PSScriptRoot
  }

  if ($PSCommandPath) {
    return Split-Path -Parent $PSCommandPath
  }

  return Split-Path -Parent $MyInvocation.MyCommand.Path
}

# Ensure the local NuGet folder exists and return its absolute path.
# Example: for repo root C:\repo\TplQueue.Adapter, ensures C:\repo\TplQueue.NugetLocal and returns that path.
function Ensure-NugetLocal {
  param([string]$RepoRoot)

  $nugetRoot = Join-Path $RepoRoot '..\TplQueue.NugetLocal'
  if (-not (Test-Path $nugetRoot)) {
    New-Item -ItemType Directory -Path $nugetRoot -Force | Out-Null
  }

  return (Resolve-Path $nugetRoot).Path
}

# Register the local NuGet folder as a source if it is missing.
# Example: Ensure-NugetSource -SourceName 'TplQueue.NugetLocal' -SourcePath C:\repo\TplQueue.NugetLocal
function Ensure-NugetSource {
  param(
    [string]$SourceName,
    [string]$SourcePath
  )

  $sources = (& dotnet nuget list source) | Out-String
  if ($LASTEXITCODE -ne 0) {
    throw 'dotnet nuget list source failed.'
  }

  if ($sources -notmatch [regex]::Escape($SourcePath)) {
    Invoke-Dotnet -DotnetArgs @('nuget', 'add', 'source', $SourcePath, '-n', $SourceName)
  }
}

# Resolve NuGet global-packages folder dynamically.
function Get-GlobalPackagesPath {
  $output = (& dotnet nuget locals global-packages -l) | Out-String
  if ($LASTEXITCODE -ne 0) {
    throw 'dotnet nuget locals global-packages failed.'
  }

  $match = [regex]::Match($output, ':\s*(.+)$')
  if ($match.Success) {
    return $match.Groups[1].Value.Trim()
  }

  if ($env:NUGET_PACKAGES) {
    return $env:NUGET_PACKAGES
  }

  return (Join-Path $env:USERPROFILE '.nuget\\packages')
}

# Clear stale local cache entries for Fmacias packages before packing.
function Clear-LocalNugetCache {
  $packagesRoot = Get-GlobalPackagesPath
  if (-not (Test-Path $packagesRoot)) {
    return
  }

  Get-ChildItem -Path $packagesRoot -Directory |
    Where-Object { $_.Name -like 'fmacias.tplqueue*' -or $_.Name -like 'fmaciasruano.tplqueue*' } |
    Remove-Item -Recurse -Force
}

# Return the dependency pack-local scripts that should run before packing this repo.
# Example: includes ..\TplQueue.Abstractions\pack-local.ps1.
function Get-DependencyScripts {
  param([string]$RepoRoot)

  return @(
    (Join-Path $RepoRoot '..\TplQueue.Abstractions\pack-local.ps1')
  )
}

# Execute dependency pack-local scripts when they exist.
# Example: Pack-Dependencies -ScriptPaths (Get-DependencyScripts -RepoRoot C:\repo\TplQueue.Adapter)
function Pack-Dependencies {
  param(
    [string[]]$ScriptPaths,
    [string]$Version,
    [string]$StrongNameKeyFile,
    [string]$StrongNamePublicKey
  )

  foreach ($scriptPath in $ScriptPaths) {
    if (Test-Path $scriptPath) {
      Write-Host "Packing dependency via $scriptPath..."
      Invoke-PackScript -ScriptPath $scriptPath -Version $Version -StrongNameKeyFile $StrongNameKeyFile -StrongNamePublicKey $StrongNamePublicKey
    }
  }
}

# Return local project paths that should be packed before the solution.
# Example: includes src\Fmacias.TplQueue.Observers\Fmacias.TplQueue.Observers.csproj.
function Get-LocalProjects {
  param([string]$RepoRoot)

  return @(
    (Join-Path $RepoRoot 'src\Fmacias.TplQueue.Cache.Abstract\Fmacias.TplQueue.Cache.Abstract.csproj'),
    (Join-Path $RepoRoot 'src\Fmacias.TplQueue.Cache.MemCache\Fmacias.TplQueue.Cache.MemCache.csproj'),
    (Join-Path $RepoRoot 'src\Fmacias.TplQueue.Observers\Fmacias.TplQueue.Observers.csproj'),
    (Join-Path $RepoRoot 'src\Fmacias.TplQueue.RetryPolicies\Fmacias.TplQueue.RetryPolicies.csproj'),
    (Join-Path $RepoRoot 'src\Fmacias.TplQueue.Serialization.SystemTextJson\Fmacias.TplQueue.Serialization.SystemTextJson.csproj'),
    (Join-Path $RepoRoot 'src\Fmacias.TplQueue.Serialization.Xml\Fmacias.TplQueue.Serialization.Xml.csproj'),
    (Join-Path $RepoRoot 'src\Fmacias.TplQueue.Microsoft.DependencyInjection\Fmacias.TplQueue.Microsoft.DependencyInjection.csproj'),
    (Join-Path $RepoRoot 'src\Fmacias.TplQueue\Fmacias.TplQueue.csproj')
  )
}

# Pack each local project to the NuGet folder when it exists on disk.
# Example: Pack-LocalProjects -ProjectPaths (Get-LocalProjects -RepoRoot C:\repo\TplQueue.Adapter) -NugetRoot C:\repo\TplQueue.NugetLocal
function Pack-LocalProjects {
  param(
    [string[]]$ProjectPaths,
    [string]$NugetRoot,
    [string]$Version,
    [string]$StrongNameKeyFile,
    [string]$StrongNamePublicKey
  )

  foreach ($projectPath in $ProjectPaths) {
    if (Test-Path $projectPath) {
      Write-Host "Packing $projectPath to $NugetRoot..."
      $dotnetArgs = @(
        'pack',
        $projectPath,
        '-c', 'Release',
        '-o', $NugetRoot,
        '-p:SkipPackLocal=true',
        '-p:RestoreNoCache=true',
        '-p:RestoreForce=true'
      ) + (Get-PackProperties -Version $Version -StrongNameKeyFile $StrongNameKeyFile -StrongNamePublicKey $StrongNamePublicKey)

      Invoke-Dotnet -DotnetArgs $dotnetArgs
    }
  }
}

# Pick a packing target: prefer *.Pack.sln, fall back to first *.sln, else the repo root.
# Example: returns C:\repo\TplQueue.Adapter\TplQueue.Adapter.Pack.sln if it exists.
function Get-PackTarget {
  param([string]$RepoRoot)

  $packSolution = Get-ChildItem -Path $RepoRoot -Filter '*.Pack.sln' | Select-Object -First 1
  if (-not $packSolution) {
    $packSolution = Get-ChildItem -Path $RepoRoot -Filter '*.sln' | Select-Object -First 1
  }

  if ($packSolution) {
    return $packSolution.FullName
  }

  return $RepoRoot
}

# Run dotnet pack to generate nupkg files into the local NuGet folder.
# Example: Pack-Local -PackTarget C:\repo\TplQueue.Adapter\TplQueue.Adapter.Pack.sln -NugetRoot C:\repo\TplQueue.NugetLocal
function Pack-Local {
  param(
    [string]$PackTarget,
    [string]$NugetRoot,
    [string]$RepoRoot,
    [string]$Version,
    [string]$StrongNameKeyFile,
    [string]$StrongNamePublicKey
  )

  $nugetConfigPath = Join-Path $RepoRoot 'NuGet.config'
  if (-not (Test-Path $nugetConfigPath)) {
    throw "NuGet.config not found at: $nugetConfigPath"
  }

  Write-Host "Packing $PackTarget to $NugetRoot..."
  $dotnetArgs = @(
    'pack',
    $PackTarget,
    '-c', 'Release',
    '-o', $NugetRoot,
    '--configfile', $nugetConfigPath,
    '-p:SkipPackLocal=true',
    '-p:RestoreNoCache=true',
    '-p:RestoreForce=true'
  ) + (Get-PackProperties -Version $Version -StrongNameKeyFile $StrongNameKeyFile -StrongNamePublicKey $StrongNamePublicKey)

  Invoke-Dotnet -DotnetArgs $dotnetArgs
  Write-Host 'Local NuGet packages created successfully.'
}

# Orchestrate the steps needed to build local packages in a predictable order.
# Example: running the script in TplQueue.Adapter will pack dependencies first, then this repo.
function Main {
  $repoRoot = Get-RepoRoot
  if (-not [string]::IsNullOrWhiteSpace($Version)) {
    Write-Host "Coordinated package version: $Version"
  }
  if (-not [string]::IsNullOrWhiteSpace($StrongNameKeyFile)) {
    Write-Host 'Official strong-name signing: enabled'
  }

  $nugetRoot = Ensure-NugetLocal -RepoRoot $repoRoot
  Ensure-NugetSource -SourceName 'TplQueue.NugetLocal' -SourcePath $nugetRoot
  Clear-LocalNugetCache

  $dependencyScripts = Get-DependencyScripts -RepoRoot $repoRoot
  Pack-Dependencies -ScriptPaths $dependencyScripts -Version $Version -StrongNameKeyFile $StrongNameKeyFile -StrongNamePublicKey $StrongNamePublicKey

  $localProjects = Get-LocalProjects -RepoRoot $repoRoot
  Pack-LocalProjects -ProjectPaths $localProjects -NugetRoot $nugetRoot -Version $Version -StrongNameKeyFile $StrongNameKeyFile -StrongNamePublicKey $StrongNamePublicKey
}

try {
  Main
} catch {
  Write-Error $_.Exception.Message
  exit 1
}
