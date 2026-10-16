from pathlib import Path
import zipfile,shutil,json,hashlib,xml.etree.ElementTree as ET
r=Path(__file__).resolve().parents[1];out=r/'outputs';w=r/'work';exe=out/'决构编辑器-MDPro3-安装器.exe'
manual=out/'MDPro3-Mono-适配验证版'
assert manual.resolve().parent==out.resolve() and manual.name=='MDPro3-Mono-适配验证版'
if manual.exists():shutil.rmtree(manual)
manual.mkdir()
with zipfile.ZipFile(w/'installer/mono-payload.zip') as z:z.extractall(manual)
p=manual/'doorstop_config.ini';p.write_text(p.read_text(encoding='utf-8').replace('@DUEL_MONO_LIBS@','BepInEx/DuelStructureMonoRuntime'),encoding='utf-8')
shutil.copy2(out/'MDPro3-安装器使用说明.txt',manual/'使用说明.txt')
checks='''决构编辑器 MDPro3 安装器 0.2.6 / Mono 适配验证版

输入：用户提供的 MDPro3-适配资料-Mono.zip，Unity 6000.0.24f1。
Mono 主程序 SHA256：5c9f5fa0eeeabf1121b4d4120853f1236a3a8ae638e3e1e822701ed70c565246

已完成：
- Mono 扩展与最小成员访问适配器编译通过。
- 211 个 MDPro3 成员引用可访问；51 个所需私有成员仅在内存开放。
- 原本体 Assembly-CSharp.dll 未被修改。
- 扩展标准库 API 检查无缺失；BepInEx 加载器/预加载器 API 检查无缺失。
- Windows Mono JIT 框架下 64 位核心 83 项断言通过：包含开局发牌、双方操作权、回溯、存档、素材、指示物及录像导出。
- 效果记录 JSON/GZip 序列化与还原通过。
- Mono 安装、重复更新、卸载通过；未混入 IL2CPP 加载器与互操作程序集。
- 原 IL2CPP 安装/更新/卸载、原文件还原、用户数据保留、未知加载器/版本拦截通过。
- 文件被占用导致更新失败时，事务回滚测试通过。
- 安装器窗口控件范围与按钮检查通过，已渲染核对。

验证边界：资料中没有完整可运行的 Mono 本体。本次未完成该本体的实际游戏启动、原生界面与完整对局操作验证。Mono 安装测试使用程序身份文件构造的安装目录，不等同游戏运行测试。请在目标机器试装，异常时使用安装器“导出适配资料”回传启动信息。
'''
(out/'MDPro3-Mono-适配验证.txt').write_text(checks,encoding='utf-8-sig')
sha=hashlib.sha256(exe.read_bytes()).hexdigest();(out/'决构编辑器-MDPro3-安装器.sha256.txt').write_text(sha+'  '+exe.name+'\n',encoding='utf-8')
meta={'installer':str(exe),'installer_version':'0.2.6','bytes':exe.stat().st_size,'sha256':sha,'supported_body':'MDPro3 v1.4.8 Windows x64 / Unity 6000.0.24f1','mono_runtime_install_supported':True,'mono_release_status':'adaptation candidate; full game startup not verified','full_mono_game_startup_verified':False,'mono_assembly_sha256':'5c9f5fa0eeeabf1121b4d4120853f1236a3a8ae638e3e1e822701ed70c565246','mono_tests':{'native_core_assertions':83,'game_member_references':211,'private_members_promoted_in_memory':51,'framework_api_missing':0,'archive_roundtrip':True,'install_update_uninstall':True},'installer_tests':{'il2cpp_install_update_uninstall':True,'preserves_player_data':True,'foreign_loader_version_rejection':True,'failed_update_rollback':True,'gui_rendered_and_checked':True},'source':str(out/'决构编辑器-MDPro3-安装器源码.zip')}
(out/'MDPro3-安装器构建.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
paths=[]
for directory in ['installer','extension','extension-mono','tools','tests','docs','native/core']:
 for p in (r/directory).rglob('*'):
  if p.is_file() and not any(x in ('build','bin','obj','.git','__pycache__') for x in p.relative_to(r).parts):paths.append(p)
for p in (r/'native').glob('*.cpp'):paths.append(p)
for p in (r/'upstream/Assets/Scripts/MDPro3/DuelEditor').glob('*.cs'):paths.append(p)
for p in r.glob('*.cs'):paths.append(p)
for name in ['MDPro3-安装器使用说明.txt','MDPro3-Mono-适配验证.txt','MDPro3-安装器构建.json']:paths.append(out/name)
for name in ['mono-access-tests.log','mono-api-audit.log','mono-loader-complete-api-audit.log','mono-archive-tests.log','mono64-complete-framework-core-tests.log','installer/mono-install-tests.log','installer/engine-tests-v023.log','installer/rollback-tests-v022.log']:paths.append(w/name)
with zipfile.ZipFile(out/'决构编辑器-MDPro3-安装器源码.zip','w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for p in sorted(set(paths)):z.write(p,p.relative_to(r).as_posix())
print(json.dumps(meta,ensure_ascii=False,indent=2))
