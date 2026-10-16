param([string]$GameRoot='D:\Game\MDPro3')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
Set-Location $project
$runtime='D:\Unity\Editors\6000.3.21f1\Editor\Data\NetCoreRuntime\shared\Microsoft.NETCore.App\6.0.21'
$argsList=@('/nologo','/target:library','/out:extension\build\DuelStructure.dll','/nostdlib+','/langversion:latest')
$argsList+=Get-ChildItem $runtime -Filter '*.dll' | Where-Object { ($_.Name -like 'System.*' -and $_.Name -notlike '*.Native.dll') -or $_.Name -in 'mscorlib.dll','netstandard.dll','Microsoft.CSharp.dll' } | ForEach-Object { '/r:'+ $_.FullName }
$argsList+=Get-ChildItem "$GameRoot\BepInEx\interop" -Filter '*.dll' | ForEach-Object {'/r:'+ $_.FullName}
$argsList+=Get-ChildItem 'vendor\bepinex-il2cpp\BepInEx\core' -Filter '*.dll' | Where-Object {$_.Name -in 'BepInEx.Core.dll','BepInEx.Unity.IL2CPP.dll','BepInEx.Unity.Common.dll','0Harmony.dll','Il2CppInterop.Runtime.dll'} | ForEach-Object {'/r:'+ $_.FullName}
$argsList+=Get-ChildItem 'extension' -Filter '*.cs' | ForEach-Object {$_.FullName}
$argsList+='upstream\Assets\Scripts\MDPro3\DuelEditor\EditorNative.cs','upstream\Assets\Scripts\MDPro3\DuelEditor\EditorSession.cs'
$argsList | Set-Content 'work\plugin.rsp'
& 'D:\Unity\Editors\6000.3.21f1\Editor\Data\NetCoreRuntime\dotnet.exe' 'D:\Unity\Editors\6000.3.21f1\Editor\Data\DotNetSdkRoslyn\csc.dll' '@work\plugin.rsp'
if($LASTEXITCODE -ne 0){throw 'Extension compilation failed'}
