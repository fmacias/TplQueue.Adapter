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
  
  Write-Host "---> Nuget root1  $nugetRoot..."

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
# Example: includes ..\TplQueue.Abstractions\pack-local.ps1 and ..\TplQueue.Cache.Abstract\pack-local.ps1.
function Get-DependencyScripts {
  param([string]$RepoRoot)

  return @(
    (Join-Path $RepoRoot '..\TplQueue.Abstractions\pack-local.ps1')
  )
}

# Execute dependency pack-local scripts when they exist.
# Example: Pack-Dependencies -ScriptPaths (Get-DependencyScripts -RepoRoot C:\repo\TplQueue.Adapter)
function Pack-Dependencies {
  param([string[]]$ScriptPaths)

  foreach ($scriptPath in $ScriptPaths) {
    if (Test-Path $scriptPath) {
      Write-Host "Packing dependency via $scriptPath..."
      & powershell -NoProfile -ExecutionPolicy Bypass -File $scriptPath
      if ($LASTEXITCODE -ne 0) {
        throw "Dependency pack-local failed: $scriptPath"
      }
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
    (Join-Path $RepoRoot 'src\Fmacias.TplQueue.Microsoft.DependencyInjection\Fmacias.TplQueue.Microsoft.DependencyInjection.csproj')
  )
}

# Pack each local project to the NuGet folder when it exists on disk.
# Example: Pack-LocalProjects -ProjectPaths (Get-LocalProjects -RepoRoot C:\repo\TplQueue.Adapter) -NugetRoot C:\repo\TplQueue.NugetLocal
function Pack-LocalProjects {
  param(
    [string[]]$ProjectPaths,
    [string]$NugetRoot
  )

  foreach ($projectPath in $ProjectPaths) {
  Write-Host "---> ---> Project Path  $projectPath..."
  Write-Host "---> ---> Project Path  $NugetRoot..."

    if (Test-Path $projectPath) {
      Write-Host "Packing $projectPath to $NugetRoot..."
      Invoke-Dotnet -DotnetArgs @(
        'pack',
        $projectPath,
        '-c', 'Release',
        '-o', $NugetRoot,
        '-p:SkipPackLocal=true',
        '-p:RestoreNoCache=true',
        '-p:RestoreForce=true'
      )
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
    [string]$RepoRoot
  )

  $nugetConfigPath = Join-Path $RepoRoot 'NuGet.config'
  if (-not (Test-Path $nugetConfigPath)) {
    throw "NuGet.config not found at: $nugetConfigPath"
  }

  Write-Host "Packing $PackTarget to $NugetRoot..."
  Invoke-Dotnet -DotnetArgs @(
    'pack',
    $PackTarget,
    '-c', 'Release',
    '-o', $NugetRoot,
    '--configfile', $nugetConfigPath,
    '-p:SkipPackLocal=true',
    '-p:RestoreNoCache=true',
    '-p:RestoreForce=true'
  )
  Write-Host 'Local NuGet packages created successfully.'
}

# Orchestrate the steps needed to build local packages in a predictable order.
# Example: running the script in TplQueue.Adapter will pack dependencies first, then this repo.
function Main {
  $repoRoot = Get-RepoRoot
  $nugetRoot = Ensure-NugetLocal -RepoRoot $repoRoot
  Ensure-NugetSource -SourceName 'TplQueue.NugetLocal' -SourcePath $nugetRoot
  Clear-LocalNugetCache

  $dependencyScripts = Get-DependencyScripts -RepoRoot $repoRoot
  Pack-Dependencies -ScriptPaths $dependencyScripts

  
  $localProjects = Get-LocalProjects -RepoRoot $repoRoot
  Pack-LocalProjects -ProjectPaths $localProjects -NugetRoot $nugetRoot
   
  Write-Host 'Pack local Project success'

  $packTarget = Get-PackTarget -RepoRoot $repoRoot

  Write-Host "Packing local not working $PackTarget to $NugetRoot..."  
  Pack-Local -PackTarget $packTarget -NugetRoot $nugetRoot -RepoRoot $repoRoot
  Write-Host 'Pack Local success.'

}

try {
  Main
} catch {
  Write-Error $_.Exception.Message
  exit 1
}
