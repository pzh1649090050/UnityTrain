param(
    [ValidateSet('Test','Build','Generate')][string]$Task = 'Test',
    [string]$Editor = 'D:\UnityHub\Editor\2022.3.55f1c1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$trainingRoot = Split-Path -Parent $PSScriptRoot
if (!(Test-Path -LiteralPath $Editor)) { throw "找不到指定编辑器：$Editor" }
New-Item -ItemType Directory -Force -Path "$trainingRoot/Logs", "$trainingRoot/TestResults" | Out-Null
$trainingArgs = @('-batchmode', '-projectPath', ('"{0}"' -f $trainingRoot), '-logFile', ('"{0}/Logs/{1}.log"' -f $trainingRoot,$Task))
switch ($Task) {
    'Test' { $trainingArgs += @('-nographics','-quit','-executeMethod','TrainingBatchTests.Run') }
    'Build' { $trainingArgs += @('-quit','-executeMethod','UnityTrain.Editor.TrainingProjectBuilder.BuildWindows') }
    'Generate' { $trainingArgs += @('-quit','-executeMethod','UnityTrain.Editor.TrainingProjectBuilder.Generate') }
}
$trainingProcess = Start-Process -FilePath $Editor -ArgumentList $trainingArgs -WorkingDirectory $trainingRoot -WindowStyle Hidden -Wait -PassThru
if ($trainingProcess.ExitCode -ne 0) { throw "Unity $Task 失败，退出码 $($trainingProcess.ExitCode)，请查看 Logs/$Task.log" }
if ($Task -eq 'Test') {
    [xml]$trainingResult = Get-Content -Raw -LiteralPath "$trainingRoot/TestResults/editmode.xml"
    if ($trainingResult.'test-run'.result -ne 'Passed') { throw 'Unity 测试未全部通过' }
    Write-Host "测试通过：$($trainingResult.'test-run'.passed)"
}
Write-Host "Unity $Task 完成"
