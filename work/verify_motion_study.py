from pathlib import Path
from collections import deque
import json
import shutil
import numpy as np
from PIL import Image

root=Path(r'C:\Underwrite')
out=root/'Outputs/ProtagonistMotion_v1'
report=json.loads((out/'export_report.json').read_text())
validation={}
sources=out/'Sources'; sources.mkdir(exist_ok=True)
for name,entry in report.items():
    shutil.copy2(entry['source'],sources/(name+'_source.png'))
    records=[]
    for idx in range(8):
        file=out/name/f'{name.lower()}_{idx:02d}.png'
        image=Image.open(file).convert('RGBA')
        rgba=np.array(image); a=rgba[:,:,3]
        h,w=a.shape
        remaining=set(np.flatnonzero(a.ravel()>0).tolist())
        sizes=[]
        while remaining:
            first=remaining.pop(); queue=deque([first]); size=0
            while queue:
                p=queue.popleft(); x,y=p%w,p//w; size+=1
                for ny in range(max(0,y-1),min(h,y+2)):
                    for nx in range(max(0,x-1),min(w,x+2)):
                        n=ny*w+nx
                        if n in remaining: remaining.remove(n); queue.append(n)
            sizes.append(size)
        bbox=image.getbbox()
        assert list(image.size)==entry['canvas']
        assert bbox[0]>0 and bbox[1]>0 and bbox[2]<w and bbox[3]<h
        assert len(sizes)==1,(name,idx,sizes)
        hidden=int(((a==0)&(rgba[:,:,:3].max(axis=2)>0)).sum())
        assert hidden==0
        if name.startswith('Walk'): assert bbox[3]==711
        records.append({'frame':idx,'bbox':bbox,'alpha_components':len(sizes),'hidden_background_rgb_pixels':hidden})
    preview=Image.open(out/name/f'{name}_preview.png')
    assert preview.n_frames==8
    validation[name]=records
assert len(validation)==9
(out/'validation.json').write_text(json.dumps(validation,indent=2),encoding='utf-8')
print('Verified 9 motions / 72 frames: no detached alpha pixels, hidden background RGB, or canvas clipping; previews contain 8 frames each.')
