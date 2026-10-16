from pathlib import Path
from PIL import Image
import json, shutil
root=Path(r'C:\Underwrite')
record=json.loads((root/'work/front_master_source.json').read_text(encoding='utf-8'))
shutil.copy2(record['source'],record['target'])
image=Image.open(record['target'])
print('Saved:', record['target'], 'Size:', image.size, 'Mode:', image.mode)
if image.mode=='RGBA':
    a=image.getchannel('A')
    print('Alpha range:',a.getextrema(),'Corners:',[a.getpixel(p) for p in [(0,0),(image.width-1,0),(0,image.height-1),(image.width-1,image.height-1)]])
    for xy in [(100,500),(200,500),(300,1100),(500,900),(500,400)]:print(xy,image.getpixel(xy))
