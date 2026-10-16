from pathlib import Path
from PIL import Image
import json,shutil,hashlib,zipfile
root=Path(r'C:\Underwrite');out=root/'Outputs/AnimationComparison'
manifest=json.loads((out/'animation_manifest.json').read_text(encoding='utf-8'))
source=root/'Assets/Underwrite/AnimationPilot/Frames'
report=[]
for motion in manifest['motions']:
    name=motion['id']; files=sorted((source/name).glob('*.png'))
    assert len(files)==motion['frames'],(name,len(files))
    dest=out/'frames'/name;dest.mkdir(parents=True,exist_ok=True)
    frames=[]; hashes=[];bounds=[]
    for file in files:
        im=Image.open(file).convert('RGBA');assert im.size==(512,640)
        a=im.getchannel('A');assert a.getextrema()==(0,255)
        assert all(a.getpixel(p)==0 for p in [(0,0),(511,0),(0,639),(511,639)])
        b=a.point(lambda v:255 if v>=128 else 0).getbbox();assert b and b[0]>0 and b[2]<512 and b[1]>0 and b[3]<640
        hashes.append(hashlib.sha256(im.tobytes()).hexdigest());bounds.append(b)
        frames.append(im.copy());shutil.copy2(file,dest/file.name)
    durations=[1000*(i+1)//12-1000*i//12 for i in range(len(frames))]
    frames[0].save(out/(name+'.png'),format='PNG',save_all=True,append_images=frames[1:],duration=durations,loop=0,disposal=0,blend=0)
    check=Image.open(out/(name+'.png'));assert check.n_frames==len(frames)
    assert len(set(hashes))>len(files)//2,(name,'insufficient visible motion')
    report.append(dict(action=name,frames=len(files),unique_frames=len(set(hashes)),alpha_corners_zero=True,canvas=[512,640],bottom_pixel_range=[min(b[3] for b in bounds),max(b[3] for b in bounds)]))
(out/'frame_validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
with zipfile.ZipFile(root/'Outputs/Underwrite_주인공_PNG_애니메이션_v1.zip','w',zipfile.ZIP_DEFLATED) as z:
    for file in (out/'frames').rglob('*.png'):z.write(file,file.relative_to(out))
    for name in ['animation_manifest.json','frame_validation.json','사용안내.md']:
        if (out/name).exists():z.write(out/name,name)
with zipfile.ZipFile(root/'Outputs/Underwrite_6인_8방향_원화_v1.zip','w',zipfile.ZIP_DEFLATED) as z:
    for file in (root/'Outputs/DirectionArt').iterdir():
        if file.is_file():z.write(file,file.name)
print(json.dumps(report,ensure_ascii=False,indent=2))
