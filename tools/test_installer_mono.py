from pathlib import Path
import shutil,subprocess,xml.etree.ElementTree as ET,zipfile
r=Path.cwd();w=r/'work/installer';target=w/'mono-body-fixture';managed=target/'MDPro3_Data/Managed';shutil.copy2(r/'work/mono-reference/Assembly-CSharp.dll',managed/'Assembly-CSharp.dll')
exe=r/'outputs/决构编辑器-MDPro3-安装器.exe'
def call(mode,expected=True):
 report=w/'mono-new-install-result.txt';p=subprocess.run([str(exe),mode,str(target),str(report)],creationflags=subprocess.CREATE_NO_WINDOW,timeout=180);text=report.read_text(encoding='utf-8-sig');assert (p.returncode==0)==expected,text;print(text,flush=True)
call('--check');call('--install')
assert (target/'BepInEx/plugins/DuelStructure/DuelStructure.Mono.dll').read_bytes()==(r/'extension/build/DuelStructure.Mono.dll').read_bytes()
assert not (target/'BepInEx/interop').exists() and not (target/'dotnet').exists() and not (target/'BepInEx/core/BepInEx.Unity.IL2CPP.dll').exists()
assert (target/'BepInEx/patchers/DuelStructure/DuelStructure.Mono.Access.dll').exists()
assert (target/'BepInEx/DuelStructureMonoRuntime/mscorlib.dll').read_bytes()==Path('D:/Unity/Editors/6000.3.21f1/Editor/Data/MonoBleedingEdge/lib/mono/unityjit-win32/mscorlib.dll').read_bytes()
assert (target/'MonoPosixHelper.dll').exists()
assert '@DUEL_MONO_LIBS@' not in (target/'doorstop_config.ini').read_text(encoding='utf-8')
assert (managed/'Assembly-CSharp.dll').read_bytes()==(r/'work/mono-reference/Assembly-CSharp.dll').read_bytes()
print('PASS Mono install selects Mono loader, access patcher and isolated complete framework; leaves body assemblies untouched',flush=True)
call('--install');call('--uninstall')
assert not (target/'BepInEx/patchers/DuelStructure/DuelStructure.Mono.Access.dll').exists()
assert not (target/'BepInEx/plugins/DuelStructure/DuelStructure.Mono.dll').exists()
assert (managed/'Assembly-CSharp.dll').read_bytes()==(r/'work/mono-reference/Assembly-CSharp.dll').read_bytes()
print('SUCCESS Mono fresh install repeat update and patcher-aware uninstall',flush=True)
