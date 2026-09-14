[CmdletBinding()]
param(
    [ValidatePattern('^[A-Za-z0-9_-]+$')]
    [string]$BuildName = 'BoneSubsetV16',
    [switch]$Wait,
    [switch]$PrintOnly
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$playerPath = Join-Path $projectRoot "Builds\$BuildName\NyaForge.exe"
if (-not (Test-Path -LiteralPath $playerPath -PathType Leaf)) {
    throw "Windows Player was not found: $playerPath. Build it with Tools/Build-NyaForge.ps1 -Target Player -BuildName $BuildName."
}
$playerPath = (Resolve-Path -LiteralPath $playerPath).Path
Write-Output "Nya Ekaki 3D Authoring: $playerPath"
if ($PrintOnly) { return }

$process = Start-Process -FilePath $playerPath -ArgumentList @('--authoring', 'true') -WorkingDirectory (Split-Path -Parent $playerPath) -PassThru
Write-Output "Started PID $($process.Id)"
if ($Wait) {
    $process.WaitForExit()
    $process.Refresh()
    Write-Output "Exited with code $($process.ExitCode)"
    exit $process.ExitCode
}
