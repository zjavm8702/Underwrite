from pathlib import Path
import json
import numpy as np
from PIL import Image

root=Path(r'C:\Underwrite')
frames=[np.asarray(Image.open(p).convert('RGBA')) for p in sorted((root/'Assets/Images/Characters/Protagonist/Idle/S').glob('*.png'))]
regions={'vest_left':[105,170,140,275], 'shirt_center':[160,165,200,280]}
report={}
for name,(x0,y0,x1,y1) in regions.items():
    block=np.stack([f[y0:y1,x0:x1,:3].astype(float) for f in frames])
    diffs=np.abs(np.diff(block,axis=0))
    report[name]={'rect':[x0,y0,x1,y1],
                  'mean_adjacent_rgb_difference':float(diffs.mean()),
                  'fraction_pixels_with_any_channel_change_over_10':float((diffs.max(axis=3)>10).mean()),
                  'note':'Includes real breathing/registration/line motion; not a pure lighting metric.'}
dest=root/'Outputs/ArtQuality_20261015'
dest.mkdir(parents=True,exist_ok=True)
(dest/'source_variation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
