from build_config import path_setting
from pathlib import Path
import hashlib,subprocess,json,shutil,os
ROOT=Path(__file__).resolve().parents[1];WORK=ROOT/'work/installer';EXE=ROOT/'outputs/决构编辑器-MDPro3-安装器.exe';GAME=path_setting('game_root');TARGET=WORK/'fixture-测试本体';TARGET.mkdir(parents=True,exist_ok=True)
def h(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def check(value,text):
 if not value:raise AssertionError(text)
 print('PASS',text,flush=True)
def invoke(mode,target,expected=True):
 report=WORK/'command-result.txt';p=subprocess.run([str(EXE),mode,str(target),str(report)],creationflags=subprocess.CREATE_NO_WINDOW,timeout=180)
 text=report.read_text(encoding='utf-8-sig');check((p.returncode==0)==expected,mode+' '+text);return text
for name in ['MDPro3.exe','UnityPlayer.dll','GameAssembly.dll']:
 p=TARGET/name
 if not p.exists():os.link(GAME/name,p)
meta=TARGET/'MDPro3_Data/il2cpp_data/Metadata/global-metadata.dat';meta.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(GAME/'MDPro3_Data/il2cpp_data/Metadata/global-metadata.dat',meta)
plugin=TARGET/'BepInEx/plugins/DuelStructure';plugin.mkdir(parents=True,exist_ok=True);(plugin/'DuelStructure.dll').write_bytes(b'original editor before setup')
config=TARGET/'BepInEx/config/BepInEx.cfg';config.parent.mkdir(parents=True,exist_ok=True);config.write_text('[Logging.Console]\nEnabled = true\n[Personal]\nRetain = yes\n',encoding='utf-8')
keep={TARGET/'Deck/自用卡组.ydk':b'#main\n89631139',TARGET/'Replay/keep.yrp3d':b'player replay',TARGET/'Data/DuelStructureSaves/keep.mdges':b'player save',TARGET/'BepInEx/plugins/OtherPlugin/keep.dll':b'another plugin'}
for p,data in keep.items():p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(data)
invoke('--check',TARGET);invoke('--install',TARGET)
check(h(plugin/'DuelStructure.dll')==h(ROOT/'extension/build/DuelStructure.dll'),'fresh installer deploys exact latest extension')
check(all(p.read_bytes()==data for p,data in keep.items()),'install preserves decks saves replays and unrelated plugins')
text=config.read_text(encoding='utf-8-sig');check('Enabled = false' in text and 'Retain = yes' in text,'only console setting is changed while personal options remain')
check((TARGET/'dotnet/coreclr.dll').is_file() and (TARGET/'BepInEx/interop/Assembly-CSharp.dll').is_file() and (TARGET/'BepInEx/unity-libs/UnityEngine.CoreModule.dll').is_file(),'offline loader runtime interop and Unity cache are all installed')
receipt=TARGET/'BepInEx/DuelStructureInstaller/install.xml';first=receipt.read_bytes();invoke('--install',TARGET);check((plugin/'DuelStructure.dll').is_file(),'repeat installation succeeds without losing original backups')
config.write_text(config.read_text(encoding='utf-8-sig')+'\nUserEditedLater = yes\n',encoding='utf-8');invoke('--uninstall',TARGET)
check((plugin/'DuelStructure.dll').read_bytes()==b'original editor before setup','uninstall restores original editor rather than deleting it')
check('UserEditedLater = yes' in config.read_text(encoding='utf-8-sig'),'uninstall preserves config edited after installation')
check(all(p.read_bytes()==data for p,data in keep.items()),'uninstall preserves all player data and unrelated plugins')
check((TARGET/'winhttp.dll').exists(),'uninstall keeps shared loader dependencies')
invoke('--install',TARGET);dll=TARGET/'winhttp.dll';original=dll.read_bytes();dll.write_bytes(b'foreign loader');before=h(plugin/'DuelStructure.dll');invoke('--install',TARGET,False);check(h(plugin/'DuelStructure.dll')==before,'unknown loader is rejected before extension files change');dll.write_bytes(original)
wrong=WORK/'invalid-version';wrong.mkdir(exist_ok=True)
for name in ['MDPro3.exe','UnityPlayer.dll','GameAssembly.dll']:
 p=wrong/name
 if not p.exists():os.link(GAME/name,p)
p=wrong/'MDPro3_Data/il2cpp_data/Metadata/global-metadata.dat';p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(b'unsupported metadata');invoke('--install',wrong,False);check(not (wrong/'BepInEx').exists(),'unsupported body version is rejected without writing loader files')
invoke('--install',WORK,False)
check(not list(TARGET.glob('.duelstructure-install-*')),'temporary extraction folders are removed after installation')
print('SUCCESS installer fresh install update uninstall restore compatibility and preservation',flush=True)
