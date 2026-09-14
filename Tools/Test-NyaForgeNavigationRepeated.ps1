[CmdletBinding()]
param(
    [ValidateRange(1, 500)][int]$Count = 50,
    [ValidateRange(1000, 3840)][int]$Width = 1280,
    [ValidateRange(700, 2160)][int]$Height = 800,
    [ValidatePattern('^[A-Za-z0-9_-]+$')][string]$BuildName = 'Windows',
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$singleRunScript = Join-Path $PSScriptRoot 'Test-NyaForgeNavigation.ps1'
if (-not (Test-Path -LiteralPath $singleRunScript)) {
    throw "Navigation test script was not found: $singleRunScript"
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $projectRoot ("Artifacts\Navigation-Repeated-{0}-{1}.json" -f $BuildName, (Get-Date -Format 'yyyyMMdd-HHmmss'))
} elseif (-not [IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $projectRoot $OutputPath
}

$cycles = [System.Collections.Generic.List[object]]::new()
for ($cycle = 1; $cycle -le $Count; $cycle++) {
    $output = @(& powershell -NoProfile -ExecutionPolicy Bypass -File $singleRunScript `
        -BuildName $BuildName -Width $Width -Height $Height 2>&1)
    $passLine = $output | Where-Object { $_.ToString() -match '^PASS: ' } | Select-Object -Last 1
    if (-not $passLine) {
        throw "Navigation cycle $cycle/$Count failed: $($output -join ' ')"
    }

    $reportPath = $passLine.ToString().Substring(6)
    $cycles.Add([pscustomobject]@{
            cycle = $cycle
            pass = $true
            report = $reportPath
        })
    Write-Output "cycle $cycle/$Count PASS"
}

$parent = Split-Path -Parent $OutputPath
if (-not (Test-Path -LiteralPath $parent)) {
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
}

[pscustomobject]@{
    buildName = $BuildName
    width = $Width
    height = $Height
    count = $cycles.Count
    passed = ($cycles.Count -eq $Count)
    cycles = $cycles
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $OutputPath -Encoding utf8

Write-Output "WROTE: $OutputPath"
