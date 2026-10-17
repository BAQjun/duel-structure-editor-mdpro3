from build_config import unity_data
from pathlib import Path
import hashlib,zipfile,xml.etree.ElementTree as ET
r=Path(__file__).resolve().parents[1];w=r/'work/installer';vendor=r/'vendor/bepinex-mono';files={}
def add(path,rel,scope):files[rel]=(path,scope)
for p in vendor.rglob('*'):
 if p.is_file():add(p,p.relative_to(vendor).as_posix(),'loader')
config=w/'mono-doorstop.ini';config.write_text((vendor/'doorstop_config.ini').read_text(encoding='utf-8-sig').replace('dll_search_path_override =','dll_search_path_override = @DUEL_MONO_LIBS@'),encoding='utf-8');add(config,'doorstop_config.ini','loader')
# Keep the body assemblies untouched; Doorstop resolves complete framework libraries in an isolated mod directory.
framework=(unity_data()/'MonoBleedingEdge/lib/mono/unityjit-win32')
add(framework.parents[2]/'EmbedRuntime/MonoPosixHelper.dll','MonoPosixHelper.dll','loader')
for p in framework.glob('*.dll'):
 if p.name=='mscorlib.dll' or p.name.startswith(('System','I18N','Microsoft.CSharp')) or p.name in ('Mono.Security.dll','Mono.Data.Sqlite.dll'):add(p,'BepInEx/DuelStructureMonoRuntime/'+p.name,'loader')
for p in (framework/'Facades').glob('*.dll'):
 rel='BepInEx/DuelStructureMonoRuntime/'+p.name
 if rel not in files:add(p,rel,'loader')
pack=r/'outputs/MDPro3-UI-YGO-layout'
for p in (pack/'BepInEx/plugins/DuelStructure').rglob('*'):
 if p.is_file() and p.name!='DuelStructure.dll':add(p,p.relative_to(pack).as_posix(),'extension')
add(r/'extension/build/DuelStructure.Mono.dll','BepInEx/plugins/DuelStructure/DuelStructure.Mono.dll','extension')
add(r/'extension/build/DuelStructure.Mono.Access.dll','BepInEx/patchers/DuelStructure/DuelStructure.Mono.Access.dll','patcher')
cfg=w/'mono-bepinex.cfg';cfg.write_text((pack/'BepInEx/config/BepInEx.cfg').read_text(encoding='utf-8-sig').replace('UnityLogListening = true','UnityLogListening = false'),encoding='utf-8');add(cfg,'BepInEx/config/BepInEx.cfg','config')
readme=w/'mono-compatibility.txt';readme.write_text('Mono 适配组件按提供的 Assembly-CSharp.dll 构建。加载器：BepInEx 5.4.23.5。完整框架组件单独放在 BepInEx/DuelStructureMonoRuntime，原本体 Managed 程序组件不覆盖。访问适配器仅在加载时开放扩展实际使用的成员，不修改本体文件。\n',encoding='utf-8');add(readme,'BepInEx/plugins/DuelStructure/licenses/Mono-compatibility.txt','extension')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
root=ET.Element('Payload');ET.SubElement(root,'Version').text='0.3.3';ET.SubElement(root,'Runtime').text='Mono';ET.SubElement(root,'MonoAssemblySha').text=sha(r/'work/mono-reference/Assembly-CSharp.dll');ET.SubElement(root,'UnityVersion').text='6000.0.24.1178165';items=ET.SubElement(root,'Files')
with zipfile.ZipFile(w/'mono-payload.zip','w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for rel,(p,scope) in sorted(files.items()):
  f=ET.SubElement(items,'PayloadFile');ET.SubElement(f,'Path').text=rel;ET.SubElement(f,'Sha').text=sha(p);ET.SubElement(f,'Scope').text=scope;z.write(p,rel)
ET.ElementTree(root).write(w/'mono-payload-manifest.xml',encoding='utf-8',xml_declaration=True)
print('Mono payload',len(files),'files,',(w/'mono-payload.zip').stat().st_size,'bytes')
