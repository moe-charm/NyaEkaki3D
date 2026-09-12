[CmdletBinding()]
param(
    [ValidateSet('All', 'Fixture', 'Player')]
    [string]$Target = 'All',
    [string]$UnityPath,
    [ValidatePattern('^[A-Za-z0-9_-]+$')][string]$BuildName = 'Windows',
    [switch]$ShowLog
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ($Target -in @('All', 'Player')) {
    $playerOutput = Join-Path $projectRoot "Builds\$BuildName\NyaForge.exe"
    $runningTarget = @(Get-Process -Name NyaForge -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $playerOutput })
    if ($runningTarget.Count -gt 0) { throw "The target Player is running. Keep it open and choose another -BuildName, or close it before replacing $playerOutput" }
}
$versionPath = Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'
$versionLine = Get-Content -LiteralPath $versionPath | Where-Object { $_ -match '^m_EditorVersion: ' } | Select-Object -First 1
if (-not $versionLine) { throw "Cannot determine the Unity version from $versionPath" }
$editorVersion = $versionLine.Substring('m_EditorVersion: '.Length).Trim()

if (-not $UnityPath) {
    $UnityPath = Join-Path ${env:ProgramFiles} "Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
}
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity $editorVersion was not found. Install that Editor with Windows Build Support, or pass -UnityPath."
}
$UnityPath = (Resolve-Path -LiteralPath $UnityPath).Path
$installedVersion = (Get-Item -LiteralPath $UnityPath).VersionInfo.ProductVersion
if ($installedVersion -notlike "$editorVersion*") {
    throw "Expected Unity $editorVersion; executable reports $installedVersion."
}
if (Test-Path -LiteralPath (Join-Path $projectRoot 'Temp\UnityLockfile')) {
    throw 'This project appears open in Unity. Close its Editor before using the batch build.'
}

$logDirectory = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logPath = Join-Path $logDirectory ("build-{0}-{1}.log" -f $Target.ToLowerInvariant(), (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
$method = @{ All = 'BuildAll'; Fixture = 'BuildFixture'; Player = 'BuildPlayer' }[$Target]
$methodName = "NyaForge.EditorTools.NyaForgeBuild.$method"
# All arguments are fixed tokens or validated paths. Windows paths cannot contain a quote.
# Quote the project/log paths because Start-Process joins ArgumentList into one command line.
$arguments = @('-batchmode', '-quit', '-projectPath', ('"{0}"' -f $projectRoot),
    '-buildTarget', 'Win64', '-executeMethod', $methodName, '--nyaforge-build-name', $BuildName, '-logFile', ('"{0}"' -f $logPath))
Write-Output "Unity $editorVersion / $Target"
Write-Output "Log: $logPath"
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru
$process.WaitForExit()
$process.Refresh()
if ($ShowLog -and (Test-Path -LiteralPath $logPath)) { Get-Content -LiteralPath $logPath -Tail 100 }
if ($process.ExitCode -ne 0) { throw "Unity failed with exit code $($process.ExitCode). See $logPath" }
if (-not (Test-Path -LiteralPath $logPath)) { throw 'Unity exited without producing its requested build log.' }
$logText = Get-Content -LiteralPath $logPath -Raw
if ($Target -in @('All', 'Fixture') -and $logText -notmatch 'NYAFORGE_FIXTURE_OK ') { throw "Fixture success marker missing. See $logPath" }
if ($Target -in @('All', 'Player') -and $logText -notmatch 'NYAFORGE_PLAYER_OK ') { throw "Player success marker missing. See $logPath" }
Write-Output "Build complete. Log: $logPath"
if ($Target -in @('All', 'Player')) { Write-Output (Join-Path $projectRoot "Builds\$BuildName\NyaForge.exe") }
if ($Target -in @('All', 'Fixture')) { Write-Output (Join-Path $projectRoot 'GeneratedPacks\NyaForgeFixture') }
