param(
    [string]$SkillsRoot = (Join-Path $env:USERPROFILE '.agents\skills')
)

$ErrorActionPreference = 'Stop'
$sourceRoot = Join-Path $PSScriptRoot 'skills'
$skillNames = @('tplqueue-implement', 'tplqueue-review', 'tplqueue-refactor')
$destinations = @()

# Preflight every destination before changing anything. Never replace a user's skill.
foreach ($skillName in $skillNames) {
    $source = (Resolve-Path -LiteralPath (Join-Path $sourceRoot $skillName)).Path
    if (-not (Test-Path -LiteralPath (Join-Path $source 'SKILL.md') -PathType Leaf)) {
        throw "Missing SKILL.md in $source"
    }
    $destination = [IO.Path]::GetFullPath((Join-Path $SkillsRoot $skillName))
    $existing = Get-Item -LiteralPath $destination -Force -ErrorAction SilentlyContinue
    if ($existing) {
        if ($existing.LinkType -notin @('Junction', 'SymbolicLink') -or
            @($existing.Target).Count -ne 1 -or
            [IO.Path]::GetFullPath([string]$existing.Target[0]) -ne $source) {
            throw "Existing skill destination differs; left untouched: $destination"
        }
    }
    $destinations += [pscustomobject]@{ Source = $source; Path = $destination; Exists = [bool]$existing }
}

New-Item -ItemType Directory -Path $SkillsRoot -Force | Out-Null
foreach ($destination in $destinations) {
    if (-not $destination.Exists) {
        New-Item -ItemType Junction -Path $destination.Path -Target $destination.Source | Out-Null
    }
    $sourceHash = (Get-FileHash -LiteralPath (Join-Path $destination.Source 'SKILL.md')).Hash
    $installedHash = (Get-FileHash -LiteralPath (Join-Path $destination.Path 'SKILL.md')).Hash
    if ($sourceHash -ne $installedHash) { throw "Installed skill mismatch: $($destination.Path)" }
    Write-Output "Available: $($destination.Path) -> $($destination.Source)"
}
