"""Stage six imagegen keyframe edits while retaining original frame coordinates."""
from pathlib import Path
from collections import deque
import json
import sys
import numpy as np
from PIL import Image

root=Path(r'C:\Underwrite'); out=root/'Outputs/WalkingArmFix_v2'
before=json.loads((out/'before.json').read_text(encoding='utf-8'))
backup=Path(before['backup'])
manifest=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'))

def main_cut(image):
    rgba=np.array(image.convert('RGBA')); h,w=rgba.shape[:2]
    todo=set(np.flatnonzero(rgba[:,:,3].ravel()>=32).tolist()); largest=[]
    while todo:
        first=todo.pop(); queue=deque([first]); group=[first]
        while queue:
            p=queue.popleft(); x,y=p%w,p//w
            for ny in range(max(0,y-1),min(h,y+2)):
                for nx in range(max(0,x-1),min(w,x+2)):
                    n=ny*w+nx
                    if n in todo: todo.remove(n); queue.append(n); group.append(n)
        if len(group)>len(largest): largest=group
    mask=np.zeros(h*w,bool); mask[largest]=True
    rgba[~mask.reshape((h,w))]=0
    clean=Image.fromarray(rgba); bbox=clean.getbbox()
    return clean.crop(bbox),bbox

def head_center(image):
    a=np.asarray(image)[:,:,3]>128
    ys,xs=np.where(a[round(image.height*.05):round(image.height*.18)])
    return (int(xs.min())+int(xs.max())+1)/2

frames={}; report={}
for direction in ['SE','NE','NW']:
    folder=out/'Keyframes'/direction; folder.mkdir(parents=True,exist_ok=True)
    frames[direction]=[]
    for idx in range(8):
        filename=f'walk_{direction.lower()}_{idx:02d}.png'
        original=Image.open(backup/direction/filename).convert('RGBA')
        key=f'{direction}_{idx:02d}'
        if key not in manifest:
            frames[direction].append(original)
            continue
        source=Image.open(manifest[key]['path']).convert('RGBA')
        piece,source_box=main_cut(source)
        old_piece,old_box=main_cut(original)
        scale=old_piece.height/piece.height
        resized=piece.resize((round(piece.width*scale),old_piece.height),Image.Resampling.LANCZOS)
        clean,clean_box=main_cut(resized)
        x=round(old_box[0]+head_center(old_piece)-head_center(clean))
        y=old_box[3]-clean.height
        assert x>0 and x+clean.width<640 and y>0 and y+clean.height<724,(key,x,y,clean.size)
        frame=Image.new('RGBA',(640,724)); frame.paste(clean,(x,y))
        frame.save(folder/filename)
        frames[direction].append(frame)
        report[key]={'source':manifest[key]['path'],'old_bbox':old_box,'new_bbox':frame.getbbox(),'scale':scale,'stage':str(folder/filename)}
    sheet=Image.new('RGBA',(2560,1448))
    for idx,frame in enumerate(frames[direction]): sheet.paste(frame,((idx%4)*640,(idx//4)*724))
    sheet.save(out/f'Walk_{direction}_keyframe_correction_sheet.png')
    frames[direction][0].save(out/f'Walk_{direction}_keyframe_preview.png',save_all=True,append_images=frames[direction][1:],duration=100,loop=0,disposal=0,blend=0)
combined=[]
for phase in range(8):
    page=Image.new('RGBA',(1920,724))
    for pos,direction in enumerate(['SE','NE','NW']): page.paste(frames[direction][phase],(pos*640,0))
    combined.append(page)
combined[0].save(out/'Walking_arm_corrections_preview.png',save_all=True,append_images=combined[1:],duration=100,loop=0,disposal=0,blend=0)
(out/'keyframe_staging.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('Staged '+str(len(report))+' corrected keyframes; no Assets or .meta writes.')
