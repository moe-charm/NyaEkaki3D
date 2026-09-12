[CmdletBinding()]
param([ValidateRange(1000,3840)][int]$Width = 1280, [ValidateRange(700,2160)][int]$Height = 800,
    [ValidatePattern('^[A-Za-z0-9_-]+$')][string]$BuildName = 'Windows')
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$playerPath = Join-Path $projectRoot "Builds\$BuildName\NyaForge.exe"
$packPath = Join-Path $projectRoot 'GeneratedPacks\NyaForgeFixture\current.StandaloneWindows64.json'
if (-not (Test-Path -LiteralPath $packPath)) { throw 'Build the public fixture first.' }
$checkDirectory = Join-Path $projectRoot ('Artifacts\Navigation-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $checkDirectory | Out-Null
$settingsPath = Join-Path $checkDirectory 'settings'
$logPath = Join-Path $checkDirectory 'player.log'
$arguments = @('--startup-empty', 'true', '--navigation-check-output', ('"{0}"' -f $checkDirectory),
    '--navigation-pack', ('"{0}"' -f $packPath), '--settings-root', ('"{0}"' -f $settingsPath),
    '-screen-width', $Width, '-screen-height', $Height, '-screen-fullscreen', '0', '-logFile', ('"{0}"' -f $logPath))
Write-Output "Navigation check: $checkDirectory"
$checkProcess = Start-Process -FilePath $playerPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
if (-not $checkProcess.WaitForExit(60000)) { $checkProcess.Kill(); throw "Verification timed out: $logPath" }
$reportPath = Join-Path $checkDirectory 'report.json'
if (-not (Test-Path -LiteralPath $reportPath)) { throw "Report missing: $logPath" }
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if (-not $report.passed) { throw "Verification failed: $($report.failures -join '; ')" }
Write-Output "PASS: $reportPath"
