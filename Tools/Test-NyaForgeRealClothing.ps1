[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ModelPath,
    [ValidatePattern('^[A-Za-z0-9_-]+$')]
    [string]$BuildName = 'PerformanceV39',
    [string]$UnityPath,
    [ValidateRange(60, 1800)]
    [int]$TimeoutSeconds = 900
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$model = (Resolve-Path -LiteralPath $ModelPath -ErrorAction Stop).Path
if ([IO.Path]::GetExtension($model) -notin @('.glb', '.vrm')) {
    throw 'ModelPath must point to a .glb or .vrm file.'
}

# Keep private model bytes outside this public repository. The Player receives
# the source path and writes only derived verification artifacts below Artifacts.
$authoringArgs = @{
    BuildName = $BuildName
    ImportModel = $model
    ImportAllModel = $true
    VrmExport = $true
    TimeoutSeconds = $TimeoutSeconds
}
$authoringOutput = @(& (Join-Path $PSScriptRoot 'Test-NyaForgeAuthoring.ps1') @authoringArgs 2>&1)
$authoringOutput | ForEach-Object { Write-Output $_ }
$checkLine = $authoringOutput | Where-Object { $_ -match '^Authoring check:\s*' } | Select-Object -First 1
if (-not $checkLine) { throw 'Authoring check directory was not reported.' }
$checkRoot = ($checkLine -replace '^Authoring check:\s*', '').Trim()
$checkRoot = (Resolve-Path -LiteralPath $checkRoot -ErrorAction Stop).Path

$packageManifests = @(Get-ChildItem -LiteralPath $checkRoot -Recurse -File -Filter 'skinned-clothing.nyaforge.json')
if ($packageManifests.Count -ne 1) {
    throw "Expected exactly one clothing package manifest under $checkRoot; found $($packageManifests.Count)."
}
$packageDirectory = $packageManifests[0].Directory.FullName
$skeletonPath = Join-Path $packageDirectory 'skeleton.nyaforge.bin'
if (-not (Test-Path -LiteralPath $skeletonPath -PathType Leaf)) {
    throw "Clothing package skeleton sidecar is missing: $skeletonPath"
}
$skeletonBytes = [IO.File]::ReadAllBytes($skeletonPath)
if ($skeletonBytes.Length -lt 12) { throw 'Clothing package skeleton sidecar is truncated.' }
$skeletonMagic = [BitConverter]::ToInt32($skeletonBytes, 0)
$skeletonVersion = [BitConverter]::ToInt32($skeletonBytes, 4)
$skeletonBoneCount = [BitConverter]::ToInt32($skeletonBytes, 8)
if ($skeletonMagic -ne 0x5359524e -or $skeletonVersion -ne 1 -or $skeletonBoneCount -lt 1 -or $skeletonBoneCount -gt 256) {
    throw "Clothing package skeleton sidecar header is invalid (magic=$skeletonMagic version=$skeletonVersion bones=$skeletonBoneCount)."
}
Write-Output "Clothing skeleton: $skeletonBoneCount bones"
$bridgeArgs = @{
    PlayerCheckDirectory = $checkRoot
    ClothingPackageManifest = $packageManifests[0].FullName
    TimeoutSeconds = $TimeoutSeconds
}
if ($UnityPath) { $bridgeArgs.UnityPath = $UnityPath }
& (Join-Path $PSScriptRoot 'Test-NyaForgeUnityBridge.ps1') @bridgeArgs

Write-Output "Real clothing acceptance passed."
Write-Output "Player report: $(Join-Path $checkRoot 'report.json')"
Write-Output "Clothing package: $($packageManifests[0].FullName)"
