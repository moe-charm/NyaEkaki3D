[CmdletBinding()]
param(
    [string]$PointerPath = ''
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($PointerPath)) {
    $PointerPath = Join-Path $projectRoot 'GeneratedPacks\NyaForgeFixture\current.StandaloneWindows64.json'
}
$pointer = (Resolve-Path -LiteralPath $PointerPath -ErrorAction Stop).Path
$base = (Get-Item -LiteralPath $pointer).Directory.FullName

try { $reference = Get-Content -Raw -LiteralPath $pointer | ConvertFrom-Json }
catch { throw "Pointer JSON is invalid: $pointer" }

if ($reference.schemaVersion -ne 1) { throw "Unsupported pointer schemaVersion: $($reference.schemaVersion)" }
if ($reference.buildTarget -ne 'StandaloneWindows64') { throw "Pointer buildTarget is not StandaloneWindows64: $($reference.buildTarget)" }
foreach ($name in @('packId', 'revision', 'manifestPath', 'manifestSha256')) {
    if ([string]::IsNullOrWhiteSpace([string]$reference.$name)) { throw "Pointer field is missing: $name" }
}
if ($reference.manifestSha256 -notmatch '^[a-fA-F0-9]{64}$') { throw 'Pointer manifestSha256 is not a SHA-256 string.' }

$relativeManifest = [string]$reference.manifestPath
if ([IO.Path]::IsPathRooted($relativeManifest)) { throw 'Pointer manifestPath must be relative to the pointer directory.' }
$manifest = [IO.Path]::GetFullPath((Join-Path $base $relativeManifest))
$basePrefix = $base.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $manifest.StartsWith($basePrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Pointer manifestPath escapes its pointer directory.' }
if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) { throw "Pointer manifest is missing: $manifest" }

$actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $manifest).Hash.ToLowerInvariant()
if ($actualHash -ne ([string]$reference.manifestSha256).ToLowerInvariant()) {
    throw "Pointer manifest hash mismatch: expected $($reference.manifestSha256), actual $actualHash"
}
try { $manifestDocument = Get-Content -Raw -LiteralPath $manifest | ConvertFrom-Json }
catch { throw "Manifest JSON is invalid: $manifest" }
if ([string]$manifestDocument.packId -ne [string]$reference.packId) { throw 'Pointer packId does not match the manifest.' }
if ([string]$manifestDocument.revision -ne [string]$reference.revision) { throw 'Pointer revision does not match the manifest.' }

[pscustomobject]@{
    status = 'passed'
    pointerPath = $pointer
    manifestPath = $manifest
    packId = [string]$reference.packId
    revision = [string]$reference.revision
    manifestSha256 = $actualHash
} | ConvertTo-Json -Depth 3
