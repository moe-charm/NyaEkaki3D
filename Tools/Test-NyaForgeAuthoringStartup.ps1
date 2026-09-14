[CmdletBinding()]
param(
    [ValidateRange(800, 3840)][int]$Width = 1600,
    [ValidateRange(600, 2160)][int]$Height = 1000,
    [ValidatePattern('^[A-Za-z0-9_-]+$')][string]$BuildName = 'Windows',
    [ValidateRange(30, 300)][int]$TimeoutSeconds = 120
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$playerPath = Join-Path $projectRoot "Builds\$BuildName\NyaForge.exe"
if (-not (Test-Path -LiteralPath $playerPath -PathType Leaf)) { throw 'Build the Windows Player first with Tools/Build-NyaForge.ps1.' }
$runningPlayer = @(Get-Process -Name NyaForge -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and [IO.Path]::GetFullPath($_.Path) -eq $playerPath })
if ($runningPlayer.Count -gt 0) { throw "The Player is already running for BuildName '$BuildName'. Close PID $($runningPlayer[0].Id) first." }
$checkDirectory = Join-Path $projectRoot ('Artifacts\AuthoringStartup-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $checkDirectory | Out-Null
$logPath = Join-Path $checkDirectory 'player.log'
$arguments = @('--startup-empty', 'true', '--authoring', 'true', '--authoring-startup-output', ('"{0}"' -f $checkDirectory),
    '-screen-width', $Width, '-screen-height', $Height, '-screen-fullscreen', '0', '-logFile', ('"{0}"' -f $logPath))
Write-Output "Authoring startup probe: $checkDirectory"
$process = Start-Process -FilePath $playerPath -ArgumentList $arguments -PassThru
if (-not $process.WaitForExit($TimeoutSeconds * 1000)) { $process.Kill(); throw "Verification timed out: $logPath" }
$reportPath = Join-Path $checkDirectory 'report.json'
if (-not (Test-Path -LiteralPath $reportPath)) { throw "Report missing: $logPath" }
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if (-not $report.passed) { throw "Authoring startup probe failed: $($report.failures -join '; ')" }
if (-not (Test-Path -LiteralPath $report.screenshot -PathType Leaf)) { throw 'Verification image missing.' }
Write-Output "PASS: $reportPath"
Write-Output "Image: $($report.screenshot)"
