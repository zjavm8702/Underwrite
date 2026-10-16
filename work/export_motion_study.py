"""Package generated animation art; clean alpha and register complete silhouettes."""
from pathlib import Path
from collections import deque
import json
import sys
import numpy as np
from PIL import Image

ROOT=Path(r'C:\Underwrite')
OUT=ROOT/'Outputs/ProtagonistMotion_v1'
ASSETS=ROOT/'Assets/Images/Characters/Protagonist/Motion_v1'
OUT.mkdir(parents=True,exist_ok=True)
manifest=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'))

def components(mask):
    h,w=mask.shape
    remaining=set(np.flatnonzero(mask.ravel()).tolist())
    groups=[]
    while remaining:
        first=remaining.pop(); queue=deque([first]); group=[first]
        while queue:
            p=queue.popleft(); x,y=p%w,p//w
            for ny in range(max(0,y-1),min(h,y+2)):
                for nx in range(max(0,x-1),min(w,x+2)):
                    n=ny*w+nx
                    if n in remaining:
                        remaining.remove(n); queue.append(n); group.append(n)
        groups.append(group)
    return groups

def component_image(rgba,group):
    h,w=rgba.shape[:2]
    points=np.array(group)
    ys,xs=points//w,points%w
    x0,x1=int(xs.min()),int(xs.max())+1
    y0,y1=int(ys.min()),int(ys.max())+1
    cut=rgba[y0:y1,x0:x1].copy()
    mask=np.zeros(cut.shape[:2],bool)
    mask[ys-y0,xs-x0]=True
    cut[~mask]=0
    return Image.fromarray(cut),(x0,y0,x1,y1)

def waist_center(image):
    # Locate the broad torso span, avoiding an advancing foot as the pivot.
    mask=np.asarray(image)[:,:,3]>=128
    midpoints=[]
    for y in range(round(image.height*.43),round(image.height*.50)):
        xs=np.flatnonzero(mask[y])
        if len(xs)==0: continue
        runs=np.split(xs,np.where(np.diff(xs)>1)[0]+1)
        run=max(runs,key=len)
        midpoints.append((int(run[0])+int(run[-1])+1)/2)
    return float(np.median(midpoints))

def clean_resampled(image):
    rgba=np.array(image)
    rgba[rgba[:,:,3]<32]=0
    groups=components(rgba[:,:,3]>0)
    group=max(groups,key=len)
    mask=np.zeros(rgba.shape[0]*rgba.shape[1],bool); mask[group]=True
    rgba[~mask.reshape(rgba.shape[:2])]=0
    return Image.fromarray(rgba)

report={}; walk_frames={}
for name,info in manifest.items():
    source=Image.open(info['path']).convert('RGBA')
    rgba=np.array(source)
    groups=sorted(components(rgba[:,:,3]>=32),key=len,reverse=True)[:8]
    if len(groups)!=8 or min(map(len,groups))<1000:
        raise ValueError(name+': eight full figures not detected')
    # Rows can be uneven in generated sheets; sort complete figures into 4+4.
    groups.sort(key=lambda g: float(np.mean(np.array(g)//source.width)))
    groups=sorted(groups[:4],key=lambda g:min(p%source.width for p in g))+sorted(groups[4:],key=lambda g:min(p%source.width for p in g))
    pieces=[component_image(rgba,g) for g in groups]
    is_walk=name.startswith('Walk_')
    if is_walk:
        cw,ch,baseline=640,724,711
        scale=min(699/max(p.height for p,b in pieces),590/max(p.width for p,b in pieces))
        duration=[100]*8
    else:
        cw,ch,baseline=512,960,947
        scale=699/pieces[0][0].height
        duration=[180,120,100,120,160,120,360,220]
    folder=OUT/name; asset_folder=ASSETS/name
    folder.mkdir(parents=True,exist_ok=True); asset_folder.mkdir(parents=True,exist_ok=True)
    frames=[]; details=[]
    ground_source=pieces[0][1][3]
    for idx,(piece,bbox) in enumerate(pieces):
        resized=clean_resampled(piece.resize((round(piece.width*scale),round(piece.height*scale)),Image.Resampling.LANCZOS))
        if is_walk:
            x=round(cw/2-waist_center(piece)*scale)
            y=baseline-resized.getbbox()[3]
        else:
            cell_x=(idx%4)*source.width/4
            cell_y=(idx//4)*source.height/2
            x=round(cw/2+(bbox[0]-cell_x-source.width/8)*scale)
            y=round(baseline+(bbox[1]-cell_y-ground_source)*scale)
            if idx==4:
                # Root placement for the jump; art retains its own proportions.
                y-=100
        if x<1 or x+resized.width>=cw or y<1 or y+resized.height>=ch:
            raise ValueError(f'{name} frame{idx}: clipping at {x,y,resized.size}')
        frame=Image.new('RGBA',(cw,ch)); frame.paste(resized,(x,y))
        filename=f'{name.lower()}_{idx:02d}.png'
        frame.save(folder/filename); frame.save(asset_folder/filename)
        frames.append(frame)
        details.append({'source_bbox':bbox,'output_bbox':frame.getbbox(),'offset':[x,y]})
    sheet=Image.new('RGBA',(cw*4,ch*2))
    for idx,frame in enumerate(frames): sheet.paste(frame,((idx%4)*cw,(idx//4)*ch))
    sheet.save(folder/f'{name}_sheet.png')
    frames[0].save(folder/f'{name}_preview.png',save_all=True,append_images=frames[1:],duration=duration,loop=0 if is_walk else 1,disposal=0,blend=0)
    if is_walk: walk_frames[name.removeprefix('Walk_')]=frames
    report[name]={'source':info['path'],'scale':scale,'canvas':[cw,ch],
                  'pivot_pixels_bottom_left':[cw/2,ch-baseline],
                  'duration_ms':duration,'motion_loop':is_walk,'frames':details}
    print(name+' exported',flush=True)

if len(walk_frames)==8:
    montage=[]
    order=['S','SE','E','NE','N','NW','W','SW']
    for phase in range(8):
        page=Image.new('RGBA',(640*4,724*2))
        for pos,direction in enumerate(order): page.paste(walk_frames[direction][phase],((pos%4)*640,(pos//4)*724))
        montage.append(page)
    montage[0].save(OUT/'Walk_8directions_preview.png',save_all=True,append_images=montage[1:],duration=[100]*8,loop=0,disposal=0,blend=0)
    montage[0].save(OUT/'Walk_8directions_contact.png')
(OUT/'export_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('Export finished.')
