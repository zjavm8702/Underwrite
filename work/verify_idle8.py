from pathlib import Path
from collections import deque
import json
from PIL import Image
import numpy as np

root=Path(r'C:\Underwrite\Outputs\ProtagonistIdle8_v1')
result={}
for direction in ['S','SE','E','NE','N','NW','W','SW']:
    records=[]
    for file in sorted((root/direction).glob(f'idle_{direction}_??.png')):
        image=Image.open(file)
        rgba=np.array(image)
        a=rgba[:,:,3]
        h,w=a.shape
        remaining=set(np.flatnonzero(a.ravel()>0).tolist())
        sizes=[]
        while remaining:
            first=remaining.pop()
            queue=deque([first]); size=0
            while queue:
                p=queue.popleft(); x,y=p%w,p//w; size+=1
                for ny in range(max(0,y-1),min(h,y+2)):
                    for nx in range(max(0,x-1),min(w,x+2)):
                        n=ny*w+nx
                        if n in remaining:
                            remaining.remove(n);queue.append(n)
            sizes.append(size)
        bbox=image.getbbox()
        record={'file':file.name,'size':list(image.size),'bbox':bbox,
                'alpha_components':len(sizes),'detached_pixels':sum(sorted(sizes)[:-1]),
                'hidden_background_rgb_pixels':int(((a==0)&(rgba[:,:,:3].max(axis=2)>0)).sum())}
        assert image.size==(362,724)
        assert bbox[0]>0 and bbox[1]>0 and bbox[2]<362 and bbox[3]==711
        records.append(record)
    assert len(records)==6
    preview=Image.open(root/direction/f'Idle_{direction}_preview.png')
    assert preview.n_frames==6
    result[direction]=records
(root/'validation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps({'directions':len(result),'frames':sum(map(len,result.values())),
    'detached_pixels':sum(f['detached_pixels'] for frames in result.values() for f in frames),
    'hidden_background_rgb_pixels':sum(f['hidden_background_rgb_pixels'] for frames in result.values() for f in frames)},indent=2))
