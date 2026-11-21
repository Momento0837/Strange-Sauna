$ErrorActionPreference = 'Stop'
$projectPath = $PSScriptRoot
$unityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.26f1\Editor\Unity.exe'
if (-not (Test-Path -LiteralPath $unityPath)) { throw 'Unity 6000.3.26f1 설치 경로를 확인하세요.' }
New-Item -ItemType Directory -Force (Join-Path $projectPath 'Logs') | Out-Null
$logPath = Join-Path $projectPath 'Logs\unity-build.log'
$arguments = '-batchmode -nographics -quit -projectPath "' + $projectPath + '" -executeMethod SaunaProjectBuilder.Build -logFile "' + $logPath + '"'
$buildProcess = Start-Process -FilePath $unityPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($buildProcess.ExitCode -ne 0) { throw ('Unity build failed. See ' + $logPath) }
Write-Host '완료: Builds\Windows\수상한 찜질방.exe'
