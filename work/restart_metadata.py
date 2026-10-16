from pathlib import Path
from PIL import Image
from collections import deque
import json
root=Path(r'C:\Underwrite')
src=root/'Assets/Images/Characters/Protagonist/Source'
im=Image.open(src/'protagonist_front_parts_v1.png')
w,h=im.size
alpha=list(im.getchannel('A').getdata())
remaining={i for i,a in enumerate(alpha) if a>=128}
components=[]
while remaining:
    first=remaining.pop(); queue=deque([first]); group=[first]
    while queue:
        p=queue.popleft();x=p%w;y=p//w
        for n in ([p-1] if x else [])+([p+1] if x<w-1 else [])+([p-w] if y else [])+([p+w] if y<h-1 else []):
            if n in remaining: remaining.remove(n);queue.append(n);group.append(n)
    if len(group)>1500:
        xs=[p%w for p in group];ys=[p//w for p in group]
        components.append(dict(area=len(group),box=[min(xs),min(ys),max(xs)+1,max(ys)+1]))
components.sort(key=lambda c:-c['area'])
print('parts canvas',im.size,'alpha',im.getchannel('A').getextrema())
print(json.dumps(components,indent=2))
fi=Image.open(src/'protagonist_face_sets_v1.png')
print('face canvas',fi.size,'alpha',fi.getchannel('A').getextrema())
(root/'work/restart_components.json').write_text(json.dumps(dict(canvas=[w,h],components=components,face_canvas=list(fi.size)),indent=2))
