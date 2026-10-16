from pathlib import Path
from PIL import Image
import json,hashlib,shutil,zipfile
root=Path(r'C:\Underwrite');out=root/'Outputs/ProtagonistParts_v2'
manifest=json.loads((out/'parts_manifest.json').read_text(encoding='utf-8'))
report=[]
for part in manifest['parts']:
    path=out/'parts'/(part['name']+'.png');im=Image.open(path)
    assert im.mode=='RGBA'
    assert im.size==tuple(part['atlas_rect'][2:])
    alpha=im.getchannel('A');assert alpha.getextrema()==(0,255)
    assert alpha.getbbox()
    assert all(alpha.getpixel(p)==0 for p in [(0,0),(im.width-1,0),(0,im.height-1),(im.width-1,im.height-1)])
    report.append({'name':part['name'],'size':list(im.size),'transparent':True})
old=root/'Assets/Underwrite/Art/Parts/protagonist_parts_v1.png.meta'
old_hash=hashlib.sha256(old.read_bytes()).hexdigest().upper()
expected='083639449741F8C5BD2BF8279CF7DFCFCA3149965FC72C8A0D2EDE6E25EBC5CC'
assert old_hash==expected,'The artist-edited legacy metadata changed unexpectedly.'
(out/'validation.json').write_text(json.dumps({'parts':report,'legacy_artist_metadata_preserved':True},indent=2),encoding='utf-8')
shutil.copy2(root/'Outputs/CharacterBase/주인공_정면_기준원화_v1.png',out/'reference_master.png')
archive=root/'Outputs/Underwrite_주인공_파츠분리_v2.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
    for p in out.rglob('*'):
        if p.is_file():z.write(p,p.relative_to(out))
print(json.dumps(report,ensure_ascii=False,indent=2));print('Legacy trim metadata preserved:',old_hash);print('Archive:',archive,archive.stat().st_size)
