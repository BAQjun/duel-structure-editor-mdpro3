from pathlib import Path
import subprocess,os,hashlib,zipfile
r=Path.cwd();w=r/'work/installer';target=w/'mono-body-fixture';target.mkdir(exist_ok=True);exe=r/'outputs/决构编辑器-MDPro3-安装器.exe';game=Path('D:/Game/MDPro3')
for name in ['MDPro3.exe','UnityPlayer.dll']:
 p=target/name
 if not p.exists():os.link(game/name,p)
(target/'MonoBleedingEdge').mkdir(exist_ok=True);managed=target/'MDPro3_Data/Managed';managed.mkdir(parents=True,exist_ok=True)
(managed/'Assembly-CSharp.dll').write_bytes(b'Mono body fixture assembly - not used to execute')
(managed/'UnityEngine.CoreModule.dll').write_bytes(b'Mono fixture Unity API')
for name in ['Deck/private.ydk','Data/account.conf','Replay/private.yrp3d','Data/DuelStructureSaves/private.mdges']:
 p=target/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(b'private player data must never enter support archive')
def invoke(mode,args):return subprocess.run([str(exe),mode,str(target),*map(str,args)],creationflags=subprocess.CREATE_NO_WINDOW,timeout=180)
report=w/'mono-detection-result.txt';result=invoke('--check',[report]);text=report.read_text(encoding='utf-8-sig');assert result.returncode==1 and 'Mono' in text and '文件夹选择正确' in text,text
print('PASS Mono body is recognized correctly rather than reported as wrong directory',flush=True)
result=invoke('--install',[report]);assert result.returncode==1 and not (target/'BepInEx').exists();assert not (target/'winhttp.dll').exists();print('PASS unsupported Mono body receives no IL2CPP loader or extension files',flush=True)
archive=w/'mono-support-info.zip'
if archive.exists():archive.unlink()
result=invoke('--export-info',[archive]);assert result.returncode==0
with zipfile.ZipFile(archive) as z:
 names=set(z.namelist());assert names=={'版本信息.txt','MDPro3_Data/Managed/Assembly-CSharp.dll','MDPro3_Data/Managed/UnityEngine.CoreModule.dll'},names
 assert 'Runtime: Mono' in z.read('版本信息.txt').decode('utf-8')
 assert z.read('MDPro3_Data/Managed/Assembly-CSharp.dll')==(managed/'Assembly-CSharp.dll').read_bytes()
 assert not any(x.startswith(('Deck/','Data/','Replay/')) for x in names)
print('PASS support export contains exact Managed components and runtime report without player data',flush=True)
before=archive.read_bytes();result=invoke('--export-info',[archive]);assert result.returncode==1 and archive.read_bytes()==before;print('PASS existing support ZIP is preserved on repeated export',flush=True)
print('SUCCESS Mono recognition unsupported-install protection and scoped support export',flush=True)
