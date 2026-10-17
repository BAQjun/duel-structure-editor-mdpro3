$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'build_config.ps1')
$baseline = Get-BuildPath 'ygopro_root' '../duel-structure-editor-ygopro'
$toolBin = Get-BuildPath 'llvm_mingw_bin' (Join-Path $baseline 'tools/llvm-mingw-20260616-ucrt-x86_64/bin')
$env:PATH = "$toolBin;$env:PATH"
New-Item -ItemType Directory -Force (Join-Path $taskRoot 'work') | Out-Null
$env:TMP = $env:TEMP = Join-Path $taskRoot 'work'
$dest = Join-Path $taskRoot 'native\build'
New-Item -ItemType Directory -Force $dest | Out-Null
$taskSources = @(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'native\core') -Filter '*.cpp' | ForEach-Object FullName)
& "$toolBin\clang++.exe" -shared -std=c++14 -O2 -DOCGCORE_EXPORT_FUNCTIONS "-I$baseline/upstream/lua/src" @taskSources (Join-Path $taskRoot 'native\editor_state.cpp') (Join-Path $baseline 'upstream\bin\release\lua.lib') '-Wl,--export-all-symbols' -o (Join-Path $dest 'ocgcore_editor.dll')
if ($LASTEXITCODE -ne 0) { throw 'Native editor build failed.' }
$plugin = Join-Path $taskRoot 'upstream\Assets\Plugins\Windows\x86_64'
New-Item -ItemType Directory -Force $plugin | Out-Null
Copy-Item -LiteralPath (Join-Path $dest 'ocgcore_editor.dll') -Destination $plugin
foreach ($dependency in @('libc++.dll','libunwind.dll')) {
 Copy-Item -LiteralPath (Join-Path $toolBin $dependency) -Destination $dest
 Copy-Item -LiteralPath (Join-Path $toolBin $dependency) -Destination $plugin
}
Write-Output 'Built separate editor core; original ocgcore.dll unchanged.'
