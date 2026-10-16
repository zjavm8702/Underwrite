from pathlib import Path
from datetime import datetime
import hashlib
import json
import shutil
from PIL import Image

root=Path(r'C:\Underwrite')
walking=root/'Assets/Images/Characters/Protagonist/Walking'
out=root/'Outputs/WalkingArmFix_v2'
backup=root/'work'/('walking_arm_fix_backup_'+datetime.now().strftime('%Y%m%d_%H%M%S'))
out.mkdir(parents=True,exist_ok=True); backup.mkdir(parents=True,exist_ok=True)
digest=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
metadata={str(p.relative_to(root)):digest(p) for p in walking.rglob('*.meta')}
parent_meta=walking.with_suffix('.meta')
if parent_meta.exists(): metadata[str(parent_meta.relative_to(root))]=digest(parent_meta)
manifest={'backup':str(backup),'metadata_hashes':metadata,'frames':{}}
for direction in ['SE','NE','NW']:
    sheet=Image.new('RGBA',(640*4,724*2))
    for idx in range(8):
        path=walking/direction/f'walk_{direction.lower()}_{idx:02d}.png'
        image=Image.open(path).convert('RGBA')
        if image.size!=(640,724): raise ValueError(f'Unexpected canvas {path}: {image.size}')
        dest=backup/direction/path.name
        dest.parent.mkdir(exist_ok=True)
        shutil.copy2(path,dest)
        sheet.paste(image,((idx%4)*640,(idx//4)*724))
        manifest['frames'][str(path.relative_to(root))]={'sha256':digest(path),'size':[640,724]}
    sheet.save(out/f'Walk_{direction}_before_sheet.png')
(out/'before.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(f'Backed up 24 PNGs. Recorded {len(metadata)} .meta hashes. No project metadata modified.')
