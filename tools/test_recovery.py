from pathlib import Path
import hashlib
import subprocess
import uuid
import zipfile

root = Path(__file__).resolve().parents[1]
fixture = root / 'work' / ('recovery-test-' + uuid.uuid4().hex)
fixture.mkdir()
exe = root / 'outputs/决构编辑器-Mono启动恢复.exe'
(fixture / 'MDPro3.exe').write_bytes(b'fixture only')
(fixture / 'winhttp.dll').write_bytes(b'loader fixture')
receipt = fixture / 'BepInEx/DuelStructureInstaller/install.xml'
receipt.parent.mkdir(parents=True)
sha = hashlib.sha256((fixture / 'winhttp.dll').read_bytes()).hexdigest()
receipt.write_text(f'<Receipt><Product>cn.duelstructure.mdpro3</Product><Installed>false</Installed><Files><InstalledFile><Path>winhttp.dll</Path><Sha>{sha}</Sha></InstalledFile></Files></Receipt>')
config = fixture / 'doorstop_config.ini'
original = b'[General]\r\nenabled = true\r\ntarget_assembly=BepInEx\\core\\BepInEx.Preloader.dll\r\n[UnityMono]\r\ndll_search_path_override = private/framework\r\n'
config.write_bytes(original)
deck = fixture / 'Deck/keep.ydk'
deck.parent.mkdir()
deck.write_bytes(b'#main\n89631139\n')
(fixture / 'preloader_test.log').write_text('Example failure')
def run(ok):
    result = subprocess.run([str(exe), '--recover', str(fixture)], creationflags=subprocess.CREATE_NO_WINDOW, timeout=20)
    assert (result.returncode == 0) == ok
run(True)
assert 'enabled = false' in config.read_text()
assert 'dll_search_path_override = private/framework' in config.read_text()
assert next(fixture.glob('doorstop_config.ini.before-recovery-*')).read_bytes() == original
assert deck.read_bytes() == b'#main\n89631139\n'
with zipfile.ZipFile(next(fixture.glob('*.zip'))) as archive:
    assert archive.read('doorstop_config.ini') == original
    assert archive.read('preloader_test.log') == b'Example failure'
    assert 'Deck/keep.ydk' not in archive.namelist()
print('PASS uninstalled receipt recovery, exact backup, loader disable, log export, deck preservation')
config.write_bytes(original)
(fixture / 'winhttp.dll').write_bytes(b'foreign loader')
run(False)
assert config.read_bytes() == original
print('PASS foreign loader rejected without configuration changes')
(fixture / 'winhttp.dll').write_bytes(b'loader fixture')
config.write_bytes(b'[General]\nenabled = true\ntarget_assembly=OtherPlugin.dll\n')
before = config.read_bytes()
run(False)
assert config.read_bytes() == before
print('PASS foreign entrypoint rejected without configuration changes')
