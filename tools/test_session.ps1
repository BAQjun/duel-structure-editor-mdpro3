$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'build_config.ps1')
$unityData=Get-UnityData
$env:DUEL_EDITOR_YGOPRO_RUNTIME = Get-BuildPath 'ygopro_runtime'
$mono=Join-Path $unityData 'MonoBleedingEdge\bin\mono.exe'
$csc=Join-Path $unityData 'MonoBleedingEdge\lib\mono\msbuild\Current\bin\Roslyn\csc.exe'
$managed=Join-Path $taskRoot 'upstream\Assets\Scripts\MDPro3\DuelEditor'
& $mono $csc /nologo /langversion:latest /out:$taskRoot/work/SessionSmoke.exe "$managed/EditorNative.cs" "$managed/EditorSession.cs" "$taskRoot/tests/SessionSmoke.cs"
if ($LASTEXITCODE -ne 0){throw 'Managed editor compilation failed'}
Push-Location (Join-Path $taskRoot 'work')
try {& (Join-Path $unityData 'NetCoreRuntime\dotnet.exe') './SessionSmoke.exe';if($LASTEXITCODE -ne 0){throw 'Session smoke failed'}} finally {Pop-Location}
