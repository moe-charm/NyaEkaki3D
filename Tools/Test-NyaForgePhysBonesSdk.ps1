[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$UnityProjectPath,
    [string]$TargetManifestPath,
    [string]$OutputPath,
    [switch]$RequireSdk
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path -LiteralPath $UnityProjectPath -ErrorAction Stop).Path.TrimEnd('\', '/')
$manifestPath = Join-Path $project 'Packages\manifest.json'
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$report = [ordered]@{
    schemaVersion = 1
    checkedAtUtc = [DateTime]::UtcNow.ToString('o')
    unityProject = $project
    targetManifest = $null
    status = 'unavailable'
    sdkPackageIds = @()
    componentTypeName = $null
    componentMatches = @()
    diagnostics = @()
}

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    $report.status = 'invalid_project'
    $report.diagnostics += 'Packages/manifest.json was not found.'
}
else {
    try {
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        $dependencies = @($manifest.dependencies.PSObject.Properties)
        $sdkIds = @($dependencies | Where-Object { $_.Name -match '^com\.vrchat\.' } | Select-Object -ExpandProperty Name)
        $report.sdkPackageIds = $sdkIds
        if ($sdkIds.Count -eq 0) { $report.diagnostics += 'No com.vrchat.* dependency is declared in Packages/manifest.json.' }
    }
    catch {
        $report.status = 'invalid_project'
        $report.diagnostics += 'Packages/manifest.json could not be parsed: ' + $_.Exception.Message
    }
}

if ($TargetManifestPath) {
    $target = (Resolve-Path -LiteralPath $TargetManifestPath -ErrorAction Stop).Path
    $report.targetManifest = $target
    try {
        $targetJson = Get-Content -LiteralPath $target -Raw | ConvertFrom-Json
        $report.componentTypeName = [string]$targetJson.ComponentTypeName
        if ([string]::IsNullOrWhiteSpace($report.componentTypeName)) {
            $report.diagnostics += 'Target manifest does not specify ComponentTypeName; receiver compatibility fallback may be used.'
        }
    }
    catch {
        $report.status = 'invalid_target'
        $report.diagnostics += 'Target manifest could not be parsed: ' + $_.Exception.Message
    }
}

# Search only Unity-owned source/package locations. This is a read-only probe;
# it never imports packages or edits the project. Use VRChat-specific names so
# NyaForge's own PhysBones fixture/bridge files cannot count as an SDK signal.
$searchRoots = @(
    (Join-Path $project 'Assets'),
    (Join-Path $project 'Packages'),
    (Join-Path $project 'Library\PackageCache')
) | Where-Object { Test-Path -LiteralPath $_ -PathType Container }
$componentFiles = New-Object System.Collections.Generic.List[string]
foreach ($root in $searchRoots) {
    $files = Get-ChildItem -LiteralPath $root -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match 'VRCPhysBone|VRCSDK|VRChat' } |
        Select-Object -First 64
    foreach ($file in $files) {
        $componentFiles.Add($file.FullName.Substring($project.Length).TrimStart('\', '/'))
    }
}
$report.componentMatches = @($componentFiles | Sort-Object -Unique)

if ($report.status -eq 'unavailable' -and $report.sdkPackageIds.Count -gt 0 -and $report.componentMatches.Count -gt 0) {
    $report.status = 'candidate_found'
    $report.diagnostics += 'VRChat package and PhysBone-named files were found. Run the Unity receiver preflight to validate the exact component type and member shape.'
}
elseif ($report.status -eq 'unavailable') {
    $report.diagnostics += 'Install the VRChat SDK through the project package workflow, then rerun this read-only probe.'
}

$json = $report | ConvertTo-Json -Depth 5
if ($OutputPath) {
    $output = [IO.Path]::GetFullPath($OutputPath)
    $parent = Split-Path -Parent $output
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) { New-Item -ItemType Directory -Path $parent | Out-Null }
    [IO.File]::WriteAllText($output, $json, [Text.UTF8Encoding]::new($false))
    Write-Output "Report: $output"
}
else { Write-Output $json }

if ($RequireSdk -and $report.status -notin @('candidate_found')) {
    throw "VRChat PhysBone SDK was not found or could not be identified. Status: $($report.status)"
}
