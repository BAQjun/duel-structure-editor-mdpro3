from pathlib import Path
import zipfile,hashlib,xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[1];WORK=ROOT/'work/installer';GAME=Path('D:/Game/MDPro3');WORK.mkdir(parents=True,exist_ok=True)
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for chunk in iter(lambda:f.read(1024*1024),b''):h.update(chunk)
 return h.hexdigest()
files={}
def add(src,rel,scope):
 assert rel not in files and '..' not in Path(rel).parts
 files[rel]=(src,scope)
vendor=ROOT/'vendor/bepinex-il2cpp'
for p in vendor.rglob('*'):
 if p.is_file():
  rel=p.relative_to(vendor).as_posix()
  if rel.startswith(('BepInEx/core/','dotnet/')) or rel in ['.doorstop_version','doorstop_config.ini','winhttp.dll','changelog.txt']:add(p,rel,'loader')
for folder in ['unity-libs','interop']:
 for p in (GAME/'BepInEx'/folder).iterdir():
  if p.is_file() and p.suffix.lower() in ['.dll','.txt','.zip']:add(p,'BepInEx/'+folder+'/'+p.name,'loader')
pack=ROOT/'outputs/MDPro3-UI-YGO-layout'
for p in (pack/'BepInEx/plugins/DuelStructure').rglob('*'):
 if p.is_file():add(p,p.relative_to(pack).as_posix(),'extension')
assert (ROOT/'extension/build/DuelStructure.dll').read_bytes()==(pack/'BepInEx/plugins/DuelStructure/DuelStructure.dll').read_bytes()
add(pack/'BepInEx/config/BepInEx.cfg','BepInEx/config/BepInEx.cfg','config')
license_dir=WORK/'licenses';license_dir.mkdir(exist_ok=True)
license_text='''决构编辑器 MDPro3 安装包，2026-10-15。
适配 MDPro3 v1.4.8 Windows x64 / Unity 6000.0.24f1。

MDPro3 本体不包含在安装包中。BepInEx IL2CPP 加载器来自 https://github.com/BepInEx/BepInEx ，采用 LGPL-2.1 许可证；具体二进制及其依赖版权属于原作者。
核心基于 Fluorohydride/ygopro-core，采用 MIT，完整许可证随包保留。与本次二进制对应的编辑器和原生核心源码另附 source 安装器源码压缩包。
包内 .NET 运行库、Harmony、Il2CppInterop、MonoMod、Cpp2IL 等保留各自的版权和许可，来源为 BepInEx 官方 IL2CPP 发布包。
interop 是针对适配本体自动生成的互操作绑定；unity-libs 为 BepInEx 使用的 Unity 基础库缓存，用于离线初始化。
'''
(license_dir/'THIRD-PARTY-NOTICES.txt').write_text(license_text,encoding='utf-8')
add(license_dir/'THIRD-PARTY-NOTICES.txt','BepInEx/plugins/DuelStructure/licenses/THIRD-PARTY-NOTICES.txt','extension')
add(ROOT/'native/core/LICENSE','BepInEx/plugins/DuelStructure/licenses/ygopro-core-LICENSE.txt','extension')
root=ET.Element('Payload');ET.SubElement(root,'Version').text='0.2.0.20261015';ET.SubElement(root,'MetadataSha').text=sha(GAME/'MDPro3_Data/il2cpp_data/Metadata/global-metadata.dat');ET.SubElement(root,'UnitySha').text=sha(GAME/'UnityPlayer.dll');ET.SubElement(root,'GameAssemblySha').text=sha(GAME/'GameAssembly.dll');items=ET.SubElement(root,'Files')
with zipfile.ZipFile(WORK/'payload.zip','w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for rel,(p,scope) in sorted(files.items()):
  node=ET.SubElement(items,'PayloadFile');ET.SubElement(node,'Path').text=rel;ET.SubElement(node,'Sha').text=sha(p);ET.SubElement(node,'Scope').text=scope;z.write(p,rel)
ET.ElementTree(root).write(WORK/'payload-manifest.xml',encoding='utf-8',xml_declaration=True)
print('Payload:',len(files),'files,',(WORK/'payload.zip').stat().st_size,'bytes')
