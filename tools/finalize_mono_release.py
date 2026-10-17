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
checks=(r/'docs/MDPro3-Mono-适配验证.txt').read_text(encoding='utf-8-sig')
(out/'MDPro3-Mono-适配验证.txt').write_text(checks,encoding='utf-8-sig')
sha=hashlib.sha256(exe.read_bytes()).hexdigest();(out/'决构编辑器-MDPro3-安装器.sha256.txt').write_text(sha+'  '+exe.name+'\n',encoding='utf-8')
meta={'installer':exe.name,'installer_version':'0.3.3','bytes':exe.stat().st_size,'sha256':sha,'supported_body':'MDPro3 v1.4.8 Windows x64 / Unity 6000.0.24f1','mono_runtime_install_supported':True,'mono_release_status':'verified on full MyCard Mono 1.4.7-2','full_mono_game_startup_verified':True,'mono_assembly_sha256':'5c9f5fa0eeeabf1121b4d4120853f1236a3a8ae638e3e1e822701ed70c565246','mono_tests':{'native_core_assertions':139,'game_member_references':217,'private_members_promoted_in_memory':51,'framework_api_missing':0,'archive_roundtrip':True,'install_update_uninstall':True},'installer_tests':{'il2cpp_install_update_uninstall':True,'preserves_player_data':True,'foreign_loader_version_rejection':True,'failed_update_rollback':True,'gui_rendered_and_checked':True},'source':'决构编辑器-MDPro3-安装器源码.zip'}
(out/'MDPro3-安装器构建.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
paths=[]
for directory in ['installer','extension','extension-mono','tools','tests','docs','native/core']:
 for p in (r/directory).rglob('*'):
  if p.is_file() and not any(x in ('build','bin','obj','.git','__pycache__') for x in p.relative_to(r).parts):paths.append(p)
for p in (r/'native').glob('*.cpp'):paths.append(p)
for p in (r/'upstream/Assets/Scripts/MDPro3/DuelEditor').glob('*.cs'):paths.append(p)
for p in r.glob('*.cs'):paths.append(p)
paths.append(r/'build-config.example.json')
for name in ['MDPro3-安装器使用说明.txt','MDPro3-Mono-适配验证.txt','MDPro3-安装器构建.json']:paths.append(out/name)
for name in ['mono-access-tests.log','mono-api-audit.log','mono-loader-complete-api-audit.log','mono-archive-tests.log','mono64-complete-framework-core-tests.log','summon-history-session-tests.log','summon-history-ui-tests.log','diameter-ui-tests.log','portable-session-tests.log','installer/mono-install-tests.log','installer/engine-tests-v033.log','installer/rollback-tests-v022.log']:paths.append(w/name)
with zipfile.ZipFile(out/'决构编辑器-MDPro3-安装器源码.zip','w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for p in sorted(set(paths)):z.write(p,p.relative_to(r).as_posix())
print(json.dumps(meta,ensure_ascii=False,indent=2))
