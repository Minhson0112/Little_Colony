param([string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.2f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
New-Item -ItemType Directory -Force -Path (Join-Path $projectRoot 'Logs') | Out-Null
$logPath = Join-Path $projectRoot 'Logs\build-webgl.log'
$buildProcess = Start-Process -FilePath $UnityPath -ArgumentList "-batchmode -nographics -quit -projectPath `"$projectRoot`" -buildTarget WebGL -executeMethod LittleColony.Editor.ProjectSetup.BuildWebGL -logFile `"$logPath`"" -WindowStyle Hidden -PassThru
$buildProcess.WaitForExit()
if ($buildProcess.ExitCode -ne 0) { throw "Unity build failed ($($buildProcess.ExitCode)). See $logPath" }
Write-Output "WebGL build ready: $projectRoot\Builds\WebGL\index.html"
