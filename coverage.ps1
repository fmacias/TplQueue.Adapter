param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$EnforceBaseline,
    [switch]$NoBuild,
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$settingsPath = Join-Path $root "coverage.runsettings"
$baselinePath = Join-Path $root "coverage-baseline.json"
$artifactsRoot = Join-Path $root "artifacts\coverage"
$rawRoot = Join-Path $artifactsRoot "raw"
$reportRoot = Join-Path $artifactsRoot "reports"
$htmlRoot = Join-Path $artifactsRoot "html"
$summaryPath = Join-Path $artifactsRoot "coverage-summary.json"

function Invoke-Dotnet {
    param([string[]]$DotnetArgs)

    & dotnet @DotnetArgs | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($DotnetArgs -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Get-Baselines {
    if (-not (Test-Path $baselinePath)) {
        return @{}
    }

    $result = @{}
    $json = Get-Content -Path $baselinePath -Raw | ConvertFrom-Json
    if ($json.lineCoverageThresholds) {
        foreach ($property in $json.lineCoverageThresholds.PSObject.Properties) {
            $result[$property.Name] = [double]$property.Value
        }
    }

    return $result
}

function Get-LineRatePercent {
    param([string]$CoverageFilePath)

    [xml]$coverage = Get-Content -Path $CoverageFilePath
    return [Math]::Round(([double]$coverage.coverage.'line-rate') * 100, 2)
}

function Resolve-ReportGeneratorTool {
    $candidateRoots = @(
        $root,
        (Join-Path $root "..\WorkspaceTplQueue")
    ) | ForEach-Object {
        try {
            (Resolve-Path -Path $_ -ErrorAction Stop).Path
        }
        catch {
            $null
        }
    } | Where-Object { $_ } | Select-Object -Unique

    foreach ($candidateRoot in $candidateRoots) {
        if (Test-Path (Join-Path $candidateRoot ".config\dotnet-tools.json")) {
            return [pscustomobject]@{
                Kind = "LocalTool"
                Root = $candidateRoot
            }
        }
    }

    $command = Get-Command reportgenerator -ErrorAction SilentlyContinue
    if ($command) {
        return [pscustomobject]@{
            Kind = "Command"
            Path = $command.Source
        }
    }

    return $null
}

function Invoke-ReportGenerator {
    param(
        [string[]]$CoverageReports,
        [string]$TargetDirectory,
        [switch]$AllowToolRestore
    )

    $tool = Resolve-ReportGeneratorTool
    if (-not $tool) {
        Write-Warning "ReportGenerator is not available. Restore the workspace local tool from WorkspaceTplQueue or install reportgenerator globally to produce HTML coverage reports."
        return $null
    }

    New-Item -ItemType Directory -Path $TargetDirectory -Force | Out-Null
    $resolvedReports = $CoverageReports | ForEach-Object { (Resolve-Path -Path $_).Path }
    $toolArgs = @(
        "-reports:$([string]::Join(';', $resolvedReports))",
        "-targetdir:$TargetDirectory",
        "-reporttypes:Html"
    )

    try {
        if ($tool.Kind -eq "LocalTool") {
            Push-Location $tool.Root
            try {
                if ($AllowToolRestore) {
                    Invoke-Dotnet -DotnetArgs @("tool", "restore")
                }

                Invoke-Dotnet -DotnetArgs (@("tool", "run", "reportgenerator", "--") + $toolArgs)
            }
            finally {
                Pop-Location
            }
        }
        else {
            & $tool.Path @toolArgs | Out-Host
            if ($LASTEXITCODE -ne 0) {
                throw "reportgenerator failed with exit code $LASTEXITCODE."
            }
        }

        return (Join-Path $TargetDirectory "index.html")
    }
    catch {
        Write-Warning "HTML coverage report generation failed: $($_.Exception.Message)"
        return $null
    }
}

$targets = @(
@{ Name = "Fmacias.TplQueue"; Project = Join-Path $root "test\Fmacias.TplQueue.Unit.Test\Fmacias.TplQueue.Test.csproj" },
    @{ Name = "Fmacias.TplQueue.Cache.Abstract"; Project = Join-Path $root "test\Fmacias.TplQueue.Cache.Abstract.Test\Fmacias.TplQueue.Cache.Abstract.Test.csproj" },
    @{ Name = "Fmacias.TplQueue.Cache.MemCache"; Project = Join-Path $root "test\Fmacias.TplQueue.Cache.MemCache.Test\Fmacias.TplQueue.Cache.MemCache.Test.csproj" },
    @{ Name = "Fmacias.TplQueue.Microsoft.DependencyInjection"; Project = Join-Path $root "test\Fmacias.TplQueue.Microsoft.DependencyInjection.Unit.Test\Fmacias.TplQueue.Microsoft.DependencyInjection.Unit.Test.csproj" },
    @{ Name = "Fmacias.TplQueue.Observers"; Project = Join-Path $root "test\Fmacias.TplQueue.Observers.Unit.Test\Fmacias.TplQueue.Observers.Unit.Test.csproj" },
    @{ Name = "Fmacias.TplQueue.RetryPolicies"; Project = Join-Path $root "test\Fmacias.TplQueue.RetryPolicies.Unit.Test\Fmacias.TplQueue.RetryPolicies.Test.csproj" },
    @{ Name = "Fmacias.TplQueue.Serialization.SystemTextJson"; Project = Join-Path $root "test\Fmacias.TplQueue.Serialization.SystemTextJson.Unit.Test\Fmacias.TplQueue.Serialization.SystemTextJson.Unit.Test.csproj" },
    @{ Name = "Fmacias.TplQueue.Serialization.Xml"; Project = Join-Path $root "test\Fmacias.TplQueue.Serialization.Xml.Unit.Test\Fmacias.TplQueue.Serialization.Xml.Unit.Test.csproj" }
)

$baselines = Get-Baselines
$summary = @()

Remove-Item -Path $artifactsRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $rawRoot -Force | Out-Null
New-Item -ItemType Directory -Path $reportRoot -Force | Out-Null

foreach ($target in $targets) {
    $targetRawRoot = Join-Path $rawRoot $target.Name
    Remove-Item -Path $targetRawRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $targetRawRoot -Force | Out-Null

    $dotnetArgs = @(
        "test",
        $target.Project,
        "--configuration", $Configuration,
        "--settings", $settingsPath,
        "--collect:XPlat Code Coverage",
        "--results-directory", $targetRawRoot
    )

    if ($NoBuild) {
        $dotnetArgs += "--no-build"
    }

    if ($NoRestore) {
        $dotnetArgs += "--no-restore"
    }

    Invoke-Dotnet -DotnetArgs $dotnetArgs

    $coverageFile = Get-ChildItem -Path $targetRawRoot -Filter "coverage.cobertura.xml" -Recurse | Select-Object -First 1
    if (-not $coverageFile) {
        throw "Coverage output was not produced for target '$($target.Name)'."
    }

    $reportPath = Join-Path $reportRoot ($target.Name + ".cobertura.xml")
    Copy-Item -Path $coverageFile.FullName -Destination $reportPath -Force
    $lineRate = Get-LineRatePercent -CoverageFilePath $reportPath

    if ($EnforceBaseline -and $baselines.ContainsKey($target.Name) -and $lineRate -lt $baselines[$target.Name]) {
        throw "Coverage for $($target.Name) dropped to $lineRate%, below the accepted baseline of $($baselines[$target.Name])%."
    }

    $summary += [pscustomobject]@{
        Name = $target.Name
        LineRate = $lineRate
        ReportPath = $reportPath
    }
}

$htmlReportPath = Invoke-ReportGenerator -CoverageReports $summary.ReportPath -TargetDirectory $htmlRoot -AllowToolRestore:(-not $NoRestore)
foreach ($entry in $summary) {
    $entry | Add-Member -NotePropertyName HtmlReportPath -NotePropertyValue $htmlReportPath
}

$summary | ConvertTo-Json -Depth 3 | Set-Content -Path $summaryPath -Encoding UTF8
$summary | Format-Table -AutoSize | Out-String | Write-Host
Write-Host "Coverage summary written to $summaryPath"
if ($htmlReportPath) {
    Write-Host "HTML coverage report written to $htmlReportPath"
}
