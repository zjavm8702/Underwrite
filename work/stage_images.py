from pathlib import Path
import json, shutil
from PIL import Image
root=Path(r'C:\Underwrite')
items=json.loads((root/'work/assets_manifest.json').read_text(encoding='utf-8'))
for item in items:
    folder=root/('Assets/Underwrite/Art/Parts' if item['id'].endswith('parts') else 'Outputs/DirectionArt')
    folder.mkdir(parents=True,exist_ok=True)
    target=folder/(item['id']+'_v1.png')
    if not target.exists(): shutil.copy2(item['path'],target)
    im=Image.open(target)
    print(item['id'],im.size,im.mode)
    if item['id'].endswith('parts'):
        a=im.getchannel('A')
        print('alpha range',a.getextrema(),'corners',a.getpixel((0,0)))
        w,h=im.size
        for row in range(4):
            for col in range(4):
                cell=a.crop((col*w//4,row*h//4,(col+1)*w//4,(row+1)*h//4))
                bounds=cell.point(lambda x:255 if x>=128 else 0).getbbox()
                print(row,col,bounds)
