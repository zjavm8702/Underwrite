from pathlib import Path
from PIL import Image
from collections import deque
import json,shutil
root=Path(r'C:\Underwrite');record=json.loads((root/'work/front_parts_source.json').read_text(encoding='utf-8'))
out=root/'Outputs/ProtagonistParts_v2';art=root/'Assets/Underwrite/Art/ProtagonistBaseV2'
out.mkdir(parents=True,exist_ok=True);art.mkdir(parents=True,exist_ok=True)
atlas=out/'주인공_7파츠_원화_v2.png';shutil.copy2(record['source'],atlas)
shutil.copy2(record['source'],art/'protagonist_parts_seven_v1.png')
im=Image.open(atlas);assert im.mode=='RGBA';w,h=im.size
raw=list(im.getchannel('A').getdata());remaining={i for i,a in enumerate(raw) if a>=128};components=[]
while remaining:
    first=remaining.pop();q=deque([first]);group=[first]
    while q:
        p=q.popleft();x=p%w;y=p//w
        around=[]
        if x:around.append(p-1)
        if x<w-1:around.append(p+1)
        if y:around.append(p-w)
        if y<h-1:around.append(p+w)
        for n in around:
            if n in remaining:remaining.remove(n);q.append(n);group.append(n)
    if len(group)>1000:
        xs=[p%w for p in group];ys=[p//w for p in group]
        components.append({'area':len(group),'box':[min(xs),min(ys),max(xs)+1,max(ys)+1]})
assert len(components)==7,components
head=min(components,key=lambda c:c['box'][1]);pouch=min(components,key=lambda c:c['area'])
rest=[c for c in components if c is not head and c is not pouch];rest.sort(key=lambda c:(c['box'][0]+c['box'][2])/2)
rleg,rarm,torso,larm,lleg=rest
names=[('01_head_neck',head,.5,.05),('02_torso_pelvis',torso,.5,.15),('03_arm_R',rarm,.5,.95),('04_arm_L',larm,.5,.95),('05_leg_R',rleg,.5,.97),('06_leg_L',lleg,.5,.97),('07_coin_pouch',pouch,.5,.96)]
def intersects(a,b):return min(a[2],b[2])>max(a[0],b[0]) and min(a[3],b[3])>max(a[1],b[1])
parts=[];lines=[]
for name,c,px,py in names:
    for pad in range(6,-1,-1):
        b=c['box'];rect=[max(0,b[0]-pad),max(0,b[1]-pad),min(w,b[2]+pad),min(h,b[3]+pad)]
        if not any(intersects(rect,o['box']) for o in components if o is not c):break
    x0,y0,x1,y1=rect
    part=dict(name=name,atlas_rect=[x0,h-y1,x1-x0,y1-y0],top_left_box=rect,pivot=[px,py],alpha_body_pixels=c['area'])
    parts.append(part);lines.append('\t'.join(map(str,[name,*part['atlas_rect'],px,py])))
    print(part)
(root/'work/front_parts_rects.tsv').write_text('\n'.join(lines),encoding='utf-8')
(out/'parts_manifest.json').write_text(json.dumps({'canvas':[w,h],'reference_master':str(record['reference']),'method':'image-generated artwork, isolated by Unity sprite slicing','parts':parts},ensure_ascii=False,indent=2),encoding='utf-8')
(out/'제작_프롬프트.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
print('Atlas alpha:', im.getchannel('A').getextrema(), 'Corners:',[im.getchannel('A').getpixel(p) for p in [(0,0),(w-1,0),(0,h-1),(w-1,h-1)]])
