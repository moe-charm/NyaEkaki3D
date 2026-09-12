[CmdletBinding()]
param(
    [ValidateRange(800, 3840)][int]$Width = 1600,
    [ValidateRange(600, 2160)][int]$Height = 1000,
    [ValidatePattern('^[A-Za-z0-9_-]+$')][string]$BuildName = 'Windows',
    [string]$ReopenProject,
    [switch]$DensePaint,
    [string]$McpProbe,
    [ValidateRange(30,900)][int]$TimeoutSeconds = 120
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$playerPath = Join-Path $projectRoot "Builds\$BuildName\NyaForge.exe"
if (-not (Test-Path -LiteralPath $playerPath -PathType Leaf)) { throw 'Build the Windows Player first with Tools/Build-NyaForge.ps1.' }
$checkDirectory = Join-Path $projectRoot ('Artifacts\Authoring-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $checkDirectory | Out-Null
$logPath = Join-Path $checkDirectory 'player.log'
$arguments = @('--startup-empty', 'true', '--authoring-check-output', ('"{0}"' -f $checkDirectory),
    '-screen-width', $Width, '-screen-height', $Height, '-screen-fullscreen', '0', '-logFile', ('"{0}"' -f $logPath))
Write-Output "Authoring check: $checkDirectory"
if ($DensePaint) { $arguments += '--authoring-dense-paint' }
if ($McpProbe) { $arguments += @('--authoring-mcp-probe', ('"{0}"' -f (Resolve-Path -LiteralPath $McpProbe).Path)) }
if ($ReopenProject) {
    $reopenPath = (Resolve-Path -LiteralPath $ReopenProject).Path
    $arguments += @('--authoring-reopen-project', ('"{0}"' -f $reopenPath))
}
$checkProcess = Start-Process -FilePath $playerPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
if (-not $checkProcess.WaitForExit($TimeoutSeconds * 1000)) {
    # Terminate only this script's verification process, never other viewer instances.
    $checkProcess.Kill()
    throw "Authoring verification timed out. See $logPath"
}
$checkProcess.Refresh()
if ($checkProcess.ExitCode -ne 0) { throw "Player exited with code $($checkProcess.ExitCode). See $logPath" }
$reportPath = Join-Path $checkDirectory 'report.json'
if (-not (Test-Path -LiteralPath $reportPath)) { throw "Verification report missing. See $logPath" }
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if (-not $report.passed) { throw "Authoring verification failed: $($report.failure)" }
if (-not (Test-Path -LiteralPath $report.screenshot -PathType Leaf)) { throw 'Verification image missing.' }
Write-Output "PASS: $reportPath"
Write-Output "Image: $($report.screenshot)"
Write-Output 'Use this check directory with Tools/Test-NyaForgeUnityBridge.ps1 -PlayerCheckDirectory.'
