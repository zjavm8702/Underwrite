"""Prepare imagegen arm corrections as PNG replacements; never write .meta."""
from pathlib import Path
from collections import deque
import json
import sys
import numpy as np
from PIL import Image

ROOT=Path(r'C:\Underwrite')
OUT=ROOT/'Outputs/WalkingArmFix_v2'
specs=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'))

def groups(mask):
    h,w=mask.shape
    todo=set(np.flatnonzero(mask.ravel()).tolist()); found=[]
    while todo:
        first=todo.pop(); queue=deque([first]); group=[first]
        while queue:
            p=queue.popleft(); x,y=p%w,p//w
            for ny in range(max(0,y-1),min(h,y+2)):
                for nx in range(max(0,x-1),min(w,x+2)):
                    n=ny*w+nx
                    if n in todo: todo.remove(n); queue.append(n); group.append(n)
        found.append(group)
    return found

def cutout(rgba,points):
    w=rgba.shape[1]; points=np.array(points)
    ys,xs=points//w,points%w
    x0,x1=int(xs.min()),int(xs.max())+1; y0,y1=int(ys.min()),int(ys.max())+1
    data=rgba[y0:y1,x0:x1].copy(); mask=np.zeros(data.shape[:2],bool)
    mask[ys-y0,xs-x0]=True; data[~mask]=0
    return Image.fromarray(data),(x0,y0,x1,y1)

def waist(image):
    mask=np.array(image)[:,:,3]>=128; mids=[]
    for y in range(round(image.height*.43),round(image.height*.50)):
        xs=np.flatnonzero(mask[y])
        if len(xs):
            run=max(np.split(xs,np.where(np.diff(xs)>1)[0]+1),key=len)
            mids.append((int(run[0])+int(run[-1])+1)/2)
    return float(np.median(mids))

report={}; all_frames={}
for direction,info in specs.items():
    source=Image.open(info['path']).convert('RGBA'); rgba=np.array(source)
    figures=sorted(groups(rgba[:,:,3]>=32),key=len,reverse=True)[:8]
    assert len(figures)==8 and min(map(len,figures))>1000
    figures.sort(key=lambda g:float(np.mean(np.array(g)//source.width)))
    figures=sorted(figures[:4],key=lambda g:min(p%source.width for p in g))+sorted(figures[4:],key=lambda g:min(p%source.width for p in g))
    pieces=[cutout(rgba,g) for g in figures]
    scale=699/max(im.height for im,b in pieces)
    folder=OUT/direction; folder.mkdir(exist_ok=True)
    frames=[]; details=[]
    for idx,(piece,bbox) in enumerate(pieces):
        resized=piece.resize((round(piece.width*scale),round(piece.height*scale)),Image.Resampling.LANCZOS)
        data=np.array(resized); data[data[:,:,3]<32]=0
        body=max(groups(data[:,:,3]>0),key=len)
        mask=np.zeros(data.shape[0]*data.shape[1],bool); mask[body]=True
        data[~mask.reshape(data.shape[:2])]=0; resized=Image.fromarray(data)
        x=round(320-waist(piece)*scale); y=711-resized.getbbox()[3]
        assert x>0 and x+resized.width<640 and y>0 and y+resized.height<724,(direction,idx)
        frame=Image.new('RGBA',(640,724)); frame.paste(resized,(x,y))
        path=folder/f'walk_{direction.lower()}_{idx:02d}.png'; frame.save(path)
        frames.append(frame); details.append({'frame':idx,'bbox':frame.getbbox(),'source_bbox':bbox,'offset':[x,y]})
    sheet=Image.new('RGBA',(2560,1448))
    for idx,frame in enumerate(frames): sheet.paste(frame,((idx%4)*640,(idx//4)*724))
    sheet.save(folder/f'Walk_{direction}_corrected_sheet.png')
    frames[0].save(folder/f'Walk_{direction}_preview.png',save_all=True,append_images=frames[1:],duration=100,loop=0,disposal=0,blend=0)
    report[direction]={'source':info['path'],'scale':scale,'frames':details}; all_frames[direction]=frames
    print(direction+' staged: 8 PNG frames, 640x724',flush=True)
montage=[]
for phase in range(8):
    canvas=Image.new('RGBA',(640*3,724))
    for idx,direction in enumerate(['SE','NE','NW']): canvas.paste(all_frames[direction][phase],(idx*640,0))
    montage.append(canvas)
montage[0].save(OUT/'Walking_arm_corrections_preview.png',save_all=True,append_images=montage[1:],duration=100,loop=0,disposal=0,blend=0)
(OUT/'staging_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('Staging only. No Assets PNG or .meta files modified.')
