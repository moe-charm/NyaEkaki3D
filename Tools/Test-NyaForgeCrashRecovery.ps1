[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$PlayerCheckDirectory,
    [string]$UnityPath=(Join-Path ${env:ProgramFiles} 'Unity\Hub\Editor\2022.3.22f1\Editor\Unity.exe'),
    [ValidateRange(60,1200)][int]$TimeoutSeconds=300
)
$ErrorActionPreference='Stop'
$checkRoot=(Resolve-Path -LiteralPath $PlayerCheckDirectory).Path
# The normal suite creates a unique disposable receiver and a changed Bake fixture.
$lines=@(& (Join-Path $PSScriptRoot 'Test-NyaForgeUnityBridge.ps1') -PlayerCheckDirectory $checkRoot -UnityPath $UnityPath -MultiMaterialFixture -MaterialFixture -SurfaceFixture -TimeoutSeconds $TimeoutSeconds)
$lines | Write-Output
$receiverLine=@($lines | Where-Object { $_ -like 'Receiver: *' })
if($receiverLine.Count -ne 1) { throw 'Expected one new receiver.' }
$receiver=[IO.Path]::GetFullPath($receiverLine[0].Substring('Receiver: '.Length))
$allowed=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Artifacts')).TrimEnd('\')+'\BridgeReceiver-'
if(-not $receiver.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)) { throw 'Receiver is outside the test artifact scope.' }
[IO.File]::WriteAllText((Join-Path $receiver 'crash-test-authorized.txt'),[IO.Path]::GetFullPath((Join-Path $receiver 'Assets')))
$source=Join-Path $checkRoot 'multi-material-export\materials.nyaforge-bake.json'
$update=Join-Path $receiver 'updated-bake\materials.nyaforge-bake.json'
function Run-Probe([string]$Method,[string]$Phase,[string]$Marker,[string]$Result,[bool]$ExpectCrash) {
    $log=Join-Path $receiver ($Phase+'-'+$Method+'.log')
    $probeArguments=@('-batchmode','-projectPath',('"{0}"' -f $receiver),'-executeMethod',('NyaForge.UnityBridge.Editor.BridgeBatch.'+$Method),
        '--crash-phase',$Phase,'--crash-source',('"{0}"' -f $source),'--crash-update',('"{0}"' -f $update),
        '--crash-marker',('"{0}"' -f $Marker),'--crash-result',('"{0}"' -f $Result),'-logFile',('"{0}"' -f $log))
    $probe=Start-Process -FilePath $UnityPath -ArgumentList $probeArguments -WorkingDirectory $receiver -WindowStyle Hidden -PassThru
    $timer=[Diagnostics.Stopwatch]::StartNew()
    while(-not $probe.WaitForExit(1000)) {
        if($timer.Elapsed.TotalSeconds -gt $TimeoutSeconds) {
            Stop-Process -Id $probe.Id -Force -ErrorAction SilentlyContinue
            throw "Probe timed out: $log"
        }
    }
    $probe.Refresh()
    if($ExpectCrash) {
        $evidence=if($Method -eq 'CrashDuringRecovery') {$Marker+'.recovery-stop.json'} else {$Marker}
        if($probe.ExitCode -eq 0 -or -not (Test-Path -LiteralPath $evidence)) { throw "Expected checkpoint termination did not occur: $log" }
        $mark=Get-Content -LiteralPath $evidence -Raw | ConvertFrom-Json
        if($mark.crashedPid -ne $probe.Id -or $mark.phase -ne $Phase) { throw 'Crash marker process/checkpoint mismatch.' }
    } else {
        if($probe.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $Result)) { throw "Recovery failed: $log" }
        $report=Get-Content -LiteralPath $Result -Raw | ConvertFrom-Json
        if(-not $report.passed -or $report.recoveredPid -ne $probe.Id -or $report.crashedPid -eq $report.recoveredPid) { throw 'Invalid restart recovery result.' }
    }
}
foreach($phase in @('materials','prefab','receipt')) {
    $marker=Join-Path $receiver ($phase+'-crash.json');$result=Join-Path $receiver ($phase+'-recovery.json')
    Run-Probe 'CrashDuringUpdate' $phase $marker $result $true
    if($phase -eq 'materials') { Run-Probe 'CrashDuringRecovery' $phase $marker $result $true }
    Run-Probe 'RecoverAfterCrash' $phase $marker $result $false
    Write-Output "PASS restarted recovery: $result"
}
Write-Output "Crash recovery suite passed: $receiver"
