from PIL import Image
from pathlib import Path
from collections import deque
import json
root=Path(r'C:\Underwrite')
im=Image.open(root/'Assets/Underwrite/Art/Parts/protagonist_parts_v1.png')
alpha=im.getchannel('A'); w,h=im.size
rows=[(0,295),(296,618),(622,925),(930,1254)]
names=['Head_Neutral','Head_Anxious','Head_Surprised','Head_Closed','Torso','Hips','RightUpperArm','LeftUpperArm','RightForearm','LeftForearm','RightThigh','LeftThigh','RightShin','LeftShin','Ledger','Pouch']
parts=[]
for i,name in enumerate(names):
    r,c=divmod(i,4); x0=c*w//4;x1=(c+1)*w//4;y0,y1=rows[r]
    cw,ch=x1-x0,y1-y0
    raw=list(alpha.crop((x0,y0,x1,y1)).getdata())
    remaining={j for j,a in enumerate(raw) if a>=128}
    largest=[]
    while remaining:
        start=remaining.pop(); queue=deque([start]); group=[start]
        while queue:
            k=queue.popleft(); x=k%cw;y=k//cw
            near=[]
            if x: near.append(k-1)
            if x<cw-1: near.append(k+1)
            if y: near.append(k-cw)
            if y<ch-1: near.append(k+cw)
            for n in near:
                if n in remaining: remaining.remove(n);queue.append(n);group.append(n)
        if len(group)>len(largest): largest=group
    xmin=max(0,min(k%cw for k in largest)-3);xmax=min(cw,max(k%cw for k in largest)+4)
    ymin=max(0,min(k//cw for k in largest)-3);ymax=min(ch,max(k//cw for k in largest)+4)
    px,py=(.5,.02) if name.startswith('Head') or name=='Torso' else ((.5,.5) if name in ['Ledger','Pouch'] else (.5,.94))
    part=dict(name=name,x=x0+xmin,y=h-(y0+ymax),w=xmax-xmin,h=ymax-ymin,px=px,py=py)
    if name.endswith('Forearm'):
        part['h'] -= 85
        part['px'] = .42 if name.startswith('Right') else .52
    parts.append(part)
    print(part)
(root/'work/parts_layout.json').write_text(json.dumps(dict(parts=parts),indent=2),encoding='utf-8')
