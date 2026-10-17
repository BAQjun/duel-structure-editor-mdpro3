$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location $project
$env:TEMP=Join-Path $project 'work\installer'
$env:TMP=$env:TEMP
. (Join-Path $PSScriptRoot 'build_config.ps1')
$compiler=Get-BuildPath 'csc' (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe')
& $compiler /nologo /target:winexe /platform:x64 /optimize+ '/out:outputs\决构编辑器-MDPro3-安装器.exe' /win32manifest:installer\app.manifest /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll '/resource:work\installer\payload.zip,duelstructure.payload.zip' '/resource:work\installer\payload-manifest.xml,duelstructure.manifest.xml' '/resource:work\installer\mono-payload.zip,duelstructure.mono.payload.zip' '/resource:work\installer\mono-payload-manifest.xml,duelstructure.mono.manifest.xml' installer\Setup.cs installer\AssemblyInfo.cs
if($LASTEXITCODE -ne 0){throw 'Installer build failed'}
