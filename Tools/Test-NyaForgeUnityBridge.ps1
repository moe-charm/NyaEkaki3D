[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PlayerCheckDirectory,
    [string]$UnityPath,
    [switch]$GraphFixtures,
    [switch]$SurfaceFixture,
    [switch]$ItemFixture,
    [switch]$RenderSurface,
    [switch]$MaterialFixture,
    [switch]$MultiMaterialFixture,
    [string]$ClothingPackageManifest,
    [ValidateRange(60, 7200)]
    [int]$TimeoutSeconds = 1200,
    [switch]$ShowLog
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$checkRoot = (Resolve-Path -LiteralPath $PlayerCheckDirectory).Path.TrimEnd('\', '/')
$playerReportPath = Join-Path $checkRoot 'report.json'
if (-not (Test-Path -LiteralPath $playerReportPath -PathType Leaf)) {
    throw "Player report was not found: $playerReportPath"
}
$playerReport = Get-Content -LiteralPath $playerReportPath -Raw | ConvertFrom-Json
if ($playerReport.passed -ne $true) { throw 'The Player verification report must be passed before testing its Unity Bridge output.' }
$bakeManifests = if ($GraphFixtures) { @($playerReport.graphBakeManifests) } else { @($playerReport.bakeManifests) }
if ($bakeManifests.Count -ne 2) { throw 'Expected exactly two Player bake manifests in scale 1, scale 100 order.' }
for ($i = 0; $i -lt $bakeManifests.Count; $i++) {
    if (-not ($bakeManifests[$i] -is [string]) -or -not [IO.Path]::IsPathRooted($bakeManifests[$i])) {
        throw 'Player bake manifest paths must be absolute paths.'
    }
    $bakeManifests[$i] = [IO.Path]::GetFullPath($bakeManifests[$i])
    if (-not $bakeManifests[$i].StartsWith($checkRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Bake manifest must be inside the selected Player check directory: $($bakeManifests[$i])"
    }
    if (-not (Test-Path -LiteralPath $bakeManifests[$i] -PathType Leaf)) {
        throw "Bake manifest does not exist: $($bakeManifests[$i])"
    }
}
if ($bakeManifests[0] -eq $bakeManifests[1]) { throw 'The scale fixtures must have distinct manifests.' }

# This is the installed compatibility-test target, not a claim about VRChat's current required version.
if (-not $UnityPath) {
    $UnityPath = Join-Path ${env:ProgramFiles} 'Unity\Hub\Editor\2022.3.22f1\Editor\Unity.exe'
}
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw 'Unity 2022.3.22f1 was not found. Install it or pass another supported Editor with -UnityPath.'
}
$UnityPath = (Resolve-Path -LiteralPath $UnityPath).Path
$productVersion = (Get-Item -LiteralPath $UnityPath).VersionInfo.ProductVersion
if ($productVersion -notmatch '^(\d+)\.(\d+)\.\d+[abfp]\d+') {
    throw "Cannot determine the Editor version from $UnityPath ($productVersion)."
}
$editorVersion = $Matches[0]
if ([int]$Matches[1] -lt 2022 -or ([int]$Matches[1] -eq 2022 -and [int]$Matches[2] -lt 3)) {
    throw 'The Bridge package requires Unity 2022.3 or later.'
}
$corePath = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Assets\NyaForge\Authoring'))
$bridgePath = [IO.Path]::GetFullPath((Join-Path $projectRoot 'UnityBridge'))
$renderingPath = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Assets\NyaForge\Rendering'))
foreach ($packagePath in @($corePath, $bridgePath, $renderingPath)) {
    if (-not (Test-Path -LiteralPath (Join-Path $packagePath 'package.json') -PathType Leaf)) {
        throw "Local package is missing: $packagePath"
    }
}

$artifactRoot = Join-Path $projectRoot 'Artifacts'
if (-not (Test-Path -LiteralPath $artifactRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $artifactRoot | Out-Null
}
$receiverName = 'BridgeReceiver-{0}-{1}' -f (Get-Date -Format 'yyyyMMdd-HHmmss-fff'), [Guid]::NewGuid().ToString('N')
$receiverPath = Join-Path $artifactRoot $receiverName
# No Force: an existing directory is never reused or rewritten. All results remain for inspection.
New-Item -ItemType Directory -Path $receiverPath | Out-Null
foreach ($directory in @('Assets', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Path (Join-Path $receiverPath $directory) | Out-Null
}
$manifest = [ordered]@{
    dependencies = [ordered]@{
        'com.nyaforge.authoring' = 'file:' + $corePath.Replace('\', '/')
        'com.nyaforge.unity-bridge' = 'file:' + $bridgePath.Replace('\', '/')
        'com.nyaforge.rendering' = 'file:' + $renderingPath.Replace('\', '/')
        'com.unity.nuget.newtonsoft-json' = '3.2.1'
        'com.unity.modules.imgui' = '1.0.0'
        'com.unity.modules.jsonserialize' = '1.0.0'
    }
}
$utf8 = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText((Join-Path $receiverPath 'Packages\manifest.json'), ($manifest | ConvertTo-Json -Depth 4), $utf8)
[IO.File]::WriteAllText((Join-Path $receiverPath 'ProjectSettings\ProjectVersion.txt'), "m_EditorVersion: $editorVersion`n", $utf8)
if ($MaterialFixture -or $MultiMaterialFixture) {
    [IO.File]::WriteAllText((Join-Path $receiverPath 'ProjectSettings\ProjectSettings.asset'), "%YAML 1.1`n%TAG !u! tag:unity3d.com,2011:`n--- !u!129 &1`nPlayerSettings:`n  m_ActiveColorSpace: 1`n", $utf8)
}
$logPath = Join-Path $receiverPath 'bridge.log'
$reportPath = Join-Path $receiverPath 'bridge-report.json'

# Start-Process combines ArgumentList; quote path tokens rather than constructing shell commands.
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"{0}"' -f $receiverPath),
    '-executeMethod', 'NyaForge.UnityBridge.Editor.BridgeBatch.VerifyRoundTrip',
    '--nyaforge-bake-scale1', ('"{0}"' -f $bakeManifests[0]),
    '--nyaforge-bake-scale100', ('"{0}"' -f $bakeManifests[1]),
    '--nyaforge-report', ('"{0}"' -f $reportPath), '-logFile', ('"{0}"' -f $logPath))
Write-Output "Receiver: $receiverPath"
if ($SurfaceFixture) {
    $surfacePath = Join-Path $checkRoot 'surface-export\surface.nyaforge-bake.json'
    if (-not (Test-Path -LiteralPath $surfacePath -PathType Leaf)) { throw "Surface fixture missing: $surfacePath" }
    $arguments += @('--nyaforge-surface', ('"{0}"' -f $surfacePath))
}
if ($ItemFixture) {
    if ($SurfaceFixture) { throw 'Choose SurfaceFixture or ItemFixture, not both.' }
    $itemExports = Join-Path $checkRoot 'item-project\exports'
    $itemManifests = @(Get-ChildItem -LiteralPath $itemExports -Recurse -Filter 'surface.nyaforge-bake.json' -File)
    if ($itemManifests.Count -ne 1) { throw 'Expected one exported item surface fixture.' }
    $arguments += @('--nyaforge-surface', ('"{0}"' -f $itemManifests[0].FullName))
}
Write-Output "Unity: $editorVersion"
if ($MaterialFixture) {
    $materialPath=Join-Path $checkRoot 'material-export\material.nyaforge-bake.json'
    if (-not (Test-Path -LiteralPath $materialPath -PathType Leaf)) { throw "Material fixture missing: $materialPath" }
    $arguments=@($arguments | Where-Object { $_ -ne '-nographics' })
    $arguments+=@('--nyaforge-material', ('"{0}"' -f $materialPath), '--nyaforge-material-image', ('"{0}"' -f (Join-Path $receiverPath 'material.png')))
}
if ($RenderSurface) {
    if (-not ($SurfaceFixture -or $ItemFixture)) { throw 'RenderSurface requires a surface or item fixture.' }
    $arguments = @($arguments | Where-Object { $_ -ne '-nographics' })
    $arguments += @('--nyaforge-surface-image', ('"{0}"' -f (Join-Path $receiverPath 'surface.png')))
}
if ($MultiMaterialFixture) {
    $multiPath=Join-Path $checkRoot 'multi-material-export\materials.nyaforge-bake.json'
    if (-not (Test-Path -LiteralPath $multiPath -PathType Leaf)) { throw "Multi-material fixture missing: $multiPath" }
    $arguments=@($arguments | Where-Object { $_ -ne '-nographics' })
    $arguments+=@('--nyaforge-materials', ('"{0}"' -f $multiPath))
}
if ($ClothingPackageManifest) {
    $clothingPackagePath = (Resolve-Path -LiteralPath $ClothingPackageManifest).Path
    if (-not (Test-Path -LiteralPath $clothingPackagePath -PathType Leaf)) { throw "Clothing package manifest missing: $clothingPackagePath" }
    $arguments += @('--nyaforge-clothing-package', ('"{0}"' -f $clothingPackagePath))
}
Write-Output "Log: $logPath"
Write-Output "Report: $reportPath"
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WorkingDirectory $receiverPath -WindowStyle Hidden -PassThru
$timer = [Diagnostics.Stopwatch]::StartNew()
while (-not $process.WaitForExit(1000)) {
    if ($timer.Elapsed.TotalSeconds -gt $TimeoutSeconds) {
        # Only the process launched by this script is stopped. Its project and logs are retained.
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        throw "Receiver verification exceeded $TimeoutSeconds seconds. See $logPath"
    }
}
$process.Refresh()
if ($ShowLog -and (Test-Path -LiteralPath $logPath -PathType Leaf)) { Get-Content -LiteralPath $logPath -Tail 100 }
if ($process.ExitCode -ne 0) { throw "Unity Bridge failed with exit code $($process.ExitCode). See $logPath" }
if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) { throw "Unity exited without a Bridge report. See $logPath" }
$bridgeReport = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if ($bridgeReport.status -ne 'passed') { throw "Unity Bridge verification did not pass: $($bridgeReport.error). See $reportPath" }
if ($bridgeReport.unityVersion -ne $editorVersion) { throw "Bridge report Editor version differs from the selected executable. See $reportPath" }
if (@($bridgeReport.checks).Count -lt 6) { throw "Bridge report is missing acceptance checks. See $reportPath" }
if (-not (Test-Path -LiteralPath $logPath -PathType Leaf) -or
    (Get-Content -LiteralPath $logPath -Raw) -notmatch 'NYAFORGE_BRIDGE_ROUNDTRIP_PASSED') {
    throw "Bridge success marker is missing. See $logPath"
}
Write-Output "Unity Bridge verification passed. Receiver: $receiverPath"
Write-Output "Report: $reportPath"
