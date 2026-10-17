$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Set-Location $root
New-Item -ItemType Directory -Force work,extension\build | Out-Null
. (Join-Path $PSScriptRoot 'build_config.ps1')
$python = Get-BuildSetting 'python' 'python'
& $python tools/generate_mono_sources.py
if ($LASTEXITCODE -ne 0) { throw 'Mono source generation failed' }
$unity=Get-UnityData
New-Item -ItemType Directory -Force work\mono-public | Out-Null
& "$unity\MonoBleedingEdge\bin\mono.exe" "$unity\MonoBleedingEdge\lib\mono\msbuild\Current\bin\Roslyn\csc.exe" /nologo /out:work\MonoPublicize.exe /r:vendor\bepinex-mono\BepInEx\core\Mono.Cecil.dll tools\MonoPublicize.cs
if($LASTEXITCODE -ne 0){throw 'Mono reference preparation compilation failed'}
Copy-Item vendor\bepinex-mono\BepInEx\core\Mono.Cecil.dll work\Mono.Cecil.dll -Force
& "$unity\MonoBleedingEdge\bin\mono.exe" work\MonoPublicize.exe work\mono-reference\Assembly-CSharp.dll work\mono-public\Assembly-CSharp.dll
if($LASTEXITCODE -ne 0){throw 'Mono reference preparation failed'}
$argsList=@('/nologo','/target:library','/out:extension\build\DuelStructure.Mono.dll','/nostdlib+','/langversion:latest')
$argsList+=Get-ChildItem work\mono-reference -Filter '*.dll' | Where-Object { $_.Name -ne 'Assembly-CSharp.dll' -and $_.Name -ne 'mscorlib.dll' -and $_.Name -notlike 'System*' -and $_.Name -notlike 'Mono.*' } | ForEach-Object {'/r:'+ $_.FullName}
$api=Join-Path $unity 'MonoBleedingEdge\lib\mono\4.7.2-api'
$argsList+=Get-ChildItem $api -Filter '*.dll' | ForEach-Object {'/r:'+ $_.FullName}
$argsList+='/r:'+ (Join-Path $api 'Facades\netstandard.dll')
$argsList+='/r:work\mono-public\Assembly-CSharp.dll'
$argsList+=@('/r:vendor\bepinex-mono\BepInEx\core\BepInEx.dll','/r:vendor\bepinex-mono\BepInEx\core\0Harmony.dll')
$argsList+=Get-ChildItem work\mono-generated -Filter '*.cs' | Where-Object Name -ne 'SessionSmokeMono.cs' | ForEach-Object FullName
$argsList+=Get-ChildItem extension-mono -Filter '*.cs' | Where-Object Name -ne 'AccessPatcher.cs' | ForEach-Object FullName

$argsList | Set-Content work\mono-plugin.rsp
& "$unity\MonoBleedingEdge\bin\mono.exe" "$unity\MonoBleedingEdge\lib\mono\msbuild\Current\bin\Roslyn\csc.exe" '@work\mono-plugin.rsp'
if($LASTEXITCODE -ne 0){throw 'Mono extension compilation failed'}

& "$unity\MonoBleedingEdge\bin\mono.exe" "$unity\MonoBleedingEdge\lib\mono\msbuild\Current\bin\Roslyn\csc.exe" /nologo /target:library /out:extension\build\DuelStructure.Mono.Access.dll /r:vendor\bepinex-mono\BepInEx\core\BepInEx.dll /r:vendor\bepinex-mono\BepInEx\core\Mono.Cecil.dll extension-mono\AccessPatcher.cs
if($LASTEXITCODE -ne 0){throw 'Mono access patcher compilation failed'}
