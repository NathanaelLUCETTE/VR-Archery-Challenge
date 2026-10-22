param([string]$UnityData = 'C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data', [switch]$ProgressionOnly)
$ErrorActionPreference = 'Stop'
$testRoot = Join-Path $env:TEMP ('ArcheryScores-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
$runtime = Get-ChildItem "$UnityData/NetCoreRuntime/shared/Microsoft.NETCore.App" -Directory | Sort-Object Name -Descending | Select-Object -First 1
$compiler = Get-ChildItem "$UnityData/DotNetSdk/sdk" -Filter csc.dll -Recurse | Select-Object -First 1
$testAssembly = Join-Path $testRoot 'ScoreChecks.dll'
$testRsp = Join-Path $testRoot 'tests.rsp'
$compilerOptions = @('/nologo','/target:exe','/nostdlib+',('/out:"' + $testAssembly + '"'))
foreach ($assembly in Get-ChildItem $runtime.FullName -Filter '*.dll') {
    try { [System.Reflection.AssemblyName]::GetAssemblyName($assembly.FullName) | Out-Null }
    catch { continue }
    $compilerOptions += '/reference:"' + $assembly.FullName + '"'
}
$projectRoot = Split-Path $PSScriptRoot -Parent
if ($ProgressionOnly) {
    $compilerOptions += '"' + (Join-Path $projectRoot 'Assets/Scripts/ProgressionRules.cs') + '"'
    $compilerOptions += '"' + (Join-Path $PSScriptRoot 'ProgressionChecks.cs') + '"'
} else {
    $compilerOptions += '"' + (Join-Path $projectRoot 'Assets/Scripts/ScoreRepository.cs') + '"'
    $compilerOptions += '"' + (Join-Path $PSScriptRoot 'ScoreRepositoryChecks.cs') + '"'
}
Set-Content -LiteralPath $testRsp -Value $compilerOptions -Encoding utf8
& "$UnityData/NetCoreRuntime/dotnet.exe" $compiler.FullName "@$testRsp"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
@{ runtimeOptions = @{ tfm = 'net8.0'; framework = @{ name = 'Microsoft.NETCore.App'; version = $runtime.Name } } } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $testRoot 'ScoreChecks.runtimeconfig.json') -Encoding utf8
& "$UnityData/NetCoreRuntime/dotnet.exe" $testAssembly (Join-Path $testRoot 'data')
exit $LASTEXITCODE
