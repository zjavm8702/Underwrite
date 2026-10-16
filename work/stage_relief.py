from pathlib import Path
from PIL import Image
import json,shutil
root=Path(r'C:\Underwrite')
record=json.loads((root/'work/relief_source.json').read_text(encoding='utf-8'))
out=root/'Assets/Underwrite/Art/Parts/protagonist_head_relief_v1.png'
shutil.copy2(record['path'],out)
im=Image.open(out);a=im.getchannel('A')
b=a.point(lambda v:255 if v>=200 else 0).getbbox()
x0,y0,x1,y1=b
part=dict(name='Head_Relieved',x=x0,y=im.height-y1,w=x1-x0,h=y1-y0,px=.5,py=.02)
(root/'work/relief_layout.json').write_text(json.dumps({'parts':[part]}),encoding='utf-8')
(root/'Outputs/DirectionArt/안도_표정_프롬프트.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
print(im.size,b,part)
