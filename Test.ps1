param([switch]$Retail)
$ErrorActionPreference = 'Stop'
$projectPath = $PSScriptRoot
$exePath = Join-Path $projectPath 'Builds\QA\수상한 찜질방.exe'
if (-not (Test-Path -LiteralPath $exePath)) { throw '먼저 Unity 메뉴에서 수상한 찜질방 > 테스트용 빌드를 실행하세요.' }
$outputPath = Join-Path $projectPath $(if ($Retail) { 'Logs\RetailQA' } else { 'Logs\FoodQA' })
New-Item -ItemType Directory -Force $outputPath | Out-Null
$testFlag = if ($Retail) { '--sauna-retail-smoke' } else { '--sauna-smoke' }
$arguments = $testFlag + ' --sauna-output "' + $outputPath + '" -screen-fullscreen 0 -screen-width 1440 -screen-height 900 -logFile "' + (Join-Path $outputPath 'player.log') + '"'
$testProcess = Start-Process -FilePath $exePath -ArgumentList $arguments -Wait -PassThru
if ($testProcess.ExitCode -ne 0) { throw ('Runtime checks failed. See ' + $outputPath) }
Get-Content -LiteralPath (Join-Path $outputPath 'runtime-checks.txt')
